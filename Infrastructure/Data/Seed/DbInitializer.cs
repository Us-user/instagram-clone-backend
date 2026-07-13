using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Seed;

/// <summary>
/// Применяет миграции и наполняет БД начальными данными:
/// роли Admin/User, тестовые пользователи с профилями, локации и связи подписок/постов.
/// Идемпотентна — повторный запуск не создаёт дубликатов.
/// </summary>
public static class DbInitializer
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var context = services.GetRequiredService<DataContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Миграции.
        await context.Database.MigrateAsync();

        // 2. Роли.
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Seed: создана роль {Role}", role);
            }
        }

        // 3. Пользователи + профили (только если пользователей ещё нет).
        if (!await userManager.Users.AnyAsync())
        {
            var admin = await CreateUserAsync(context, userManager, logger,
                "admin", "Администратор", "admin@instaclone.dev", "Admin123!", Gender.Male,
                "Главный администратор системы.", new[] { AdminRole, UserRole });

            var alice = await CreateUserAsync(context, userManager, logger,
                "alice", "Alice Walker", "alice@instaclone.dev", "User123!", Gender.Female,
                "Люблю фотографировать закаты.", new[] { UserRole });

            var bob = await CreateUserAsync(context, userManager, logger,
                "bob", "Bob Stone", "bob@instaclone.dev", "User123!", Gender.Male,
                "Путешествия и кофе.", new[] { UserRole });

            var carol = await CreateUserAsync(context, userManager, logger,
                "carol", "Carol Reed", "carol@instaclone.dev", "User123!", Gender.Female,
                null, new[] { UserRole });

            await context.SaveChangesAsync();

            // 4. Подписки: alice → admin, bob; bob → alice; carol → alice.
            await SeedFollowsAsync(context,
                (alice, admin), (alice, bob), (bob, alice), (carol, alice));

            // 5. Пара тестовых постов с лайком и комментарием (без изображений).
            var alicePost = await SeedPostsAsync(context, alice, bob, admin);

            await context.SaveChangesAsync();

            // 6. Тестовые уведомления (после SaveChanges — нужны сгенерированные Id постов).
            SeedNotifications(context, alicePost, alice, bob, admin, carol);

            await context.SaveChangesAsync();
            logger.LogInformation("Seed: тестовые пользователи, подписки, посты и уведомления созданы");
        }

        // 7. Справочник локаций.
        if (!await context.Locations.AnyAsync())
        {
            context.Locations.AddRange(
                new Location { City = "New York", State = "NY", ZipCode = "10001", Country = "USA" },
                new Location { City = "Los Angeles", State = "CA", ZipCode = "90001", Country = "USA" },
                new Location { City = "London", State = "England", ZipCode = "SW1A", Country = "UK" },
                new Location { City = "Berlin", State = "Berlin", ZipCode = "10115", Country = "Germany" },
                new Location { City = "Tokyo", State = "Tokyo", ZipCode = "100-0001", Country = "Japan" });

            await context.SaveChangesAsync();
            logger.LogInformation("Seed: справочник локаций заполнен");
        }
    }

    private static async Task<User> CreateUserAsync(
        DataContext context,
        UserManager<User> userManager,
        ILogger logger,
        string userName,
        string fullName,
        string email,
        string password,
        Gender gender,
        string? about,
        string[] roles)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Не удалось создать пользователя {userName}: {errors}");
        }

        await userManager.AddToRolesAsync(user, roles);

        // При регистрации у пользователя создаётся профиль (правило контракта).
        context.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            Gender = gender,
            About = about
        });

        logger.LogInformation("Seed: создан пользователь {UserName}", userName);
        return user;
    }

    private static async Task SeedFollowsAsync(
        DataContext context,
        params (User Follower, User Target)[] follows)
    {
        foreach (var (follower, target) in follows)
        {
            context.FollowingRelationShips.Add(new FollowingRelationShip
            {
                UserId = follower.Id,
                FollowingUserId = target.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await Task.CompletedTask;
    }

    private static async Task<Post> SeedPostsAsync(DataContext context, User alice, User bob, User admin)
    {
        var alicePost = new Post
        {
            UserId = alice.Id,
            Title = "Sunset",
            Content = "Закат над океаном 🌅",
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            IsReel = false,
            Likes = { new PostLike { UserId = bob.Id, CreatedAt = DateTime.UtcNow.AddHours(-2) } },
            Comments = { new PostComment { UserId = admin.Id, Comment = "Красота!", CreatedAt = DateTime.UtcNow.AddHours(-1) } }
        };

        var bobReel = new Post
        {
            UserId = bob.Id,
            Title = "Road trip",
            Content = "Короткое видео с дороги",
            CreatedAt = DateTime.UtcNow.AddHours(-5),
            IsReel = true
        };

        context.Posts.AddRange(alicePost, bobReel);
        await Task.CompletedTask;
        return alicePost;
    }

    /// <summary>
    /// Тестовые уведомления для alice: подписки (bob, carol), лайк (bob) и комментарий (admin)
    /// к её посту — соответствуют засеянным связям/взаимодействиям. Правило «не себе» соблюдено.
    /// </summary>
    private static void SeedNotifications(
        DataContext context, Post alicePost, User alice, User bob, User admin, User carol)
    {
        context.Notifications.AddRange(
            new Notification
            {
                RecipientUserId = alice.Id,
                ActorUserId = bob.Id,
                Type = NotificationType.Follow,
                EntityType = NotificationEntityType.User,
                EntityId = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-4)
            },
            new Notification
            {
                RecipientUserId = alice.Id,
                ActorUserId = carol.Id,
                Type = NotificationType.Follow,
                EntityType = NotificationEntityType.User,
                EntityId = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new Notification
            {
                RecipientUserId = alice.Id,
                ActorUserId = bob.Id,
                Type = NotificationType.Like,
                EntityType = NotificationEntityType.Post,
                EntityId = alicePost.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new Notification
            {
                RecipientUserId = alice.Id,
                ActorUserId = admin.Id,
                Type = NotificationType.Comment,
                EntityType = NotificationEntityType.Post,
                EntityId = alicePost.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            });
    }
}
