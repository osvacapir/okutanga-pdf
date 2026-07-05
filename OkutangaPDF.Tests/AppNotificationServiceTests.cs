using OkutangaPDF.Models;
using OkutangaPDF.Services;

namespace OkutangaPDF.Tests;

[TestClass]
public sealed class AppNotificationServiceTests
{
    [TestMethod]
    public void Notify_DeliversToRegisteredListener()
    {
        var service = new AppNotificationService();
        ToastMessage? received = null;
        service.AddListener(msg => received = msg);

        var message = new ToastMessage { Summary = "Teste", Severity = ToastSeverity.Success };
        service.Notify(message);

        Assert.AreSame(message, received);
    }

    [TestMethod]
    public void RemoveListener_StopsDelivery()
    {
        var service = new AppNotificationService();
        var count = 0;
        Action<ToastMessage> handler = _ => count++;
        service.AddListener(handler);
        service.RemoveListener(handler);

        service.Notify(new ToastMessage { Summary = "X" });
        Assert.AreEqual(0, count);
    }
}
