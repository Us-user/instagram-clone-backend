using Domain.DTOs.Chat;
using Domain.DTOs.Story;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Сторис с жизнью 24 часа: ленты (подписки/юзер/мои), лайк-тумблер, просмотр (уникально
/// на юзера), создание из поста или файла, удаление автором. Текущий юзер — из claims.
/// §9: аудитория close-friends, ответы в директ, репост поста.
/// </summary>
public interface IStoryService
{
    /// <summary>Активные (&lt; 24ч) сторис тех, на кого подписан текущий юзер, сгруппированные по авторам.</summary>
    Task<Response<List<GetStoryDto>>> GetStoriesAsync();

    /// <summary>Активные (&lt; 24ч) сторис конкретного пользователя.</summary>
    Task<Response<List<GetStoryDto>>> GetUserStoriesAsync(string userId);

    /// <summary>Активные (&lt; 24ч) сторис текущего пользователя.</summary>
    Task<Response<List<GetStoryDto>>> GetMyStoriesAsync();

    /// <summary>Тумблер лайка сторис. Возвращает текстовый результат.</summary>
    Task<Response<string>> LikeStoryAsync(int? storyId);

    /// <summary>Сторис по id со сводкой по зрителям (viewerDto).</summary>
    Task<Response<GetStoryDto>> GetStoryByIdAsync(int? id);

    /// <summary>Создание сторис из поста (PostId) или из файла (multipart Image).</summary>
    Task<Response<GetStoryDto>> AddStoriesAsync(int? postId, AddStoryDto dto);

    /// <summary>Удаление сторис — только автор. Собственный файл удаляется с диска.</summary>
    Task<Response<bool>> DeleteStoryAsync(int? id);

    /// <summary>Зафиксировать просмотр сторис (уникально на юзера). Идемпотентно.</summary>
    Task<Response<GetStoryViewDto>> AddStoryViewAsync(int? storyId);

    /// <summary>
    /// Ответ на активную чужую сторис (§9): уходит в директ автора личным сообщением с привязкой
    /// к сторис + уведомление <c>StoryReply</c>. Учитывает настройку «кто может отвечать на сторис».
    /// </summary>
    Task<Response<GetMessageDto>> ReplyAsync(int? storyId, StoryReplyRequestDto dto);

    /// <summary>
    /// Репост чужого публичного поста в свою сторис (§9): создаёт сторис со ссылкой на оригинал
    /// (<c>SharedPostId</c>) + уведомление автору <c>PostShared</c>. Приватные посты репостить нельзя.
    /// </summary>
    Task<Response<GetStoryDto>> SharePostAsync(int? postId);
}
