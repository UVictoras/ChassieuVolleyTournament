using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    /// ----------------------------------------------------------------
    /// Colors and fonts of the public display.
    /// ----------------------------------------------------------------
    internal static class Theme
    {
        public static readonly Color Gold = Color.FromArgb(243, 167, 18);
        public static readonly Color GoldLight = Color.FromArgb(255, 211, 100);
        public static readonly Color Sky = Color.FromArgb(120, 200, 255);

        public static readonly Color NavyTop = Color.FromArgb(9, 40, 82);
        public static readonly Color NavyBottom = Color.FromArgb(2, 15, 38);

        public static readonly Color CardTop = Color.FromArgb(22, 70, 122);
        public static readonly Color CardBottom = Color.FromArgb(11, 43, 85);
        public static readonly Color CardBorder = Color.FromArgb(58, 116, 178);

        public static readonly Color CourtLeft = Color.FromArgb(33, 124, 192);
        public static readonly Color CourtRight = Color.FromArgb(17, 80, 140);

        public static readonly Color Muted = Color.FromArgb(150, 186, 224);
        public static readonly Color Win = Color.FromArgb(104, 222, 138);
        public static readonly Color Lose = Color.FromArgb(255, 125, 115);

        public const string Font = "Segoe UI";
        public const string Digits = "Consolas";
    }

    /// ----------------------------------------------------------------
    /// Small drawing helpers (rounded rectangles, auto-sized text).
    /// ----------------------------------------------------------------
    internal static class Gfx
    {
        public static void Prepare(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Max(0.1f, Math.Min(radius * 2f, Math.Min(r.Width, r.Height)));

            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Color top, Color bottom)
        {
            if (r.Width < 2 || r.Height < 2) return;

            using (var path = Round(r, radius))
            using (var brush = new LinearGradientBrush(r, top, bottom, 90f))
                g.FillPath(brush, path);
        }

        public static void FillRound(Graphics g, RectangleF r, float radius, Color color)
        {
            if (r.Width < 2 || r.Height < 2) return;

            using (var path = Round(r, radius))
            using (var brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        public static void StrokeRound(Graphics g, RectangleF r, float radius, Color color, float width)
        {
            if (r.Width < 2 || r.Height < 2) return;

            using (var path = Round(r, radius))
            using (var pen = new Pen(color, width))
                g.DrawPath(pen, path);
        }

        /// <summary>Largest font (pixels) that makes the text fit on ONE line in maxWidth.</summary>
        public static Font Fit(Graphics g, string text, string family, FontStyle style, float maxPx, float minPx, float maxWidth)
        {
            float size = Math.Max(minPx, maxPx);

            while (true)
            {
                var font = new Font(family, size, style, GraphicsUnit.Pixel);
                float width = g.MeasureString(text ?? "", font, new SizeF(100000, 100000), StringFormat.GenericTypographic).Width;

                if (width <= maxWidth || size <= minPx) return font;

                font.Dispose();
                size -= Math.Max(1f, size * 0.06f);
            }
        }

        /// <summary>Largest font (pixels) that makes the text fit in a box, wrapping on several lines.</summary>
        public static Font FitWrap(Graphics g, string text, string family, FontStyle style, float maxPx, float minPx, float width, float height)
        {
            float size = Math.Max(minPx, maxPx);

            while (true)
            {
                var font = new Font(family, size, style, GraphicsUnit.Pixel);
                SizeF measured = g.MeasureString(text ?? "", font, new SizeF(Math.Max(1, width), 100000), StringFormat.GenericTypographic);

                if (measured.Height <= height || size <= minPx) return font;

                font.Dispose();
                size -= Math.Max(1f, size * 0.06f);
            }
        }

        public static void Text(Graphics g, string text, Font font, Color color, RectangleF rect,
            StringAlignment horizontal = StringAlignment.Center,
            StringAlignment vertical = StringAlignment.Center,
            bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            using (var format = new StringFormat(StringFormat.GenericTypographic))
            using (var brush = new SolidBrush(color))
            {
                format.Alignment = horizontal;
                format.LineAlignment = vertical;
                format.Trimming = StringTrimming.EllipsisCharacter;

                // GenericTypographic hides any line taller than the box (LineLimit): not wanted here.
                format.FormatFlags &= ~StringFormatFlags.LineLimit;
                if (!wrap) format.FormatFlags |= StringFormatFlags.NoWrap;

                g.DrawString(text, font, brush, rect, format);
            }
        }

        /// <summary>One line of text, shrunk to fit the rectangle width.</summary>
        public static void FitText(Graphics g, string text, string family, FontStyle style, float maxPx, float minPx,
            Color color, RectangleF rect, StringAlignment horizontal = StringAlignment.Center)
        {
            if (string.IsNullOrEmpty(text)) return;

            using (Font font = Fit(g, text, family, style, maxPx, minPx, rect.Width))
                Text(g, text, font, color, rect, horizontal);
        }

        public static void Star(Graphics g, PointF center, float radius, Color color)
        {
            var points = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double angle = -Math.PI / 2 + i * Math.PI / 5;
                float r = (i % 2 == 0) ? radius : radius * 0.42f;
                points[i] = new PointF(center.X + (float)Math.Cos(angle) * r, center.Y + (float)Math.Sin(angle) * r);
            }

            using (var brush = new SolidBrush(color))
                g.FillPolygon(brush, points);
        }

        public static bool IsPlaceholder(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;

            string n = name.Trim().ToLowerInvariant();
            return n == "équipe 1" || n == "équipe 2" || n == "---" || n == "-";
        }
    }

    // =====================================================================
    //  Data shown by the controls (kept by the window, so nothing is lost
    //  when a layout is rebuilt)
    // =====================================================================

    internal class CourtData
    {
        public string Team1 = "ÉQUIPE 1";
        public string Team2 = "ÉQUIPE 2";
        public string Score1 = "0";
        public string Score2 = "0";
        public string Referee = "-";
        public string NextTeam1 = "---";
        public string NextTeam2 = "---";
        public string NextReferee = "-";
    }

    internal class RankData
    {
        public string Title = "POULE";
        public string[] Names = { "ÉQUIPE 1", "ÉQUIPE 2", "ÉQUIPE 3", "ÉQUIPE 4" };
        public string[] Points = { "0", "0", "0", "0" };
        public string[] Diffs = { "+0", "+0", "+0", "+0" };
    }

    internal class BracketMatch
    {
        public string TeamA = "Équipe 1";
        public string TeamB = "Équipe 2";
        public int ScoreA;
        public int ScoreB;
    }

    /// ----------------------------------------------------------------
    /// Base of the custom controls: flicker-free and transparent
    /// (the rounded corners show the background gradient).
    /// ----------------------------------------------------------------
    internal abstract class CardBase : Control
    {
        protected CardBase()
        {
            SetStyle(ControlStyles.UserPaint
                   | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer
                   | ControlStyles.ResizeRedraw
                   | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        /// <summary>Scale factor relative to a reference size.</summary>
        protected float Scale(float refWidth, float refHeight)
        {
            return Math.Max(0.25f, Math.Min(Width / refWidth, Height / refHeight));
        }
    }

    /// ----------------------------------------------------------------
    /// Window background: vertical gradient and a gold line at the top.
    /// ----------------------------------------------------------------
    internal class GradientPanel : Panel
    {
        public GradientPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width < 2 || Height < 2) return;

            using (var brush = new LinearGradientBrush(ClientRectangle, Theme.NavyTop, Theme.NavyBottom, 90f))
                e.Graphics.FillRectangle(brush, ClientRectangle);

            using (var brush = new SolidBrush(Theme.Gold))
                e.Graphics.FillRectangle(brush, 0, 0, Width, Math.Max(3, Height / 220));
        }
    }

    // =====================================================================
    //  Header: logo, title, phase, timer
    // =====================================================================
    internal class HeaderBar : CardBase
    {
        public Image Logo;
        public string Title = "TOURNOI DE CHASSIEU VOLLEY";
        public string Subtitle = "";
        public string TimerText = "00:00";
        public string TimerMode = "";
        public bool TimerCritical;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g);

            float H = Height, W = Width;
            if (W < 50 || H < 30) return;

            float x = 0;

            // ---- logo ----
            if (Logo != null)
            {
                float size = H * 0.96f;
                float lw = size * Logo.Width / Logo.Height;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Logo, new RectangleF(4, (H - size) / 2, lw, size));
                x = lw + H * 0.25f;
            }

            // ---- timer pill (right) ----
            float pillH = H * 0.96f;
            float pillW = Math.Min(W * 0.34f, pillH * 3.5f);
            var pill = new RectangleF(W - pillW - 4, (H - pillH) / 2, pillW, pillH);

            bool running = !string.IsNullOrEmpty(TimerMode);
            Gfx.FillRound(g, pill, pillH * 0.22f, Color.FromArgb(150, 0, 0, 0));
            Gfx.StrokeRound(g, pill, pillH * 0.22f, running ? Theme.Gold : Theme.CardBorder, Math.Max(2f, H * 0.03f));

            string label = running ? TimerMode : "TEMPS RESTANT";
            using (var f = new Font(Theme.Font, pillH * 0.17f, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, label, f, running ? Theme.Gold : Theme.Muted,
                    new RectangleF(pill.Left, pill.Top + pillH * 0.06f, pill.Width, pillH * 0.26f));

            Color digitColor = !running ? Color.FromArgb(120, 150, 185)
                             : TimerCritical ? Color.FromArgb(255, 92, 80) : Color.White;
            using (var f = new Font(Theme.Digits, pillH * 0.52f, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, TimerText, f, digitColor,
                    new RectangleF(pill.Left, pill.Top + pillH * 0.30f, pill.Width, pillH * 0.62f));

            // ---- title + phase ----
            float textW = pill.Left - x - H * 0.3f;
            if (textW > 50)
            {
                bool hasSub = !string.IsNullOrEmpty(Subtitle);

                var titleRect = new RectangleF(x, hasSub ? H * 0.08f : 0, textW, hasSub ? H * 0.50f : H);
                Gfx.FitText(g, Title, Theme.Font, FontStyle.Bold, H * 0.42f, 12, Color.White, titleRect, StringAlignment.Near);

                if (hasSub)
                {
                    var subRect = new RectangleF(x, H * 0.56f, textW, H * 0.34f);
                    Gfx.FitText(g, Subtitle, Theme.Font, FontStyle.Bold, H * 0.28f, 10, Theme.Gold, subRect, StringAlignment.Near);
                }
            }
        }
    }

    // =====================================================================
    //  Court card: a volleyball court with the live score
    // =====================================================================
    internal class CourtCard : CardBase
    {
        public int Number = 1;
        public CourtData Data = new CourtData();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g);

            float W = Width, H = Height;
            if (W < 120 || H < 120) return;

            float u = Scale(600f, 540f);
            CourtData d = Data ?? new CourtData();

            // ---- card ----
            var card = new RectangleF(1, 1, W - 2, H - 2);
            Gfx.FillRound(g, card, 22 * u, Theme.CardTop, Theme.CardBottom);
            Gfx.StrokeRound(g, card, 22 * u, Theme.CardBorder, 2 * u);

            // ---- title ----
            using (var f = new Font(Theme.Font, 38 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, "TERRAIN " + Number, f, Theme.Gold, new RectangleF(0, 12 * u, W, 54 * u));

            // ---- court ----
            float margin = 18 * u;
            float bottomBlock = 118 * u;
            float refBlock = 54 * u;
            float availH = H - 72 * u - refBlock - bottomBlock - 22 * u;
            float courtW = W - 2 * margin;
            float courtH = Math.Max(60 * u, Math.Min(courtW / 2f, availH));
            var court = new RectangleF(margin, 72 * u, courtW, courtH);

            DrawCourt(g, court, u);

            float half = court.Width / 2f;
            var left = new RectangleF(court.Left, court.Top, half, court.Height);
            var right = new RectangleF(court.Left + half, court.Top, half, court.Height);

            DrawHalf(g, left, d.Team1, d.Score1, d.Score2, u);
            DrawHalf(g, right, d.Team2, d.Score2, d.Score1, u);

            // ---- referee ----
            float y = court.Bottom + 12 * u;
            using (var f = new Font(Theme.Font, 16 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, "ARBITRE", f, Theme.Muted, new RectangleF(0, y, W, 20 * u));
            Gfx.FitText(g, (d.Referee ?? "-").ToUpper(), Theme.Font, FontStyle.Bold, 27 * u, 12, Color.White,
                new RectangleF(margin, y + 20 * u, W - 2 * margin, 32 * u));

            // ---- next match ----
            float ny = H - bottomBlock + 4 * u;
            using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.5f * u))
                g.DrawLine(pen, margin * 2, ny - 6 * u, W - margin * 2, ny - 6 * u);

            using (var f = new Font(Theme.Font, 16 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, "PROCHAIN MATCH", f, Theme.Gold, new RectangleF(0, ny, W, 22 * u));

            string next = (d.NextTeam1 ?? "---") + "   vs   " + (d.NextTeam2 ?? "---");
            Gfx.FitText(g, next.ToUpper(), Theme.Font, FontStyle.Bold, 26 * u, 11, Color.White,
                new RectangleF(margin, ny + 24 * u, W - 2 * margin, 34 * u));

            string nextRef = string.IsNullOrEmpty(d.NextReferee) || d.NextReferee == "Arbitre" || d.NextReferee == "-"
                ? "" : "Arbitre : " + d.NextReferee;
            Gfx.FitText(g, nextRef, Theme.Font, FontStyle.Regular, 18 * u, 10, Theme.Muted,
                new RectangleF(margin, ny + 60 * u, W - 2 * margin, 26 * u));
        }

        private static void DrawCourt(Graphics g, RectangleF court, float u)
        {
            float radius = 12 * u;
            float half = court.Width / 2f;

            // two halves, slightly different blues
            using (var path = Gfx.Round(court, radius))
            {
                g.SetClip(path);
                using (var brush = new LinearGradientBrush(court, Theme.CourtLeft, Theme.CourtRight, 0f))
                    g.FillRectangle(brush, court);
                g.ResetClip();
            }

            Gfx.StrokeRound(g, court, radius, Color.FromArgb(120, 255, 255, 255), 2 * u);

            // lines
            var line = new RectangleF(court.Left + 12 * u, court.Top + 12 * u, court.Width - 24 * u, court.Height - 24 * u);
            using (var pen = new Pen(Color.FromArgb(150, 255, 255, 255), 3 * u))
            {
                g.DrawRectangle(pen, line.X, line.Y, line.Width, line.Height);

                // attack lines (3 m)
                float a = half / 3f;
                float cx = court.Left + half;
                g.DrawLine(pen, cx - a, line.Top, cx - a, line.Bottom);
                g.DrawLine(pen, cx + a, line.Top, cx + a, line.Bottom);
            }

            // net
            float cxNet = court.Left + half;
            using (var pen = new Pen(Color.FromArgb(235, 255, 255, 255), 5 * u))
                g.DrawLine(pen, cxNet, court.Top + 4 * u, cxNet, court.Bottom - 4 * u);

            using (var brush = new SolidBrush(Theme.Gold))
            {
                float r = 6 * u;
                g.FillEllipse(brush, cxNet - r, court.Top + 2 * u - r / 2, r * 2, r * 2);
                g.FillEllipse(brush, cxNet - r, court.Bottom - 2 * u - r * 1.5f, r * 2, r * 2);
            }
        }

        private static void DrawHalf(Graphics g, RectangleF area, string team, string score, string otherScore, float u)
        {
            // team name (top of the half, wraps on two lines if needed)
            var nameRect = new RectangleF(area.Left + 20 * u, area.Top + 16 * u, area.Width - 40 * u, area.Height * 0.34f);
            string name = (team ?? "").ToUpper();

            using (Font f = Gfx.FitWrap(g, name, Theme.Font, FontStyle.Bold, 30 * u, 11, nameRect.Width, nameRect.Height))
            {
                Gfx.Text(g, name, f, Color.FromArgb(120, 0, 0, 0), new RectangleF(nameRect.X + 1.5f * u, nameRect.Y + 1.5f * u, nameRect.Width, nameRect.Height), StringAlignment.Center, StringAlignment.Near, true);
                Gfx.Text(g, name, f, Color.White, nameRect, StringAlignment.Center, StringAlignment.Near, true);
            }

            // score
            int s, o;
            int.TryParse(score, out s);
            int.TryParse(otherScore, out o);
            bool leading = s > o;

            var scoreRect = new RectangleF(area.Left, area.Top + area.Height * 0.36f, area.Width, area.Height * 0.62f);
            using (var f = new Font(Theme.Font, Math.Min(scoreRect.Height * 0.95f, area.Width * 0.62f), FontStyle.Bold, GraphicsUnit.Pixel))
            {
                Gfx.Text(g, score, f, Color.FromArgb(110, 0, 0, 0), new RectangleF(scoreRect.X + 3 * u, scoreRect.Y + 3 * u, scoreRect.Width, scoreRect.Height));
                Gfx.Text(g, score, f, leading ? Theme.GoldLight : Color.White, scoreRect);
            }
        }
    }

    // =====================================================================
    //  Ranking card of one pool
    // =====================================================================
    internal class RankingCard : CardBase
    {
        public RankData Data = new RankData();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g);

            float W = Width, H = Height;
            if (W < 120 || H < 100) return;

            float u = Scale(450f, 330f);
            RankData d = Data ?? new RankData();

            var card = new RectangleF(1, 1, W - 2, H - 2);
            Gfx.FillRound(g, card, 20 * u, Theme.CardTop, Theme.CardBottom);
            Gfx.StrokeRound(g, card, 20 * u, Theme.CardBorder, 2 * u);

            // title band
            var band = new RectangleF(1, 1, W - 2, 50 * u);
            using (var path = Gfx.Round(card, 20 * u))
            {
                g.SetClip(path);
                using (var brush = new SolidBrush(Theme.Gold))
                    g.FillRectangle(brush, band);
                g.ResetClip();
            }
            using (var f = new Font(Theme.Font, 27 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, d.Title, f, Color.FromArgb(10, 33, 66), band);

            // column headers
            float pad = 14 * u;
            float ptsW = 74 * u, diffW = 82 * u;
            float headerY = band.Bottom + 6 * u;
            float headerH = 24 * u;

            float ptsX = W - pad - diffW - ptsW;
            float diffX = W - pad - diffW;

            using (var f = new Font(Theme.Font, 15 * u, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                Gfx.Text(g, "ÉQUIPE", f, Theme.Muted, new RectangleF(pad + 52 * u, headerY, 200 * u, headerH), StringAlignment.Near);
                Gfx.Text(g, "PTS", f, Theme.Muted, new RectangleF(ptsX, headerY, ptsW, headerH));
                Gfx.Text(g, "DIFF", f, Theme.Muted, new RectangleF(diffX, headerY, diffW, headerH));
            }

            // rows
            float top = headerY + headerH + 2 * u;
            float rowH = (H - top - 12 * u) / 4f;

            for (int i = 0; i < 4; i++)
            {
                var row = new RectangleF(pad, top + i * rowH, W - 2 * pad, rowH - 5 * u);

                Gfx.FillRound(g, row, 12 * u, i == 0 ? Color.FromArgb(120, 4, 20, 50) : Color.FromArgb(i % 2 == 0 ? 22 : 12, 255, 255, 255));
                if (i == 0) Gfx.StrokeRound(g, row, 12 * u, Theme.Gold, 2.5f * u);

                // rank badge
                float d0 = row.Height * 0.66f;
                var badge = new RectangleF(row.Left + 9 * u, row.Top + (row.Height - d0) / 2, d0, d0);
                using (var brush = new SolidBrush(i == 0 ? Theme.Gold : Color.FromArgb(48, 112, 176)))
                    g.FillEllipse(brush, badge);
                using (var f = new Font(Theme.Font, d0 * 0.58f, FontStyle.Bold, GraphicsUnit.Pixel))
                    Gfx.Text(g, (i + 1).ToString(), f, i == 0 ? Color.FromArgb(10, 33, 66) : Color.White, badge);

                // team name
                float nameX = badge.Right + 12 * u;
                var nameRect = new RectangleF(nameX, row.Top, ptsX - nameX - 4 * u, row.Height);
                Gfx.FitText(g, (Get(d.Names, i, "")).ToUpper(), Theme.Font, FontStyle.Bold, row.Height * 0.46f, 10, Color.White, nameRect, StringAlignment.Near);

                // points
                using (var f = new Font(Theme.Font, row.Height * 0.52f, FontStyle.Bold, GraphicsUnit.Pixel))
                    Gfx.Text(g, Get(d.Points, i, "0"), f, i == 0 ? Theme.GoldLight : Color.White, new RectangleF(ptsX, row.Top, ptsW, row.Height));

                // difference
                string diff = Get(d.Diffs, i, "+0");
                Color diffColor = diff.StartsWith("-") ? Theme.Lose : (diff == "+0" ? Theme.Muted : Theme.Win);
                using (var f = new Font(Theme.Font, row.Height * 0.40f, FontStyle.Bold, GraphicsUnit.Pixel))
                    Gfx.Text(g, diff, f, diffColor, new RectangleF(diffX, row.Top, diffW, row.Height));
            }
        }

        private static string Get(string[] values, int i, string fallback)
        {
            return values != null && i < values.Length && values[i] != null ? values[i] : fallback;
        }
    }

    // =====================================================================
    //  Knockout bracket (both brackets, drawn side by side)
    // =====================================================================
    internal class BracketView : CardBase
    {
        public Dictionary<string, BracketMatch> Matches = new Dictionary<string, BracketMatch>();
        public Dictionary<string, string> Champions = new Dictionary<string, string>();

        /// <summary>Raised when the staff clicks a champion box (argument: "P" or "C").</summary>
        public event Action<string> ChampionClicked;

        private readonly Dictionary<string, RectangleF> championRects = new Dictionary<string, RectangleF>();

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g);

            float W = Width, H = Height;
            if (W < 300 || H < 200) return;

            championRects.Clear();

            float gap = Math.Max(14f, W * 0.012f);
            float halfW = (W - gap) / 2f;

            DrawHalf(g, new RectangleF(0, 0, halfW, H), "P", "PHASE PRINCIPALE", Theme.Gold, Color.FromArgb(10, 33, 66));
            DrawHalf(g, new RectangleF(halfW + gap, 0, halfW, H), "C", "PHASE CONSOLANTE", Theme.Sky, Color.FromArgb(10, 33, 66));
        }

        private void DrawHalf(Graphics g, RectangleF r, string prefix, string title, Color accent, Color titleText)
        {
            float u = Math.Max(0.3f, Math.Min(r.Width / 900f, r.Height / 800f));

            Gfx.FillRound(g, r, 22 * u, Color.FromArgb(70, 8, 30, 64), Color.FromArgb(70, 4, 18, 44));
            Gfx.StrokeRound(g, r, 22 * u, Color.FromArgb(120, accent), 2.5f * u);

            // title band
            var band = new RectangleF(r.Left + 18 * u, r.Top + 16 * u, r.Width - 36 * u, 56 * u);
            Gfx.FillRound(g, band, 14 * u, accent);
            using (var f = new Font(Theme.Font, 32 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, title, f, titleText, band);

            // geometry
            float pad = 24 * u;
            float colGap = 44 * u;
            float cw = (r.Width - 2 * pad - 2 * colGap) / 3f;
            float x0 = r.Left + pad;
            float x1 = x0 + cw + colGap;
            float x2 = x1 + cw + colGap;

            float headerY = band.Bottom + 12 * u;
            float areaTop = headerY + 34 * u;
            float areaH = r.Bottom - areaTop - 24 * u;
            float slotH = areaH / 4f;
            float ch = Math.Min(slotH * 0.78f, 120 * u);

            using (var f = new Font(Theme.Font, 18 * u, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                Gfx.Text(g, "QUARTS", f, Theme.Muted, new RectangleF(x0, headerY, cw, 28 * u));
                Gfx.Text(g, "DEMI-FINALES", f, Theme.Muted, new RectangleF(x1, headerY, cw, 28 * u));
                Gfx.Text(g, "FINALE", f, Theme.Muted, new RectangleF(x2, headerY, cw, 28 * u));
            }

            float[] qy = new float[4];
            for (int i = 0; i < 4; i++) qy[i] = areaTop + slotH * (i + 0.5f);
            float[] sy = { (qy[0] + qy[1]) / 2f, (qy[2] + qy[3]) / 2f };
            float fy = (sy[0] + sy[1]) / 2f;

            // connectors (drawn first, under the cards)
            using (var pen = new Pen(Color.FromArgb(150, accent), 3 * u))
            {
                Connect(g, pen, x0 + cw, qy[0], x1, sy[0], qy[1], colGap);
                Connect(g, pen, x0 + cw, qy[2], x1, sy[1], qy[3], colGap);
                Connect(g, pen, x1 + cw, sy[0], x2, fy, sy[1], colGap);
            }

            // matches
            for (int i = 0; i < 4; i++)
                DrawMatch(g, new RectangleF(x0, qy[i] - ch / 2, cw, ch), Get(prefix + "_QF" + (i + 1)), accent, u);

            for (int i = 0; i < 2; i++)
                DrawMatch(g, new RectangleF(x1, sy[i] - ch / 2, cw, ch), Get(prefix + "_SF" + (i + 1)), accent, u);

            var finalRect = new RectangleF(x2, fy - ch / 2, cw, ch);
            DrawMatch(g, finalRect, Get(prefix + "_FINAL"), accent, u, true);

            // champion (above the final)
            float champH = Math.Min(ch * 0.95f, 104 * u);
            var champRect = new RectangleF(x2, finalRect.Top - champH - 26 * u, cw, champH);
            DrawChampion(g, champRect, prefix, accent, u);

            // 3rd place (below the final)
            float thirdTop = finalRect.Bottom + 52 * u;
            using (var f = new Font(Theme.Font, 18 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, "3E PLACE", f, Theme.Muted, new RectangleF(x2, thirdTop - 30 * u, cw, 28 * u));
            DrawMatch(g, new RectangleF(x2, thirdTop, cw, ch), Get(prefix + "_3RD"), accent, u);
        }

        private BracketMatch Get(string slot)
        {
            BracketMatch m;
            return Matches.TryGetValue(slot, out m) ? m : new BracketMatch();
        }

        private static void Connect(Graphics g, Pen pen, float xFrom, float yFrom, float xTo, float yTo, float yFrom2, float colGap)
        {
            float mid = xFrom + colGap / 2f;
            g.DrawLine(pen, xFrom, yFrom, mid, yFrom);
            g.DrawLine(pen, xFrom, yFrom2, mid, yFrom2);
            g.DrawLine(pen, mid, yFrom, mid, yFrom2);
            g.DrawLine(pen, mid, yTo, xTo, yTo);
        }

        private static void DrawMatch(Graphics g, RectangleF r, BracketMatch m, Color accent, float u, bool isFinal = false)
        {
            Gfx.FillRound(g, r, 12 * u, Theme.CardTop, Theme.CardBottom);
            Gfx.StrokeRound(g, r, 12 * u, isFinal ? accent : Theme.CardBorder, (isFinal ? 3f : 2f) * u);

            float rowH = r.Height / 2f;
            bool decided = m.ScoreA != m.ScoreB;

            DrawRow(g, new RectangleF(r.Left, r.Top, r.Width, rowH), m.TeamA, m.ScoreA, decided && m.ScoreA > m.ScoreB, accent, u);

            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255), 1.5f * u))
                g.DrawLine(pen, r.Left + 10 * u, r.Top + rowH, r.Right - 10 * u, r.Top + rowH);

            DrawRow(g, new RectangleF(r.Left, r.Top + rowH, r.Width, rowH), m.TeamB, m.ScoreB, decided && m.ScoreB > m.ScoreA, accent, u);
        }

        private static void DrawRow(Graphics g, RectangleF row, string name, int score, bool winner, Color accent, float u)
        {
            float scoreW = row.Width * 0.24f;

            if (winner)
            {
                var hi = new RectangleF(row.Left + 3 * u, row.Top + 3 * u, row.Width - 6 * u, row.Height - 6 * u);
                Gfx.FillRound(g, hi, 9 * u, Color.FromArgb(48, 255, 255, 255));
                Gfx.FillRound(g, new RectangleF(hi.Left, hi.Top + 4 * u, 6 * u, hi.Height - 8 * u), 3 * u, accent);
            }

            bool placeholder = Gfx.IsPlaceholder(name);
            var nameRect = new RectangleF(row.Left + 16 * u, row.Top, row.Width - scoreW - 26 * u, row.Height);

            FontStyle style = placeholder ? FontStyle.Italic : FontStyle.Bold;
            Gfx.FitText(g, (name ?? "").ToUpper(), Theme.Font, style, row.Height * 0.46f, 9,
                placeholder ? Color.FromArgb(120, 160, 195) : Color.White, nameRect, StringAlignment.Near);

            using (var f = new Font(Theme.Font, row.Height * 0.62f, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, score.ToString(), f, winner ? Theme.GoldLight : Color.White,
                    new RectangleF(row.Right - scoreW - 6 * u, row.Top, scoreW, row.Height));
        }

        private void DrawChampion(Graphics g, RectangleF r, string prefix, Color accent, float u)
        {
            string name;
            Champions.TryGetValue(prefix, out name);
            bool empty = string.IsNullOrWhiteSpace(name);

            Gfx.FillRound(g, r, 14 * u, Color.FromArgb(235, 255, 214, 92), Color.FromArgb(235, 243, 167, 18));
            Gfx.StrokeRound(g, r, 14 * u, Color.FromArgb(255, 255, 240, 170), 2.5f * u);

            Gfx.Star(g, new PointF(r.Left + 26 * u, r.Top + r.Height / 2f), 15 * u, Color.FromArgb(10, 33, 66));
            Gfx.Star(g, new PointF(r.Right - 26 * u, r.Top + r.Height / 2f), 15 * u, Color.FromArgb(10, 33, 66));

            using (var f = new Font(Theme.Font, 15 * u, FontStyle.Bold, GraphicsUnit.Pixel))
                Gfx.Text(g, "VAINQUEUR", f, Color.FromArgb(10, 33, 66), new RectangleF(r.Left, r.Top + 8 * u, r.Width, 20 * u));

            Gfx.FitText(g, empty ? "?" : name.ToUpper(), Theme.Font, FontStyle.Bold, r.Height * 0.38f, 10,
                Color.FromArgb(10, 33, 66), new RectangleF(r.Left + 46 * u, r.Top + 28 * u, r.Width - 92 * u, r.Height - 34 * u));

            championRects[prefix] = r;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = HitChampion(e.Location) != null ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            string hit = HitChampion(e.Location);
            if (hit != null) ChampionClicked?.Invoke(hit);
        }

        private string HitChampion(Point p)
        {
            foreach (var pair in championRects)
            {
                if (pair.Value.Contains(p)) return pair.Key;
            }
            return null;
        }
    }

    // =====================================================================
    //  Pause screen: logo and an editable message
    // =====================================================================
    internal class PauseView : CardBase
    {
        public Image Logo;
        public string Message = "";

        public event Action Clicked;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Gfx.Prepare(g);

            float W = Width, H = Height;
            if (W < 100 || H < 100) return;

            float logoSize = Math.Min(H * 0.42f, W * 0.4f);
            float textH = H * 0.30f;
            float gap = H * 0.05f;
            float total = logoSize + gap + textH;
            float top = (H - total) / 2f;

            if (Logo != null)
            {
                float lw = logoSize * Logo.Width / Logo.Height;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Logo, new RectangleF((W - lw) / 2f, top, lw, logoSize));
            }

            var box = new RectangleF(W * 0.08f, top + logoSize + gap, W * 0.84f, textH);
            Gfx.FillRound(g, box, 26, Color.FromArgb(110, 0, 12, 36));
            Gfx.StrokeRound(g, box, 26, Color.FromArgb(200, Theme.Gold), 3);

            var inner = new RectangleF(box.Left + 30, box.Top + 10, box.Width - 60, box.Height - 20);
            using (Font f = Gfx.FitWrap(g, Message, Theme.Font, FontStyle.Bold, H * 0.09f, 18, inner.Width, inner.Height))
                Gfx.Text(g, Message, f, Color.White, inner, StringAlignment.Center, StringAlignment.Center, true);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Clicked?.Invoke();
        }
    }
}
