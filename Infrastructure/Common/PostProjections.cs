using System.Linq.Expressions;
using Domain.DTOs.Post;
using Domain.Entities;

namespace Infrastructure.Common;

/// <summary>
/// Переиспользуемая проекция <see cref="Post"/> → <see cref="GetPostDto"/>: поля поста,
/// счётчики лайков/комментов/просмотров и флаги isLiked/isFavorite для указанного юзера.
/// Написана выражением, чтобы EF перевёл её в SQL (использовать в <c>Select</c> над
/// <see cref="IQueryable{Post}"/>). Общий источник для лент поста и избранного профиля.
/// </summary>
public static class PostProjections
{
    /// <summary>Проекция поста в DTO с учётом текущего пользователя (<paramref name="currentUserId"/>).</summary>
    public static Expression<Func<Post, GetPostDto>> ToDto(string currentUserId) =>
        p => new GetPostDto
        {
            Id = p.Id,
            Title = p.Title,
            Content = p.Content,
            CreatedAt = p.CreatedAt,
            IsReel = p.IsReel,
            UserId = p.UserId,
            UserName = p.User!.UserName!,
            UserImage = p.User.Avatar,
            IsVerified = p.User.IsVerified,
            Images = p.Images.Select(i => i.ImageName).ToList(),
            LikeCount = p.Likes.Count,
            CommentCount = p.Comments.Count,
            ViewCount = p.Views.Count,
            IsLiked = p.Likes.Any(l => l.UserId == currentUserId),
            IsFavorite = p.Favorites.Any(f => f.UserId == currentUserId)
        };
}
