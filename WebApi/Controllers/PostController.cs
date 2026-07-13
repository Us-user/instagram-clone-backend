using Domain.DTOs.Post;
using Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// Посты и взаимодействия: ленты (общая/reels/подписки), CRUD, лайки/просмотры/комменты/избранное.
/// Пути/методы/параметры воспроизводят контракт дословно. Id текущего юзера — из claims.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService) => _postService = postService;

    /// <summary>Лента с фильтром по автору/заголовку/тексту, счётчиками и флагами текущего юзера.</summary>
    [HttpGet("get-posts")]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string? userId,
        [FromQuery] string? title,
        [FromQuery] string? content,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _postService.GetPostsAsync(userId, title, content, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Только reels (IsReel = true) с пагинацией.</summary>
    [HttpGet("get-reels")]
    public async Task<IActionResult> GetReels(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _postService.GetReelsAsync(pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Пост по id.</summary>
    [HttpGet("get-post-by-id")]
    public async Task<IActionResult> GetPostById([FromQuery] int? id)
    {
        var result = await _postService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Посты текущего пользователя.</summary>
    [HttpGet("get-my-posts")]
    public async Task<IActionResult> GetMyPosts()
    {
        var result = await _postService.GetMyPostsAsync();
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Лента из постов тех, на кого подписан UserId (по умолчанию — текущий юзер).</summary>
    [HttpGet("get-following-post")]
    public async Task<IActionResult> GetFollowingPost(
        [FromQuery] string? userId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _postService.GetFollowingPostAsync(userId, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Создание поста (multipart/form-data): Title, Content, Images (required).</summary>
    [HttpPost("add-post")]
    public async Task<IActionResult> AddPost([FromForm] AddPostDto dto)
    {
        var result = await _postService.AddPostAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удаление поста — только автор.</summary>
    [HttpDelete("delete-post")]
    public async Task<IActionResult> DeletePost([FromQuery] int? id)
    {
        var result = await _postService.DeletePostAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Тумблер лайка поста.</summary>
    [HttpPost("like-post")]
    public async Task<IActionResult> LikePost([FromQuery] int? postId)
    {
        var result = await _postService.LikePostAsync(postId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Зафиксировать просмотр поста (уникально на юзера).</summary>
    [HttpPost("view-post")]
    public async Task<IActionResult> ViewPost([FromQuery] int? postId)
    {
        var result = await _postService.ViewPostAsync(postId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Добавить комментарий к посту или ответ на комментарий (необязательный parentCommentId).</summary>
    [HttpPost("add-comment")]
    public async Task<IActionResult> AddComment([FromBody] AddPostCommentDto dto)
    {
        var result = await _postService.AddCommentAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Тумблер лайка комментария.</summary>
    [HttpPost("like-comment")]
    public async Task<IActionResult> LikeComment([FromQuery] int? commentId)
    {
        var result = await _postService.LikeCommentAsync(commentId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Ответы под комментарием (2-й уровень) с пагинацией.</summary>
    [HttpGet("get-comment-replies")]
    public async Task<IActionResult> GetCommentReplies(
        [FromQuery] int? commentId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var result = await _postService.GetCommentRepliesAsync(commentId, pageNumber, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Удалить комментарий — только автор комментария.</summary>
    [HttpDelete("delete-comment")]
    public async Task<IActionResult> DeleteComment([FromQuery] int? commentId)
    {
        var result = await _postService.DeleteCommentAsync(commentId);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Тумблер избранного для поста.</summary>
    [HttpPost("add-post-favorite")]
    public async Task<IActionResult> AddPostFavorite([FromBody] AddPostFavoriteDto dto)
    {
        var result = await _postService.AddPostFavoriteAsync(dto);
        return StatusCode(result.StatusCode, result);
    }
}
