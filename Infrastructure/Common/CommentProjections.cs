using System.Linq.Expressions;
using Domain.DTOs.Post;
using Domain.Entities;

namespace Infrastructure.Common;

/// <summary>
/// Переиспользуемая проекция <see cref="PostComment"/> → <see cref="GetPostCommentDto"/>:
/// поля комментария, автор из навигации, счётчики ответов/лайков и флаг isLiked текущего юзера
/// (Phase 14). Написана выражением, чтобы EF перевёл её в SQL (использовать в <c>Select</c> над
/// <see cref="IQueryable{PostComment}"/>). Общий источник для add-comment и get-comment-replies.
/// Список упомянутых юзеров дозаполняется отдельно через <see cref="MentionEnrichment"/>.
/// </summary>
public static class CommentProjections
{
    /// <summary>Проекция комментария в DTO с учётом текущего пользователя (<paramref name="currentUserId"/>).</summary>
    public static Expression<Func<PostComment, GetPostCommentDto>> ToDto(string currentUserId) =>
        c => new GetPostCommentDto
        {
            Id = c.Id,
            PostId = c.PostId,
            Comment = c.Comment,
            CreatedAt = c.CreatedAt,
            UserId = c.UserId,
            UserName = c.User!.UserName!,
            UserImage = c.User.Avatar,
            IsVerified = c.User.IsVerified,
            ParentCommentId = c.ParentCommentId,
            RepliesCount = c.Replies.Count,
            LikesCount = c.CommentLikes.Count,
            IsLiked = c.CommentLikes.Any(l => l.UserId == currentUserId)
        };
}
