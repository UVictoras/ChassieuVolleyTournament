#region ---- Includes ---- 
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
    /// --------------------------------------------------------------------------------------
    /// Represents the main display window for the Chassieu Volleyball Tournament.
    /// Handles pool layouts, final brackets, pause screens, timers, court and ranking panels, 
    /// and live updates of team names, scores, and rankings.
    /// --------------------------------------------------------------------------------------
    public class DisplayWindow : Form
    {
        #region ---- Properties ----
        private Label timerText, timerValue;
        private PictureBox logo;

        private Panel[] matchPanels = new Panel[3];
        private Label[][] courtLabels = new Label[3][];
        private Panel[] rankingPanels = new Panel[4];
        private Label[] refereeLabels = new Label[3];

        private Panel matchAreaPanel;
        private Panel rankingAreaPanel;

        private Label pauseLabel;

        private Dictionary<string, Panel> bracketBlocks = new Dictionary<string, Panel>();

        #endregion

        #region ---- Constructor ----

        /// ------------------------------------------------
        /// Initializes a new instance of DisplayWindow.
        /// Sets window title, maximized state, background,
        /// icon, and triggers the pool layout on show.
        /// ------------------------------------------------
        public DisplayWindow()
        {
            Text = "Chassieu Volley Tournament Display";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(0, 38, 84);
            Icon = new Icon("../../Images/ChassieuLogo.ico");

            bracketBlocks = new Dictionary<string, Panel>();

            Shown += (s, e) => BeginInvoke(new Action(PoolLayout));
        }

        #endregion

        #region ---- Methods ----

        #region ---- Pool Layout ----

        /// --------------------------------------------------
        /// Creates the pool layout:
        /// - Match area with courts and upcoming matches
        /// - Timer and tournament logo
        /// - Ranking area with 4 ranking panels
        /// --------------------------------------------------
        public void PoolLayout()
        {
            Controls.Clear();

            matchAreaPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 650),
                BackColor = Color.FromArgb(10, 43, 75),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(matchAreaPanel);

            rankingAreaPanel = new Panel
            {
                Location = new Point(0, 650),
                Size = new Size(ClientSize.Width, ClientSize.Height - 650),
                BackColor = Color.FromArgb(243, 167, 18),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            Controls.Add(rankingAreaPanel);

            var df = new Font("Segoe UI", 14, FontStyle.Bold);
            var tf = new Font("Consolas", 42, FontStyle.Bold);

            // ====== TIMER ======
            timerText = new Label
            {
                Text = "TEMPS RESTANT :",
                ForeColor = Color.White,
                Font = df,
                AutoSize = true
            };
            matchAreaPanel.Controls.Add(timerText);

            timerValue = new Label
            {
                Text = "00:00",
                Font = tf,
                ForeColor = Color.Red,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D,
                AutoSize = true
            };
            matchAreaPanel.Controls.Add(timerValue);

            logo = new PictureBox
            {
                Image = Image.FromFile("../../Images/ChassieuLogo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 120)
            };
            matchAreaPanel.Controls.Add(logo);

            // ====== MATCH PANELS ET COURTS ======
            for (int i = 0; i < 3; i++)
            {
                int x = 330 * (i + 1) + 200 * i;

                // ===== PROCHAIN MATCH =====
                var mp = CreateMatchPanel(x, 200);
                matchPanels[i] = mp;
                matchAreaPanel.Controls.Add(mp);

                // ===== TITRE DU TERRAIN (centré au-dessus du panneau) =====
                Label terrainTitle = new Label
                {
                    Text = $"TERRAIN {i + 1}",
                    Font = new Font("Segoe UI", 24, FontStyle.Bold),
                    ForeColor = Color.White,
                    AutoSize = true
                };
                matchAreaPanel.Controls.Add(terrainTitle);

                // Centrage horizontal dynamique du titre au-dessus du panel
                matchAreaPanel.Layout += (s, e) =>
                {
                    terrainTitle.Location = new Point(
                        mp.Left + (mp.Width / 2) - (terrainTitle.Width / 2),
                        mp.Top - 60
                    );
                };

                // ===== TERRAIN (image) =====
                var courtPanel = CreateCourtPanel(x, 320, i);
                matchAreaPanel.Controls.Add(courtPanel);

                Label refereeLabel = new Label
                {
                    Text = $"Arbitre : Équipe {i + 4}",
                    Font = new Font("Segoe UI", 16, FontStyle.Italic),
                    ForeColor = Color.White,
                    AutoSize = true,
                    Location = new Point(courtPanel.Right + 30, courtPanel.Top + (courtPanel.Height / 2) - 20)
                };
                matchAreaPanel.Controls.Add(refereeLabel);

                refereeLabels[i] = refereeLabel;
                matchAreaPanel.Controls.Add(refereeLabel);
            }

            // ===== BOUTONS =====
            matchAreaPanel.Controls.Add(CreateStyledButton("MATCHS", 50, 300, Color.FromArgb(243, 167, 18)));
            rankingAreaPanel.Controls.Add(CreateStyledButton("CLASSEMENT", 50, 70, Color.FromArgb(10, 43, 75)));

            // ===== CLASSEMENTS =====
            string[] titles = { "POULE 1", "POULE 2", "POULE 3", "POULE VOLANTE" };
            for (int i = 0; i < titles.Length; i++)
            {
                var rp = CreateRankingPanel(144 * (i + 1) + 300 * i, 70, titles[i]);
                rankingPanels[i] = rp;
                rankingAreaPanel.Controls.Add(rp);
            }

            // ===== PLACEMENT DYNAMIQUE =====
            matchAreaPanel.Layout += (s, e) =>
            {
                timerText.Location = new Point((960 - timerText.Width / 2), 20);
                timerValue.Location = new Point((960 - timerValue.Width / 2), 60);
                logo.Location = new Point(1920 - logo.Width - 50, 20);
            };
        }


        #endregion

        #region ---- Final Layout ----

        /// -------------------------------------------------------------
        /// Displays the final 8-team bracket layout, including main and 
        /// consolation phases, finals, 3rd place, and winner panels, 
        /// with connector lines.
        /// -------------------------------------------------------------
        public void FinalLayout()
        {
            Controls.Clear();
            BackColor = Color.FromArgb(0, 38, 84);

            var title1 = new Label
            {
                Text = "PHASE PRINCIPALE",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point((ClientSize.Width - 400) / 2, 20)
            };
            Controls.Add(title1);

            var title2 = new Label
            {
                Text = "PHASE CONSOLANTE",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point((ClientSize.Width - 420) / 2, ClientSize.Height / 2 + 20)
            };
            Controls.Add(title2);

            var dividerLine = new Panel
            {
                BackColor = Color.White,
                Height = 4,
                Width = ClientSize.Width,
                Location = new Point(0, ClientSize.Height / 2),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(dividerLine);

            BuildBracket((ClientSize.Width - 1100) / 2 - 110, 100, "P"); 
            BuildBracket((ClientSize.Width - 1100) / 2 - 110, ClientSize.Height / 2 + 100, "C");

            logo = new PictureBox
            {
                Image = Image.FromFile("../../Images/ChassieuLogo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(150, 150),
                Location = new Point(ClientSize.Width - 180, 20)
            };
            Controls.Add(logo);
        }

        #endregion

            #region ---- Pause Layout ----

        /// ---------------------------------------------------
        /// Displays a pause screen with message label
        /// and tournament logo. Supports editing of the text.
        /// ---------------------------------------------------
        public void PauseLayout()
        {
            Controls.Clear();
            BackColor = Color.FromArgb(0, 38, 84);

            pauseLabel = new Label
            {
                Text = "Pause midi - Reprise à 13h30",
                Font = new Font("Segoe UI", 50, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(ClientSize.Width / 2 - 500, ClientSize.Height / 2 - 200),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(pauseLabel);

            pauseLabel.Click += (s, e) =>
            {
                using (var dlg = new TextInputDialog("Éditer", "Modifier le texte de pause :", pauseLabel.Text))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        pauseLabel.Text = dlg.InputText;
                    }
                }
            };

            logo = new PictureBox
            {
                Image = Image.FromFile("../../Images/ChassieuLogo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(500, 500),
                Location = new Point(ClientSize.Width / 2 - 250, 475)
            };
            Controls.Add(logo);
        }

        #endregion

            #region ---- Helpers ----

        /// -----------------------------------------------
        /// Creates a panel representing an upcoming match
        /// at a specific location with default text.
        /// -----------------------------------------------
        private Panel CreateMatchPanel(int x, int y)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(200, 80),
                BackColor = Color.FromArgb(0, 51, 102),
                BorderStyle = BorderStyle.FixedSingle
            };
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "PROCHAIN MATCH\nÉQUIPE 1 VS ÉQUIPE 2\n📢 ARBITRE",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            p.Controls.Add(lbl);
            return p;
        }

        /// -------------------------------------------------
        /// Creates a panel representing a volleyball court,
        /// with team labels, scores, and background image.
        /// Stores labels for later updates.
        /// -------------------------------------------------
        private Panel CreateCourtPanel(int x, int y, int idx)
        {
            var hp = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(200, 300),
                BackgroundImage = Image.FromFile("../../Images/VolleyballField.jpg"),
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 🔹 Labels plus grands et mieux centrés
            var top = CreateCourtLabel("ÉQUIPE 1", new Point(0, 30), new Size(hp.Width, 40), 14, ContentAlignment.TopCenter);
            var bot = CreateCourtLabel("ÉQUIPE 2", new Point(0, hp.Height - 80), new Size(hp.Width, 40), 14, ContentAlignment.BottomCenter);

            var scT = CreateCourtLabel("0", new Point(0, 100), new Size(hp.Width, 40), 26, ContentAlignment.MiddleCenter);
            var scB = CreateCourtLabel("0", new Point(0, 140), new Size(hp.Width, 40), 26, ContentAlignment.MiddleCenter);

            hp.Controls.AddRange(new Control[] { top, bot, scT, scB });
            courtLabels[idx] = new[] { top, bot, scT, scB };
            return hp;
        }

        /// ------------------------------------------------------
        /// Creates a label for a court panel at a given location,
        /// with default font and colors (extended parameters).
        /// ------------------------------------------------------
        private Label CreateCourtLabel(string text, Point loc, Size? size = null, float fontSize = 10, ContentAlignment align = ContentAlignment.MiddleLeft)
        {
            var lbl = new Label
            {
                Text = text,
                Location = loc,
                AutoSize = false,
                Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = align
            };

            if (size.HasValue)
                lbl.Size = size.Value;
            else
                lbl.AutoSize = true;

            return lbl;
        }


        /// -------------------------------------------------
        /// Creates a vertical styled button with a specific
        /// color and location.
        /// -------------------------------------------------
        private Button CreateStyledButton(string txt, int x, int y, Color color)
        {
            var b = new Button
            {
                Text = string.Join("\n", txt.ToCharArray()),
                Location = new Point(x, y),
                Size = new Size(50, 200),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        /// -------------------------------------------------
        /// Creates a ranking panel with a title and a table
        /// for team rank, points, and difference.
        /// -------------------------------------------------
        private Panel CreateRankingPanel(int x, int y, string title)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(300, 200),
                BackColor = Color.FromArgb(0, 51, 102),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };

            var lbl = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            p.Controls.Add(lbl);

            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            // ✅ Chaque ligne aura la même hauteur
            for (int i = 0; i < t.RowCount; i++)
            {
                t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 5f));
            }

            // En-têtes
            string[] hdr = { "RANG", "ÉQUIPE", "PTS", "DIFF" };
            foreach (var h in hdr)
                t.Controls.Add(CreateRankingCell(h, true));

            // Lignes d'équipes
            string[] names = { "ÉQUIPE 1", "ÉQUIPE 2", "ÉQUIPE 3", "ÉQUIPE 4" };
            for (int i = 0; i < names.Length; i++)
            {
                t.Controls.Add(CreateRankingCell((i + 1).ToString()));
                t.Controls.Add(CreateRankingCell(names[i]));
                t.Controls.Add(CreateRankingCell("0"));
                t.Controls.Add(CreateRankingCell("0"));
            }

            p.Controls.Add(t);
            return p;
        }


        /// --------------------------------------------
        /// Creates a cell label for the ranking table.
        /// hdr indicates whether it is a header.
        /// --------------------------------------------
        private Label CreateRankingCell(string txt, bool hdr = false) =>
            new Label
            {
                Text = txt,
                Font = new Font("Segoe UI", hdr ? 9 : 8, hdr ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

        /// --------------------------------------------------------
        /// Updates all ranking panel names using a provided array.
        /// --------------------------------------------------------
        public void UpdateRankingTeamNames(string[] teamNames)
        {
            for (int panelIndex = 0; panelIndex < rankingPanels.Length; panelIndex++)
            {
                var panel = rankingPanels[panelIndex];
                if (panel == null) continue;

                var table = panel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
                if (table == null) continue;

                for (int teamIndex = 0; teamIndex < 4; teamIndex++)
                {
                    int controlIndex = (teamIndex + 1) * 4 + 1;
                    if (controlIndex >= table.Controls.Count) continue;

                    var lbl = table.Controls[controlIndex] as Label;
                    if (lbl != null)
                    {
                        int globalTeamIndex = panelIndex * 4 + teamIndex;
                        if (globalTeamIndex < teamNames.Length)
                        {
                            lbl.Text = teamNames[globalTeamIndex];
                        }
                    }
                }
            }
        }

        /// -------------------------------------------
        /// Updates all occurrences of a team name in:
        /// - Court labels
        /// - Next match panels
        /// - Ranking tables
        /// -------------------------------------------
        public void UpdateTeamNameInMatches(string oldName, string newName)
        {
            for (int i = 0; i < courtLabels.Length; i++)
            {
                var labels = courtLabels[i];
                if (labels == null || labels.Length < 4)
                    continue;

                string t1 = labels[0].Text;
                string t2 = labels[1].Text;
                string s1 = labels[2].Text;
                string s2 = labels[3].Text;

                if (t1 == oldName) t1 = newName;
                if (t2 == oldName) t2 = newName;

                SetFieldText(i, t1, t2, s1, s2);
            }

            for (int i = 0; i < matchPanels.Length; i++)
            {
                var panel = matchPanels[i];
                if (panel == null || panel.Controls.Count == 0)
                    continue;

                if (!(panel.Controls[0] is Label matchLabel))
                    continue;

                string text = matchLabel.Text;
                var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 3)
                    continue;

                string matchLine = lines[1];
                string[] parts = matchLine.Split(new string[] { " VS " }, StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                string t1 = parts[0];
                string t2 = parts[1];
                string refName = "";

                if (t1 == oldName) t1 = newName;
                if (t2 == oldName) t2 = newName;

                SetNextMatchText(i, t1, t2, refName);
            }

            for (int panelIndex = 0; panelIndex < rankingPanels.Length; panelIndex++)
            {
                int teamsPerPanel = 4;
                int startTeamIndex = panelIndex * teamsPerPanel;

                string[] teamNames = new string[teamsPerPanel];
                string[] teamDiffs = new string[teamsPerPanel];
                string[] teamPoints = new string[teamsPerPanel];

                for (int j = 0; j < teamsPerPanel; j++)
                {
                    int teamIdx = startTeamIndex + j;
                    if (teamIdx >= Tournament.Instance.Teams.Length)
                    {
                        teamNames[j] = "";
                        teamDiffs[j] = "";
                        teamPoints[j] = "";
                        continue;
                    }

                    var team = Tournament.Instance.Teams[teamIdx];

                    teamNames[j] = (team.Name == oldName) ? newName : team.Name;
                    teamDiffs[j] = team.Statistics.Difference.ToString();
                    teamPoints[j] = team.Statistics.TournamentPoints.ToString();
                }

                SetRankingText(panelIndex, teamNames, teamDiffs, teamPoints);
            }
        }

        /// ---------------------------------------------------------
        /// Updates a specific bracket block's team names and scores.
        /// Automatically updates winner panel if applicable.
        /// ---------------------------------------------------------
        public void UpdateBracketBlock(string blockKey, string teamA, int scoreA, int scoreB, string teamB)
        {
            if (!bracketBlocks.TryGetValue(blockKey, out var blockPanel)) return;

            bool isWinner = blockPanel.BackColor == Color.Gold;

            if (isWinner)
            {
                var label = blockPanel.Controls.OfType<Label>().FirstOrDefault();
                if (label != null)
                {
                    label.Text = "🏆 " + teamA.ToUpper(); 
                }
            }
            else
            {
                var fullTextPanel = blockPanel.Controls.OfType<Panel>().FirstOrDefault();
                if (fullTextPanel != null)
                {
                    var label = fullTextPanel.Controls.OfType<Label>().FirstOrDefault();
                    if (label != null)
                    {
                        label.Text = $"{teamA} - {scoreA} / {scoreB} - {teamB}";
                    }
                }
            }
        }

        /// ---------------------------------------------------
        /// Builds a single bracket half (main or consolation)
        /// starting at a given position, with a prefix for
        /// identifying panels in the dictionary.
        /// ---------------------------------------------------
        private void BuildBracket(int startX, int startY, string prefix)
        {
            int panelW = 220, panelH = 40, hSpacing = 120, vSpacing = 60;

            var bracketContainer = new Panel
            {
                Location = new Point(startX, startY),
                Size = new Size(panelW * 4 + hSpacing * 3, 400),
                BackColor = Color.Transparent
            };
            Controls.Add(bracketContainer);

            List<Point> qCenters = new List<Point>();
            List<Point> sCenters = new List<Point>();
            Point fCenter = Point.Empty, wCenter = Point.Empty;

            for (int i = 0; i < 4; i++)
            {
                int x = i * (panelW + hSpacing);
                int y = 3 * (panelH + vSpacing);
                var panel = CreateBracketBlock("ÉQUIPE 1", 0, 0, "ÉQUIPE 2", panelW, panelH);
                panel.Location = new Point(x, y);
                bracketBlocks[$"{prefix}_QF{i + 1}"] = panel;
                bracketContainer.Controls.Add(panel);
                qCenters.Add(GetPanelCenter(panel));
            }

            for (int i = 0; i < 2; i++)
            {
                int x = i * 2 * (panelW + hSpacing) + (panelW + hSpacing) / 2;
                int y = 2 * (panelH + vSpacing);
                var panel = CreateBracketBlock("ÉQUIPE 1", 0, 0, "ÉQUIPE 2", panelW, panelH);
                panel.Location = new Point(x, y);
                bracketBlocks[$"{prefix}_SF{i + 1}"] = panel;
                bracketContainer.Controls.Add(panel);
                sCenters.Add(GetPanelCenter(panel));
            }

            int finalX = (panelW + hSpacing) + (panelW + hSpacing) / 2;
            int finalY = panelH + vSpacing;
            var final = CreateBracketBlock("ÉQUIPE 1", 0, 0, "ÉQUIPE 2", panelW, panelH);
            final.Location = new Point(finalX, finalY);
            bracketBlocks[$"{prefix}_FINAL"] = final;
            bracketContainer.Controls.Add(final);
            fCenter = GetPanelCenter(final);

            int extraX = finalX + panelW + hSpacing;
            int extraY = finalY;
            var extra = CreateBracketBlock("ÉQUIPE 1", 0, 0, "ÉQUIPE 2", panelW, panelH);
            extra.Location = new Point(extraX, extraY);
            bracketBlocks[$"{prefix}_3RD"] = extra;
            bracketContainer.Controls.Add(extra);

            var winner = CreateBracketBlock("ÉQUIPE 1", 0, 0, "ÉQUIPE 2", panelW, panelH, true);
            winner.Location = new Point(finalX, 0);
            bracketBlocks[$"{prefix}_WINNER"] = winner;
            bracketContainer.Controls.Add(winner);
            wCenter = GetPanelCenter(winner);

            bracketContainer.Paint += (s, e) =>
            {
                Pen pen = new Pen(Color.White, 2);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                e.Graphics.DrawLine(pen, qCenters[0], sCenters[0]);
                e.Graphics.DrawLine(pen, qCenters[1], sCenters[0]);
                e.Graphics.DrawLine(pen, qCenters[2], sCenters[1]);
                e.Graphics.DrawLine(pen, qCenters[3], sCenters[1]);

                e.Graphics.DrawLine(pen, sCenters[0], fCenter);
                e.Graphics.DrawLine(pen, sCenters[1], fCenter);

                e.Graphics.DrawLine(pen, fCenter, wCenter);
            };
        }

        /// ---------------------------------------------------
        /// Creates a single bracket block panel displaying
        /// team names, scores, and optionally a winner trophy.
        /// Supports editing team names via click.
        /// ---------------------------------------------------
        private Panel CreateBracketBlock(string teamA, int scoreA, int scoreB, string teamB, int w = 200, int h = 60, bool winner = false)
        {
            var panel = new Panel
            {
                Size = new Size(w, h),
                BackColor = winner ? Color.Gold : Color.FromArgb(243, 167, 18),
                BorderStyle = BorderStyle.FixedSingle
            };

            if (winner)
            {
                string currentName = "Winner";

                var winLabel = new Label
                {
                    Text = "🏆 " + currentName.ToUpper(),
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.Black,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand
                };

                winLabel.Click += (s, e) =>
                {
                    using (var dlg = new TextInputDialog("Éditer le nom du gagnant", "Modifier le nom de l'équipe gagnante :", currentName))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            currentName = dlg.InputText.Trim();
                            winLabel.Text = "🏆 " + currentName.ToUpper();
                        }
                    }
                };

                panel.Controls.Add(winLabel);
                return panel;
            }

            var fullTextPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5),
                BackColor = Color.Transparent
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = $"{teamA} - {scoreA} / {scoreB} - {teamB}",
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };

            label.Click += (s, e) =>
            {
                int clickX = ((MouseEventArgs)e).X;

                if (clickX < panel.Width / 2)
                {
                    using (var dlg = new TextInputDialog("Éditer le nom", "Modifier le nom de l'équipe A :", teamA))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            teamA = dlg.InputText.Trim();
                            label.Text = $"{teamA} - {scoreA} / {scoreB} - {teamB}";
                        }
                    }
                }
                else
                {
                    using (var dlg = new TextInputDialog("Éditer le nom", "Modifier le nom de l'équipe B :", teamB))
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            teamB = dlg.InputText.Trim();
                            label.Text = $"{teamA} - {scoreA} / {scoreB} - {teamB}";
                        }
                    }
                }
            };

            fullTextPanel.Controls.Add(label);
            panel.Controls.Add(fullTextPanel);

            return panel;
        }

        /// ----------------------------------------------
        /// Returns the center point of a panel, used for
        /// drawing connector lines in the bracket.
        /// ----------------------------------------------
        private Point GetPanelCenter(Panel panel)
        {
            return new Point(panel.Left + panel.Width / 2, panel.Top + panel.Height / 2);
        }

        /// -------------------------------------------------
        /// Update the current referee name on corresponding
        /// field.
        /// -------------------------------------------------
        public void UpdateReferee(int courtIndex, string refereeName)
        {
            // Sécurité : éviter les erreurs d'index
            if (courtIndex < 0 || courtIndex >= refereeLabels.Length)
                return;

            // Si le label existe déjà, on met simplement à jour le texte
            if (refereeLabels[courtIndex] != null)
            {
                refereeLabels[courtIndex].Text = $"Arbitre : {refereeName}";
            }
        }

        #endregion

        #endregion

        #region ---- Getters & Setters ----

        /// --------------------------------------------------
        /// Updates the court labels for a given match index.
        /// --------------------------------------------------
        public void SetFieldText(int idx, string t1, string t2, string s1, string s2)
        {
            if (idx < 0 || idx >= courtLabels.Length || courtLabels[idx] == null) return;

            courtLabels[idx][0].Text = t1;
            courtLabels[idx][1].Text = t2;
            courtLabels[idx][2].Text = s1;
            courtLabels[idx][3].Text = s2;
        }

        /// ----------------------------------------------------
        /// Updates the next match panel text at a given index.
        /// ----------------------------------------------------
        public void SetNextMatchText(int idx, string t1, string t2, string refName)
        {
            if (idx < 0 || idx >= matchPanels.Length || matchPanels[idx] == null) return;

            ((Label)matchPanels[idx].Controls[0]).Text =
                $"PROCHAIN MATCH\n{t1} VS {t2}\n📢 {refName}";
        }

        /// ------------------------------------------------------------
        /// Updates the countdown timer display in minutes and seconds.
        /// ------------------------------------------------------------
        public void SetTimerText(float seconds)
        {
            int m = (int)(seconds / 60), s = (int)(seconds % 60);
            if (timerValue != null) timerValue.Text = $"{m:D2}:{s:D2}";
        }

        /// ----------------------------------------------------
        /// Updates the ranking table for a specific panel with
        /// team names, points, and difference values.
        /// ----------------------------------------------------
        public void SetRankingText(int index, string[] TeamNames, string[] TeamDiffs, string[] TeamPoints)
        {
            if (index < 0 || index >= rankingPanels.Length) return;
            var panel = rankingPanels[index];
            if (panel == null) return;

            var table = panel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (table == null) return;

            for (int teamIndex = 0; teamIndex < 4; teamIndex++)
            {
                int baseIndex = 4 + teamIndex * 4;

                if (baseIndex + 3 >= table.Controls.Count) continue;

                if (teamIndex < TeamNames.Length)
                {
                    var nameLabel = table.Controls[baseIndex + 1] as Label;
                    if (nameLabel != null)
                        nameLabel.Text = TeamNames[teamIndex];
                }

                if (teamIndex < TeamPoints.Length)
                {
                    var pointsLabel = table.Controls[baseIndex + 2] as Label;
                    if (pointsLabel != null)
                        pointsLabel.Text = TeamPoints[teamIndex];
                }

                if (teamIndex < TeamDiffs.Length)
                {
                    var diffLabel = table.Controls[baseIndex + 3] as Label;
                    if (diffLabel != null)
                        diffLabel.Text = TeamDiffs[teamIndex];
                }
            }
        }

        public Label[][] CourtLabels => courtLabels;

        #endregion 
    }
}
