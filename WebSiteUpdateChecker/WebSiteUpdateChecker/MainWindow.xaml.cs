using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using TiqUtils.Serialize;
using TiqUtils.TypeSpeccific;
using TiQWpfUtils;
using TiQWpfUtils.Color;
using TiQWpfUtils.Controls.Extensions;

namespace WebSiteUpdateChecker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string PrevPath = "prev.data";
        private string _regexLookupPattern;
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            var prev = Json.DeserializeDataJson<PrevState>(PrevPath);
            if (prev == null) return;
            UrlBox.Text = prev.Url;
            TagBox.Text = prev.Element;
            RegexPatternBox.Text = prev.RegexPattern;
        }

        private void StorePrevious()
        {
            var prev = new PrevState
            {
                Url = UrlBox.Text,
                Element = TagBox.Text,
                RegexPattern = RegexPatternBox.Text
            };
            prev.SerializeDataJson(PrevPath);
        }

        private WebWatcher _webWatcher;

        private void ToggleUi()
        {
            if (!IsWebWatcherOperating)
            {
                ToggleButton.Content = "Stop";
                UrlBox.IsReadOnly = true;
                TagBox.IsReadOnly = true;
                RegexPatternBox.IsReadOnly = true;
            }
            else
            {
                ToggleButton.Content = "Start";
                UrlBox.IsReadOnly = false;
                TagBox.IsReadOnly = false;
                RegexPatternBox.IsReadOnly = false;
            }
        }

        private bool IsWebWatcherOperating => _webWatcher != null && _webWatcher.Running;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StorePrevious();
            ToggleUi();
            if (!IsWebWatcherOperating)
            {
                _regexLookupPattern = RegexPatternBox.Text;
                _webWatcher = new WebWatcher(UrlBox.Text, 30, TagBox.Text);
                _webWatcher.LogEntry += WebWatcherOnLogEntry;
                _webWatcher.NewFound += WebWatcherOnNewFound;
                _webWatcher.CheckDone += WebWatcherOnCheckDone;
                Task.Run(() => _webWatcher.Run());
            }
            else
            {
                _webWatcher.Stop();
            }
        }

        private void WebWatcherOnCheckDone(int count)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                CounterBlock.Text = count.ToString();
            }));
        }

        private void WebWatcherOnNewFound(string text, int level)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                SendToast(text);
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                LogBox.AppendParagraph(text.WrapTimeStamp(), ColorUtils.GetColorFromLevel(level));
            }));
        }


        private bool CheckPromoExists(string text)
        {
            return text.MatchPattern(_regexLookupPattern);
        }

        private void SendToast(string text, string title = "")
        {
            if (string.IsNullOrEmpty(title))
                title = Title;
            if (!string.IsNullOrWhiteSpace(_regexLookupPattern) && !CheckPromoExists(text)) return;
            ToastVisual visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = text.GetByPattern(_regexLookupPattern)
                        }
                    }
                }
            };
            var toastContent = new ToastContent()
            {
                Visual = visual
            };
            var doc = new XmlDocument();
            doc.LoadXml(toastContent.GetContent());
            var toast = new ToastNotification(doc)
            {
                ExpirationTime = DateTime.Now.AddHours(2),
                Tag = "1",
                Group = "PromoFound"
            };

            ToastNotificationManager.CreateToastNotifier(Title).Show(toast);
        }

        private void WebWatcherOnLogEntry(string text, int level)
        {
            Dispatcher.Invoke(new ThreadStart(delegate
            {
                LogBox.AppendParagraph(text.WrapTimeStamp(), ColorUtils.GetColorFromLevel(level));
            }));
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }
    }
}
