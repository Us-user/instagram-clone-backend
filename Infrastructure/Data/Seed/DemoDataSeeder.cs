using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data.Seed;

/// <summary>
/// Наполняет БД <b>объёмными демо-данными</b> для ручного тестирования: ~20 тематических
/// пользователей с аватарами, сеть подписок, посты и рилсы с картинками/хэштегами/лайками/
/// комментами/просмотрами и активные сторис. Временные метки разнесены на ~60 дней, чтобы
/// проект выглядел «давно работающим».
/// <para>
/// Запускается <b>после</b> <see cref="DbInitializer"/> (роли и базовые пользователи уже есть).
/// Идемпотентен: маркер — существование первого демо-пользователя; повторный запуск ничего не
/// добавляет. Управляется флагом конфигурации <c>Seed:DemoData</c> (если не задан — включён
/// только в Development). Картинки — самодостаточные SVG (<see cref="DemoAssets"/>).
/// </para>
/// </summary>
public static class DemoDataSeeder
{
    private const string DemoPassword = "User123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoDataSeeder");
        var config = services.GetRequiredService<IConfiguration>();
        var env = services.GetRequiredService<IWebHostEnvironment>();

        var enabled = config.GetValue<bool?>("Seed:DemoData") ?? env.IsDevelopment();
        if (!enabled)
        {
            logger.LogInformation("DemoDataSeeder: пропущен (Seed:DemoData выключен, окружение {Env}).", env.EnvironmentName);
            return;
        }

        var context = services.GetRequiredService<DataContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var personas = Personas;

        // Папка для SVG-плейсхолдеров (как в FileService: WebRootPath или ContentRoot/wwwroot).
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var imagesFolder = Path.Combine(webRoot, "images");
        Directory.CreateDirectory(imagesFolder);

        // Одноразовый сброс: при Seed:ResetDemoData=true сносим ВСЕ сидовые демо-данные (пользователи-
        // персоны и весь их контент — БД каскадно уносит посты/сторис/лайки/подписки и т.д.) вместе со
        // старыми SVG, чтобы ниже наполнить заново. Реальные/базовые аккаунты не трогаем. После первого
        // редеплоя флаг нужно выключить, иначе демо будет пересоздаваться на каждом старте.
        var reset = config.GetValue<bool?>("Seed:ResetDemoData") ?? false;
        if (reset)
            await ResetDemoDataAsync(context, imagesFolder, logger);

        // Маркер идемпотентности: если первый демо-пользователь уже есть — считаем, что сид отработал.
        // SVG-плейсхолдеры демо при этом всё равно перегенерируем: на PaaS с эфемерным диском (Render
        // free) wwwroot стирается при рестарте, а сам сид пропускается (данные в БД остались) — без
        // перегенерации демо-картинки пропали бы. Файлы детерминированы, восстанавливаем их из БД.
        if (await userManager.FindByNameAsync(personas[0].Handle) is not null)
        {
            await EnsureDemoAssetsAsync(context, imagesFolder);
            logger.LogInformation("DemoDataSeeder: демо-данные уже есть — SVG перегенерированы, сид пропущен.");
            return;
        }

        if (!await roleManager.RoleExistsAsync(DbInitializer.UserRole))
            await roleManager.CreateAsync(new IdentityRole(DbInitializer.UserRole));

        // Детерминированный ГПСЧ — стабильные объёмы/распределения между запусками на чистой БД.
        var rnd = new Random(20260716);
        var now = DateTime.UtcNow;

        // ── 1. Пользователи + профили + аватары ───────────────────────────────────
        var users = new List<DemoUser>(personas.Length);
        foreach (var p in personas)
        {
            var user = new User
            {
                UserName = p.Handle,
                Email = $"{p.Handle.Replace('.', '_')}@instaclone.dev",
                EmailConfirmed = true,
                FullName = p.FullName,
                IsPrivate = p.Private,
                IsVerified = p.Verified,
                Avatar = $"demo-ava-{Slug(p.Handle)}.svg",
                LastSeen = now.AddMinutes(-rnd.Next(3, 4320)) // «был(а) в сети» от 3 мин до ~3 дней назад
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"DemoDataSeeder: не удалось создать {p.Handle}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(user, DbInitializer.UserRole);

            context.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                Gender = p.Gender,
                About = p.About,
                Image = user.Avatar
            });

            if (p.Private)
                context.PrivacySettings.Add(new PrivacySettings { UserId = user.Id, IsPrivate = true });

            DemoAssets.Write(imagesFolder, user.Avatar!, DemoAssets.Avatar(p.Handle, Initials(p.FullName)));

            users.Add(new DemoUser(user, p));
        }

        await context.SaveChangesAsync();
        logger.LogInformation("DemoDataSeeder: создано {Count} демо-пользователей.", users.Count);

        // ── 2. Сеть подписок ──────────────────────────────────────────────────────
        var follows = new HashSet<(string, string)>();
        foreach (var follower in users)
        {
            var targets = Sample(rnd, users, rnd.Next(6, 13), follower);
            foreach (var target in targets)
            {
                if (!follows.Add((follower.Id, target.Id)))
                    continue;

                // На приватный аккаунт часть подписок оставляем «в ожидании» (запрос на подписку).
                var status = target.Persona.Private && rnd.NextDouble() < 0.4
                    ? FollowStatus.Pending
                    : FollowStatus.Accepted;

                context.FollowingRelationShips.Add(new FollowingRelationShip
                {
                    UserId = follower.Id,
                    FollowingUserId = target.Id,
                    Status = status,
                    CreatedAt = now.AddDays(-rnd.Next(1, 90)).AddMinutes(-rnd.Next(0, 1440))
                });
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("DemoDataSeeder: создано {Count} связей подписки.", follows.Count);

        // ── 3. Посты и рилсы (+ картинки, хэштеги, лайки, комменты, просмотры, избранное) ──
        var hashtags = await LoadHashtagsAsync(context);
        var postSeq = 0;
        var reelCount = 0;
        var postCount = 0;

        foreach (var author in users)
        {
            var count = rnd.Next(4, 8); // 4–7 постов на автора
            for (var i = 0; i < count; i++)
            {
                var isReel = rnd.NextDouble() < 0.3;
                var caption = Pick(rnd, author.Persona.Captions);
                var tags = PickTags(rnd, author.Persona.Hashtags);
                var createdAt = now.AddDays(-rnd.Next(0, 60)).AddMinutes(-rnd.Next(0, 1440));
                var imageName = $"demo-post-{++postSeq}.svg";

                var svg = isReel
                    ? DemoAssets.Reel(imageName, author.Persona.Emoji, caption)
                    : DemoAssets.Post(imageName, author.Persona.Emoji, caption);
                DemoAssets.Write(imagesFolder, imageName, svg);

                var post = new Post
                {
                    UserId = author.Id,
                    Title = Truncate(caption, 60),
                    Content = $"{caption} {string.Join(" ", tags.Select(t => "#" + t))}",
                    CreatedAt = createdAt,
                    IsReel = isReel,
                    Images = { new PostImage { ImageName = imageName } }
                };

                foreach (var tag in tags)
                {
                    var h = GetOrCreateHashtag(context, hashtags, tag, createdAt);
                    h.PostsCount++;
                    post.PostHashtags.Add(new PostHashtag { Hashtag = h });
                }

                // Лайки: случайное подмножество других пользователей, после публикации.
                foreach (var liker in Sample(rnd, users, rnd.Next(2, 15), author))
                    post.Likes.Add(new PostLike { UserId = liker.Id, CreatedAt = AfterUpTo(rnd, createdAt, now) });

                // Просмотры: обычно больше, чем лайков (уникальны на пару Post/User).
                foreach (var viewer in Sample(rnd, users, rnd.Next(6, 19), author))
                    post.Views.Add(new PostView { UserId = viewer.Id });

                // Избранное: у части постов.
                foreach (var fav in Sample(rnd, users, rnd.Next(0, 4), author))
                    post.Favorites.Add(new PostFavorite { UserId = fav.Id, CreatedAt = AfterUpTo(rnd, createdAt, now) });

                // Комментарии верхнего уровня + иногда ответ автора.
                var commenters = Sample(rnd, users, rnd.Next(0, 5), author);
                foreach (var commenter in commenters)
                {
                    var at = AfterUpTo(rnd, createdAt, now);
                    var top = new PostComment
                    {
                        UserId = commenter.Id,
                        Comment = Pick(rnd, Comments),
                        CreatedAt = at
                    };
                    // Часть комментов автор лайкает.
                    if (rnd.NextDouble() < 0.4)
                        top.CommentLikes.Add(new CommentLike { UserId = author.Id, CreatedAt = AfterUpTo(rnd, at, now) });
                    // Иногда автор отвечает (@упоминание, как в рантайме — авто-@).
                    if (rnd.NextDouble() < 0.35)
                        post.Comments.Add(new PostComment
                        {
                            UserId = author.Id,
                            ParentComment = top,
                            Comment = $"@{commenter.Handle} {Pick(rnd, Replies)}",
                            CreatedAt = AfterUpTo(rnd, at, now)
                        });
                    post.Comments.Add(top);
                }

                context.Posts.Add(post);
                if (isReel) reelCount++; else postCount++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("DemoDataSeeder: создано {Posts} постов и {Reels} рилсов.", postCount, reelCount);

        // ── 4. Активные сторис (моложе 24ч) + просмотры/лайки ──────────────────────
        var storySeq = 0;
        var storyTotal = 0;
        foreach (var author in users)
        {
            if (rnd.NextDouble() < 0.35)
                continue; // не у всех есть активная сторис

            var count = rnd.Next(1, 3);
            for (var i = 0; i < count; i++)
            {
                var createdAt = now.AddHours(-rnd.Next(1, 22)).AddMinutes(-rnd.Next(0, 60));
                var fileName = $"demo-story-{++storySeq}.svg";
                DemoAssets.Write(imagesFolder, fileName,
                    DemoAssets.Story(fileName, author.Handle, Pick(rnd, author.Persona.Captions)));

                var story = new Story
                {
                    UserId = author.Id,
                    FileName = fileName,
                    Audience = StoryAudience.All,
                    CreatedAt = createdAt
                };

                foreach (var viewer in Sample(rnd, users, rnd.Next(3, 16), author))
                {
                    story.Views.Add(new StoryView { ViewUserId = viewer.Id });
                    if (rnd.NextDouble() < 0.25)
                        story.Likes.Add(new StoryLike { UserId = viewer.Id });
                }

                context.Stories.Add(story);
                storyTotal++;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("DemoDataSeeder: создано {Count} активных сторис.", storyTotal);
        logger.LogInformation("DemoDataSeeder: готово. Логин под любым: <handle> / {Pwd}", DemoPassword);
    }

    // ───────────────────────── helpers ─────────────────────────

    /// <summary>
    /// Полностью сносит сидовые демо-данные для чистого пересоздания: удаляет пользователей-персон
    /// (по <see cref="Persona.Handle"/>), а БД каскадно уносит весь их контент — посты/рилсы с
    /// картинками, лайки/комменты/просмотры/избранное, сторис, подписки, уведомления, профили и т.п.
    /// (все связи к <c>User</c> настроены как Cascade). Базовые/реальные аккаунты не затрагиваются.
    /// Денормализованный <c>PostsCount</c> хэштегов пересчитывается по факту, а старые демо-SVG удаляются
    /// с диска (наполнение перезапишет их под теми же именами). Идемпотентно: без демо-юзеров — no-op.
    /// </summary>
    private static async Task ResetDemoDataAsync(DataContext context, string imagesFolder, ILogger logger)
    {
        var handles = Personas.Select(p => p.Handle).ToArray();
        var demoUserIds = await context.Users
            .Where(u => handles.Contains(u.UserName))
            .Select(u => u.Id)
            .ToListAsync();

        if (demoUserIds.Count == 0)
        {
            logger.LogInformation("DemoDataSeeder: сброс запрошен, но демо-пользователей нет — пропуск.");
            return;
        }

        var removed = await context.Users
            .Where(u => demoUserIds.Contains(u.Id))
            .ExecuteDeleteAsync();

        // Демо-посты ушли — пересчитываем PostsCount по оставшимся связям, чтобы после повторного
        // наполнения (которое снова инкрементит счётчики) значения не задвоились.
        var hashtags = await context.Hashtags.ToListAsync();
        foreach (var h in hashtags)
            h.PostsCount = await context.PostHashtags.CountAsync(ph => ph.HashtagId == h.Id);
        await context.SaveChangesAsync();

        // Старые демо-SVG с диска (перегенерируются при наполнении под теми же именами).
        if (Directory.Exists(imagesFolder))
            foreach (var file in Directory.EnumerateFiles(imagesFolder, "demo-*.svg").ToList())
                File.Delete(file);

        logger.LogInformation(
            "DemoDataSeeder: сброс демо — удалено {Users} пользователей и весь их контент, счётчики хэштегов пересчитаны.",
            removed);
    }

    /// <summary>
    /// Перегенерирует детерминированные SVG-плейсхолдеры демо-данных на диск, восстанавливая их из
    /// того, что уже есть в БД (по префиксам <c>demo-ava-</c>/<c>demo-post-</c>/<c>demo-story-</c>).
    /// Вызывается на каждом старте, когда сид пропущен: на PaaS с эфемерным диском wwwroot стирается
    /// при рестарте, а имена файлов в БД остаются — так демо-картинки не исчезают. Реальные (не демо)
    /// загрузки живут во внешнем хранилище и здесь не участвуют. Пишем только отсутствующие файлы.
    /// </summary>
    private static async Task EnsureDemoAssetsAsync(DataContext context, string imagesFolder)
    {
        Directory.CreateDirectory(imagesFolder);
        var personaByHandle = Personas.ToDictionary(p => p.Handle, StringComparer.OrdinalIgnoreCase);

        // Аватары.
        var avatars = await context.Users
            .Where(u => u.Avatar != null && u.Avatar.StartsWith("demo-ava-"))
            .Select(u => new { u.UserName, u.FullName, u.Avatar })
            .ToListAsync();
        foreach (var a in avatars)
            WriteIfMissing(imagesFolder, a.Avatar!,
                () => DemoAssets.Avatar(a.UserName ?? string.Empty, Initials(a.FullName ?? string.Empty)));

        // Обложки постов и рилсов.
        var posts = await context.PostImages
            .Where(pi => pi.ImageName.StartsWith("demo-post-"))
            .Select(pi => new { pi.ImageName, pi.Post!.IsReel, pi.Post.Title, Handle = pi.Post.User!.UserName })
            .ToListAsync();
        foreach (var p in posts)
        {
            var emoji = p.Handle != null && personaByHandle.TryGetValue(p.Handle, out var persona)
                ? persona.Emoji : "📷";
            var caption = p.Title ?? string.Empty;
            WriteIfMissing(imagesFolder, p.ImageName, () => p.IsReel
                ? DemoAssets.Reel(p.ImageName, emoji, caption)
                : DemoAssets.Post(p.ImageName, emoji, caption));
        }

        // Сторис.
        var stories = await context.Stories
            .Where(s => s.FileName != null && s.FileName.StartsWith("demo-story-"))
            .Select(s => new { s.FileName, Handle = s.User!.UserName, s.User.FullName })
            .ToListAsync();
        foreach (var s in stories)
        {
            var text = s.Handle != null && personaByHandle.TryGetValue(s.Handle, out var persona)
                       && persona.Captions.Length > 0
                ? persona.Captions[0]
                : s.FullName ?? string.Empty;
            WriteIfMissing(imagesFolder, s.FileName!,
                () => DemoAssets.Story(s.FileName!, s.Handle ?? string.Empty, text));
        }
    }

    /// <summary>Пишет SVG на диск, только если файла с таким именем ещё нет (свежий/эфемерный диск).</summary>
    private static void WriteIfMissing(string imagesFolder, string fileName, Func<string> svgFactory)
    {
        if (!File.Exists(Path.Combine(imagesFolder, fileName)))
            DemoAssets.Write(imagesFolder, fileName, svgFactory());
    }

    private sealed record DemoUser(User Entity, Persona Persona)
    {
        public string Id => Entity.Id;
        public string Handle => Persona.Handle;
    }

    private sealed record Persona(
        string Handle, string FullName, Gender Gender, string About,
        string Emoji, string[] Hashtags, string[] Captions,
        bool Verified = false, bool Private = false);

    /// <summary>Загружает уже существующие хэштеги (из <see cref="DbInitializer"/>) для переиспользования.</summary>
    private static async Task<Dictionary<string, Hashtag>> LoadHashtagsAsync(DataContext context)
    {
        var existing = await context.Hashtags.ToListAsync();
        return existing.ToDictionary(h => h.Tag, StringComparer.OrdinalIgnoreCase);
    }

    private static Hashtag GetOrCreateHashtag(DataContext context, Dictionary<string, Hashtag> cache, string tag, DateTime createdAt)
    {
        if (cache.TryGetValue(tag, out var existing))
            return existing;

        var created = new Hashtag { Tag = tag, PostsCount = 0, CreatedAt = createdAt };
        context.Hashtags.Add(created);
        cache[tag] = created;
        return created;
    }

    /// <summary>Случайное подмножество пользователей заданного размера, исключая <paramref name="exclude"/>.</summary>
    private static List<DemoUser> Sample(Random rnd, List<DemoUser> source, int count, DemoUser exclude)
    {
        var pool = source.Where(u => u.Id != exclude.Id).ToList();
        for (var i = pool.Count - 1; i > 0; i--) // Фишер–Йейтс
        {
            var j = rnd.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        return pool.Take(Math.Clamp(count, 0, pool.Count)).ToList();
    }

    private static T Pick<T>(Random rnd, IReadOnlyList<T> items) => items[rnd.Next(items.Count)];

    /// <summary>2–3 уникальных хэштега из тематического набора автора.</summary>
    private static List<string> PickTags(Random rnd, string[] pool)
    {
        var n = Math.Min(pool.Length, rnd.Next(2, 4));
        return pool.OrderBy(_ => rnd.Next()).Take(n).ToList();
    }

    /// <summary>Случайный момент строго после <paramref name="from"/>, но не позже <paramref name="max"/>.</summary>
    private static DateTime AfterUpTo(Random rnd, DateTime from, DateTime max)
    {
        var span = max - from;
        if (span <= TimeSpan.Zero) return from;
        return from.AddSeconds(rnd.NextDouble() * span.TotalSeconds);
    }

    private static string Initials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}"
            : fullName.Length >= 2 ? fullName[..2] : fullName;
    }

    private static string Slug(string handle) => handle.Replace('.', '-');

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];

    // ───────────────────────── data ─────────────────────────

    private static readonly string[] Comments =
    {
        "Огонь! 🔥", "Обожаю это 😍", "Класс 👏", "Невероятно!", "Где это снято?",
        "Топ контент 💯", "Красота!", "Согласен на все 100", "Сохранил себе", "Вау ✨",
        "Это шедевр", "Как всегда на высоте 👌", "Вдохновляет!", "Хочу так же", "Лучшее сегодня"
    };

    private static readonly string[] Replies =
    {
        "спасибо! 🙏", "рад, что зашло", "старался 😉", "полностью согласен", "да, это было круто",
        "скоро ещё выложу", "обнял ✨"
    };

    /// <summary>20 тематических персон: несколько верифицированных, пара приватных.</summary>
    private static readonly Persona[] Personas =
    {
        new("liam_travels", "Liam Carter", Gender.Male, "Собираю страны как магниты на холодильник 🌍",
            "🌍", new[] { "travel", "wanderlust", "adventure", "sunset" },
            new[] { "Затерялся в этом городе ✨", "Лучший вид за всю поездку", "Утро в новом месте",
                    "Дорога снова зовёт", "Ещё одна отметка на карте 📍" }, Verified: true),

        new("sofia.captures", "Sofia Reyes", Gender.Female, "Ловлю свет и моменты 📷",
            "📷", new[] { "photography", "portrait", "moment", "bnw" },
            new[] { "Свет решает всё", "Кадр, который ждал час", "Люди в городе", "Тени и линии",
                    "Один дубль — один момент" }, Verified: true),

        new("noah_eats", "Noah Kim", Gender.Male, "Ем вкусно и рассказываю где 🍜",
            "🍜", new[] { "food", "foodie", "yummy", "streetfood" },
            new[] { "Обед, ради которого стоило прийти", "Тот самый рамен 🍜", "Завтрак чемпионов",
                    "Нашёл лучшее место в районе", "Просто посмотрите на это" }),

        new("mia_fitlife", "Mia Torres", Gender.Female, "Сильная > идеальная 💪",
            "💪", new[] { "fitness", "workout", "motivation", "gym" },
            new[] { "Ещё один подход", "Прогресс, а не совершенство", "Утренняя тренировка ✅",
                    "Тело благодарит за движение", "Дисциплина > мотивация" }),

        new("ethan.codes", "Ethan Novak", Gender.Male, "Пишу код и кофе ☕💻",
            "💻", new[] { "tech", "coding", "developer", "startup" },
            new[] { "Наконец-то зелёные тесты 💚", "Рефакторинг выходного дня", "Новый проект в работе",
                    "Тёмная тема — единственная тема", "Деплой в пятницу? Смело" }),

        new("ava_paints", "Ava Bennett", Gender.Female, "Крашу мир по одному холсту 🎨",
            "🎨", new[] { "art", "painting", "creative", "illustration" },
            new[] { "Новая работа готова", "Цвет дня", "В процессе...", "Люблю запах красок",
                    "Абстракция настроения" }),

        new("lucas_onwheels", "Lucas Meyer", Gender.Male, "Жизнь на четырёх колёсах 🏎️",
            "🏎️", new[] { "cars", "drive", "auto", "roadtrip" },
            new[] { "Выходные — это дорога", "Звук мотора — лучшая музыка", "Чистая после мойки ✨",
                    "Закат и трасса", "Мечта детства в гараже" }),

        new("isabella.style", "Isabella Rossi", Gender.Female, "Стиль — это язык без слов 👗",
            "👗", new[] { "fashion", "ootd", "style", "lookbook" },
            new[] { "Образ дня", "Осень — сезон пальто", "Деталь решает всё", "Минимализм в цвете",
                    "Примерочная настроения" }, Verified: true),

        new("mason_beats", "Mason Cole", Gender.Male, "Делаю биты по ночам 🎧",
            "🎧", new[] { "music", "beats", "studio", "producer" },
            new[] { "Новый трек на подходе 🎶", "Ночь в студии", "Луп, который не отпускает",
                    "Звук готов, ждите", "Вайб пятницы" }),

        new("amelia_green", "Amelia Ford", Gender.Female, "Растения — мои дети 🌿",
            "🌿", new[] { "nature", "plants", "green", "garden" },
            new[] { "Новый листик 🌱", "Джунгли дома", "Утро на балконе", "Зелёный — цвет спокойствия",
                    "Пересадка выходного дня" }),

        new("james_hikes", "James Walker", Gender.Male, "Выше облаков ⛰️",
            "⛰️", new[] { "hiking", "mountains", "trail", "nature" },
            new[] { "Вершина взята 🏔️", "Тропа на рассвете", "Тишина гор", "18 км и оно того стоило",
                    "Палатка с видом" }),

        new("charlotte.bakes", "Charlotte Dubois", Gender.Female, "Пеку счастье 🧁",
            "🧁", new[] { "baking", "dessert", "sweet", "homemade" },
            new[] { "Свежая партия 🧁", "Пахнет на весь дом", "Тесто не прощает спешки", "Воскресный пирог",
                    "Сахарная пудра — это любовь" }),

        new("ben_lifts", "Ben Harper", Gender.Male, "Железо не врёт 🏋️",
            "🏋️", new[] { "gym", "workout", "fitness", "nopainnogain" },
            new[] { "Новый рекорд в жиме", "Ноги сегодня 🦵", "Форма — это привычка", "Разминка обязательна",
                    "После зала лучше всех" }),

        new("emily_reads", "Emily Clark", Gender.Female, "Читаю больше, чем сплю 📚 (приватный)",
            "📚", new[] { "books", "reading", "booklover", "cozy" },
            new[] { "Дочитала за ночь", "Стопка на неделю 📚", "Чай и хорошая книга", "Эта цитата зацепила",
                    "Осень для чтения" }, Private: true),

        new("daniel_shots", "Daniel Ivanov", Gender.Male, "Улицы и истории 🎞️ (приватный)",
            "🎞️", new[] { "streetphotography", "photography", "urban", "moment" },
            new[] { "Город живёт в деталях", "Случайный кадр", "Отражения вечера", "Момент между делом",
                    "Плёнка помнит всё" }, Private: true),

        new("grace_yoga", "Grace Lee", Gender.Female, "Дыши и отпускай 🧘",
            "🧘", new[] { "yoga", "wellness", "mindfulness", "balance" },
            new[] { "Утро с ковриком", "Баланс — это практика", "Дыхание решает", "Растяжка дня",
                    "Спокойствие внутри" }),

        new("henry_brews", "Henry Adams", Gender.Male, "Жизнь слишком коротка для плохого кофе ☕",
            "☕", new[] { "coffee", "morning", "espresso", "cafe" },
            new[] { "Первый эспрессо дня", "Латте-арт вышел ✨", "Зёрна недели", "Утро начинается здесь",
                    "Кофе и тишина" }),

        new("lily_draws", "Lily Nguyen", Gender.Female, "Рисую то, что чувствую ✏️",
            "✏️", new[] { "illustration", "art", "drawing", "sketch" },
            new[] { "Скетч дня", "Персонаж родился", "Линии и настроение", "В блокноте снова тесно",
                    "Цифра или бумага? И то и то" }),

        new("oscar_flies", "Oscar Blanco", Gender.Male, "Мир сверху красивее 🚁",
            "🚁", new[] { "drone", "aerial", "fromabove", "landscape" },
            new[] { "Кадр с высоты 200м", "Земля — это узоры", "Побережье сверху 🌊", "Дрон и рассвет",
                    "Вид, который не увидишь снизу" }),

        new("zoe_wanders", "Zoe Martin", Gender.Female, "Живу между рейсами ✈️",
            "✈️", new[] { "travel", "lifestyle", "wanderlust", "citylife" },
            new[] { "Новый город — новый я", "Кофе в незнакомом месте", "Чемодан снова собран",
                    "Улицы, по которым хочется гулять", "Каждый рейс — история" }, Verified: true),
    };
}
