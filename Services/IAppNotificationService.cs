using Radzen;

namespace OlondongeApp.Services;

/// <summary>
/// Serviço centralizado de notificações (toaster) para toda a aplicação.
/// </summary>
public interface IAppNotificationService
{
    void Notify(NotificationMessage message);

    void AddListener(Action<NotificationMessage> handler);

    void RemoveListener(Action<NotificationMessage> handler);
}

