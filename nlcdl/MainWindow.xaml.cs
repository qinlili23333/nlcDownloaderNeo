using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace nlcdl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebview();
        }

        private async Task InitializeWebview()
        {
            string WebviewArgu = "--disable-features=msSmartScreenProtection,ElasticOverscroll,PersistentHistograms,SubresourceFilter --disk-cache-size=1 --renderer-process-limit=1";
            CoreWebView2EnvironmentOptions options = new()
            {
                AdditionalBrowserArguments = WebviewArgu
            };
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\QinliliWebview2\");
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, Environment.CurrentDirectory + @"\QinliliWebview2\", options);
            await WebView.EnsureCoreWebView2Async(webView2Environment);
            await ControlWebView.EnsureCoreWebView2Async(webView2Environment);
            WebView.IsEnabled = true;
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            WebView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                URLBox.Text = e.Uri;
            };
            WebView.CoreWebView2.NewWindowRequested += (sender, e) =>
            {
                e.Handled = true;
                WebView.CoreWebView2.Navigate(e.Uri);
            };
            ControlWebView.IsEnabled = true;
            ControlWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            ControlWebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            WebView.CoreWebView2.Navigate("http://read.nlc.cn/user/index");
            ControlWebView.CoreWebView2.NavigateToString(WebRes.Control);
        }

        private void URLBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                WebView.CoreWebView2.Navigate(URLBox.Text);
            }
        }

    }
}