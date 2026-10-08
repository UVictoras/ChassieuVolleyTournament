using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    /// ------------------------------------------------------
    /// Entry point of the application.
    /// Creates the two windows, the UI timer, the referee web
    /// server and the Internet tunnel (so referees can connect
    /// from any network).
    /// ------------------------------------------------------
    static class Program
    {
        public static DisplayWindow window1;
        public static StaffWindow window2;

        private static System.Windows.Forms.Timer uiTimer;
        private static RefereeWebServer webServer;
        private static TunnelManager tunnel;

        private static string tunnelMessage = "";
        private static string publicUrl;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var mutex = new Mutex(true, @"Local\ChassieuVolleyTournament", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("L'application est déjà ouverte.", "Tournoi de Chassieu Volley",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => ReportError(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportError(e.ExceptionObject as Exception);

                try
                {
                    Run();
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                }
                finally
                {
                    Shutdown();
                }
            }
        }

        private static void Run()
        {
            window1 = new DisplayWindow();
            window2 = new StaffWindow();

            Tournament.Instance.SetWindows(window1, window2);

            // Closing either window ends the application (it used to keep running invisibly).
            window1.FormClosed += (s, e) => Application.Exit();
            window2.FormClosed += (s, e) => Application.Exit();

            window1.Show();
            window2.Show();

            // Build the staff table before the tournament pushes data into it.
            window2.EnsureLayout();

            Tournament.Instance.InitializeMorningPhase();

            // Countdown: ticks on the UI thread, so no cross-thread access to the controls.
            uiTimer = new System.Windows.Forms.Timer { Interval = 100 };
            uiTimer.Tick += (s, e) =>
            {
                try { Tournament.Instance.CycleTimer(); }
                catch (Exception ex) { AppPaths.Log("Timer : " + ex); }
            };
            uiTimer.Start();

            StartWeb();

            Application.Run();
        }

        /// -----------------------------------------------------
        /// Starts the web server, then the Internet tunnel.
        /// -----------------------------------------------------
        private static void StartWeb()
        {
            webServer = new RefereeWebServer(window2);

            if (!webServer.Start(8080))
            {
                window2.SetWebInfo("Serveur web impossible à démarrer (voir log.txt dans " + AppPaths.DataDir + ")", null, null);
                return;
            }

            tunnel = new TunnelManager();
            tunnel.StateChanged += (state, message) => OnUi(() => { tunnelMessage = message; UpdateWebInfo(); });
            tunnel.UrlChanged += url => OnUi(() => { publicUrl = url; UpdateWebInfo(); });

            UpdateWebInfo();
            tunnel.Start(webServer.Port);
        }

        private static void UpdateWebInfo()
        {
            if (webServer == null || window2 == null || window2.IsDisposed) return;

            string local = null;
            if (webServer.AcceptsNetworkClients)
            {
                var addresses = RefereeWebServer.GetLocalAddresses();
                if (addresses.Count > 0)
                    local = string.Join("   ", addresses.Select(a => "http://" + a + ":" + webServer.Port + "/"));
            }
            else
            {
                local = "http://localhost:" + webServer.Port + "/   (ce PC uniquement)";
            }

            string status = "Internet : " + (string.IsNullOrEmpty(tunnelMessage) ? "..." : tunnelMessage);
            if (!webServer.AcceptsNetworkClients)
                status += "   |   R\u00e9seau local : indisponible (lancez l'application en administrateur pour l'activer)";

            window2.SetWebInfo(status, publicUrl, local);
        }

        private static void OnUi(Action action)
        {
            try
            {
                if (window2 != null && !window2.IsDisposed && window2.IsHandleCreated)
                    window2.BeginInvoke(action);
            }
            catch (Exception ex)
            {
                AppPaths.Log("OnUi : " + ex.Message);
            }
        }

        private static void Shutdown()
        {
            try { uiTimer?.Stop(); } catch { }
            try { webServer?.Stop(); } catch { }
            try { tunnel?.Dispose(); } catch { }   // also kills cloudflared
        }

        private static void ReportError(Exception ex)
        {
            if (ex == null) return;

            AppPaths.Log("Erreur : " + ex);

            try
            {
                MessageBox.Show("Une erreur est survenue :\n\n" + ex.Message +
                    "\n\nDétails dans " + AppPaths.DataDir + "\\log.txt",
                    "Tournoi de Chassieu Volley", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
