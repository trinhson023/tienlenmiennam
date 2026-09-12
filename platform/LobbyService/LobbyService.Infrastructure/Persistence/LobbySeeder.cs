using LobbyService.Domain.Games;
using LobbyService.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace LobbyService.Infrastructure.Persistence;

public sealed class LobbySeeder(LobbyDbContext db)
{
    private static readonly (Guid Id, GameType Type, string Slug, string Name, string Icon, int Min, int Max, bool Enabled)[] Definitions =
    [
        (Guid.Parse("11111111-1111-4111-8111-111111111111"), GameType.TienLen, "tien-len", "Tiến Lên Miền Nam", "🂡", 2, 4, true),
        (Guid.Parse("22222222-2222-4222-8222-222222222222"), GameType.Sam, "sam-loc", "Sâm Lốc", "🃏", 2, 4, false),
        (Guid.Parse("33333333-3333-4333-8333-333333333333"), GameType.CoTuong, "co-tuong", "Cờ Tướng", "♟", 2, 2, false),
        (Guid.Parse("44444444-4444-4444-8444-444444444444"), GameType.Phom, "phom", "Phỏm / Tá Lả", "🀄", 2, 4, false)
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var item in Definitions)
        {
            if (!await db.Games.AnyAsync(x => x.Slug == item.Slug, ct))
                db.Games.Add(new GameDefinition(item.Id, item.Type, item.Slug, item.Name, item.Icon, item.Min, item.Max, item.Enabled));
        }
        await db.SaveChangesAsync(ct);
    }
}
