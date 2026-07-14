using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Контекст БД. Наследует <see cref="IdentityDbContext{TUser,TRole,TKey}"/>,
/// поэтому таблицы Identity (AspNetUsers/Roles/…) создаются автоматически.
/// Ключ пользователя — строка (<see cref="User"/> : IdentityUser&lt;string&gt;).
/// </summary>
public class DataContext : IdentityDbContext<User, IdentityRole, string>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostImage> PostImages => Set<PostImage>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<PostView> PostViews => Set<PostView>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<PostFavorite> PostFavorites => Set<PostFavorite>();

    public DbSet<Story> Stories => Set<Story>();
    public DbSet<StoryLike> StoryLikes => Set<StoryLike>();
    public DbSet<StoryView> StoryViews => Set<StoryView>();
    public DbSet<StoryReply> StoryReplies => Set<StoryReply>();
    public DbSet<CloseFriend> CloseFriends => Set<CloseFriend>();

    public DbSet<FollowingRelationShip> FollowingRelationShips => Set<FollowingRelationShip>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<UserSearchHistory> UserSearchHistories => Set<UserSearchHistory>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PrivacySettings> PrivacySettings => Set<PrivacySettings>();
    public DbSet<Block> Blocks => Set<Block>();

    public DbSet<Hashtag> Hashtags => Set<Hashtag>();
    public DbSet<PostHashtag> PostHashtags => Set<PostHashtag>();
    public DbSet<Mention> Mentions => Set<Mention>();

    public DbSet<GroupChat> GroupChats => Set<GroupChat>();
    public DbSet<GroupChatMember> GroupChatMembers => Set<GroupChatMember>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Применяем все IEntityTypeConfiguration из этой сборки (Infrastructure).
        builder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
