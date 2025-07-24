using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChassieuVolleyTournament
{
    public struct TeamStatistics
    {
        public int TournamentPoints;
        public int ScoredPoints;
        public int TakenPoints;
        public int Difference;
    }

    public class RoundedPanel : Panel
    {
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
    }

    public class Timer
    {
        private float currentTimer;
        private float matchTime;
        private float skirmishTime;

        private bool isTimerStarted;

        public event EventHandler OnTimerStop;

        public Timer()
        {
            this.currentTimer = 0.0f;
            this.matchTime = 900.0f;
            this.skirmishTime = 300.0f;

            this.isTimerStarted = false;
        }

        public void StartTimer(bool isMatch)
        { 
            currentTimer = isMatch ? matchTime : skirmishTime;
            isTimerStarted = true;
        }

        public void DecrementTimer(float time)
        {
            currentTimer -= time;

            if (currentTimer <= 0.0f) 
                StopTimer();
        }

        public void StopTimer()
        {
            currentTimer = 0.0f;
            isTimerStarted = false;

            EventArgs e = new EventArgs();
            OnTimerStop?.Invoke(this, e);
        }

        public float GetCurrentTime()
        {
            return currentTimer;
        }

        public bool GetTimerIsEnabled()
        {
            return isTimerStarted;
        }
    }
}
