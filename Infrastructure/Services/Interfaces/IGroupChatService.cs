using Domain.DTOs.GroupChat;
using Domain.Responses;

namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Групповые чаты (§7) — отдельная ветка от личных 1:1. Id текущего юзера — из claims.
/// Роли: Admin управляет участниками/инфо и назначает админов; Member пишет и выходит.
/// Служебные (System) сообщения фиксируют изменения состава/инфо. Отправка рассылается
/// участникам через SignalR.
/// </summary>
public interface IGroupChatService
{
    /// <summary>Создать группу: создатель — Admin, остальные — Member; служебные сообщения о создании/добавлении.</summary>
    Task<Response<GetGroupChatByIdDto>> CreateAsync(CreateGroupChatDto dto);

    /// <summary>Группы текущего юзера с последним сообщением и непрочитанными, с пагинацией.</summary>
    Task<PagedResponse<List<GetGroupChatDto>>> GetMyGroupsAsync(int? pageNumber, int? pageSize);

    /// <summary>Карточка группы: инфо + участники + сообщения; помечает входящие прочитанными.</summary>
    Task<Response<GetGroupChatByIdDto>> GetGroupByIdAsync(int? groupId);

    /// <summary>Добавить участника (только Admin) + служебное сообщение.</summary>
    Task<Response<GetGroupChatByIdDto>> AddMemberAsync(int? groupId, string? userId);

    /// <summary>Удалить участника (только Admin) + служебное сообщение.</summary>
    Task<Response<GetGroupChatByIdDto>> RemoveMemberAsync(int? groupId, string? userId);

    /// <summary>Назначить участника админом (только Admin) + служебное сообщение.</summary>
    Task<Response<GetGroupChatByIdDto>> PromoteAdminAsync(int? groupId, string? userId);

    /// <summary>Выйти из группы самому + служебное сообщение (с авто-передачей админства при необходимости).</summary>
    Task<Response<bool>> LeaveAsync(int? groupId);

    /// <summary>Обновить название/аватар группы (только Admin) + служебное сообщение о смене названия.</summary>
    Task<Response<GetGroupChatByIdDto>> UpdateInfoAsync(int? groupId, UpdateGroupInfoDto dto);

    /// <summary>Отправить сообщение в группу (участник) + рассылка через SignalR.</summary>
    Task<Response<GetGroupMessageDto>> SendMessageAsync(int? groupId, SendGroupMessageDto dto);

    /// <summary>Удалить сообщение (автор или админ группы).</summary>
    Task<Response<bool>> DeleteMessageAsync(int? messageId);
}
