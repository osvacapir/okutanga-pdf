using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

#if ANDROID
using Android.OS;
using Microsoft.AspNetCore.Components.WebView;
#endif

namespace OlondongeApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

#if IOS
        // iOS: respeitar notch/status bar/home indicator
        this.On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>().SetUseSafeArea(true);
#endif

#if ANDROID
        // Preferir o evento oficial: o WebView já está criado; HandlerChanged pode disparar cedo demais em alguns OEMs.
        blazorWebView.BlazorWebViewInitialized += (_, e) =>
        {
            if (e.WebView is not Android.Webkit.WebView webView)
            {
                return;
            }

            webView.Post(() =>
            {
                webView.SetOnApplyWindowInsetsListener(new SafeAreaInsetsListener(webView));
                webView.RequestApplyInsets();
            });
        };
#endif
    }

#if ANDROID
    /// <summary>Botão físico/gesto Voltar do Android: usa o histórico do WebView (Blazor Router).
    /// Só fecha a app quando estamos na primeira página.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (blazorWebView.Handler?.PlatformView is Android.Webkit.WebView webView && webView.CanGoBack())
        {
            webView.Post(() => webView.GoBack());
            return true;
        }

        return base.OnBackButtonPressed();
    }
#endif

#if ANDROID
    private sealed class SafeAreaInsetsListener : Java.Lang.Object, Android.Views.View.IOnApplyWindowInsetsListener
    {
        private readonly Android.Webkit.WebView _webView;
        private int _lastTop = int.MinValue;
        private int _lastBottom = int.MinValue;
        private int _lastLeft = int.MinValue;
        private int _lastRight = int.MinValue;

        public SafeAreaInsetsListener(Android.Webkit.WebView webView)
        {
            _webView = webView;
        }

        public Android.Views.WindowInsets OnApplyWindowInsets(Android.Views.View v, Android.Views.WindowInsets insets)
        {
            // API 30+: preferir barras de estado/navegação. Em falha (OEM / WebView), cair para SystemWindowInset*.
            var top = insets.SystemWindowInsetTop;
            var bottom = insets.SystemWindowInsetBottom;
            var left = insets.SystemWindowInsetLeft;
            var right = insets.SystemWindowInsetRight;
#pragma warning disable CA1422
            try
            {
                if ((int)Build.VERSION.SdkInt >= (int)BuildVersionCodes.R)
                {
                    var status = insets.GetInsets(Android.Views.WindowInsets.Type.StatusBars());
                    var nav = insets.GetInsets(Android.Views.WindowInsets.Type.NavigationBars());
                    top = status.Top;
                    bottom = nav.Bottom;
                }
            }
            catch (System.Exception)
            {
                // Manter SystemWindowInset* já atribuídos (GetInsets pode falhar em alguns OEM).
            }
#pragma warning restore CA1422

            if (top == _lastTop && bottom == _lastBottom && left == _lastLeft && right == _lastRight)
            {
                return insets;
            }

            _lastTop = top;
            _lastBottom = bottom;
            _lastLeft = left;
            _lastRight = right;

            var js =
                "try{" +
                $"document.documentElement.style.setProperty('--safe-top','{top}px');" +
                $"document.documentElement.style.setProperty('--safe-bottom','{bottom}px');" +
                $"document.documentElement.style.setProperty('--safe-left','{left}px');" +
                $"document.documentElement.style.setProperty('--safe-right','{right}px');" +
                "}catch(e){}";

            _webView.Post(() => _webView.EvaluateJavascript(js, null));
            return insets;
        }
    }
#endif
}
