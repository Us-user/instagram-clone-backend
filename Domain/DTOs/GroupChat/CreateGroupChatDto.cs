namespace Domain.DTOs.GroupChat;

/// <summary>Создание группового чата: название и стартовые участники (кроме создателя).</summary>
public class CreateGroupChatDto
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Id пользователей, добавляемых в группу. Может быть пустым (группа только с создателем).</summary>
    public List<string> MemberUserIds { get; set; } = new();
}
