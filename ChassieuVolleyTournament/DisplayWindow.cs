using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    /// --------------------------------------------------------------------------------------
    /// Public display of the Chassieu Volleyball Tournament (projector / TV).
    /// Pool phases: header + 3 courts + 4 rankings. Final phase: both brackets.
    /// Pause: logo and a message.
    ///
    /// Everything is drawn proportionally to the window size (any resolution works), and
    /// the data is kept in this class, so rebuilding a layout never loses anything.
    /// --------------------------------------------------------------------------------------
    public class DisplayWindow : Form
    {
        #region ---- Properties ----

        private enum Mode { None, Pool, Final, Pause }

        private Mode mode = Mode.None;
        private GradientPanel root;

        private HeaderBar header;
        private readonly CourtCard[] courts = new CourtCard[3];
        private readonly RankingCard[] rankings = new RankingCard[4];
        private BracketView bracket;
        private PauseView pauseView;

        // ---- data (survives layout changes) ----
        private readonly CourtData[] courtData = { new CourtData(), new CourtData(), new CourtData() };
        private readonly RankData[] rankData = { new RankData(), new RankData(), new RankData(), new RankData() };
        private readonly Dictionary<string, BracketMatch> bracketData = new Dictionary<string, BracketMatch>();
        private readonly Dictionary<string, string> champions = new Dictionary<string, string>();

        private string timerText = "00:00";
        private string timerMode = "";
        private bool timerCritical;
        private string phaseTitle = "";
        private string pauseText = "Pause midi - Reprise à 13h30";

        private static readonly string[] MorningTitles = { "POULE 1", "POULE 2", "POULE 3", "POULE VOLANTE" };
        private static readonly string[] LevelTitles = { "NIVEAU 1", "NIVEAU 2", "NIVEAU 3", "NIVEAU 4" };

        #endregion

        #region ---- Constructor ----

        public DisplayWindow()
        {
            Text = "Tournoi de Chassieu Volley - Affichage";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.NavyBottom;
            DoubleBuffered = true;

            Icon icon = AppPaths.AppIcon;
            if (icon != null) Icon = icon;

            for (int i = 0; i < rankData.Length; i++)
                rankData[i].Title = MorningTitles[i];

            Shown += (s, e) => BeginInvoke(new Action(PoolLayout));
        }

        #endregion

        #region ---- Layouts ----

        /// <summary>Removes and disposes the current layout.</summary>
        private void ResetRoot()
        {
            SuspendLayout();

            mode = Mode.None;     // no layout work while the old controls are being removed

            if (root != null)
            {
                root.SizeChanged -= OnRootSizeChanged;
                Controls.Remove(root);
                root.Dispose();      // disposes the children; shared images are not owned by them
            }

            root = new GradientPanel { Dock = DockStyle.Fill };
            root.SizeChanged += OnRootSizeChanged;
            Controls.Add(root);

            header = null;
            bracket = null;
            pauseView = null;
            for (int i = 0; i < courts.Length; i++) courts[i] = null;
            for (int i = 0; i < rankings.Length; i++) rankings[i] = null;

            ResumeLayout();
        }

        private void OnRootSizeChanged(object sender, EventArgs e)
        {
            ApplyLayout();
        }

        private HeaderBar CreateHeader()
        {
            var h = new HeaderBar { Logo = AppPaths.Logo };
            root.Controls.Add(h);
            UpdateHeader(h);
            return h;
        }

        private void UpdateHeader(HeaderBar h)
        {
            h.Subtitle = phaseTitle;
            h.TimerText = timerText;
            h.TimerMode = timerMode;
            h.TimerCritical = timerCritical;
            h.Invalidate();
        }

        /// -----------------------------------------------------
        /// Pool phases: header, 3 courts, 4 ranking tables.
        /// -----------------------------------------------------
        public void PoolLayout()
        {
            ResetRoot();

            header = CreateHeader();

            for (int i = 0; i < 3; i++)
            {
                courts[i] = new CourtCard { Number = i + 1, Data = courtData[i] };
                root.Controls.Add(courts[i]);
            }

            for (int i = 0; i < 4; i++)
            {
                rankings[i] = new RankingCard { Data = rankData[i] };
                root.Controls.Add(rankings[i]);
            }

            mode = Mode.Pool;      // all controls exist now: resizing may lay them out
            ApplyLayout();
        }

        /// -----------------------------------------------------
        /// Final phase: principal and consolation brackets.
        /// -----------------------------------------------------
        public void FinalLayout()
        {
            ResetRoot();

            header = CreateHeader();

            bracket = new BracketView { Matches = bracketData, Champions = champions };
            bracket.ChampionClicked += OnChampionClicked;
            root.Controls.Add(bracket);

            mode = Mode.Final;
            ApplyLayout();
        }

        /// -----------------------------------------------------
        /// Pause screen: click the text to change it.
        /// -----------------------------------------------------
        public void PauseLayout()
        {
            ResetRoot();

            pauseView = new PauseView { Logo = AppPaths.Logo, Message = pauseText };
            pauseView.Clicked += () =>
            {
                using (var dlg = new TextInputDialog("Éditer", "Modifier le texte de pause :", pauseText))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.InputText))
                    {
                        pauseText = dlg.InputText.Trim();
                        pauseView.Message = pauseText;
                        pauseView.Invalidate();
                    }
                }
            };
            root.Controls.Add(pauseView);

            mode = Mode.Pause;
            ApplyLayout();
        }

        /// -------------------------------------------------------------
        /// Places the controls according to the current window size.
        /// -------------------------------------------------------------
        private void ApplyLayout()
        {
            if (root == null || mode == Mode.None) return;

            int W = root.ClientSize.Width;
            int H = root.ClientSize.Height;
            if (W < 100 || H < 100) return;

            int pad = Math.Max(12, W / 100);
            int gap = Math.Max(12, W / 100);
            int accent = Math.Max(3, H / 220);

            switch (mode)
            {
                case Mode.Pool:
                {
                    int headerH = (int)(H * 0.115);
                    int top = accent + pad / 2;
                    int courtsH = (int)(H * 0.505);
                    int ranksTop = top + headerH + gap + courtsH + gap;
                    int ranksH = H - ranksTop - pad;

                    header.SetBounds(pad, top, W - 2 * pad, headerH);

                    int cw = (W - 2 * pad - 2 * gap) / 3;
                    for (int i = 0; i < 3; i++)
                        courts[i].SetBounds(pad + i * (cw + gap), top + headerH + gap, cw, courtsH);

                    int rw = (W - 2 * pad - 3 * gap) / 4;
                    for (int i = 0; i < 4; i++)
                        rankings[i].SetBounds(pad + i * (rw + gap), ranksTop, rw, ranksH);
                    break;
                }

                case Mode.Final:
                {
                    int headerH = (int)(H * 0.105);
                    int top = accent + pad / 2;

                    header.SetBounds(pad, top, W - 2 * pad, headerH);
                    bracket.SetBounds(pad, top + headerH + gap, W - 2 * pad, H - top - headerH - gap - pad);
                    break;
                }

                case Mode.Pause:
                    pauseView.SetBounds(0, 0, W, H);
                    break;
            }
        }

        private void OnChampionClicked(string prefix)
        {
            string current;
            champions.TryGetValue(prefix, out current);

            using (var dlg = new TextInputDialog("Éditer le vainqueur", "Nom de l'équipe gagnante :", current ?? ""))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    champions[prefix] = dlg.InputText.Trim();
                    bracket?.Invalidate();
                }
            }
        }

        #endregion

        #region ---- Updates from the tournament ----

        /// <summary>Teams and score shown on a court.</summary>
        public void SetFieldText(int idx, string t1, string t2, string s1, string s2)
        {
            if (idx < 0 || idx >= courtData.Length) return;

            CourtData d = courtData[idx];
            if (d.Team1 == t1 && d.Team2 == t2 && d.Score1 == s1 && d.Score2 == s2) return;

            d.Team1 = t1;
            d.Team2 = t2;
            d.Score1 = s1;
            d.Score2 = s2;
            courts[idx]?.Invalidate();
        }

        /// <summary>Referee of the match played on a court.</summary>
        public void UpdateReferee(int courtIndex, string refereeName)
        {
            if (courtIndex < 0 || courtIndex >= courtData.Length) return;

            string value = refereeName ?? "-";
            if (courtData[courtIndex].Referee == value) return;

            courtData[courtIndex].Referee = value;
            courts[courtIndex]?.Invalidate();
        }

        /// <summary>The "next match" block of a court.</summary>
        public void SetNextMatchText(int idx, string t1, string t2, string refName)
        {
            if (idx < 0 || idx >= courtData.Length) return;

            CourtData d = courtData[idx];
            d.NextTeam1 = t1;
            d.NextTeam2 = t2;
            d.NextReferee = refName;
            courts[idx]?.Invalidate();
        }

        /// <summary>Countdown, shown as MM:SS.</summary>
        public void SetTimerText(float seconds)
        {
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            string text = (total / 60).ToString("D2") + ":" + (total % 60).ToString("D2");
            bool critical = timerMode != "" && total <= 60;

            if (text == timerText && critical == timerCritical) return;

            timerText = text;
            timerCritical = critical;
            if (header != null) UpdateHeader(header);
        }

        /// <summary>"MATCH", "ÉCHAUFFEMENT" or "" when no timer is running.</summary>
        public void SetTimerMode(string modeText)
        {
            modeText = modeText ?? "";
            if (modeText == timerMode) return;

            timerMode = modeText;
            if (modeText == "") timerCritical = false;
            if (header != null) UpdateHeader(header);
        }

        /// <summary>Subtitle of the header, e.g. "POULES DU MATIN · TOUR 3/8".</summary>
        public void SetPhaseTitle(string title)
        {
            title = title ?? "";
            if (title == phaseTitle) return;

            phaseTitle = title;
            if (header != null) UpdateHeader(header);
        }

        /// <summary>Titles of the four ranking tables (morning pools, or level pools).</summary>
        public void SetPoolTitles(bool levelPhase)
        {
            string[] titles = levelPhase ? LevelTitles : MorningTitles;

            for (int i = 0; i < rankData.Length; i++)
            {
                if (rankData[i].Title == titles[i]) continue;

                rankData[i].Title = titles[i];
                rankings[i]?.Invalidate();
            }
        }

        /// <summary>Ranking of one pool (rows in ranking order).</summary>
        public void SetRankingText(int index, string[] TeamNames, string[] TeamDiffs, string[] TeamPoints)
        {
            if (index < 0 || index >= rankData.Length) return;

            RankData d = rankData[index];
            d.Names = (string[])TeamNames.Clone();
            d.Diffs = (string[])TeamDiffs.Clone();
            d.Points = (string[])TeamPoints.Clone();
            rankings[index]?.Invalidate();
        }

        /// <summary>Sets all team names (16, pool by pool).</summary>
        public void UpdateRankingTeamNames(string[] teamNames)
        {
            for (int p = 0; p < rankData.Length; p++)
            {
                for (int t = 0; t < 4; t++)
                {
                    int global = p * 4 + t;
                    if (global < teamNames.Length) rankData[p].Names[t] = teamNames[global];
                }
                rankings[p]?.Invalidate();
            }
        }

        public void UpdateTeamNameInMatches(string oldName, string newName)
        {
            Tournament.Instance.RefreshAll();
        }

        /// ---------------------------------------------------------
        /// Updates one bracket match (names and scores). The special
        /// slots "P_WINNER" / "C_WINNER" set the winner box.
        /// ---------------------------------------------------------
        public void UpdateBracketBlock(string blockKey, string teamA, int scoreA, int scoreB, string teamB)
        {
            if (string.IsNullOrEmpty(blockKey)) return;

            if (blockKey.EndsWith("_WINNER"))
            {
                champions[blockKey.Substring(0, 1)] = teamA;
            }
            else
            {
                BracketMatch m;
                if (!bracketData.TryGetValue(blockKey, out m))
                {
                    m = new BracketMatch();
                    bracketData[blockKey] = m;
                }

                m.TeamA = teamA;
                m.TeamB = teamB;
                m.ScoreA = scoreA;
                m.ScoreB = scoreB;
            }

            bracket?.Invalidate();
        }

        #endregion
    }
}
