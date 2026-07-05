using Microsoft.Maui.Controls;
using OkutangaPDF.Services;

namespace OkutangaPDF;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        blazorWebView.HandlerChanged += (_, _) =>
        {
#if WINDOWS
            if (blazorWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 wv2)
            {
                PdfLocalViewerHost.TryConfigure(wv2);
            }
#endif
        };

        blazorWebView.BlazorWebViewInitialized += (_, e) =>
        {
            PdfLocalViewerHost.TryConfigure(e.WebView);

#if ANDROID
            if (e.WebView is not Android.Webkit.WebView webView)
            {
                return;
            }

            webView.Post(() =>
            {
                webView.SetOnApplyWindowInsetsListener(new SafeAreaInsetsListener(webView));
                webView.RequestApplyInsets();
            });
#endif
        };
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
            int top;
            int bottom;
            int left;
            int right;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var status = insets.GetInsets(Android.Views.WindowInsets.Type.StatusBars());
                var nav = insets.GetInsets(Android.Views.WindowInsets.Type.NavigationBars());
                top = status.Top;
                bottom = nav.Bottom;
                left = status.Left;
                right = status.Right;
            }
            else
            {
#pragma warning disable CA1422 // SystemWindowInset* é o caminho suportado em API 24–29.
                top = insets.SystemWindowInsetTop;
                bottom = insets.SystemWindowInsetBottom;
                left = insets.SystemWindowInsetLeft;
                right = insets.SystemWindowInsetRight;
#pragma warning restore CA1422
            }

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
