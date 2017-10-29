using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using log4net;
using log4net.Repository.Hierarchy;

namespace TreasureHunter.SecretShop
{
    public class AccountMonitor : BrowserWatcherBase
    {
        public AccountMonitor() : base(ConfigurationManager.AppSettings["TempFolderPath"])
        {
        }
        private static readonly ILog Log = LogManager.GetLogger(typeof(AccountMonitor));
        private static readonly string Url = "https://internet-banking.dbs.com.sg/";
        protected override Task StartBrowser()
        {
            try
            {
                var cookieManager = Cef.GetGlobalCookieManager();
                // Create the offscreen Chromium browser.
                var browser = new ChromiumWebBrowser(Url);
                browser.LoadingStateChanged += Login;
                PageAnalyzeFinished.WaitOne();
                browser.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error("WebMonitor Failed", ex);
                throw;
            }
            return Task.FromResult<object>(null);
        }

        private async void Login(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (e.IsLoading || wb == null)
            {
                return;
            }
            Log.Info("Logging to IBanking");
            var scriptTask = await wb.EvaluateScriptAsync(
                $"document.getElementById('UID')");
            while (!scriptTask.Success)
            {
                scriptTask = await wb.EvaluateScriptAsync(
                    $"document.getElementById('UID')");
            }
            await wb.SavePageScreenShot(@"C:\Users\killjaeden\Desktop\a.png");
            string date = ConfigurationManager.AppSettings["TradeDate"]?.ToString();
            scriptTask = await wb.EvaluateScriptAsync(
                $"document.getElementById('UID').value = '{""}'");
            scriptTask = await wb.EvaluateScriptAsync(
                $"document.getElementById('PIN').value = '{""}'");
            wb.LoadingStateChanged -= Login;
            wb.LoadingStateChanged += Monitor;
            scriptTask = await wb.EvaluateScriptAsync(
                $"document.getElementsByClassName('btn btn-primary block mBot-12')[0].click()");
        }

        private async void Monitor(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (e.IsLoading)
            {
                return;
            }
            Log.Info("Logged in");
            await wb.SavePageScreenShot(@"C:\Users\killjaeden\Desktop\a.png");
            PageAnalyzeFinished.Set();
        }

    }
}
