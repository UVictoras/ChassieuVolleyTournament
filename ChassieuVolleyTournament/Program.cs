#region ---- Includes ---- 
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

#endregion

namespace ChassieuVolleyTournament
{
    /// ------------------------------------------------------
    /// Entry point of the application.
    /// Initializes windows, timer, and referee web server.
    /// Also manages the application main loop and data keys.
    /// ------------------------------------------------------
    static class Program
    {
        #region ---- Properties ----
        static Dictionary<string, Team[]> validKeysWithTeams;

        public static DisplayWindow window1;
        public static StaffWindow window2;

        private static Timer timer;
        private static RefereeWebServer refereeWebServer;

        #endregion

        #region ---- Methods & Tasks ----
        /// --------------------------------------------------
        /// Main method — initializes the application,
        /// sets up windows, starts web server and main loop.
        /// --------------------------------------------------
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            window1 = new DisplayWindow();
            window2 = new StaffWindow();
            timer = new Timer();

            Tournament.Instance.SetWindows(window1, window2);

            window1.Show();
            window2.Show();

            EventHandler onIdle = null;
            onIdle = (s, e) =>
            {
                Application.Idle -= onIdle;
                Tournament.Instance.InitializeMorningPhase();

                SetKeysInfo();

                Task.Run(() => StartWebServer());
            };
            Application.Idle += onIdle;

            window1.Refresh();
            window2.Refresh();

            Task.Run(() => MainLoop());

            Application.Run();
        }

        /// -------------------------------------------------------
        /// Scans current tournament phase and collects match keys
        /// with their corresponding team pairs.
        /// -------------------------------------------------------
        public static void SetKeysInfo()
        {
            validKeysWithTeams = new Dictionary<string, Team[]>();

            var phase = Tournament.Instance.GetCurrentPhase() as PoolPhase;
            if (phase == null) return;

            foreach (Pool pool in phase.GetPools())
            {
                foreach (Match match in pool.Matches)
                {
                    if (!string.IsNullOrEmpty(match.Key))
                    {
                        validKeysWithTeams[match.Key] = new Team[] { match.Team1, match.Team2 };
                    }
                }
            }
        }

        /// ----------------------------------------------------
        /// Sends formatted team name strings to the web server
        /// for each valid key.
        /// ----------------------------------------------------
        public static void GiveWebServerKeysInfo()
        {
            foreach (var kvp in validKeysWithTeams)
            {
                refereeWebServer.SetInfoForKey(kvp.Key, $"{kvp.Value[0].Name}|{kvp.Value[1].Name}");
            }
        }

        /// ----------------------------------------------
        /// Sends the dictionary of valid keys and teams
        /// directly to the web server.
        /// ----------------------------------------------
        public static void GiveWebServerKeys()
        {
            refereeWebServer.AddValidKeys(validKeysWithTeams);
        }

        /// -------------------------------------------------------
        /// Main tournament loop — continuously updates tournament
        /// timer using a Stopwatch, with small async delay.
        /// -------------------------------------------------------
        public static async Task MainLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                Tournament.Instance.CycleTimer(stopwatch);
                await Task.Delay(50);
            }
        }

        /// ---------------------------------------------------------
        /// Starts the referee web server if valid keys are present,
        /// and provides it with necessary match/team info.
        /// ---------------------------------------------------------
        static async Task StartWebServer()
        {
            if (validKeysWithTeams == null || validKeysWithTeams.Count == 0)
                return;

            refereeWebServer = new RefereeWebServer(validKeysWithTeams.Keys, Tournament.Instance.GetDisplayWindow());

            GiveWebServerKeys();

            await refereeWebServer.Start();
        }

        #endregion
    }
}
