using Radzen;

namespace OlondongeApp.Services;

/// <summary>
/// Implementação do serviço de notificações. Emite eventos para o componente Toaster exibir as mensagens.
/// </summary>
public sealed class AppNotificationService : IAppNotificationService
{
    private readonly List<Action<NotificationMessage>> _listeners = new();

    public void Notify(NotificationMessage message)
    {
        foreach (var listener in _listeners.ToList())
        {
            listener.Invoke(message);
        }
    }

    public void AddListener(Action<NotificationMessage> handler)
    {
        if (!_listeners.Contains(handler))
        {
            _listeners.Add(handler);
        }
    }

    public void RemoveListener(Action<NotificationMessage> handler)
    {
        _listeners.Remove(handler);
    }
}

