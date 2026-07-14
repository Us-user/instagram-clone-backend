namespace Domain.Enums;

/// <summary>Роль участника группового чата (§7).</summary>
public enum GroupMemberRole
{
    /// <summary>Управляет участниками, инфо группы и назначает других админов. Создатель — стартовый админ.</summary>
    Admin = 0,

    /// <summary>Обычный участник: может писать и выйти из группы.</summary>
    Member = 1
}
