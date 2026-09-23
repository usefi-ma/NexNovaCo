using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexNovaCo.Web.Data;
using NexNovaCo.Web.Models;

namespace NexNovaCo.Auth.Tests;

// Frozen pre-media columns for upgrade tests. Never query an old schema with today's EF entity.
internal static class HistoricalHomeFixture
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        await InitializeHeroAsync(db);
        var w = HomeWelcomeDefaults.Content;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO HomeWelcomeSettings (Id,Title,Introduction,ParagraphOne,ParagraphTwo,CtaLabel,CtaHref,UpdatedAtUtc)
            VALUES (1,{w.Title},{w.Introduction},{w.Paragraphs[0]},{w.Paragraphs[1]},{w.CtaLabel},{w.CtaHref},{DateTime.UtcNow})
            """);
    }
    public static async Task InitializeHeroAsync(ApplicationDbContext db)
    {
        var h = HomeHeroDefaults.Content;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO HomeHeroSettings (Id,OpeningLine,EmphasisLine,ClosingLine,Description,CtaLabel,CtaHref,UpdatedAtUtc)
            VALUES (1,{h.OpeningLine},{h.EmphasisLine},{h.ClosingLine},{h.Description},{h.CtaLabel},{h.CtaHref},{DateTime.UtcNow})
            """);
    }
    public static async Task<string> SnapshotAsync(ApplicationDbContext db)
    {
        var rows = new List<string>();
        foreach (var sql in new[]
        {
            "SELECT Id,OpeningLine,EmphasisLine,ClosingLine,Description,CtaLabel,CtaHref,UpdatedAtUtc FROM HomeHeroSettings ORDER BY Id",
            "SELECT Id,Title,Introduction,ParagraphOne,ParagraphTwo,CtaLabel,CtaHref,UpdatedAtUtc FROM HomeWelcomeSettings ORDER BY Id"
        })
        {
            using var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                for (var i = 0; i < reader.FieldCount; i++) rows.Add(reader.GetValue(i).ToString()!);
        }
        return JsonSerializer.Serialize(rows);
    }
}
