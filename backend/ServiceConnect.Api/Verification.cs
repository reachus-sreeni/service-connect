using System.Net;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public interface ICodeDelivery { Task Send(string channel, string destination, string code, Guid challengeId); }
public sealed class CodeDelivery(IConfiguration config, IWebHostEnvironment environment, IHttpClientFactory clients) : ICodeDelivery
{
    public async Task Send(string channel, string destination, string code, Guid challengeId)
    {
        var mode = config["Verification:Delivery"] ?? "External";
        if (mode == "LocalOutbox")
        {
            if (!environment.IsDevelopment()) throw new InvalidOperationException("Local verification outbox is restricted to Development.");
            var folder = Path.Combine(environment.ContentRootPath, ".local", "outbox");
            Directory.CreateDirectory(folder);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var path = Path.Combine(folder, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{challengeId}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { channel, destination, code, challengeId, expiresInMinutes = 10 }));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            return;
        }
        if (mode != "External") throw new InvalidOperationException("Verification:Delivery must be LocalOutbox or External.");
        if (channel == "email")
        {
            var host = config["Verification:Smtp:Host"] ?? throw new InvalidOperationException("Configure SMTP for email verification.");
            using var smtp = new SmtpClient(host, config.GetValue("Verification:Smtp:Port", 587)) {
                EnableSsl = true, Credentials = new NetworkCredential(config["Verification:Smtp:Username"], config["Verification:Smtp:Password"])
            };
            using var message = new MailMessage(config["Verification:Smtp:From"] ?? throw new InvalidOperationException("Configure email sender."), destination,
                "Service Connect verification code", $"Your Service Connect code is {code}. It expires in 10 minutes. If you did not request it, ignore this message.");
            await smtp.SendMailAsync(message);
        }
        else
        {
            var sid = config["Verification:Twilio:AccountSid"] ?? throw new InvalidOperationException("Configure Twilio for phone verification.");
            var token = config["Verification:Twilio:AuthToken"] ?? throw new InvalidOperationException("Configure Twilio credentials.");
            var from = config["Verification:Twilio:From"] ?? throw new InvalidOperationException("Configure Twilio sender.");
            using var message = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(sid)}/Messages.json");
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(sid + ":" + token)));
            message.Content = new FormUrlEncodedContent(new Dictionary<string,string> { ["To"] = destination, ["From"] = from, ["Body"] = $"Service Connect code: {code}. Expires in 10 minutes." });
            using var response = await clients.CreateClient().SendAsync(message);
            response.EnsureSuccessStatusCode();
        }
    }
}
public sealed class VerificationService(AppDb db, ICodeDelivery delivery, IConfiguration config)
{
    private byte[] Hash(Guid id, string code) => HMACSHA256.HashData(Encoding.UTF8.GetBytes(config["Jwt:Key"]!), Encoding.UTF8.GetBytes(id + ":" + code));
    public async Task<object> Send(Guid accountId, string channel)
    {
        if (channel is not ("email" or "phone")) throw new RuleException(400, "Choose email or phone.");
        // Touch the account concurrency token in the same transaction to serialize sends.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.Accounts.FindAsync(accountId) ?? throw new RuleException(404, "Account not found.");
        if (channel == "email" ? account.EmailVerified : account.PhoneVerified) throw new RuleException(409, "This contact is already verified.");
        var since = DateTime.UtcNow.AddHours(-1);
        var sent = await db.VerificationChallenges.Where(x => x.AccountId == accountId && x.Channel == channel && x.CreatedAt > since).ToListAsync();
        if (sent.Count >= 5 || sent.Any(x => x.CreatedAt > DateTime.UtcNow.AddSeconds(-60)))
            throw new RuleException(429, "Wait 60 seconds between codes; at most five codes per contact per hour.");
        foreach (var prior in sent) { prior.Consumed = true; prior.Version = Guid.NewGuid(); }
        var code = RandomNumberGenerator.GetInt32(1000000).ToString("D6");
        var challenge = new VerificationChallenge { AccountId = accountId, Channel = channel, ExpiresAt = DateTime.UtcNow.AddMinutes(10) };
        challenge.CodeHash = Convert.ToHexString(Hash(challenge.Id, code));
        db.VerificationChallenges.Add(challenge); account.Version = Guid.NewGuid();
        await db.SaveChangesAsync(); await tx.CommitAsync();
        try { await delivery.Send(channel, channel == "email" ? account.Email : account.Phone, code, challenge.Id); }
        catch (Exception ex) when (ex is not RuleException)
        {
            challenge.Consumed = true; challenge.Version = Guid.NewGuid(); await db.SaveChangesAsync();
            throw new RuleException(503, "Verification delivery failed. Check delivery configuration and try again later.");
        }
        return new { challengeId = challenge.Id, expiresAt = challenge.ExpiresAt };
    }
    public async Task Verify(Guid accountId, string channel, VerifyCodeInput input)
    {
        var challenge = await db.VerificationChallenges.SingleOrDefaultAsync(x => x.Id == input.ChallengeId && x.AccountId == accountId && x.Channel == channel);
        if (challenge is null || challenge.Consumed || challenge.ExpiresAt < DateTime.UtcNow || challenge.Attempts >= 5)
            throw new RuleException(400, "Code is invalid or expired. Request a new code.");
        challenge.Attempts++; challenge.Version = Guid.NewGuid();
        var valid = input.Code is { Length: 6 } && CryptographicOperations.FixedTimeEquals(Convert.FromHexString(challenge.CodeHash), Hash(challenge.Id, input.Code));
        if (!valid)
        {
            if (challenge.Attempts >= 5) challenge.Consumed = true;
            await db.SaveChangesAsync(); throw new RuleException(400, "Incorrect verification code.");
        }
        var account = await db.Accounts.SingleAsync(x => x.Id == accountId);
        if (channel == "email") account.EmailVerified = true; else account.PhoneVerified = true;
        account.Version = Guid.NewGuid(); challenge.Consumed = true;
        await db.SaveChangesAsync();
    }
}
