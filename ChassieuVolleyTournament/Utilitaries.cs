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
    /// Custom timer logic for match and skirmish durations,
    /// supporting start, decrement, stop, and event notifications.
    /// ------------------------------------------------------------
    public class Timer
    {
        #region ---- Properties ----
        private float currentTimer;
        private float matchTime;
        private float skirmishTime;

        private bool isTimerStarted;

        public event EventHandler OnTimerStop;

        #endregion

        #region ---- Constructor ----
        /// -------------------------------------------------------------
        /// Initializes timer with default match and skirmish durations.
        /// -------------------------------------------------------------
        public Timer()
        {
            this.currentTimer = 0.0f;
            this.matchTime = 900.0f;
            this.skirmishTime = 300.0f;
            this.isTimerStarted = false;
        }

        #endregion

        #region ---- Methods ---- 

        /// -----------------------------------------------
        /// Starts the timer using match or skirmish time.
        /// -----------------------------------------------
        public void StartTimer(bool isMatch)
        {
            currentTimer = isMatch ? matchTime : skirmishTime;
            isTimerStarted = true;
            Debug.WriteLine($"[Timer] StartTimer -> {currentTimer}s");
        }

        /// ----------------------------------------------------
        /// Decrements timer by a given time step.
        /// Stops the timer automatically if time reaches zero.
        /// ----------------------------------------------------
        public void DecrementTimer(float time)
        {
            if (!isTimerStarted) return;

            currentTimer -= time;

            if (currentTimer <= 0.0f)
                StopTimer();
        }

        /// ------------------------------------------------
        /// Stops the timer and triggers OnTimerStop event.
        /// Invokes on correct thread for UI controls.
        /// ------------------------------------------------
        public void StopTimer()
        {
            currentTimer = 0.0f;
            isTimerStarted = false;

            if (OnTimerStop != null)
            {
                foreach (Delegate d in OnTimerStop.GetInvocationList())
                {
                    var handler = (EventHandler)d;
                    Debug.WriteLine($"[Timer] Invoking {handler.Method.Name} on {handler.Target?.GetType().FullName}");

                    if (handler.Target is Control ctrl && ctrl.InvokeRequired)
                    {
                        ctrl.BeginInvoke(handler, this, EventArgs.Empty);
                    }
                    else
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }
        }

        #endregion

        #region ---- Getters & Setters ----
        /// --------------------------------------------
        /// Returns the current timer value in seconds.
        /// --------------------------------------------
        public float GetCurrentTime()
        {
            return currentTimer;
        }

        /// -----------------------------------------------
        /// Returns whether the timer is currently active.
        /// -----------------------------------------------
        public bool GetTimerIsEnabled()
        {
            return isTimerStarted;
        }

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
