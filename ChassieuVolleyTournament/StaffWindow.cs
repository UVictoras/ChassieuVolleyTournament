using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    /// -------------------------------------------------------------------------------------
    /// Window for the tournament staff: team statistics (editable), current/next match keys,
    /// timer and phase buttons, referee web access (links), and the knockout bracket editor.
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

        /// <summary>The team shown in each table row (changes when rankings change).</summary>
        private readonly Team[] rowTeams = new Team[TeamCount];

        /// <summary>True while the program (not the user) writes in the text boxes.</summary>
        private bool updating;

        private readonly Label[] currentMatchKeyLabels = new Label[3];
        private readonly Label[] nextMatchKeyLabels = new Label[3];

        private Panel contentPanel;
        private Panel bottomPanel;
        private Control keyTable;
        private Label statusLabel;
        private Label webStatusLabel;
        private TextBox publicUrlBox;
        private TextBox localUrlBox;
        private Button copyPublicButton;
        private bool layoutBuilt;

        #endregion

        #region ---- Constructor ----

        public StaffWindow()
        {
            Text = "Tournoi de Chassieu Volley - Staff";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(10, 40, 80);

            Icon icon = AppPaths.AppIcon;
            if (icon != null) Icon = icon;

            Shown += (s, e) => EnsureLayout();
        }

        #endregion

        #region ---- Layout ----

        /// <summary>Builds the controls once (safe to call several times).</summary>
        public void EnsureLayout()
        {
            if (layoutBuilt) return;
            layoutBuilt = true;

            contentPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BackColor };
            bottomPanel = new Panel { Dock = DockStyle.Bottom, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = BackColor };

            Controls.Add(contentPanel);
            Controls.Add(bottomPanel);
            contentPanel.BringToFront();   // the Fill control must be docked last

            BuildTeamTable();
            BuildBottomArea();
        }

        private void BuildTeamTable()
        {
            contentPanel.Controls.Clear();

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = Columns,
                RowCount = TeamCount + 1,
                Padding = new Padding(20),
                BackColor = BackColor,
                AutoSize = true
            };

            float[] colWidths = { 0.25f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f };
            for (int i = 0; i < colWidths.Length; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, colWidths[i] * 100));

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
                int row = r;
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

                for (int c = 0; c < Columns; c++)
                {
                    int col = c;
                    var tb = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Font = new Font("Segoe UI", 10),
                        TextAlign = HorizontalAlignment.Center,
                        Margin = new Padding(2),
                        BackColor = Color.White,
                        Text = col == 0 ? "\u00c9quipe " + (r + 1) : (col == 5 ? ((r % 4) + 1).ToString() : "0")
                    };

                    if (col == 0)
                    {
                        // Enter renames the team that is displayed in THIS row.
                        tb.KeyDown += (s, e) =>
                        {
                            if (e.KeyCode != Keys.Enter) return;
                            e.Handled = true;
                            e.SuppressKeyPress = true;

                            Team team = rowTeams[row];
                            string newName = tb.Text.Trim();
                            if (team == null || newName.Length == 0) return;

                            team.Name = newName;
                            Tournament.Instance.RefreshNames();
                        };
                    }
                    else if (col == 3 || col == 5)
                    {
                        tb.ReadOnly = true;
                        tb.BackColor = Color.LightGray;
                    }
                    else
                    {
                        tb.TextChanged += (s, e) =>
                        {
                            if (updating) return;
                            Team team = rowTeams[row];
                            if (team == null) return;

                            int value;
                            if (!int.TryParse(tb.Text.Trim(), out value)) value = 0;

                            if (col == 1) team.Statistics.ScoredPoints = value;
                            else if (col == 2) team.Statistics.TakenPoints = value;
                            else if (col == 4) team.Statistics.TournamentPoints = value;

                            team.Statistics.Difference = team.Statistics.ScoredPoints - team.Statistics.TakenPoints;

                            updating = true;
                            try { textFields[row, 3].Text = FormatDiff(team.Statistics.Difference); }
                            finally { updating = false; }
                        };

                        // Enter applies the manual correction and re-sorts the pools.
                        tb.KeyDown += (s, e) =>
                        {
                            if (e.KeyCode != Keys.Enter) return;
                            e.SuppressKeyPress = true;
                            ActiveControl = null;   // so the re-sorted table can refresh this cell too
                            Tournament.Instance.RefreshAllPoolsRanking();
                        };
                    }

                    textFields[r, c] = tb;
                    table.Controls.Add(tb, c, r + 1);
                }
            }

            contentPanel.Controls.Add(table);
        }

        private void BuildBottomArea()
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 5, 20, 10),
                BackColor = BackColor
            };

            // ---- status line ----
            statusLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                Text = "En attente",
                Margin = new Padding(5)
            };
            flow.Controls.Add(statusLabel);

            // ---- keys ----
            var keys = new TableLayoutPanel
            {
                AutoSize = true,
                RowCount = 2,
                ColumnCount = 3,
                BackColor = BackColor
            };
            for (int i = 0; i < 3; i++)
            {
                keys.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

                currentMatchKeyLabels[i] = new Label
                {
                    Text = "Cl\u00e9 Terrain Actuel " + (i + 1),
                    Width = 350, Height = 34,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.LightYellow,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(5)
                };
                keys.Controls.Add(currentMatchKeyLabels[i], i, 0);

                nextMatchKeyLabels[i] = new Label
                {
                    Text = "Cl\u00e9 Match Suivant " + (i + 1),
                    Width = 350, Height = 34,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.LightBlue,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Margin = new Padding(5)
                };
                keys.Controls.Add(nextMatchKeyLabels[i], i, 1);
            }
            keyTable = keys;
            flow.Controls.Add(keys);

            // ---- buttons ----
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                MaximumSize = new Size(1800, 0),
                BackColor = BackColor
            };

            buttons.Controls.Add(CreateActionButton("Timer \u00c9chauffement", Color.Orange, (s, e) =>
            {
                Tournament t = Tournament.Instance;
                if (!ConfirmReplaceTimer()) return;
                string problem = t.GetStartTimerProblem(false);
                if (problem != null) { Warn(problem); return; }
                t.StartTimer(false);
            }));

            buttons.Controls.Add(CreateActionButton("Timer Match", Color.Green, (s, e) =>
            {
                Tournament t = Tournament.Instance;
                if (!ConfirmReplaceTimer()) return;
                string problem = t.GetStartTimerProblem(true);
                if (problem != null) { Warn(problem); return; }
                t.StartTimer(true);
            }));

            buttons.Controls.Add(CreateActionButton("Terminer le match", Color.Firebrick, (s, e) =>
            {
                Tournament t = Tournament.Instance;
                if (!t.IsMatchTimerRunning) { Warn("Aucun match en cours."); return; }

                if (MessageBox.Show(this, "Terminer les matchs maintenant et enregistrer les scores actuels ?",
                        "Terminer le match", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    t.EndMatchNow();
            }));

            buttons.Controls.Add(CreateActionButton("Annuler le timer", Color.DimGray, (s, e) =>
            {
                Tournament t = Tournament.Instance;
                if (!t.IsTimerRunning) return;

                if (MessageBox.Show(this, "Arr\u00eater le timer SANS enregistrer les scores ?",
                        "Annuler le timer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    t.CancelTimer();
            }));

            buttons.Controls.Add(CreateActionButton("Prochaine Phase", Color.Black, (s, e) =>
            {
                Tournament t = Tournament.Instance;
                string problem = t.GetNextPhaseProblem();
                if (problem != null) { Warn(problem); return; }

                if (MessageBox.Show(this, "Passer \u00e0 la phase suivante ? Les clefs actuelles ne fonctionneront plus.",
                        "Prochaine phase", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    t.GoToNextPhase();
            }));

            buttons.Controls.Add(CreateActionButton("Verrouillage des matchs", Color.DarkCyan, (s, e) => ShowLockDialog()));

            buttons.Controls.Add(CreateActionButton("Lancer la Pause", Color.Purple, (s, e) =>
            {
                Tournament.Instance.GetDisplayWindow()?.PauseLayout();
            }));

            buttons.Controls.Add(CreateActionButton("Reprendre l'affichage", Color.SteelBlue, (s, e) =>
            {
                Tournament.Instance.RestoreDisplay();
            }));

            flow.Controls.Add(buttons);

            // ---- referee web access ----
            flow.Controls.Add(CreateTitle("Acc\u00e8s arbitres (page web)"));

            webStatusLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Text = "Serveur web non d\u00e9marr\u00e9",
                Margin = new Padding(5, 2, 5, 2)
            };
            flow.Controls.Add(webStatusLabel);

            var publicRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = BackColor };
            publicRow.Controls.Add(new Label { Text = "Lien Internet (n'importe quel r\u00e9seau) :", AutoSize = true, ForeColor = Color.White, Margin = new Padding(5, 8, 5, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            publicUrlBox = CreateUrlBox();
            publicRow.Controls.Add(publicUrlBox);
            copyPublicButton = new Button { Text = "Copier", AutoSize = true, Enabled = false };
            copyPublicButton.Click += (s, e) => CopyText(publicUrlBox.Text);
            publicRow.Controls.Add(copyPublicButton);
            flow.Controls.Add(publicRow);

            var localRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = BackColor };
            localRow.Controls.Add(new Label { Text = "Lien r\u00e9seau local (m\u00eame Wi-Fi) :", AutoSize = true, ForeColor = Color.White, Margin = new Padding(5, 8, 5, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) });
            localUrlBox = CreateUrlBox();
            localRow.Controls.Add(localUrlBox);
            var copyLocal = new Button { Text = "Copier", AutoSize = true };
            copyLocal.Click += (s, e) => CopyText(localUrlBox.Text);
            localRow.Controls.Add(copyLocal);
            flow.Controls.Add(localRow);

            bottomPanel.Controls.Add(flow);
        }

        /// -------------------------------------------------------------
        /// Lets the staff decide which matches referees can still change.
        /// Checked = locked. Unchecking a finished match reopens it for a
        /// score correction (standings are updated automatically).
        /// -------------------------------------------------------------
        private void ShowLockDialog()
        {
            var matches = Tournament.Instance.GetLockableMatches();
            if (matches.Count == 0)
            {
                Warn("Aucun match terminé pour le moment.");
                return;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Verrouillage des matchs";
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Size = new Size(760, 520);
                dialog.MinimizeBox = false;
                dialog.MaximizeBox = false;

                var info = new Label
                {
                    Dock = DockStyle.Top, Height = 48, Padding = new Padding(8),
                    Text = "Coché = verrouillé (les arbitres ne peuvent plus modifier le score).\nDécochez un match pour le rouvrir à une correction."
                };
                var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, Font = new Font("Segoe UI", 10) };
                foreach (var item in matches)
                    list.Items.Add(item.Key, item.Value.Locked);

                var ok = new Button { Text = "Appliquer", Dock = DockStyle.Bottom, Height = 40, DialogResult = DialogResult.OK };

                dialog.Controls.Add(list);
                dialog.Controls.Add(info);
                dialog.Controls.Add(ok);
                dialog.AcceptButton = ok;

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                for (int i = 0; i < matches.Count; i++)
                    matches[i].Value.Locked = list.GetItemChecked(i);

                Tournament.Instance.RefreshAll();
            }
        }

        private Label CreateTitle(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.FromArgb(243, 167, 18),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Margin = new Padding(5, 12, 5, 2)
            };
        }

        private TextBox CreateUrlBox()
        {
            return new TextBox
            {
                ReadOnly = true,
                Width = 520,
                Font = new Font("Consolas", 11),
                Margin = new Padding(5, 4, 5, 4),
                BackColor = Color.White
            };
        }

        private void CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try { Clipboard.SetText(text); }
            catch (Exception ex) { Warn("Copie impossible : " + ex.Message); }
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, "Tournoi de Chassieu Volley", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>If a timer is running, asks before replacing it.</summary>
        private bool ConfirmReplaceTimer()
        {
            if (!Tournament.Instance.IsTimerRunning) return true;

            return MessageBox.Show(this,
                "Un timer est d\u00e9j\u00e0 en cours. Le remplacer ? (les scores ne sont pas enregistr\u00e9s)",
                "Timer en cours", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes
                && CancelAndTrue();
        }

        private static bool CancelAndTrue()
        {
            Tournament.Instance.CancelTimer();
            return true;
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
                Margin = new Padding(8),
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        #endregion

        #region ---- Knockout (tree) phase ----

        /// ----------------------------------------------------------------------------
        /// Rebuilds the central area for the knockout phase. The buttons (timer, pause,
        /// web links) stay available at the bottom.
        /// ----------------------------------------------------------------------------
        public void RebuildForTreePhase()
        {
            EnsureLayout();

            var treePhase = Tournament.Instance.GetCurrentPhase() as TreePhase;
            if (treePhase == null) return;

            foreach (Control old in contentPanel.Controls.Cast<Control>().ToList())
            {
                contentPanel.Controls.Remove(old);
                old.Dispose();
            }

            if (keyTable != null) keyTable.Visible = false;

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(20),
                ColumnCount = 4,
                RowCount = 9,
                BackColor = BackColor,
                AutoSize = true
            };

            string[] roundLabels = { "Quarts de finale", "Demi-finales", "Finale", "Match 3\u00e8me place" };
            for (int i = 0; i < roundLabels.Length; i++)
            {
                table.Controls.Add(new Label
                {
                    Text = roundLabels[i],
                    Dock = DockStyle.Top,
                    Height = 30,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(15, 60, 100),
                    Margin = new Padding(5)
                }, i, 0);
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

            contentPanel.Controls.Add(table);
        }

        /// -------------------------------------------------------------------------------
        /// One match of the bracket: its key, and editable team names (Enter to apply).
        /// Each slot owns its own Team objects, so editing one never changes another.
        /// -------------------------------------------------------------------------------
        private Panel CreateMatchPanel(string slot, TreePhase treePhase)
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

            Match match = treePhase.GetMatchBySlot(slot);
            if (match == null) return matchPanel;

            var txtTeam1 = new TextBox { Text = match.Team1.Name, Width = 150, Font = new Font("Segoe UI", 10), Margin = new Padding(2) };
            var txtTeam2 = new TextBox { Text = match.Team2.Name, Width = 150, Font = new Font("Segoe UI", 10), Margin = new Padding(2) };

            var lblKey = new Label
            {
                Text = slot + " : " + match.GetMatchKey(),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(2, 6, 6, 2)
            };

            Action apply = () =>
            {
                string n1 = txtTeam1.Text.Trim();
                string n2 = txtTeam2.Text.Trim();
                if (n1.Length > 0) match.Team1.Name = n1;
                if (n2.Length > 0) match.Team2.Name = n2;
                Tournament.Instance.RefreshBracket();
            };

            KeyEventHandler onKey = (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                apply();
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
            txtTeam1.KeyDown += onKey;
            txtTeam2.KeyDown += onKey;

            var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            flow.Controls.Add(lblKey);
            flow.Controls.Add(txtTeam1);
            flow.Controls.Add(new Label { Text = "vs", AutoSize = true, Margin = new Padding(5, 6, 5, 2) });
            flow.Controls.Add(txtTeam2);

            matchPanel.Controls.Add(flow);
            return matchPanel;
        }

        #endregion

        #region ---- Updates from the tournament ----

        private static string FormatDiff(int diff) => diff >= 0 ? "+" + diff : diff.ToString();

        public void SetCurrentMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= currentMatchKeyLabels.Length || currentMatchKeyLabels[idx] == null) return;
            currentMatchKeyLabels[idx].Text = "Cl\u00e9 Terrain Actuel " + (idx + 1) + " : " + key;
        }

        public void SetNextMatchKey(int idx, string key)
        {
            if (idx < 0 || idx >= nextMatchKeyLabels.Length || nextMatchKeyLabels[idx] == null) return;
            nextMatchKeyLabels[idx].Text = "Cl\u00e9 Match Suivant " + (idx + 1) + " : " + key;
        }

        public void SetStatus(string text)
        {
            if (statusLabel != null && statusLabel.Text != text) statusLabel.Text = text;
        }

        /// -------------------------------------------------------------------
        /// Fills the 4 rows of a pool, in ranking order, and remembers which
        /// team each row shows (so edits always go to the right team).
        /// -------------------------------------------------------------------
        public void SetRankingText(int poolIndex, IList<Team> orderedTeams)
        {
            if (!layoutBuilt) return;

            updating = true;
            try
            {
                for (int i = 0; i < 4 && i < orderedTeams.Count; i++)
                {
                    int row = poolIndex * 4 + i;
                    Team team = orderedTeams[i];
                    rowTeams[row] = team;

                    SetText(textFields[row, 0], team.Name);
                    SetText(textFields[row, 1], team.Statistics.ScoredPoints.ToString());
                    SetText(textFields[row, 2], team.Statistics.TakenPoints.ToString());
                    SetText(textFields[row, 3], FormatDiff(team.Statistics.Difference));
                    SetText(textFields[row, 4], team.Statistics.TournamentPoints.ToString());
                    SetText(textFields[row, 5], (i + 1).ToString());
                }
            }
            finally
            {
                updating = false;
            }
        }

        /// <summary>Only writes when the value changed, and never overwrites a cell being typed in.</summary>
        private void SetText(TextBox box, string text)
        {
            if (box.Text != text && !box.Focused) box.Text = text;
        }

        /// <summary>Shows the referee links and server/tunnel status.</summary>
        public void SetWebInfo(string status, string publicUrl, string localUrl)
        {
            if (!layoutBuilt) return;

            if (status != null) webStatusLabel.Text = status;
            publicUrlBox.Text = publicUrl ?? "";
            copyPublicButton.Enabled = !string.IsNullOrEmpty(publicUrl);
            localUrlBox.Text = localUrl ?? "";
        }

        #endregion
    }
}
