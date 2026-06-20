using Microsoft.EntityFrameworkCore;
using WellBot.Admin.Data;
using WellBot.Admin.Entities;
using WellBot.Shared.DTOs;
using WellBot.Shared.Enums;

namespace WellBot.Admin.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics").RequireAuthorization("BasicAuthentication");

        // POST /api/analytics/events - Réception batch d'événements du client
        group.MapPost("/events", async (AppDbContext db, IConfiguration config, List<AnalyticsEventDto> events) =>
        {
            var entities = events.Select(e => new AnalyticsEventEntity
            {
                MachineId = e.MachineId,
                NotificationType = e.NotificationType,
                Action = e.Action,
                Timestamp = e.Timestamp,
                SessionDurationSeconds = e.SessionDurationSeconds,
                ReceivedAt = DateTime.UtcNow
            }).ToList();

            db.AnalyticsEvents.AddRange(entities);
            await db.SaveChangesAsync();

            // Rétention configurable : suppression des événements plus anciens
            var retentionDays = config.GetValue<int>("Analytics:RetentionDays", 7);
            var retentionDate = DateTime.UtcNow.AddDays(-retentionDays);
            await db.AnalyticsEvents
                .Where(e => e.Timestamp < retentionDate)
                .ExecuteDeleteAsync();

            return Results.Ok(new { Received = entities.Count });
        })
        .WithName("PostAnalyticsEvents")
        .WithOpenApi();

        // GET /api/analytics/summary - Résumé global
        group.MapGet("/summary", async (AppDbContext db, int? days) =>
        {
            var since = DateTime.UtcNow.AddDays(-(days ?? 30));

            var totalEvents = await db.AnalyticsEvents
                .CountAsync(e => e.Timestamp >= since);

            var uniqueMachines = await db.AnalyticsEvents
                .Where(e => e.Timestamp >= since)
                .Select(e => e.MachineId)
                .Distinct()
                .CountAsync();

            var displayed = await db.AnalyticsEvents
                .CountAsync(e => e.Timestamp >= since && e.Action == AnalyticsAction.Displayed);

            var acknowledged = await db.AnalyticsEvents
                .CountAsync(e => e.Timestamp >= since && e.Action == AnalyticsAction.Acknowledged);

            var engagementRate = displayed > 0 ? (double)acknowledged / displayed * 100 : 0;

            return Results.Ok(new
            {
                Period = $"Last {days ?? 30} days",
                TotalEvents = totalEvents,
                UniqueMachines = uniqueMachines,
                NotificationsDisplayed = displayed,
                NotificationsAcknowledged = acknowledged,
                EngagementRate = Math.Round(engagementRate, 1)
            });
        })
        .WithName("GetAnalyticsSummary")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/analytics/by-type - Stats par type de notification
        group.MapGet("/by-type", async (AppDbContext db, int? days) =>
        {
            var since = DateTime.UtcNow.AddDays(-(days ?? 30));

            var stats = await db.AnalyticsEvents
                .Where(e => e.Timestamp >= since)
                .GroupBy(e => new { e.NotificationType, e.Action })
                .Select(g => new
                {
                    Type = g.Key.NotificationType.ToString(),
                    Action = g.Key.Action.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Results.Ok(stats);
        })
        .WithName("GetAnalyticsByType")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();

        // GET /api/analytics/engagement - Taux d'engagement par type
        group.MapGet("/engagement", async (AppDbContext db, int? days) =>
        {
            var since = DateTime.UtcNow.AddDays(-(days ?? 30));

            var engagementByType = await db.AnalyticsEvents
                .Where(e => e.Timestamp >= since &&
                       (e.Action == AnalyticsAction.Displayed || e.Action == AnalyticsAction.Acknowledged))
                .GroupBy(e => e.NotificationType)
                .Select(g => new
                {
                    Type = g.Key.ToString(),
                    Displayed = g.Count(e => e.Action == AnalyticsAction.Displayed),
                    Acknowledged = g.Count(e => e.Action == AnalyticsAction.Acknowledged)
                })
                .ToListAsync();

            var result = engagementByType.Select(e => new
            {
                e.Type,
                e.Displayed,
                e.Acknowledged,
                EngagementRate = e.Displayed > 0 ? Math.Round((double)e.Acknowledged / e.Displayed * 100, 1) : 0
            });

            return Results.Ok(result);
        })
        .WithName("GetEngagement")
        .RequireAuthorization("AdminOnly")
        .WithOpenApi();
    }
}
