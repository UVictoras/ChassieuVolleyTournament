#region ---- Includes ----
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
    #region ---- Structs ----
    /// ---------------------------------------------------
    /// Represents statistical data for a volleyball team.
    /// ---------------------------------------------------
    public struct TeamStatistics
    {
        #region ---- Properties ----

        public int TournamentPoints;
        public int ScoredPoints;
        public int TakenPoints;
        public int Difference;

        #endregion
    }

    #endregion

    #region ---- Classes ----
    /// ---------------------------------------------
    /// A custom Panel control with rounded corners.
    /// ---------------------------------------------
    public class RoundedPanel : Panel
    {
        #region ---- Methods ----
        /// -----------------------------------------------
        /// Overrides default painting to apply a rounded
        /// corner region to the panel.
        /// -----------------------------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(0, 0, 20, 20, 180, 90);
            path.AddArc(Width - 20, 0, 20, 20, 270, 90);
            path.AddArc(Width - 20, Height - 20, 20, 20, 0, 90);
            path.AddArc(0, Height - 20, 20, 20, 90, 90);
            path.CloseAllFigures();

            this.Region = new Region(path);
        }

        #endregion
    }

    /// ------------------------------------------------------------
    /// Countdown timer used for matches and warm-ups.
    /// A warm-up that ends must NOT count as a finished match:
    /// the two cases raise two different events.
    /// ------------------------------------------------------------
    public class Timer
    {
        #region ---- Properties ----
        private float currentTimer;
        private float matchTime;
        private float skirmishTime;

        private bool isTimerStarted;
        private bool isMatchTimer;

        /// <summary>Raised when a MATCH timer ends (naturally or when forced).</summary>
        public event EventHandler OnMatchEnd;

        /// <summary>Raised when a WARM-UP timer ends.</summary>
        public event EventHandler OnWarmupEnd;

        #endregion

        #region ---- Constructor ----
        public Timer()
        {
            this.currentTimer = 0.0f;
            this.matchTime = 900.0f;
            this.skirmishTime = 300.0f;
            this.isTimerStarted = false;
            this.isMatchTimer = false;
        }

        #endregion

        #region ---- Methods ---- 

        /// -----------------------------------------------
        /// Starts the timer using match or warm-up time.
        /// -----------------------------------------------
        public void StartTimer(bool isMatch)
        {
            currentTimer = isMatch ? matchTime : skirmishTime;
            isMatchTimer = isMatch;
            isTimerStarted = true;
            Debug.WriteLine($"[Timer] StartTimer -> {currentTimer}s (match: {isMatch})");
        }

        /// ----------------------------------------------------
        /// Decrements timer by a given time step and finishes
        /// it automatically when the time reaches zero.
        /// ----------------------------------------------------
        public void DecrementTimer(float time)
        {
            if (!isTimerStarted) return;

            currentTimer -= time;

            if (currentTimer <= 0.0f)
                StopTimer();
        }

        /// ----------------------------------------------------------
        /// Ends the running timer now and raises the matching event.
        /// Does nothing if no timer is running (this prevents a match
        /// from being counted twice).
        /// ----------------------------------------------------------
        public void StopTimer()
        {
            if (!isTimerStarted) return;

            bool wasMatch = isMatchTimer;

            currentTimer = 0.0f;
            isTimerStarted = false;
            isMatchTimer = false;

            if (wasMatch)
                OnMatchEnd?.Invoke(this, EventArgs.Empty);
            else
                OnWarmupEnd?.Invoke(this, EventArgs.Empty);
        }

        /// -------------------------------------------------
        /// Stops the timer WITHOUT raising any event
        /// (used to cancel a timer started by mistake).
        /// -------------------------------------------------
        public void CancelTimer()
        {
            currentTimer = 0.0f;
            isTimerStarted = false;
            isMatchTimer = false;
        }

        #endregion

        #region ---- Getters & Setters ----
        public float GetCurrentTime()
        {
            return currentTimer;
        }

        public bool GetTimerIsEnabled()
        {
            return isTimerStarted;
        }

        /// <summary>True while a match (not a warm-up) countdown is running.</summary>
        public bool IsMatchTimerRunning => isTimerStarted && isMatchTimer;

        #endregion
    }

    /// -----------------------------------------------------
    /// A dialog box that prompts the user to enter text.
    /// Includes OK and Cancel buttons for user interaction.
    /// -----------------------------------------------------
    public class TextInputDialog : Form
    {
        #region ---- Properties ----
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;

        /// ----------------------------------------------
        /// Gets the user-entered text from the textbox.
        /// ----------------------------------------------
        public string InputText => textBox.Text;

        #endregion

        #region ---- Constructor ----
        /// -------------------------------------------------------
        /// Initializes a new instance of the text input dialog
        /// with title, prompt message, and optional default text.
        /// -------------------------------------------------------
        public TextInputDialog(string title, string prompt, string defaultText = "")
        {
            Text = title;
            Size = new Size(400, 160);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                AutoSize = true
            };
            Controls.Add(lbl);

            textBox = new TextBox
            {
                Text = defaultText,
                Location = new Point(10, 40),
                Width = 360
            };
            Controls.Add(textBox);

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 80),
                Width = 75
            };
            Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Annuler",
                DialogResult = DialogResult.Cancel,
                Location = new Point(295, 80),
                Width = 75
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        #endregion
    }

    #endregion
}