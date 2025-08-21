#region ---- Includes ----
using System;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
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

        private void FinalizeLayout()
        {
            // === TABLE: Team Data ===
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

                                    // ✅ Update matches where this name appears
                                    Tournament.Instance.UpdateTeamNameInAllMatches(oldName, newName);
                                }

                                // Optional: update display window too
                                var display = Tournament.Instance.GetDisplayWindow();
                                display?.UpdateTeamNameInMatches(oldTeamName, newName);

                                oldTeamName = newName;
                                e.Handled = true;
                            }
                        };

                        // Optionally, you can remove the TextChanged event handler since it's no longer needed.
                    }

                    else if (c == 5)
                    {
                        // Ranking
                        tb.Text = ((r % 4) + 1).ToString();
                    }
                    else
                    {
                        tb.Text = "0";

                        capturedRow = r;
                        int capturedCol = c;

                        // Make Difference column (col 3) read-only since it's auto-calculated
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

                                            // Update name in the display
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

                                // Auto-update Difference when Scored or Taken changes
                                team.Statistics.Difference = team.Statistics.ScoredPoints - team.Statistics.TakenPoints;
                                textFields[capturedRow, 3].Text = team.Statistics.Difference.ToString();

                                // Refresh display
                                var displayRefresh = Tournament.Instance.GetDisplayWindow();
                                displayRefresh?.Update();
                            };

                            // === NEW: Press Enter to force ranking refresh ===
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

            // === TABLE: Match Keys ===
            var keyTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top, // was Fill
                AutoSize = true,      // ensures full height
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

            // === BUTTONS ===
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

        public void SetCurrentMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= currentMatchKeyLabels.Length) return;
            currentMatchKeyLabels[idx].Text = $"Cl\u00e9 Terrain Actuel {idx + 1}: {key}";
        }

        public void SetNextMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= nextMatchKeyLabels.Length) return;
            nextMatchKeyLabels[idx].Text = $"Cl\u00e9 Match Suivant {idx + 1}: {key}";
        }

        public void SetTeamNames(string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                textFields[i, 0].Text = names[i];
        }
        #endregion
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
    }

    #region ---- Helper ----
    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] arr, Action<T, int> action)
        {
            for (int i = 0; i < arr.Length; i++) action(arr[i], i);
        }
    }

    #endregion
}