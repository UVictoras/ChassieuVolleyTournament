namespace ChassieuVolleyTournament
{
    /// --------------------------------------------------
    /// Represents a match between two teams with scores,
    /// unique key, and optional referee assignment.
    /// --------------------------------------------------
    public class Match
    {
        #region ---- Properties ----
        public string Key { get; set; }
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public Team RefereeTeam { get; set; }
        public int ScoreTeam1 { get; set; }
        public int ScoreTeam2 { get; set; }
        #endregion

        #region ---- Constructors ----
        /// ------------------------------------------------------------
        /// Initializes a match with two teams, generates a unique key,
        /// and registers the key in the tournament.
        /// ------------------------------------------------------------
        public Match(Team team1, Team team2)
        {
            Team1 = team1;
            Team2 = team2;
            ScoreTeam1 = 0;
            ScoreTeam2 = 0;

            Key = PrivateKeyGenerator.Instance.GenerateKey();
            Tournament.Instance.AddValidKey(Key);
        }

        public Match(Team team1, Team team2, Team refereeTeam)
        {
            Team1 = team1;
            Team2 = team2;
            ScoreTeam1 = 0;
            ScoreTeam2 = 0;
            RefereeTeam = refereeTeam;

            Key = PrivateKeyGenerator.Instance.GenerateKey();
            Tournament.Instance.AddValidKey(Key);
        }

        #endregion

        #region ---- Getters & Setters ----
        public string GetTeam1Name() => Team1.Name;
        public string GetTeam2Name() => Team2.Name;
        public string GetRefereeName() => RefereeTeam?.Name ?? string.Empty;
        public string GetScoreTeamOne() => ScoreTeam1.ToString();
        public string GetScoreTeamTwo() => ScoreTeam2.ToString();
        public string GetMatchKey() => Key;
        public int Team1Score => ScoreTeam1;
        public int Team2Score => ScoreTeam2;
        public Team[] GetTeams() => new Team[] { Team1, Team2 };
        public string[] GetTeamsNames() => new string[] { Team1.Name, Team2.Name };

        /// --------------------------------------------------
        /// Sets the scores for both teams given their names.
        /// Assigns scores correctly regardless of order.
        /// --------------------------------------------------
        public void SetScores(string team1Name, string team2Name, int score1, int score2)
        {
            if (team1Name == Team1.Name)
            {
                ScoreTeam1 = score1;
                ScoreTeam2 = score2;
            }
            else
            {
                ScoreTeam1 = score2;
                ScoreTeam2 = score1;
            }
        }
        #endregion
    }
}
