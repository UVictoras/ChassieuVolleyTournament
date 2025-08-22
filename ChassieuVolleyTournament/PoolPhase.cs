#region ---- Includes ----
using System;
using System.Linq;
using System.Windows.Forms;

#endregion

namespace ChassieuVolleyTournament
{
    /// -------------------------------------------------------
    /// Represents the pool phase of the tournament,
    /// managing multiple pools and cycling matches
    /// across three fields. Updates team scores and rankings.
    /// -------------------------------------------------------
    internal class PoolPhase : Phase
    {
        #region ---- Properties ----
        Pool[] pools;

        int[] field1MatchsIndexes;
        int[] field1PoolsIndexes;

        int[] field2MatchsIndexes;
        int[] field2PoolsIndexes;

        int[] field3MatchsIndexes;
        int[] field3PoolsIndexes;

        int currentIndex;

        #endregion

        #region ---- Constructor ----

        /// -------------------------------------------------------
        /// Initializes a new PoolPhase with the given four pools,
        /// sets up match/pool indexing for each field, and
        /// initializes the first set of matches.
        /// -------------------------------------------------------
        public PoolPhase(Pool pool1, Pool pool2, Pool pool3, Pool pool4)
        {
            pools = new Pool[4];

            pools[0] = pool1;
            pools[1] = pool2;
            pools[2] = pool3;
            pools[3] = pool4;

            field1MatchsIndexes = new int[8] { 0, 0, 1, 2, 3, 3, 4, 5 };
            field1PoolsIndexes = new int[8] { 0, 3, 0, 0, 0, 3, 0, 0 };

            field2MatchsIndexes = new int[8] { 0, 1, 1, 2, 3, 4, 4, 5 };
            field2PoolsIndexes = new int[8] { 1, 1, 3, 1, 1, 1, 3, 1 };

            field3MatchsIndexes = new int[8] { 0, 1, 2, 2, 3, 4, 5, 5 };
            field3PoolsIndexes = new int[8] { 2, 2, 2, 3, 2, 2, 2, 3 };

            currentIndex = 0;

            CycleMatches(null, EventArgs.Empty);
        }

        #endregion

        #region ---- Methods ----
        /// ------------------------------------------------------------
        /// Advances the current matches on each field to the next
        /// scheduled match in the pools, and updates the next matches.
        /// Handles UI thread invocation if necessary.
        /// ------------------------------------------------------------
        public void CycleMatches(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => CycleMatches(sender, e)));
                    return;
                }
            }

            if (currentIndex >= 8)
                return;

            CurrentMatchField1 = pools[field1PoolsIndexes[currentIndex]].GetMatches()[field1MatchsIndexes[currentIndex]];
            CurrentMatchField2 = pools[field2PoolsIndexes[currentIndex]].GetMatches()[field2MatchsIndexes[currentIndex]];
            CurrentMatchField3 = pools[field3PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];

            currentIndex++;

            if (currentIndex >= 8)
                return;

            NextMatchField1 = pools[field1PoolsIndexes[currentIndex]].GetMatches()[field1MatchsIndexes[currentIndex]];
            NextMatchField2 = pools[field2PoolsIndexes[currentIndex]].GetMatches()[field2MatchsIndexes[currentIndex]];
            NextMatchField3 = pools[field3PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];
        }

        /// ------------------------------------------------------
        /// Updates scores and statistics for the current matches
        /// when the timer stops. Also refreshes rankings in the
        /// display and staff windows.
        /// ------------------------------------------------------
        public void IncrementTeamsScores(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => IncrementTeamsScores(sender, e)));
                    return;
                }
            }

            var display = Tournament.Instance.GetDisplayWindow();
            var staffWindow = Tournament.Instance.GetStaffWindow(); 

            if (display != null && display.InvokeRequired)
            {
                display.Invoke((MethodInvoker)(() => IncrementTeamsScores(sender, e)));
                return;
            }

            if (staffWindow != null && staffWindow.InvokeRequired)
            {
                staffWindow.Invoke((MethodInvoker)(() => IncrementTeamsScores(sender, e)));
                return;
            }

            UpdateMatchPoints(CurrentMatchField1);
            UpdateTeamStats(CurrentMatchField1);

            UpdateMatchPoints(CurrentMatchField2);
            UpdateTeamStats(CurrentMatchField2);

            UpdateMatchPoints(CurrentMatchField3);
            UpdateTeamStats(CurrentMatchField3);

            if (display == null)
                return;

            for (int poolIndex = 0; poolIndex < pools.Length; poolIndex++)
            {
                var pool = pools[poolIndex];

                pool.UpdateRanking();

                string[] teamNames = new string[4];
                string[] teamDiffs = new string[4];
                string[] teamPoints = new string[4];
                int[] teamScoredPoints = new int[4];
                int[] teamTakenPoints = new int[4];

                var orderedTeams = pool.Ranking.Values.ToList();

                for (int i = 0; i < orderedTeams.Count; i++)
                {
                    var team = orderedTeams[i];
                    teamNames[i] = team.Name;
                    teamDiffs[i] = team.Statistics.Difference >= 0
                        ? $"+{team.Statistics.Difference}"
                        : team.Statistics.Difference.ToString();
                    teamPoints[i] = team.Statistics.TournamentPoints.ToString();
                    teamScoredPoints[i] = team.Statistics.ScoredPoints;
                    teamTakenPoints[i] = team.Statistics.TakenPoints;
                }

                display.SetRankingText(poolIndex, teamNames, teamDiffs, teamPoints);
   
                if (staffWindow != null)
                {
                    staffWindow.SetRankingText(poolIndex, teamNames, teamDiffs, teamPoints, teamScoredPoints, teamTakenPoints);
                }
            }

            display.Update();

            if (staffWindow != null)
            {
                staffWindow.Update();
            }
        }

        /// -----------------------------------------------------------
        /// Updates tournament points for a match based on the scores.
        /// -----------------------------------------------------------
        private void UpdateMatchPoints(Match match)
        {
            if (match.Team1Score > match.Team2Score)
            {
                match.Team1.Statistics.TournamentPoints += 3;
                match.Team2.Statistics.TournamentPoints += 1;
            }
            else if (match.Team1Score < match.Team2Score)
            {
                match.Team1.Statistics.TournamentPoints += 1;
                match.Team2.Statistics.TournamentPoints += 3;
            }
            else
            {
                match.Team1.Statistics.TournamentPoints += 1;
                match.Team2.Statistics.TournamentPoints += 1;
            }
        }

        /// ----------------------------------------------------
        /// Updates scored points, taken points, and difference
        /// for both teams in a match.
        /// ----------------------------------------------------
        private void UpdateTeamStats(Match match)
        {
            match.Team1.Statistics.ScoredPoints += match.Team1Score;
            match.Team1.Statistics.TakenPoints += match.Team2Score;
            match.Team1.Statistics.Difference = match.Team1.Statistics.ScoredPoints - match.Team1.Statistics.TakenPoints;

            match.Team2.Statistics.ScoredPoints += match.Team2Score;
            match.Team2.Statistics.TakenPoints += match.Team1Score;
            match.Team2.Statistics.Difference = match.Team2.Statistics.ScoredPoints - match.Team2.Statistics.TakenPoints;
        }

        #endregion

        #region ---- Getters & Setters ----
        public Pool[] GetPools() => pools;

        #endregion
    }
}
