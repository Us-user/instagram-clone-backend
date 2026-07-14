namespace Infrastructure.Services.Interfaces;

/// <summary>
/// Обработка событий набора (§1): проверяет права отправителя (участие в чате/группе, отсутствие
/// блокировки) и раскладывает событие получателям через <see cref="ITypingNotifier"/>. Событие
/// эфемерное — в БД ничего не пишется. Вызывается из SignalR-хабов; при недопустимом вводе
/// молча выходит (не бросает — это «сигнал», а не команда).
/// </summary>
public interface ITypingService
{
    /// <summary>Событие набора в личном чате: доставить собеседнику, если отправитель — участник и нет блокировки.</summary>
    Task NotifyDirectTypingAsync(string currentUserId, string currentUserName, int chatId, string? kind);

    /// <summary>Событие набора в группе: обновить список печатающих и доставить его остальным участникам.</summary>
    Task NotifyGroupTypingAsync(string currentUserId, string currentUserName, int groupChatId, string? kind);
}
