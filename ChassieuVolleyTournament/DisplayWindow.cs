using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    internal class DisplayWindow : Form
    {
        private Label timerText;
        private Label timerValue;
        private PictureBox logo;

        // Now using jagged array to store labels for easy modification
        // Each court has: 0 = top team label, 1 = bottom team label, 2 = top score label, 3 = bottom score label
        private Label[][] courtLabels = new Label[3][];

        public DisplayWindow()
        {
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            Text = "Chassieu Volley Tournament Display";
            Icon = new Icon("../../Images/ChassieuLogo.ico");
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(0, 38, 84);

            Font defaultFont = new Font("Segoe UI", 14, FontStyle.Bold);
            Font timerFont = new Font("Consolas", 42, FontStyle.Bold);

            timerText = new Label
            {
                Text = "TEMPS RESTANT :",
                ForeColor = Color.White,
                Font = defaultFont,
                AutoSize = true
            };
            Controls.Add(timerText);

            timerValue = new Label
            {
                Text = "05:16",
                ForeColor = Color.Red,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.Fixed3D,
                Font = timerFont,
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

            // Create the 3 courts
            for (int i = 0; i < 3; i++)
            {
                int x = 200 + i * 400;
                Controls.Add(CreateMatchPanel(x + 30, 200));
                Controls.Add(CreateCourtPanel(x, 320, i));
            }

            Controls.Add(CreateStyledButton("MATCHS", 30, 300));
            Controls.Add(CreateStyledButton("CLASSEMENT", 30, 710));

            string[] poules = { "POULE 1", "POULE 2", "POULE 3", "POULE VOLANTE" };
            for (int i = 0; i < 4; i++)
            {
                int x = 150 + i * 350;
                Controls.Add(CreateRankingPanel(x, 700, poules[i]));
            }

            this.Layout += (s, e) =>
            {
                timerText.Location = new Point((ClientSize.Width - timerText.Width) / 2, 20);
                timerValue.Location = new Point((ClientSize.Width - timerValue.Width) / 2, 60);
                logo.Location = new Point(ClientSize.Width - logo.Width - 30, 20);
            };
        }

        private Panel CreateMatchPanel(int x, int y)
        {
            Panel panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(200, 80),
                BackColor = Color.FromArgb(0, 51, 102),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lbl = new Label
            {
                Text = "PROCHAIN MATCH\nÉQUIPE 1 VS ÉQUIPE 2\n📢 ARBITRE",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lbl);
            return panel;
        }

        private Panel CreateCourtPanel(int x, int y, int index)
        {
            Panel courtPanel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(200, 300),
                BackgroundImage = Image.FromFile("../../Images/VolleyballField.jpg"),  
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label topLabel = new Label
            {
                Text = "ÉQUIPE 1",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent, 
                Location = new Point(60, 10),
                AutoSize = true
            };
            courtPanel.Controls.Add(topLabel);

            Label bottomLabel = new Label
            {
                Text = "ÉQUIPE 2",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(60, 260),
                AutoSize = true
            };
            courtPanel.Controls.Add(bottomLabel);

            Label topScoreLabel = new Label
            {
                Text = "21",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(80, 130),
                AutoSize = true
            };
            courtPanel.Controls.Add(topScoreLabel);

            Label bottomScoreLabel = new Label
            {
                Text = "15",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(80, 150),
                AutoSize = true
            };
            courtPanel.Controls.Add(bottomScoreLabel);

            courtLabels[index] = new Label[] { topLabel, bottomLabel, topScoreLabel, bottomScoreLabel };

            return courtPanel;
        }


        private Button CreateStyledButton(string text, int x, int y)
        {
            Button button = new Button
            {
                Text = string.Join("\n", text.ToCharArray()),
                Location = new Point(x, y),
                Size = new Size(50, 180),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Orange,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderSize = 0;

            // Rounded corners
            button.Region = new Region(new GraphicsPath(new PointF[]
            {
                new PointF(0,10), new PointF(10,0),
                new PointF(button.Width-10,0), new PointF(button.Width,10),
                new PointF(button.Width,button.Height-10), new PointF(button.Width-10,button.Height),
                new PointF(10,button.Height), new PointF(0,button.Height-10)
            }, new byte[]
            {
                1,1,1,1,1,1,1,1
            }));
            return button;
        }

        private Panel CreateRankingPanel(int x, int y, string title)
        {
            Panel panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(300, 200),
                BackColor = Color.FromArgb(0, 51, 102),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(lblTitle);

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            string[] headers = { "RANG", "ÉQUIPE", "PTS", "DIFF" };
            foreach (var header in headers)
                table.Controls.Add(CreateRankingCell(header, true));

            string[] noms = { "ÉQUIPE 1", "ÉQUIPE 2", "ÉQUIPE 3", "ÉQUIPE 4" };
            for (int i = 0; i < 4; i++)
            {
                table.Controls.Add(CreateRankingCell((i + 1).ToString()));
                table.Controls.Add(CreateRankingCell(noms[i]));
                table.Controls.Add(CreateRankingCell("3"));
                table.Controls.Add(CreateRankingCell("+5"));
            }

            panel.Controls.Add(table);
            return panel;
        }

        private Label CreateRankingCell(string text, bool isHeader = false)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", isHeader ? 9 : 8, isHeader ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
        }

        public void SetTimerText(float time)
        {
            int minutes = (int)(time / 60);
            int seconds = (int)(time % 60);
            timerValue.Text = $"{minutes:D2}:{seconds:D2}";
        }

        // Example: method to update court labels later
        public void UpdateCourt(int courtIndex, string topTeam, string bottomTeam, string topScore, string bottomScore)
        {
            if (courtIndex >= 0 && courtIndex < courtLabels.Length)
            {
                courtLabels[courtIndex][0].Text = topTeam;
                courtLabels[courtIndex][1].Text = bottomTeam;
                courtLabels[courtIndex][2].Text = topScore;
                courtLabels[courtIndex][3].Text = bottomScore;
            }
        }
    }
}
