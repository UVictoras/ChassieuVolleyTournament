#region ---- Includes ----
using System;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
    /// -------------------------------------------------------------------------------------
    /// StaffWindow is a dedicated form for tournament staff to monitor and manage:
    /// - All teams’ statistics (points scored, points conceded, tournament points, ranking)
    /// - Current and next match keys for each field
    /// - Editable fields to update team names and stats
    /// - Buttons to control timers and phases
    /// - Tree-phase matches in knockout stages
    /// -------------------------------------------------------------------------------------
    public class StaffWindow : Form
    {
        #region ---- Properties ----

        private const int TeamCount = 16;
        private const int Columns = 6;
        private readonly string[] headers = {
            "\u00c9quipe", "Points Marqu\u00e9s", "Points Pris", "Diff\u00e9rence", "Points Tournoi", "Classement"
        };
        public TextBox[,] textFields = new TextBox[TeamCount, Columns];
        private Label[] currentMatchKeyLabels = new Label[3];
        private Label[] nextMatchKeyLabels = new Label[3];
        private TableLayoutPanel mainPanel;

        #endregion

        #region ---- Constructor ----

        /// ---------------------------------------------------------
        /// Initializes the StaffWindow
        /// Sets up the basic form settings and main panel.
        /// Layout finalization is deferred until the form is shown.
        /// ---------------------------------------------------------
        public StaffWindow()
        {
            Text = "Infos priv\u00e9es du tournoi";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            Icon = new Icon("../../Images/ChassieuLogo.ico");
            BackColor = Color.FromArgb(10, 40, 80);

            mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = BackColor
            };
            Controls.Add(mainPanel);

            Shown += (s, e) => BeginInvoke(new Action(FinalizeLayout));
        }

        #endregion

        #region ---- Methods ----

        /// -------------------------------------------------------
        /// Constructs the UI:
        /// - Team table (editable names, scores, points, ranking)
        /// - Labels for current and next match keys
        /// - Buttons for timers, next phase, and pause
        /// -------------------------------------------------------
        private void FinalizeLayout()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = Columns,
                RowCount = TeamCount + 1,
                AutoScroll = true,
                Padding = new Padding(20),
                BackColor = BackColor,
                AutoSize = true
            };
            float[] colWidths = { 0.25f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f };
            colWidths.ForEach((w, i) => table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, w * 100)));

            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            for (int c = 0; c < Columns; c++)
            {
                table.Controls.Add(new Label
                {
                    Text = headers[c],
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(15, 60, 100),
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(2)
                }, c, 0);
            }

            for (int r = 0; r < TeamCount; r++)
            {
                int capturedRow = r;

                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
                for (int c = 0; c < Columns; c++)
                {
                    var tb = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Font = new Font("Segoe UI", 10),
                        TextAlign = HorizontalAlignment.Center,
                        Margin = new Padding(2),
                        BackColor = Color.White
                    };

                    if (c == 0)
                    {
                        tb.Text = $"Équipe {r + 1}";

                        string oldTeamName = tb.Text;
                        tb.Enter += (s, e) => oldTeamName = tb.Text;

                        tb.KeyDown += (s, e) =>
                        {
                            if (e.KeyCode == Keys.Enter)
                            {
                                Console.WriteLine($"DEBUG: row = {capturedRow}, team count = {Tournament.Instance.Teams.Length}");

                                string newName = tb.Text;

                                if (capturedRow >= 0 && capturedRow < Tournament.Instance.Teams.Length)
                                {
                                    string oldName = Tournament.Instance.Teams[capturedRow].Name;
                                    Tournament.Instance.Teams[capturedRow].Name = newName;

                                    Tournament.Instance.UpdateTeamNameInAllMatches(oldName, newName);
                                }

                                var display = Tournament.Instance.GetDisplayWindow();
                                display?.UpdateTeamNameInMatches(oldTeamName, newName);

                                oldTeamName = newName;
                                e.Handled = true;
                            }
                        };
                    }
                    else if (c == 5)
                    {
                        tb.Text = ((r % 4) + 1).ToString();
                    }
                    else
                    {
                        tb.Text = "0";

                        capturedRow = r;
                        int capturedCol = c;

                        if (capturedCol == 3)
                        {
                            tb.ReadOnly = true;
                            tb.BackColor = Color.LightGray;
                        }
                        else
                        {
                            tb.TextChanged += (s, e) =>
                            {
                                var team = Tournament.Instance.Teams[capturedRow];
                                string newText = tb.Text.Trim();

                                switch (capturedCol)
                                {
                                    case 0:
                                        string oldName = team.Name;
                                        if (newText != oldName)
                                        {
                                            team.Name = newText;

                                            var display = Tournament.Instance.GetDisplayWindow();
                                            display?.UpdateTeamNameInMatches(oldName, newText);
                                        }
                                        break;

                                    case 1:
                                        if (!int.TryParse(newText, out int scored)) scored = 0;
                                        team.Statistics.ScoredPoints = scored;
                                        break;

                                    case 2:
                                        if (!int.TryParse(newText, out int taken)) taken = 0;
                                        team.Statistics.TakenPoints = taken;
                                        break;

                                    case 4:
                                        if (!int.TryParse(newText, out int points)) points = 0;
                                        team.Statistics.TournamentPoints = points;
                                        break;
                                }

                                team.Statistics.Difference = team.Statistics.ScoredPoints - team.Statistics.TakenPoints;
                                textFields[capturedRow, 3].Text = team.Statistics.Difference.ToString();

                                var displayRefresh = Tournament.Instance.GetDisplayWindow();
                                displayRefresh?.Update();
                            };

                            tb.KeyDown += (s, e) =>
                            {
                                if (e.KeyCode == Keys.Enter)
                                {
                                    Tournament.Instance.RefreshAllPoolsRanking();
                                    e.SuppressKeyPress = true;
                                }
                            };
                        }
                    }

                    textFields[r, c] = tb;
                    table.Controls.Add(tb, c, r + 1);
                }
            }

            mainPanel.Controls.Add(table);

            var keyTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,     
                RowCount = 2,
                ColumnCount = 3,
                Padding = new Padding(20),
                BackColor = BackColor
            };
            keyTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            keyTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            for (int i = 0; i < 3; i++)
            {
                keyTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
                var curr = new Label
                {
                    Text = $"Clef Terrain Actuel {i + 1}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.LightYellow,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(5)
                };
                currentMatchKeyLabels[i] = curr;
                keyTable.Controls.Add(curr, i, 0);

                var nxt = new Label
                {
                    Text = $"Clef Match Suivant {i + 1}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.LightBlue,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(5)
                };
                nextMatchKeyLabels[i] = nxt;
                keyTable.Controls.Add(nxt, i, 1);
            }

            mainPanel.Controls.Add(keyTable);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Padding = new Padding(20),
                WrapContents = false,
                Anchor = AnchorStyles.None
            };

            buttonPanel.Controls.Add(CreateActionButton("Timer Échauffement", Color.Orange, (s, e) =>
            {
                Tournament.Instance.StartTimer(false);
            }));

            buttonPanel.Controls.Add(CreateActionButton("Timer Match", Color.Green, (s, e) =>
            {
                Tournament.Instance.StartTimer(true);
            }));

            buttonPanel.Controls.Add(CreateActionButton("Prochaine Phase", Color.Black, (s, e) =>
            {
                Tournament.Instance.RaiseEndPhase();
            }));

            buttonPanel.Controls.Add(CreateActionButton("Lancer la Pause", Color.Purple, (s, e) =>
            {
                Tournament.Instance.GetDisplayWindow().PauseLayout();
                Tournament.Instance.GetDisplayWindow().Update();
            }));

            mainPanel.Controls.Add(buttonPanel);
        }

        /// ----------------------------------------------------------------------------
        /// Rebuilds the UI specifically for the knockout/tree phase of the tournament.
        /// Clears the previous layout and creates a structured view of:
        /// - Quarter-finals
        /// - Semi-finals
        /// - Finals
        /// - Third place matches
        /// ----------------------------------------------------------------------------
        public void RebuildForTreePhase()
        {
            var treePhase = Tournament.Instance.GetCurrentPhase() as TreePhase;
            if (treePhase == null) return;

            mainPanel.Controls.Clear();

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20),
                ColumnCount = 4,
                RowCount = 10, 
                BackColor = BackColor,
                AutoSize = true
            };

            string[] roundLabels = { "Quarts de finale", "Demi-finales", "Finale", "Match 3ème place" };
            for (int i = 0; i < roundLabels.Length; i++)
            {
                var lbl = new Label
                {
                    Text = roundLabels[i],
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(15, 60, 100),
                    Margin = new Padding(5)
                };
                table.Controls.Add(lbl, i, 0);
            }

            int row = 1;

            for (int i = 0; i < 4; i++)
            {
                table.Controls.Add(CreateMatchPanel("P_QF" + (i + 1), treePhase), 0, row++);
                table.Controls.Add(CreateMatchPanel("C_QF" + (i + 1), treePhase), 0, row++);
            }

            row = 1;
            for (int i = 0; i < 2; i++)
            {
                table.Controls.Add(CreateMatchPanel("P_SF" + (i + 1), treePhase), 1, row++);
                table.Controls.Add(CreateMatchPanel("C_SF" + (i + 1), treePhase), 1, row++);
            }

            table.Controls.Add(CreateMatchPanel("P_FINAL", treePhase), 2, 1);
            table.Controls.Add(CreateMatchPanel("C_FINAL", treePhase), 2, 2);

            table.Controls.Add(CreateMatchPanel("P_3RD", treePhase), 3, 1);
            table.Controls.Add(CreateMatchPanel("C_3RD", treePhase), 3, 2);

            mainPanel.Controls.Add(table);
        }

        /// -------------------------------------------------------------------------------
        /// Creates a panel displaying a single match (team1 vs team2) for the tree phase.
        /// Includes editable textboxes for team names and a label for the match key.
        /// Updates the main display when names are modified.
        /// 
        /// matchKey : The match identifier (e.g., "P_QF1")
        /// treePhase : Reference to the TreePhase instance
        /// --------------------------------------------------------------------------------
        private Panel CreateMatchPanel(string matchKey, TreePhase treePhase)
        {
            var matchPanel = new Panel
            {
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = true,
                Padding = new Padding(5),
                Margin = new Padding(5),
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            var match = treePhase.GetMatchByKey(matchKey);
            if (match == null) return matchPanel;

            var txtTeam1 = new TextBox
            {
                Text = match.Team1?.Name ?? "???",
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(2)
            };

            var txtTeam2 = new TextBox
            {
                Text = match.Team2?.Name ?? "???",
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Margin = new Padding(2)
            };

            var lblKey = new Label
            {
                Text = match.GetMatchKey(),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(2)
            };

            Action updateDisplay = () =>
            {
                match.Team1.Name = txtTeam1.Text;
                match.Team2.Name = txtTeam2.Text;

                Tournament.Instance.GetDisplayWindow()
                    ?.UpdateBracketBlock(
                        matchKey,
                        match.Team1.Name,
                        match.ScoreTeam1,
                        match.ScoreTeam2,
                        match.Team2.Name
                    );
            };

            txtTeam1.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    updateDisplay();
                    e.Handled = true;
                    e.SuppressKeyPress = true; 
                }
            };

            txtTeam2.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    updateDisplay();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            var flow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            flow.Controls.Add(lblKey);
            flow.Controls.Add(txtTeam1);
            flow.Controls.Add(new Label { Text = "vs", AutoSize = true, Margin = new Padding(5, 2, 5, 2) });
            flow.Controls.Add(txtTeam2);

            matchPanel.Controls.Add(flow);
            return matchPanel;
        }

        /// --------------------------------------------------------------
        /// Helper to create a styled action button for the staff window.
        /// --------------------------------------------------------------
        private Button CreateActionButton(string text, Color backColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(10),
                Margin = new Padding(10),
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        /// -----------------------------------------------------------------------
        /// Updates the labels for the current and next match keys for each field.
        /// -----------------------------------------------------------------------
        public void UpdateKeysLabels(string[] NewCurrentMatchsKeys, string[] NewNextMatchsKeys)
        {
            for (int i = 0; i < 3; i++)
            {
                currentMatchKeyLabels[i].Text = $"Clef Terrain Actuel {i + 1} : {NewCurrentMatchsKeys[i]}";
                nextMatchKeyLabels[i].Text = $"Clef Match Suivant {i + 1} : {NewNextMatchsKeys[i]}";
            }
        }

        #endregion

        #region ---- Getters & Setters ----

        /// --------------------------------------------------
        /// Update the data for a specific team in the table.
        /// --------------------------------------------------
        public void SetTeamData(int row, string name,
            int scored, int taken, int diff, int points, int rank)
        {
            if (row < 0 || row >= TeamCount) return;
            textFields[row, 0].Text = name;
            textFields[row, 1].Text = scored.ToString();
            textFields[row, 2].Text = taken.ToString();
            textFields[row, 3].Text = diff.ToString();
            textFields[row, 4].Text = points.ToString();
            textFields[row, 5].Text = rank.ToString();
        }

        /// -----------------------------------------------------------------
        /// Updates the label for the current match key of a specific field.
        /// -----------------------------------------------------------------
        public void SetCurrentMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= currentMatchKeyLabels.Length) return;
            currentMatchKeyLabels[idx].Text = $"Cl\u00e9 Terrain Actuel {idx + 1}: {key}";
        }

        /// --------------------------------------------------------------
        /// Updates the label for the next match key of a specific field.
        /// --------------------------------------------------------------

        public void SetNextMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= nextMatchKeyLabels.Length) return;
            nextMatchKeyLabels[idx].Text = $"Cl\u00e9 Match Suivant {idx + 1}: {key}";
        }

        /// --------------------------------------------
        /// Update the names of all teams in the table.
        /// --------------------------------------------
        public void SetTeamNames(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                textFields[i, 0].Text = names[i];
        }
        
        /// -----------------------------------------------------------------------------------
        /// Updates ranking-related columns for a specific pool of teams.
        /// Updates both text (names, differences, points) and numerical stats (scored/taken).
        /// -----------------------------------------------------------------------------------
        public void SetRankingText(int poolIndex, string[] teamNames, string[] teamDiffs, string[] teamPoints, int[] scoredPoints, int[] takenPoints)
        {
            int teamsPerPool = 4;
            int startRow = poolIndex * teamsPerPool;

            for (int i = 0; i < teamsPerPool; i++)
            {
                int row = startRow + i;
                textFields[row, 0].Text = teamNames[i];
                textFields[row, 1].Text = scoredPoints[i].ToString();
                textFields[row, 2].Text = takenPoints[i].ToString();
                textFields[row, 3].Text = teamDiffs[i];
                textFields[row, 4].Text = teamPoints[i];
            }
        }
        #endregion

    }

    #region ---- Helper ----

    /// -----------------------------------------------------------
    /// Extension method to iterate over arrays with index access.
    /// -----------------------------------------------------------
    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] arr, Action<T, int> action)
        {
            for (int i = 0; i < arr.Length; i++) action(arr[i], i);
        }
    }

    #endregion
}