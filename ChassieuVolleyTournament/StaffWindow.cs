using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    public class StaffWindow : Form
    {
        private const int TeamCount = 16;
        private const int Columns = 6;
        private readonly string[] headers = {
            "\u00c9quipe", "Points Marqu\u00e9s", "Points Pris", "Diff\u00e9rence", "Points Tournoi", "Classement"
        };
        public TextBox[,] textFields = new TextBox[TeamCount, Columns];
        private Label[] currentMatchKeyLabels = new Label[3];
        private Label[] nextMatchKeyLabels = new Label[3];
        private TableLayoutPanel mainPanel;

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
                RowCount = 2,
                ColumnCount = 1,
                BackColor = BackColor
            };
            Controls.Add(mainPanel);

            Shown += (s, e) => BeginInvoke(new Action(FinalizeLayout));
        }

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
                        tb.Text = "Équipe 0";
                        int capturedIndex = r;
                        tb.TextChanged += (s, e) =>
                        {
                            Tournament.Instance.Teams[capturedIndex].Name = tb.Text;

                            var display = Tournament.Instance.GetDisplayWindow();
                            if (display != null)
                            {
                                string[] allTeamNames = new string[Tournament.Instance.Teams.Length];
                                for (int i = 0; i < allTeamNames.Length; i++)
                                    allTeamNames[i] = Tournament.Instance.Teams[i].Name;

                                display.UpdateRankingTeamNames(allTeamNames);
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
                    }

                    textFields[r, c] = tb;
                    table.Controls.Add(tb, c, r + 1);
                }
            }

            mainPanel.Controls.Add(table);

            var keyTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
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
                    Text = $"Cl\u00e9 Terrain Actuel {i + 1}",
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
                    Text = $"Cl\u00e9 Match Suivant {i + 1}",
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
        }

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
    }

    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] arr, Action<T, int> action)
        {
            for (int i = 0; i < arr.Length; i++) action(arr[i], i);
        }
    }
}
