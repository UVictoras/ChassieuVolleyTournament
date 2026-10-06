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

        /// <summary>True once this match has been added to the standings.</summary>
        public bool Committed { get; internal set; }

        /// <summary>
        /// When true, referees (web page) can no longer change the score.
        /// Set automatically when a pool match ends; the staff can reopen it.
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>Score that was added to the standings (to undo it if the score is corrected later).</summary>
        internal int CommittedScore1 { get; set; }
        internal int CommittedScore2 { get; set; }
        #endregion

        #region ---- Constructors ----
        public Match(Team team1, Team team2) : this(team1, team2, null)
        {
        }

        /// ------------------------------------------------------------
        /// Initializes a match with two teams and an optional referee,
        /// generates a unique key and registers it in the tournament.
        /// ------------------------------------------------------------
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
        /// Sets both scores by position (team 1 / team 2).
        /// (The old version matched on team *names*, which broke
        /// as soon as names differed or were edited.)
        /// --------------------------------------------------
        public void SetScores(int score1, int score2)
        {
            ScoreTeam1 = score1;
            ScoreTeam2 = score2;
        }
        #endregion
    }
}
