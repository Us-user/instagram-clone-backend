using System.Linq.Expressions;
using Domain.DTOs.Story;
using Domain.Entities;

namespace Infrastructure.Common;

/// <summary>
/// Переиспользуемая проекция <see cref="Story"/> → <see cref="GetStoryDto"/>: поля сторис,
/// автор (avatar/userName/name) и агрегаты просмотров/лайков в <see cref="ViewerDto"/>.
/// Для сторис из поста <c>FileName</c> подставляется из первого изображения поста-источника.
/// Написана выражением, чтобы EF перевёл её в SQL (использовать в <c>Select</c> над IQueryable).
/// </summary>
public static class StoryProjections
{
    /// <summary>Проекция сторис в DTO (счётчики просмотров/лайков как сводка по зрителям).</summary>
    public static Expression<Func<Story, GetStoryDto>> ToDto() =>
        s => new GetStoryDto
        {
            Id = s.Id,
            // Сторис из файла → своё имя; сторис из поста → первое изображение поста-источника.
            FileName = s.FileName ?? s.Post!.Images.Select(i => i.ImageName).FirstOrDefault() ?? string.Empty,
            PostId = s.PostId,
            CreateAt = s.CreatedAt,
            UserId = s.UserId,
            UserAvatar = s.User!.Avatar ?? string.Empty,
            IsVerified = s.User.IsVerified,
            Audience = s.Audience,
            SharedPostId = s.SharedPostId,
            // Превью репоста (§9): заполнено только если сторис — репост поста.
            SharedPost = s.SharedPostId == null ? null : new SharedPostPreviewDto
            {
                PostId = s.SharedPost!.Id,
                UserId = s.SharedPost.UserId,
                UserName = s.SharedPost.User!.UserName!,
                ImageName = s.SharedPost.Images.Select(i => i.ImageName).FirstOrDefault()
            },
            ViewerDto = new ViewerDto
            {
                UserName = s.User!.UserName!,
                Name = s.User!.FullName,
                ViewCount = s.Views.Count,
                ViewLike = s.Likes.Count
            }
        };
}
