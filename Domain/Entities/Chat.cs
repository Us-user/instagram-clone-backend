namespace Domain.Entities;

/// <summary>Личный чат между двумя пользователями.</summary>
public class Chat
{
    public int Id { get; set; }
    public string User1Id { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? User1 { get; set; }
    public User? User2 { get; set; }
    public List<Message> Messages { get; set; } = new();
}
