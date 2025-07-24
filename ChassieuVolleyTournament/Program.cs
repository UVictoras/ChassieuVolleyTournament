using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ChassieuVolleyTournament
{
    static class Program
    {
        static Dictionary<string, string> validKeysWithTeams;

        public static DisplayWindow window1;
        public static StaffWindow window2;

        private static Timer timer;
        private static RefereeWebServer refereeWebServer;

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

        static void SetKeysInfo()
        {
            validKeysWithTeams = new Dictionary<string, string>();

            var phase = Tournament.Instance.GetCurrentPhase() as PoolPhase;
            if (phase == null) return;

            foreach (Pool pool in phase.GetPools())
            {
                foreach (Match match in pool.Matches)
                {
                    if (!string.IsNullOrEmpty(match.Key))
                    {
                        validKeysWithTeams[match.Key] = $"{match.Team1.Name}|{match.Team2.Name}";
                    }
                }
            }
        }

        public static async Task MainLoop()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            long lastTime = stopwatch.ElapsedMilliseconds;

            timer.StartTimer(true);

            while (true)
            {
                long currentTime = stopwatch.ElapsedMilliseconds;
                float deltaTime = (currentTime - lastTime) / 1000f;
                lastTime = currentTime;

                if (!timer.GetTimerIsEnabled())
                    return;

                timer.DecrementTimer(deltaTime);

                if (Tournament.Instance.GetDisplayWindow() != null)
                {
                    Tournament.Instance.GetDisplayWindow().Invoke((MethodInvoker)delegate
                    {
                        Tournament.Instance.GetDisplayWindow().SetTimerText(timer.GetCurrentTime());
                    });
                }

                await Task.Delay(50);
            }
        }

        static async Task StartWebServer()
        {
            if (validKeysWithTeams == null || validKeysWithTeams.Count == 0)
                return;

            refereeWebServer = new RefereeWebServer(validKeysWithTeams.Keys, Tournament.Instance.GetDisplayWindow());

            foreach (var kvp in validKeysWithTeams)
            {
                refereeWebServer.SetInfoForKey(kvp.Key, kvp.Value);
            }

            await refereeWebServer.Start();
        }
    }
}
