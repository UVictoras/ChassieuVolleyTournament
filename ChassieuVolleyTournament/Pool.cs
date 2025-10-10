#region ---- Includes ----
using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace ChassieuVolleyTournament
{
    /// ---------------------------------------------------
    /// Represents a pool of four teams in a tournament,
    /// managing matches and ranking based on performance.
    /// ---------------------------------------------------
    internal class Pool
    {
        #region ---- Properties ----
        public Dictionary<string, Team> Ranking { get; set; }
        public List<Match> Matches { get; set; }
        #endregion

        #region ---- Constructor ---- 
        /// ----------------------------------------------------------
        /// Initializes a Pool with four teams, sets initial ranking,
        /// and generates all pool matches between teams.
        /// ----------------------------------------------------------
        public Pool(Team team1, Team team2, Team team3, Team team4)
        {
            Ranking = new Dictionary<string, Team>();
            Matches = new List<Match>();

            Ranking.Add("1er", team1);
            Ranking.Add("2ème", team2);
            Ranking.Add("3ème", team3);
            Ranking.Add("4ème", team4);

            GenerateMatches();
        }

        #endregion

        #region ---- Methods ----
        /// ----------------------------------------------------
        /// Generates matches for all pairings within the pool.
        /// ----------------------------------------------------
        private void GenerateMatches()
        {
            List<Team> teams = Ranking.Values.ToList();

            Matches.Add(new Match(teams[0], teams[1], teams[2]));
            Matches.Add(new Match(teams[2], teams[3], teams[0]));
            Matches.Add(new Match(teams[0], teams[2], teams[3]));
            Matches.Add(new Match(teams[1], teams[3], teams[1]));
            Matches.Add(new Match(teams[0], teams[3], teams[3]));
            Matches.Add(new Match(teams[1], teams[2], teams[2]));
        }

        /// -----------------------------------------------------
        /// Updates the ranking dictionary by sorting teams
        /// based on TournamentPoints and then point difference.
        /// -----------------------------------------------------
        public void UpdateRanking()
        {
            List<Team> teams = GetTeams();

            Ranking.Clear();

            List<Team> orderedTeams = teams
                .OrderByDescending(t => t.Statistics.TournamentPoints)
                .ThenByDescending(t => t.Statistics.Difference)
                .ToList();

            Ranking["1er"] = orderedTeams[0];
            Ranking["2ème"] = orderedTeams[1];
            Ranking["3ème"] = orderedTeams[2];
            Ranking["4ème"] = orderedTeams[3];
        }

        #endregion

        #region ---- Getters & Setters ----
        public List<Match> GetMatches() => Matches;
        public List<Team> GetTeams() => Ranking.Values.ToList();

        #endregion
    }
}
