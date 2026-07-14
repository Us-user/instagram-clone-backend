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

            // Приватный аккаунт (Phase 12): новые подписки идут через запрос.
            var diana = await CreateUserAsync(context, userManager, logger,
                "diana", "Diana Prince", "diana@instaclone.dev", "User123!", Gender.Female,
                "Приватный аккаунт. Подписки — по запросу.", new[] { UserRole }, isPrivate: true);

            // Presence (Phase 18): «был(а) в сети» для офлайн-пользователей — чтобы get-status
            // сразу отдавал осмысленный lastSeen. Онлайн определяется по live-соединениям в рантайме.
            bob.LastSeen = DateTime.UtcNow.AddMinutes(-15);
            carol.LastSeen = DateTime.UtcNow.AddHours(-26);

            // Верификация (Phase 19): «синяя галочка» — у платформенного admin и у alice
            // (публичный автор), чтобы isVerified в DTO сразу был проверяем. Управляется
            // через /Admin/verify-user и /Admin/unverify-user (только роль Admin).
            admin.IsVerified = true;
            alice.IsVerified = true;

            await context.SaveChangesAsync();

            // 4. Подписки: alice → admin, bob; bob → alice; carol → alice (все одобренные).
            await SeedFollowsAsync(context,
                (alice, admin), (alice, bob), (bob, alice), (carol, alice));

            // 4b. Запрос на подписку на приватный аккаунт: carol → diana (Pending).
            context.FollowingRelationShips.Add(new FollowingRelationShip
            {
                UserId = carol.Id,
                FollowingUserId = diana.Id,
                Status = FollowStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            });

            // 4c. Пример блокировки (Phase 12): bob заблокировал carol.
            context.Blocks.Add(new Block
            {
                BlockerUserId = bob.Id,
                BlockedUserId = carol.Id,
                CreatedAt = DateTime.UtcNow.AddMinutes(-20)
            });

            // 5. Пара тестовых постов с лайком, комментарием, ответом и лайком коммента.
            var (alicePost, adminComment, reply) = await SeedPostsAsync(context, alice, bob, admin);

            await context.SaveChangesAsync();

            // 6. Упоминания: alice→@bob в посте (Phase 13); bob→@admin в ответе на коммент (Phase 14).
            context.Mentions.AddRange(
                new Mention
                {
                    MentionedUserId = bob.Id,
                    AuthorUserId = alice.Id,
                    EntityType = MentionEntityType.Post,
                    EntityId = alicePost.Id,
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                },
                new Mention
                {
                    MentionedUserId = admin.Id,
                    AuthorUserId = bob.Id,
                    EntityType = MentionEntityType.Comment,
                    EntityId = reply.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-50)
                });

            // 7. Тестовые уведомления (после SaveChanges — нужны сгенерированные Id постов/комментов).
            SeedNotifications(context, alicePost, adminComment, reply, alice, bob, admin, carol, diana);

            // 8. Пример группового чата (Phase 15): admin (Admin) + alice, bob (Member).
            var groupAliceHi = SeedGroupChat(context, admin, alice, bob);

            // 8b. Пример личного чата (Phase 16): alice ↔ bob с ответом (reply).
            var (directChat, directAliceMsg) = SeedDirectChat(context, alice, bob);

            // 8c. Сторис alice (Phase 17): обычная (All) и для близких друзей (CloseFriends).
            var (aliceStoryAll, aliceStoryCf) = SeedStories(context, alice);

            // 8d. Близкие друзья (Phase 17): alice добавила bob → bob видит её CloseFriends-сторис.
            context.CloseFriends.Add(new CloseFriend
            {
                UserId = alice.Id,
                FriendUserId = bob.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-6)
            });

            await context.SaveChangesAsync();

            // 8e. Реакции (§8, после SaveChanges — нужны Id сообщений): в группе admin ❤️ и bob 🔥
            // на сообщение alice; в личке bob 😂 на сообщение alice. Правило «не себе» соблюдено.
            context.MessageReactions.AddRange(
                new MessageReaction
                {
                    MessageId = groupAliceHi.Id,
                    MessageContext = MessageContext.Group,
                    UserId = admin.Id,
                    Emoji = "❤️",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-25)
                },
                new MessageReaction
                {
                    MessageId = groupAliceHi.Id,
                    MessageContext = MessageContext.Group,
                    UserId = bob.Id,
                    Emoji = "🔥",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-24)
                },
                new MessageReaction
                {
                    MessageId = directAliceMsg.Id,
                    MessageContext = MessageContext.Direct,
                    UserId = bob.Id,
                    Emoji = "😂",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-38)
                });

            // 8f. Ответ bob на сторис alice (Phase 17): личное сообщение в чат alice↔bob + связка StoryReply.
            var bobStoryReplyMsg = new Message
            {
                ChatId = directChat.Id,
                SenderUserId = bob.Id,
                MessageText = "Огонь! 🔥 Классная сторис.",
                MessageType = MessageType.Text,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-12)
            };
            context.Messages.Add(bobStoryReplyMsg);
            context.StoryReplies.Add(new StoryReply
            {
                StoryId = aliceStoryAll.Id,
                FromUserId = bob.Id,
                Message = bobStoryReplyMsg,
                CreatedAt = DateTime.UtcNow.AddMinutes(-12)
            });

            // 8g. Репост поста alice в сторис bob (Phase 17): сторис со ссылкой на оригинал.
            context.Stories.Add(new Story
            {
                UserId = bob.Id,
                SharedPostId = alicePost.Id,
                Audience = StoryAudience.All,
                CreatedAt = DateTime.UtcNow.AddMinutes(-15)
            });

            // 8h. Уведомления Phase 17: alice получает StoryReply (bob) и PostShared (bob).
            context.Notifications.AddRange(
                new Notification
                {
                    RecipientUserId = alice.Id,
                    ActorUserId = bob.Id,
                    Type = NotificationType.StoryReply,
                    EntityType = NotificationEntityType.Story,
                    EntityId = aliceStoryAll.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-12)
                },
                new Notification
                {
                    RecipientUserId = alice.Id,
                    ActorUserId = bob.Id,
                    Type = NotificationType.PostShared,
                    EntityType = NotificationEntityType.Post,
                    EntityId = alicePost.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-15)
                });

            await context.SaveChangesAsync();
            logger.LogInformation("Seed: тестовые пользователи, подписки, посты, хэштеги, упоминания, уведомления, группа, личный чат, реакции, сторис, близкие друзья, ответ на сторис и репост поста созданы");
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
        string[] roles,
        bool isPrivate = false)
    {
        var user = new User
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsPrivate = isPrivate
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

        // Для приватного аккаунта фиксируем настройки приватности (источник истины).
        if (isPrivate)
        {
            context.PrivacySettings.Add(new PrivacySettings
            {
                UserId = user.Id,
                IsPrivate = true
            });
        }

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

    private static async Task<(Post AlicePost, PostComment AdminComment, PostComment Reply)> SeedPostsAsync(
        DataContext context, User alice, User bob, User admin)
    {
        // Хэштеги (Phase 13): создаём с денормализованным PostsCount, соответствующим связям ниже.
        var now = DateTime.UtcNow;
        var sunset = new Hashtag { Tag = "sunset", PostsCount = 1, CreatedAt = now.AddHours(-3) };
        var ocean = new Hashtag { Tag = "ocean", PostsCount = 1, CreatedAt = now.AddHours(-3) };
        var travel = new Hashtag { Tag = "travel", PostsCount = 1, CreatedAt = now.AddHours(-5) };
        var roadtrip = new Hashtag { Tag = "roadtrip", PostsCount = 1, CreatedAt = now.AddHours(-5) };

        // Комментарий верхнего уровня (admin) с лайком (alice) и ответом (bob) — Phase 14.
        var adminComment = new PostComment
        {
            UserId = admin.Id,
            Comment = "Красота!",
            CreatedAt = now.AddHours(-1),
            CommentLikes = { new CommentLike { UserId = alice.Id, CreatedAt = now.AddMinutes(-40) } }
        };
        var reply = new PostComment
        {
            UserId = bob.Id,
            ParentComment = adminComment,
            // Авто-@ ответа (как в рантайме); Mention на @admin проводится ниже отдельной записью.
            Comment = "@admin полностью согласен!",
            CreatedAt = now.AddMinutes(-50)
        };

        var alicePost = new Post
        {
            UserId = alice.Id,
            Title = "Sunset",
            // Текст содержит #хэштеги и @упоминание (упоминание проводится ниже отдельной записью).
            Content = "Закат над океаном 🌅 #sunset #ocean cc @bob",
            CreatedAt = now.AddHours(-3),
            IsReel = false,
            Likes = { new PostLike { UserId = bob.Id, CreatedAt = now.AddHours(-2) } },
            Comments = { adminComment, reply },
            PostHashtags = { new PostHashtag { Hashtag = sunset }, new PostHashtag { Hashtag = ocean } }
        };

        var bobReel = new Post
        {
            UserId = bob.Id,
            Title = "Road trip",
            Content = "Короткое видео с дороги #travel #roadtrip",
            CreatedAt = now.AddHours(-5),
            IsReel = true,
            PostHashtags = { new PostHashtag { Hashtag = travel }, new PostHashtag { Hashtag = roadtrip } }
        };

        context.Posts.AddRange(alicePost, bobReel);
        await Task.CompletedTask;
        return (alicePost, adminComment, reply);
    }

    /// <summary>
    /// Пример группового чата (Phase 15): создатель <paramref name="admin"/> — Admin, alice и bob —
    /// Member. Лента начинается со служебных сообщений (создал/добавил), затем пара текстовых и
    /// ответ (reply). Весь граф добавляется в контекст; Id проставляются на общем SaveChanges.
    /// Возвращает сообщение alice «Привет всем!» — на него в Phase 16 вешаются тестовые реакции.
    /// </summary>
    private static GroupMessage SeedGroupChat(DataContext context, User admin, User alice, User bob)
    {
        var createdAt = DateTime.UtcNow.AddHours(-2);

        var aliceHi = new GroupMessage
        {
            SenderUserId = alice.Id,
            MessageText = "Привет всем!",
            MessageType = MessageType.Text,
            CreatedAt = createdAt.AddMinutes(15)
        };

        var group = new GroupChat
        {
            Name = "Команда проекта",
            CreatorUserId = admin.Id,
            CreatedAt = createdAt,
            Members =
            {
                new GroupChatMember { UserId = admin.Id, Role = GroupMemberRole.Admin, JoinedAt = createdAt, LastReadAt = createdAt.AddMinutes(20) },
                new GroupChatMember { UserId = alice.Id, Role = GroupMemberRole.Member, JoinedAt = createdAt, LastReadAt = createdAt.AddMinutes(15) },
                new GroupChatMember { UserId = bob.Id, Role = GroupMemberRole.Member, JoinedAt = createdAt }
            },
            Messages =
            {
                new GroupMessage { SenderUserId = null, MessageText = "admin создал группу", MessageType = MessageType.System, CreatedAt = createdAt },
                new GroupMessage { SenderUserId = null, MessageText = "admin добавил alice", MessageType = MessageType.System, CreatedAt = createdAt.AddSeconds(1) },
                new GroupMessage { SenderUserId = null, MessageText = "admin добавил bob", MessageType = MessageType.System, CreatedAt = createdAt.AddSeconds(2) },
                new GroupMessage { SenderUserId = admin.Id, MessageText = "Всем привет! 👋 Здесь обсуждаем проект.", MessageType = MessageType.Text, CreatedAt = createdAt.AddMinutes(10) },
                aliceHi,
                new GroupMessage { SenderUserId = bob.Id, MessageText = "@alice и тебе привет!", MessageType = MessageType.Text, ReplyToMessage = aliceHi, CreatedAt = createdAt.AddMinutes(18) }
            }
        };

        context.GroupChats.Add(group);
        return aliceHi;
    }

    /// <summary>
    /// Пример личного чата (Phase 16): alice ↔ bob с ответом (reply) bob на первое сообщение alice.
    /// Возвращает чат (для ответа на сторис в Phase 17) и сообщение alice (на него вешается тестовая
    /// реакция Direct). Id проставляются на общем SaveChanges.
    /// </summary>
    private static (Chat Chat, Message AliceMsg) SeedDirectChat(DataContext context, User alice, User bob)
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-40);

        var aliceMsg = new Message
        {
            SenderUserId = alice.Id,
            MessageText = "Привет, Боб! Как продвигается проект?",
            MessageType = MessageType.Text,
            IsRead = true,
            CreatedAt = createdAt
        };

        var chat = new Chat
        {
            User1Id = alice.Id,
            User2Id = bob.Id,
            CreatedAt = createdAt,
            Messages =
            {
                aliceMsg,
                new Message
                {
                    SenderUserId = bob.Id,
                    MessageText = "Привет! Всё по плану 🙂",
                    MessageType = MessageType.Text,
                    ReplyToMessage = aliceMsg,
                    IsRead = false,
                    CreatedAt = createdAt.AddMinutes(2)
                }
            }
        };

        context.Chats.Add(chat);
        return (chat, aliceMsg);
    }

    /// <summary>
    /// Пример сторис alice (Phase 17): обычная (<see cref="StoryAudience.All"/>) и для «близких
    /// друзей» (<see cref="StoryAudience.CloseFriends"/>). Обе активны (моложе 24ч). Файлы —
    /// плейсхолдеры (реальных изображений в сиде нет). Id проставляются на общем SaveChanges.
    /// </summary>
    private static (Story All, Story Cf) SeedStories(DataContext context, User alice)
    {
        var now = DateTime.UtcNow;

        var storyAll = new Story
        {
            UserId = alice.Id,
            FileName = "seed-story-alice.jpg",
            Audience = StoryAudience.All,
            CreatedAt = now.AddHours(-2)
        };

        var storyCf = new Story
        {
            UserId = alice.Id,
            FileName = "seed-story-alice-closefriends.jpg",
            Audience = StoryAudience.CloseFriends,
            CreatedAt = now.AddHours(-1)
        };

        context.Stories.AddRange(storyAll, storyCf);
        return (storyAll, storyCf);
    }

    /// <summary>
    /// Тестовые уведомления: для alice — подписки (bob, carol), лайк (bob) и комментарий (admin)
    /// к её посту; для bob — упоминание (alice) в посте; для admin — ответ на коммент (bob) и лайк
    /// коммента (alice) [Phase 14]; для diana — запрос на подписку (carol). Соответствуют засеянным
    /// связям/взаимодействиям. Правило «не себе» соблюдено.
    /// </summary>
    private static void SeedNotifications(
        DataContext context, Post alicePost, PostComment adminComment, PostComment reply,
        User alice, User bob, User admin, User carol, User diana)
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
            },
            // Упоминание alice → bob в посте alicePost (Phase 13).
            new Notification
            {
                RecipientUserId = bob.Id,
                ActorUserId = alice.Id,
                Type = NotificationType.Mention,
                EntityType = NotificationEntityType.Post,
                EntityId = alicePost.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            // Ответ bob → на коммент admin (Phase 14). Mention-уведомление для admin подавлено.
            new Notification
            {
                RecipientUserId = admin.Id,
                ActorUserId = bob.Id,
                Type = NotificationType.CommentReply,
                EntityType = NotificationEntityType.Comment,
                EntityId = reply.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-50)
            },
            // Лайк коммента admin от alice (Phase 14).
            new Notification
            {
                RecipientUserId = admin.Id,
                ActorUserId = alice.Id,
                Type = NotificationType.CommentLike,
                EntityType = NotificationEntityType.Comment,
                EntityId = adminComment.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-40)
            },
            // Запрос на подписку на приватный аккаунт diana от carol (Phase 12).
            new Notification
            {
                RecipientUserId = diana.Id,
                ActorUserId = carol.Id,
                Type = NotificationType.FollowRequest,
                EntityType = NotificationEntityType.User,
                EntityId = null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            });
    }
}
