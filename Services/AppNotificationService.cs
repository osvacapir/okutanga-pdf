using OkutangaPDF.Models;

namespace OkutangaPDF.Services;

public interface IAppNotificationService
{
    void Notify(ToastMessage message);

    void AddListener(Action<ToastMessage> handler);

    void RemoveListener(Action<ToastMessage> handler);
}

public sealed class AppNotificationService : IAppNotificationService
{
    private readonly List<Action<ToastMessage>> _listeners = [];

    public void Notify(ToastMessage message)
    {
        foreach (var listener in _listeners.ToArray())
        {
            listener(message);
        }
    }

    public void AddListener(Action<ToastMessage> handler) => _listeners.Add(handler);

    public void RemoveListener(Action<ToastMessage> handler) => _listeners.Remove(handler);
}
