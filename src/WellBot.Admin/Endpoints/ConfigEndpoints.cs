using Microsoft.EntityFrameworkCore;
using WellBot.Admin.Data;
using WellBot.Shared.DTOs;

namespace WellBot.Admin.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/config").WithTags("Configuration").RequireAuthorization("BasicAuthentication");

        // GET /api/config - Configuration complète pour le client desktop
        group.MapGet("/", async (AppDbContext db, string? language) =>
        {
            var lang = language ?? "fr";
            
            var allNotifications = await db.NotificationConfigs.ToListAsync();
            var frenchConfigs = allNotifications
                .Where(n => n.Language == "fr")
                .ToDictionary(n => n.Type);

            var requestedLangs = new[] { lang, "all" };
            var notifications = allNotifications
                .Where(n => requestedLangs.Contains(n.Language))
                .Select(n => {
                    var dto = new NotificationConfigDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        AnimationName = n.AnimationName,
                        IntervalMinutes = n.IntervalMinutes,
                        Language = n.Language,
                        IsEnabled = n.IsEnabled
                    };
                    
                    if (n.Language != "all" && frenchConfigs.TryGetValue(n.Type, out var fr))
                    {
                        dto.IntervalMinutes = fr.IntervalMinutes;
                        dto.IsEnabled = fr.IsEnabled;
                        dto.AnimationName = fr.AnimationName;
                    }
                    return dto;
                })
                .ToList();

            var healthTips = await db.HealthTips
                .Where(t => t.IsActive && requestedLangs.Contains(t.Language))
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

            var config = new ClientConfigDto
            {
                Notifications = notifications,
                HealthTips = healthTips
            };

            return Results.Ok(config);
        })
        .WithName("GetClientConfig")
        .WithOpenApi();

        // GET /api/config/notifications - Liste les configs notifications
        group.MapGet("/notifications", async (AppDbContext db, string? language) =>
        {
            var allNotifications = await db.NotificationConfigs.ToListAsync();
            var frenchConfigs = allNotifications
                .Where(n => n.Language == "fr")
                .ToDictionary(n => n.Type);

            var filtered = allNotifications.AsEnumerable();
            if (!string.IsNullOrEmpty(language))
            {
                filtered = filtered.Where(n => n.Language == language);
            }

            var configs = filtered
                .Select(n => {
                    var dto = new NotificationConfigDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        AnimationName = n.AnimationName,
                        IntervalMinutes = n.IntervalMinutes,
                        Language = n.Language,
                        IsEnabled = n.IsEnabled
                    };

                    if (n.Language != "all" && frenchConfigs.TryGetValue(n.Type, out var fr))
                    {
                        dto.IntervalMinutes = fr.IntervalMinutes;
                        dto.IsEnabled = fr.IsEnabled;
                        dto.AnimationName = fr.AnimationName;
                    }
                    return dto;
                })
                .ToList();

            return Results.Ok(configs);
        })
        .WithName("GetNotificationConfigs")
        .WithOpenApi();

        // PUT /api/config/notifications/{id} - Modifier une config notification (admin)
        group.MapPut("/notifications/{id:int}", async (AppDbContext db, int id, NotificationConfigDto dto) =>
        {
            var entity = await db.NotificationConfigs.FindAsync(id);
            if (entity is null) return Results.NotFound();

            entity.Title = dto.Title;
            entity.Message = dto.Message;
            entity.AnimationName = dto.AnimationName;
            entity.IntervalMinutes = dto.IntervalMinutes;
            entity.IsEnabled = dto.IsEnabled;
            entity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(dto);
        })
        .WithName("UpdateNotificationConfig")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}
