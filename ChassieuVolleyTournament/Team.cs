namespace ChassieuVolleyTournament
{
    /// ---------------------------------------------
    /// Represents a volleyball team with a name and
    /// associated statistical data.
    /// ---------------------------------------------
    public class Team
    {
        #region ---- Properties ---- 
        public string Name { get; set; }
        public TeamStatistics Statistics;

        #endregion

        #region ---- Constructor ----
        /// ----------------------------------------------------
        /// Initializes a new Team instance with the given name
        /// and resets its statistics.
        /// ----------------------------------------------------
        public Team(string name)
        {
            Name = name;
            Statistics = new TeamStatistics();
            InitializeStatistics();
        }

        #endregion

        #region ---- Methods ---- 
        /// -----------------------------------------
        /// Resets all statistical counters to zero.
        /// -----------------------------------------
        private void InitializeStatistics()
        {
            Statistics.TournamentPoints = 0;
            Statistics.ScoredPoints = 0;
            Statistics.TakenPoints = 0;
            Statistics.Difference = 0;
        }

        #endregion
    }
}
