using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using PDFWV2;
using System.IO;
using System.Net.Http;
using System.Reflection.Emit;
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
        private MemoryStream FileStreamCache = new();
        private PDFEngine Engine;
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebview();
        }

        private async Task InitializeWebview()
        {
            string WebviewArgu = "--disable-features=msSmartScreenProtection,ElasticOverscroll,PersistentHistograms,SubresourceFilter --renderer-process-limit=1";
            CoreWebView2EnvironmentOptions options = new()
            {
                AdditionalBrowserArguments = WebviewArgu
            };
            Directory.CreateDirectory(Environment.CurrentDirectory + @"\QinliliWebview2\");
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, Environment.CurrentDirectory + @"\QinliliWebview2\", options);
            await WebView.EnsureCoreWebView2Async(webView2Environment);
            PDFWV2Instance.SetWebView2Instance(webView2Environment);
            PDFWV2Options Options = new()
            {
                DebugTool = false,
                DefaultEngine = Engines.Adobe,
                SecurityHardenLevel = SecurityLevel.None
            };
            PDFWV2Instance PDF = await PDFWV2Instance.GetInstance(Options);
            Engine = await PDF.CreateEngine();
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
            WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All,CoreWebView2WebResourceRequestSourceKinds.All);
            WebView.CoreWebView2.WebResourceRequested +=  (sender, e) =>
            {
                //Console.WriteLine(e.Request.Uri);
                if (e.Request.Headers.GetHeader("Referer").Contains("WebPDFJRWorker") && e.Request.Headers.GetHeader("Myreader").Length > 0)
                {
                    //PDF请求已经捕获
                    Status.Content = "PDF请求已捕获，正在下载...";
                    var uri = e.Request.Uri;
                    var method = e.Request.Method;
                    var headers = e.Request.Headers.ToList();
                    byte[] requestBody = [];
                    if (e.Request.Content != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            e.Request.Content.CopyTo(ms);
                            requestBody = ms.ToArray();
                        }
                    }
                    var deferral = e.GetDeferral();
                    Task.Run(() => ReplayRequest(uri, method, headers, requestBody)).ContinueWith(_ => {
                        Console.WriteLine("Replay completed, setting response...");
                        Application.Current.Dispatcher.Invoke(() => {
                            e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(FileStreamCache, 200, "OK", "Content-Type: application/pdf");
                            deferral.Complete();
                        });
                    });
                }
            };
            //https://github.com/MicrosoftEdge/WebView2Feedback/issues/4926
            //微软你妈被我操死了，一年多了修不完这个bug
            //WebView.CoreWebView2.WebResourceResponseReceived += (sender, e) =>
            //{
            //    Console.WriteLine(e.Request.Uri);
            //    if (e.Response != null && e.Response.Headers != null)
            //    {
            //        if (e.Request.Headers.GetHeader("Referer").Contains("WebPDFJRWorker") && e.Response.Headers.GetHeader("Content-Disposition").Contains("attachment"))
            //        {
            //            //PDF已经捕获
            //            FileStreamCache = new MemoryStream();
            //            e.Response.GetContentAsync().ContinueWith(task =>
            //            {
            //                task.Result.CopyTo(FileStreamCache);
            //                Status.Content = "PDF已捕获，文件大小：" + FileStreamCache.Length + " 字节";
            //                EnableBtn();
            //            });
            //        }
            //    }
            //};
            WebView.CoreWebView2.Navigate("http://read.nlc.cn/user/index");
        }

        private void URLBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                WebView.CoreWebView2.Navigate(URLBox.Text);
            }
        }

        private void EnableBtn()
        {
            SaveBtn.IsEnabled = true;
            PreviewBtn.IsEnabled = true;
            DropBtn.IsEnabled = true;
        }

        private void DisableBtn()
        {
            SaveBtn.IsEnabled = false;
            PreviewBtn.IsEnabled = false;
            DropBtn.IsEnabled = false;
        }

        private void DropBtn_Click(object sender, RoutedEventArgs e)
        {
            FileStreamCache = new();
            Status.Content = "等待可捕获文件...";
            DisableBtn();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            saveFileDialog.Title = "保存文件";
            saveFileDialog.FileName = "nlc.pdf";

            // 3. Show the dialog and check the result
            if (saveFileDialog.ShowDialog() == true)
            {
                // 4. Save the content to the selected path
                using (var fileStream = File.OpenWrite(saveFileDialog.FileName))
                {
                    FileStreamCache.CopyTo(fileStream);
                }
            }
        }

        private async void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            Engine.ViewStream(FileStreamCache);
        }

        private async Task ReplayRequest(string uri, string method, IEnumerable<KeyValuePair<string, string>> headers, byte[] body)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(new HttpMethod(method), uri);
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (body != null)
                {
                    request.Content = new ByteArrayContent(body);
                    var contentType = headers.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value;
                    if (contentType != null) request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
                }

                var response = await client.SendAsync(request);
                var responseStream = await response.Content.ReadAsStreamAsync();

                responseStream.CopyTo(FileStreamCache);

                Application.Current.Dispatcher.Invoke(() => {
                    Status.Content = "PDF已读取，文件大小：" + FileStreamCache.Length + " 字节";
                    EnableBtn();
                });
            }
        }
    }
}