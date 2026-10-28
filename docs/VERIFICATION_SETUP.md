# Email, phone and provider verification

Email and phone are mandatory for Customer and Provider registration. Phones use international E.164-style format (`+` and 8–15 digits), and email/phone each have a unique database index. New accounts start with both contacts unverified. Providers also create a Pending profile with business name and qualifications.

## Contact verification

A signed-in user requests a code for their own email or phone, then submits the returned challenge ID and received six-digit code. Codes are HMAC-hashed in the database, expire after ten minutes, allow at most five attempts and are consumed on success. Resending invalidates earlier active challenges. Codes are not returned by API responses or logged as application messages. Rate limits apply per account/channel (60-second cooldown, five sends/hour) and per client IP at the HTTP layer.

LocalOutbox writes messages under `.local/outbox` and is allowed only in Development. It is a test delivery mechanism, not proof of ownership of a real email address or phone. Do not use it for real-user verification.

## Real email delivery

Set `Verification__Delivery=External` and configure an SMTP service supporting STARTTLS:

```text
Verification__Smtp__Host
Verification__Smtp__Port=587
Verification__Smtp__Username
Verification__Smtp__Password
Verification__Smtp__From
```

SMTP requires TLS; certificate validation is not disabled. Choose credentials and a verified sender accepted by your SMTP service. Some services require application credentials or sender/domain approval.

## Real phone delivery

The external phone adapter uses Twilio's Messages API over HTTPS. Configure:

```text
Verification__Twilio__AccountSid
Verification__Twilio__AuthToken
Verification__Twilio__From
```

The sender must support the destination region and applicable Twilio requirements. Provider fees and delivery rules apply. Store credentials in environment variables/secret management, not source files. Production has no fallback that silently marks a contact verified. Delivery failure returns 503 and invalidates the challenge. The user may retry after the cooldown.

Actual SMTP/SMS delivery has not been tested without user-provided service credentials. The local and automated tests use a file/capture delivery adapter, so they do not send real messages.

## Administrator review

After a provider verifies both contacts, an administrator reviews the supplied qualifications and records approval/rejection with mandatory notes. Decisions are stored with reviewer identity and timestamp, with an append-only decision history. Public registration cannot request the Admin role. Use the Development demo admin or the explicit `--create-admin` bootstrap documented in DATABASE.md.

Providers need verified email, verified phone and current Approved status to quote. Acceptance rechecks provider eligibility so revocation prevents accepting stale quotes. Existing assigned work can still be completed and reviewed after a provider's approval is revoked.

Administrator approval is a business review of the submitted profile, not a government identity, license or background check. Integrating an identity/document service is a separate future feature.

## Before real traffic

The HTTP authentication throttle is per API instance and remote IP. Configure trusted proxy forwarding and distributed limiting for a scaled deployment. Protect SMTP/Twilio credentials, monitor delivery failures and configure operational alerts. This version does not support contact edits, password reset or refreshing/revoking JWT sessions; changing contact details must be designed to clear verification and require a new challenge.
