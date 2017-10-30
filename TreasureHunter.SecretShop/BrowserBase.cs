using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using log4net;
using log4net.Repository.Hierarchy;
using Akka.Actor;
using TreasureHunter.Contract;
namespace TreasureHunter.SecretShop
{
    public abstract class BrowserWatcherBase : InputActor
    {
        #region Thread Share Fields
        protected volatile bool IsDownloadCompleted = false;
        protected volatile string DownloadFileName = "";
        protected AutoResetEvent PageAnalyzeFinished = new AutoResetEvent(false);
        protected readonly string TempFolderPath;

        #endregion
        private static readonly ILog Log = LogManager.GetLogger(typeof(BrowserWatcherBase));
        protected string DefaultCookiePath = ConfigurationManager.AppSettings["CookiePath"];
        public static void InitializeEnvironment()
        {
            if (!Cef.IsInitialized)
            {
                var settings = new CefSettings
                {
                    IgnoreCertificateErrors = true,
                    LogFile = "./log/cefLog.log",
                    BrowserSubprocessPath = "./CefSharp.BrowserSubprocess.exe"
            };
                //bug fix: theice.com SSL Certificate expired
                Cef.Initialize(settings);

            }
        }
        public static void CleanupEnvironment()
        {
            Cef.Shutdown();
        }

        protected abstract Task StartBrowser();
        public Task RunAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(StartBrowser());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public void Run()
        {
            RunAsync().Wait();
        }
        protected CefSharp.Cookie ConvertCookie(System.Net.Cookie cookie)
        {
            var c = new CefSharp.Cookie
            {
                Creation = cookie.TimeStamp,
                Domain = cookie.Domain,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly,
                Name = cookie.Name,
                Path = cookie.Path,
                Secure = cookie.Secure,
                Value = cookie.Value
            };
            return c;
        }

        protected string GetCookieHeader(HttpWebResponse response)
        {
            return response.Headers.Get("Set-Cookie");
        }

        protected async Task<bool> IsPageLoading(ChromiumWebBrowser wb, LoadingStateChangedEventArgs e)
        {
            if (e.IsLoading) return true;
            var html = await wb.GetSourceAsync();
            if (html == "<html><head></head><body></body></html>") return true;
            return false;
        }
        protected void WriteCookiesToDisk(string file, string cookieJar)
        {
            if (string.IsNullOrEmpty(file)) file = DefaultCookiePath;
            try
            {
                Log.Info("Writing cookies to disk... ");
                if (!File.Exists(file))
                {
                    File.WriteAllText(file, cookieJar);
                }
                Log.Info("Done.");
            }
            catch (Exception e)
            {
                Log.Warn("Problem writing cookies to disk: " + e.GetType());
            }
        }

        protected string ReadCookiesFromDisk(string file)
        {
            if (string.IsNullOrEmpty(file)) file = DefaultCookiePath;
            if (!File.Exists(file))
            {
                Log.Info("SSO Cookie does not exist, ask for OTP");
                return null;
            }
            try
            {
                return System.IO.File.ReadAllLines(file)[0];
            }
            catch (Exception ex)
            {
                Log.Error("Problem reading cookies from disk: ", ex);
                return null;
            }
        }

        protected BrowserWatcherBase(IActorRef commander, string temp) :
            base(commander)
        {
            TempFolderPath = temp;
        }
    }

    public static class BrowserWatcherHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BrowserWatcherHelper));
        public static async Task<object> SavePageScreenShot(this ChromiumWebBrowser wb, string path)
        {
            var task = await wb.ScreenshotAsync();
            Log.Info($"Screenshot ready. Saving to {path}");

            // Save the Bitmap to the path.
            // The image type is auto-detected via the ".png" extension.
            task.Save(path);

            // We no longer need the Bitmap.
            // Dispose it to avoid keeping the memory alive.  Especially important in 32-bit applications.
            task.Dispose();
#if DEBUG
// Tell Windows to launch the saved image.
            Log.Info("Screenshot saved.  Launching your default image viewer...");
            System.Diagnostics.Process.Start(path);
#endif
            return Task.FromResult<object>(null);
        }

        public static Task<JavascriptResponse> EvaluateXPathScriptAsync(this ChromiumWebBrowser wb, string xpath, string action)
        {
            return wb.EvaluateScriptAsync(
                $"document.evaluate(\"{xpath}\", document, null, XPathResult.ANY_TYPE, null ).iterateNext(){action}");
        }
    }
}
