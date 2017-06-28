using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WebSiteUpdateChecker
{
    internal sealed class WebWatcher
    {
        public delegate void EventLog(string text, int level);

        public delegate void CheckCounter(int count);

        public event EventLog LogEntry;
        public event EventLog NewFound;
        public event CheckCounter CheckDone;
        private string Url { get; }
        private int Interval { get; }
        private string WatchElement { get; }

        private bool WatchLast { get; }

        private int _count;

        public bool Running { get; private set; }

        private readonly System.Net.WebClient _web = new System.Net.WebClient();

        private string _previousState;
        private CancellationTokenSource _cts;

        public WebWatcher(string url, int interval, string watchElement, bool watchLast = true)
        {
            Url = url;
            Interval = interval;
            WatchElement = watchElement;
            WatchLast = watchLast;
            _web.Encoding = Encoding.UTF8;
        }

        public void Stop()
        {
            Running = false;
            _cts.Cancel();
        }

        private CancellationToken InitToken()
        {
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

        public async Task Run()
        {
            Running = true;
            OnCheckDone(0, true);
            var token = InitToken();
            try
            {
                OnLogEntry("Watch started!", 1);
                while (Running)
                {
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(await _web.DownloadStringTaskAsync(Url));

                    var tempNode = htmlDoc.DocumentNode.SelectNodes(WatchElement);

                    var node = WatchLast ? tempNode.LastOrDefault() : tempNode.FirstOrDefault();

                    if (node == null) return;

                    var innerText = ClearText(node.InnerText);

                    if (string.IsNullOrEmpty(_previousState))
                    {
                        _previousState = innerText;
                    }
                    if (_previousState != innerText)
                    {
                        _previousState = innerText;
                        OnNewFound(innerText, 2);
                    }
                    OnCheckDone();
                    await Task.Delay(Interval * 1000, token);
                }
            }
            catch (TaskCanceledException)
            {
                OnLogEntry("Watch stopped!", 1);
            }
            catch (Exception e)
            {
                OnLogEntry($"Error: {e.Message}", 0);
            }
        }

        private static string ClearText(string text)
        {
            var t = text.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var list = (from item in t where !string.IsNullOrWhiteSpace(item) select item.Trim()).ToList();
            return list.Aggregate((x, v) => x + "\r\n" + v);
        }

        private void OnLogEntry(string text, int level)
        {
            LogEntry?.Invoke(text, level);
        }

        private void OnNewFound(string text, int level)
        {
            NewFound?.Invoke(text, level);
        }

        private void OnCheckDone(int count = 1, bool reset = false)
        {
            if (reset)
            {
                _count = 0;
            }
            _count += count;
            CheckDone?.Invoke(_count);
        }
    }
}