using PolancoWatch.Domain.Entities;

namespace PolancoWatch.Application.Interfaces;

public interface ITelegramService
{
    Task SendMessageAsync(string message, NotificationSettings? settings = null);
}
