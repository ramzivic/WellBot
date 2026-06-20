using Microsoft.EntityFrameworkCore;
using WellBot.Admin.Data;
using WellBot.Admin.Entities;
using WellBot.Shared.DTOs;

namespace WellBot.Admin.Endpoints;

public static class HealthTipsEndpoints
{
    public static void MapHealthTipsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health-tips").WithTags("Health Tips").RequireAuthorization("BasicAuthentication");

        // GET /api/health-tips - Liste tous les conseils
        group.MapGet("/", async (AppDbContext db, string? language, string? category) =>
        {
            var query = db.HealthTips.AsQueryable();

            if (!string.IsNullOrEmpty(language))
                query = query.Where(t => t.Language == language);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            var tips = await query
                .Where(t => t.IsActive)
                .Select(t => new HealthTipDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Message = t.Message,
                    Category = t.Category,
                    Language = t.Language,
                    IsActive = t.IsActive
                })
                .ToListAsync();

            return Results.Ok(tips);
        })
        .WithName("GetHealthTips")
        .WithOpenApi();

        // GET /api/health-tips/random - Conseil aléatoire
        group.MapGet("/random", async (AppDbContext db, string? language) =>
        {
            var lang = language ?? "fr";
            var count = await db.HealthTips.CountAsync(t => t.IsActive && t.Language == lang);

            if (count == 0)
                return Results.NotFound("No health tips available.");

            var random = new Random();
            var skip = random.Next(0, count);

            var tip = await db.HealthTips
                .Where(t => t.IsActive && t.Language == lang)
                .Skip(skip)
                .Select(t => new HealthTipDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Message = t.Message,
                    Category = t.Category,
                    Language = t.Language,
                    IsActive = t.IsActive
                })
                .FirstOrDefaultAsync();

            return tip is not null ? Results.Ok(tip) : Results.NotFound();
        })
        .WithName("GetRandomHealthTip")
        .WithOpenApi();

        // POST /api/health-tips - Créer un conseil (admin)
        group.MapPost("/", async (AppDbContext db, HealthTipDto dto) =>
        {
            var entity = new HealthTipEntity
            {
                Title = dto.Title,
                Message = dto.Message,
                Category = dto.Category,
                Language = dto.Language,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.HealthTips.Add(entity);
            await db.SaveChangesAsync();

            dto.Id = entity.Id;
            return Results.Created($"/api/health-tips/{entity.Id}", dto);
        })
        .WithName("CreateHealthTip")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // PUT /api/health-tips/{id} - Modifier un conseil (admin)
        group.MapPut("/{id:int}", async (AppDbContext db, int id, HealthTipDto dto) =>
        {
            var entity = await db.HealthTips.FindAsync(id);
            if (entity is null) return Results.NotFound();

            entity.Title = dto.Title;
            entity.Message = dto.Message;
            entity.Category = dto.Category;
            entity.Language = dto.Language;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(dto);
        })
        .WithName("UpdateHealthTip")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // DELETE /api/health-tips/{id} - Supprimer un conseil (admin)
        group.MapDelete("/{id:int}", async (AppDbContext db, int id) =>
        {
            var entity = await db.HealthTips.FindAsync(id);
            if (entity is null) return Results.NotFound();

            db.HealthTips.Remove(entity);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteHealthTip")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}
