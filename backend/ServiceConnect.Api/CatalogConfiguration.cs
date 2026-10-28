using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace ServiceConnect;

public sealed record CatalogOptionDefinition(Guid Id, string Key, string Name, string Description);
public sealed record CatalogCategoryDefinition(Guid Id, string Key, string Name, string GroupName, string Description, bool IsFeatured, CatalogOptionDefinition[] Options);
public sealed record CatalogSourceCoverage(string SourceName, string SourceUrl, string CategoryKey, string Evidence, string ReviewedOn);
public sealed record CatalogDocument(int Version, bool LocationNeutral, string ReviewedOn, string SourceIndex, string Scope, CatalogCategoryDefinition[] Categories, CatalogSourceCoverage[] SourceCoverage);

public static class CatalogConfiguration
{
    private static readonly Lazy<CatalogDocument> Configuration = new(() => {
        var assembly = typeof(CatalogConfiguration).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(x => x.EndsWith("Catalog.india-services.json",StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        var document = JsonSerializer.Deserialize<CatalogDocument>(stream,new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The service catalog configuration is empty.");
        if (document.Categories.Select(c=>c.Id).Distinct().Count()!=document.Categories.Length || document.Categories.Select(c=>c.Key).Distinct().Count()!=document.Categories.Length)
            throw new InvalidOperationException("Catalog category IDs and keys must be unique.");
        var options = document.Categories.SelectMany(c=>c.Options).ToArray();
        if (options.Select(o=>o.Id).Distinct().Count()!=options.Length || document.Categories.Any(c=>c.Options.Select(o=>o.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=c.Options.Length))
            throw new InvalidOperationException("Catalog option IDs and scoped names must be unique.");
        if (document.SourceCoverage.Any(s=>!document.Categories.Any(c=>c.Key==s.CategoryKey)))
            throw new InvalidOperationException("Every reviewed source offering must map to a configured category.");
        return document;
    });
    public static CatalogDocument Read() => Configuration.Value;

    public static async Task Apply(AppDb db)
    {
        foreach (var definition in Read().Categories)
        {
            var category = await db.Services.FindAsync(definition.Id);
            if (category is null) { category = new ServiceCategory { Id = definition.Id }; db.Services.Add(category); }
            category.Name = definition.Name; category.Description = definition.Description;
            category.GroupName = definition.GroupName; category.IsFeatured = definition.IsFeatured;
            for (var i=0;i<definition.Options.Length;i++)
            {
                var item = definition.Options[i];
                var option = await db.ServiceOptions.FindAsync(item.Id)
                    ?? await db.ServiceOptions.SingleOrDefaultAsync(o=>o.ServiceId==category.Id && o.Name==item.Name);
                if (option is null) { option = new ServiceOption { Id = item.Id, ServiceId = category.Id }; db.ServiceOptions.Add(option); }
                option.Name = item.Name; option.Description = item.Description; option.SortOrder = 100+i;
            }
        }
        // Configuration is additive: never delete categories/options referenced by past requests.
        await db.SaveChangesAsync();
    }
}
