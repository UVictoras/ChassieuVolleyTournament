using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    public class DisplayWindow : Form
    {
        private Label timerText, timerValue;
        private PictureBox logo;
        private Panel[] matchPanels = new Panel[3];
        private Label[][] courtLabels = new Label[3][];
        private Panel[] rankingPanels = new Panel[4]; // Added explicit references for ranking panels

        public DisplayWindow()
        {
            Text = "Chassieu Volley Tournament Display";
            WindowState = FormWindowState.Maximized;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(0, 38, 84);
            Icon = new Icon("../../Images/ChassieuLogo.ico");

            Shown += (s, e) => BeginInvoke(new Action(FinalizeLayout));
        }

        private void FinalizeLayout()
        {
            // Timer and logo
            var df = new Font("Segoe UI", 14, FontStyle.Bold);
            var tf = new Font("Consolas", 42, FontStyle.Bold);

            timerText = new Label
            {
                Text = "TEMPS RESTANT :",
                ForeColor = Color.White,
                Font = df,
                AutoSize = true
            };
            Controls.Add(timerText);

            timerValue = new Label
            {
                Text = "05:16",
                Font = tf,
                ForeColor = Color.Red,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D,
                AutoSize = true
            };
            Controls.Add(timerValue);

            logo = new PictureBox
            {
                Image = Image.FromFile("../../Images/ChassieuLogo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(120, 120)
            };
            Controls.Add(logo);

            // Courts + matches
            for (int i = 0; i < 3; i++)
            {
                int x = 200 + i * 400;
                var mp = CreateMatchPanel(x + 30, 200);
                matchPanels[i] = mp;
                Controls.Add(mp);
                Controls.Add(CreateCourtPanel(x, 320, i));
            }

            // Control buttons
            Controls.Add(CreateStyledButton("MATCHS", 30, 300));
            Controls.Add(CreateStyledButton("CLASSEMENT", 30, 710));

            // Ranking panels
            string[] titles = { "POULE 1", "POULE 2", "POULE 3", "POULE VOLANTE" };
            for (int i = 0; i < titles.Length; i++)
            {
                var rp = CreateRankingPanel(150 + i * 350, 700, titles[i]);
                rankingPanels[i] = rp;
                Controls.Add(rp);
            }

            // Center timer and logo
            Layout += (s, e) =>
            {
                timerText.Location = new Point((ClientSize.Width - timerText.Width) / 2, 20);
                timerValue.Location = new Point((ClientSize.Width - timerValue.Width) / 2, 60);
                logo.Location = new Point(ClientSize.Width - logo.Width - 30, 20);
            };
        }

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

            var top = CreateCourtLabel("ÉQUIPE 1", new Point(60, 10));
            var bot = CreateCourtLabel("ÉQUIPE 2", new Point(60, 270));
            var scT = CreateCourtLabel("0", new Point(80, 130));
            var scB = CreateCourtLabel("0", new Point(80, 150));
            hp.Controls.AddRange(new Control[] { top, bot, scT, scB });
            courtLabels[idx] = new[] { top, bot, scT, scB };
            return hp;
        }

        private Label CreateCourtLabel(string text, Point loc) =>
            new Label
            {
                Text = text,
                Location = loc,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

        private Button CreateStyledButton(string txt, int x, int y)
        {
            var b = new Button
            {
                Text = string.Join("\n", txt.ToCharArray()),
                Location = new Point(x, y),
                Size = new Size(50, 180),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

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
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            string[] hdr = { "RANG", "ÉQUIPE", "PTS", "DIFF" };
            foreach (var h in hdr) t.Controls.Add(CreateRankingCell(h, true));
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

        private Label CreateRankingCell(string txt, bool hdr = false) =>
            new Label
            {
                Text = txt,
                Font = new Font("Segoe UI", hdr ? 9 : 8, hdr ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

        public void SetFieldText(int idx, string t1, string t2, string s1, string s2)
        {
            if (idx < 0 || idx >= courtLabels.Length) return;
            courtLabels[idx][0].Text = t1;
            courtLabels[idx][1].Text = t2;
            courtLabels[idx][2].Text = s1;
            courtLabels[idx][3].Text = s2;
        }

        public void SetNextMatchText(int idx, string t1, string t2, string refName)
        {
            if (idx < 0 || idx >= matchPanels.Length) return;
            ((Label)matchPanels[idx].Controls[0]).Text =
                $"PROCHAIN MATCH\n{t1} VS {t2}\n📢 {refName}";
        }

        public void SetTimerText(float seconds)
        {
            int m = (int)(seconds / 60), s = (int)(seconds % 60);
            if (timerValue != null) timerValue.Text = $"{m:D2}:{s:D2}";
        }

        public Label[][] CourtLabels => courtLabels;

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
    }
}
