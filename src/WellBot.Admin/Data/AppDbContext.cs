using Microsoft.EntityFrameworkCore;
using WellBot.Admin.Entities;
using WellBot.Shared.Enums;

namespace WellBot.Admin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HealthTipEntity> HealthTips => Set<HealthTipEntity>();
    public DbSet<NotificationConfigEntity> NotificationConfigs => Set<NotificationConfigEntity>();
    public DbSet<AnalyticsEventEntity> AnalyticsEvents => Set<AnalyticsEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // HealthTip configuration
        modelBuilder.Entity<HealthTipEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(5).HasDefaultValue("fr");
            entity.HasIndex(e => new { e.Language, e.Category });
        });

        // NotificationConfig configuration
        modelBuilder.Entity<NotificationConfigEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AnimationName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Language).IsRequired().HasMaxLength(5).HasDefaultValue("fr");
            entity.HasIndex(e => new { e.Type, e.Language }).IsUnique();
        });

        // AnalyticsEvent configuration
        modelBuilder.Entity<AnalyticsEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MachineId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.MachineId);
        });

        // Seed default notification configurations (French)
        SeedNotificationConfigs(modelBuilder);
        SeedHealthTips(modelBuilder);
    }

    private static void SeedNotificationConfigs(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<NotificationConfigEntity>().HasData(
            // French
            new NotificationConfigEntity
            {
                Id = 1, Type = NotificationType.Hydration, Language = "fr",
                Title = "Hydratation 💧", Message = "Pensez à boire un verre d'eau !",
                AnimationName = "drink", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 2, Type = NotificationType.VisualBreak, Language = "fr",
                Title = "Pause visuelle 👀", Message = "Regardez au loin pour reposer vos yeux !",
                AnimationName = "look_away", IntervalMinutes = 60, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 3, Type = NotificationType.Stretching, Language = "fr",
                Title = "Étirements 🤸", Message = "Il est temps de vous étirer ! Essayez cet exercice.",
                AnimationName = "stretch", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 4, Type = NotificationType.ActiveBreak, Language = "fr",
                Title = "Pause active 🚶", Message = "Faites une petite promenade pour vous détendre.",
                AnimationName = "walk", IntervalMinutes = 180, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 5, Type = NotificationType.Breathing, Language = "fr",
                Title = "Respiration 🧘", Message = "Prenez une pause respiration : inspirez profondément et expirez lentement.",
                AnimationName = "breathe", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 6, Type = NotificationType.HealthTip, Language = "fr",
                Title = "Conseil Santé 🏥", Message = "",
                AnimationName = "idle", IntervalMinutes = 240, IsEnabled = true, UpdatedAt = now
            },
            // English
            new NotificationConfigEntity
            {
                Id = 7, Type = NotificationType.Hydration, Language = "en",
                Title = "Hydration 💧", Message = "Remember to drink a glass of water!",
                AnimationName = "drink", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 8, Type = NotificationType.VisualBreak, Language = "en",
                Title = "Visual Break 👀", Message = "Look away from the screen to rest your eyes!",
                AnimationName = "look_away", IntervalMinutes = 60, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 9, Type = NotificationType.Stretching, Language = "en",
                Title = "Stretching 🤸", Message = "Time to stretch! Try this exercise.",
                AnimationName = "stretch", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 10, Type = NotificationType.ActiveBreak, Language = "en",
                Title = "Active Break 🚶", Message = "Take a short walk to relax.",
                AnimationName = "walk", IntervalMinutes = 180, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 11, Type = NotificationType.Breathing, Language = "en",
                Title = "Breathing 🧘", Message = "Take a breathing break: inhale deeply and exhale slowly.",
                AnimationName = "breathe", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 12, Type = NotificationType.HealthTip, Language = "en",
                Title = "Health Tip 🏥", Message = "",
                AnimationName = "idle", IntervalMinutes = 240, IsEnabled = true, UpdatedAt = now
            },
            // Arabic
            new NotificationConfigEntity
            {
                Id = 13, Type = NotificationType.Hydration, Language = "ar",
                Title = "💧 ترطيب", Message = "!تذكر أن تشرب كوبًا من الماء",
                AnimationName = "drink", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 14, Type = NotificationType.VisualBreak, Language = "ar",
                Title = "👀 راحة بصرية", Message = "!انظر بعيدًا عن الشاشة لإراحة عينيك",
                AnimationName = "look_away", IntervalMinutes = 60, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 15, Type = NotificationType.Stretching, Language = "ar",
                Title = "🤸 تمارين إطالة", Message = ".حان وقت التمدد! جرب هذا التمرين",
                AnimationName = "stretch", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 16, Type = NotificationType.ActiveBreak, Language = "ar",
                Title = "🚶 استراحة نشطة", Message = ".قم بنزهة قصيرة للاسترخاء",
                AnimationName = "walk", IntervalMinutes = 180, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 17, Type = NotificationType.Breathing, Language = "ar",
                Title = "🧘 تنفس", Message = ".خذ استراحة تنفس: استنشق بعمق وازفر ببطء",
                AnimationName = "breathe", IntervalMinutes = 120, IsEnabled = true, UpdatedAt = now
            },
            new NotificationConfigEntity
            {
                Id = 18, Type = NotificationType.HealthTip, Language = "ar",
                Title = "🏥 نصيحة صحية", Message = "",
                AnimationName = "idle", IntervalMinutes = 240, IsEnabled = true, UpdatedAt = now
            }
        );
    }

    private static void SeedHealthTips(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthTipEntity>().HasData(
            // French tips
            new HealthTipEntity { Id = 1, GroupId = "posture", Title = "Posture", Message = "Gardez votre dos droit et vos pieds à plat sur le sol pour éviter les douleurs dorsales.", Category = "ergonomie", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 2, GroupId = "screen", Title = "Écran", Message = "Placez votre écran à une distance d'un bras et à hauteur des yeux pour réduire la fatigue cervicale.", Category = "ergonomie", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 3, GroupId = "nutrition", Title = "Nutrition", Message = "Privilégiez les fruits et les noix comme encas au lieu des sucreries pour maintenir votre énergie.", Category = "nutrition", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 4, GroupId = "light", Title = "Lumière", Message = "Profitez de la lumière naturelle autant que possible. Elle améliore l'humeur et la productivité.", Category = "environnement", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 5, GroupId = "sleep", Title = "Sommeil", Message = "Visez 7 à 8 heures de sommeil par nuit pour une récupération optimale.", Category = "sommeil", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 6, GroupId = "mental-break", Title = "Pause mentale", Message = "Accordez-vous 5 minutes de pause toutes les heures pour recharger votre concentration.", Category = "stress", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 7, GroupId = "keyboard", Title = "Clavier", Message = "Gardez vos poignets droits lors de la saisie pour prévenir le syndrome du canal carpien.", Category = "ergonomie", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 8, GroupId = "hydration", Title = "Hydratation", Message = "Buvez au moins 1,5 litre d'eau par jour pour maintenir une bonne hydratation.", Category = "nutrition", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 9, GroupId = "stairs", Title = "Escaliers", Message = "Prenez les escaliers au lieu de l'ascenseur pour intégrer de l'activité physique dans votre journée.", Category = "activite", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 10, GroupId = "micro-breaks", Title = "Micro-pauses", Message = "Faites des micro-pauses de 30 secondes toutes les 30 minutes pour détendre vos muscles.", Category = "activite", Language = "fr", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // English tips
            new HealthTipEntity { Id = 11, GroupId = "posture", Title = "Posture", Message = "Keep your back straight and feet flat on the floor to prevent back pain.", Category = "ergonomics", Language = "en", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 12, GroupId = "screen", Title = "Screen", Message = "Place your screen at arm's length and eye level to reduce neck strain.", Category = "ergonomics", Language = "en", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 13, GroupId = "nutrition", Title = "Nutrition", Message = "Choose fruits and nuts as snacks instead of sweets to maintain your energy.", Category = "nutrition", Language = "en", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 14, GroupId = "light", Title = "Light", Message = "Enjoy natural light as much as possible. It improves mood and productivity.", Category = "environment", Language = "en", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 15, GroupId = "sleep", Title = "Sleep", Message = "Aim for 7-8 hours of sleep per night for optimal recovery.", Category = "sleep", Language = "en", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            // Arabic tips
            new HealthTipEntity { Id = 16, GroupId = "posture", Title = "الوضعية", Message = "حافظ على استقامة ظهرك وقدميك مسطحة على الأرض لتجنب آلام الظهر.", Category = "هندسة_بشرية", Language = "ar", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 17, GroupId = "screen", Title = "الشاشة", Message = "ضع شاشتك على مسافة ذراع وعلى مستوى العينين لتقليل إجهاد الرقبة.", Category = "هندسة_بشرية", Language = "ar", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new HealthTipEntity { Id = 18, GroupId = "nutrition", Title = "التغذية", Message = "اختر الفواكه والمكسرات كوجبات خفيفة بدلاً من الحلويات للحفاظ على طاقتك.", Category = "تغذية", Language = "ar", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
