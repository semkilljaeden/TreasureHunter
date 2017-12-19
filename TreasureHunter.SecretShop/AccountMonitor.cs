using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using CefSharp;
using CefSharp.OffScreen;
using log4net;
using log4net.Repository.Hierarchy;
using TreasureHunter.Contract.AkkaMessageObject;

namespace TreasureHunter.SecretShop
{
    public class AccountMonitor : BrowserWatcherBase
    {
        public AccountMonitor(IActorRef commander) : base(commander, ConfigurationManager.AppSettings["TempFolderPath"])
        {
            if (!System.IO.Directory.Exists(ConfigurationManager.AppSettings["TempFolderPath"]))
            {
                System.IO.Directory.CreateDirectory(ConfigurationManager.AppSettings["TempFolderPath"]);
            }
            Receive<ScheduleMessage>(msg =>
            {
                RunAsync();
            });
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
            await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            string date = ConfigurationManager.AppSettings["TradeDate"]?.ToString();
            while (!(await wb.EvaluateScriptAsync(
                $"document.getElementById('UID').value = '{"killjaeden"}'")).Success)
            {
                
            }
            while (!(await wb.EvaluateScriptAsync(
                $"document.getElementById('PIN').value = '{"910306"}'")).Success)
            {
                
            }
            await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            while (!(await wb.EvaluateScriptAsync(
                $"document.getElementsByClassName('btn btn-primary block mBot-12')[0].click()")).Success)
            {
                
            }
            wb.LoadingStateChanged -= Login;
            wb.LoadingStateChanged += Dashboard;
        }

        private async void Dashboard(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;

            if (e.IsLoading || wb == null)
            {
                return;
            }
            var scriptTask = await wb.EvaluateXPathScriptAsync(@"//a[contains(., 'POSB')]", "");
            await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            Log.Info("Logged in");
            scriptTask = await wb.EvaluateXPathScriptAsync(@"//a[contains(., 'POSB')]", ".click()");
            wb.LoadingStateChanged -= Dashboard;
            wb.LoadingStateChanged += Monitor;
        }

        private async void Monitor(object s, LoadingStateChangedEventArgs e)
        {
            var wb = s as ChromiumWebBrowser;
            if (e.IsLoading || wb == null)
            {
                return;
            }
            await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            Log.Info("Enter Otp for Ibanking");
            while (true)
            {
                var scriptTask = (await wb.EvaluateScriptAsync(
                    $"javascript:getTransactionHistory('INDEX_1','0011')"));
                await wb.SavePageScreenShot(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\a.png");
            }
            var otp = WaitForInput("Enter OTP for IBanking Logging in");            
            PageAnalyzeFinished.Set();
        }

        public static Props Props(IActorRef commander)
        {
            return Akka.Actor.Props.Create(() => new AccountMonitor(commander));
        }

    }
}
