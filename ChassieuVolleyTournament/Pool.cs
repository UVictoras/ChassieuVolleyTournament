using System;
using System.Collections.Generic;
using System.Linq;

namespace ChassieuVolleyTournament
{
    /// ---------------------------------------------------
    /// Represents a pool of four teams in a tournament,
    /// managing matches and ranking based on performance.
    /// ---------------------------------------------------
    internal class Pool
    {
        #region ---- Properties ----
        /// Teams ordered by current ranking (index 0 = first).
        private List<Team> ranked;

        public List<Match> Matches { get; private set; }
        #endregion

        #region ---- Constructor ---- 
        public Pool(Team team1, Team team2, Team team3, Team team4)
        {
            ranked = new List<Team> { team1, team2, team3, team4 };
            Matches = new List<Match>();

            GenerateMatches();
        }

        #endregion

        #region ---- Methods ----
        /// ----------------------------------------------------
        /// Generates the 6 matches of the pool. The referee of a
        /// match is always one of the two teams NOT playing it
        /// (the old table had teams refereeing their own match).
        /// Referee duties: 2-2-1-1 over the four teams.
        /// ----------------------------------------------------
        private void GenerateMatches()
        {
            List<Team> t = ranked.ToList();

            Matches.Add(new Match(t[0], t[1], t[2]));
            Matches.Add(new Match(t[2], t[3], t[0]));
            Matches.Add(new Match(t[0], t[2], t[3]));
            Matches.Add(new Match(t[1], t[3], t[2]));
            Matches.Add(new Match(t[0], t[3], t[1]));
            Matches.Add(new Match(t[1], t[2], t[0]));
        }

        /// -----------------------------------------------------
        /// Sorts teams by tournament points, then point difference,
        /// then scored points. Ties keep their previous order.
        /// -----------------------------------------------------
        public void UpdateRanking()
        {
            ranked = ranked
                .OrderByDescending(t => t.Statistics.TournamentPoints)
                .ThenByDescending(t => t.Statistics.Difference)
                .ThenByDescending(t => t.Statistics.ScoredPoints)
                .ToList();
        }

        #endregion

        #region ---- Getters & Setters ----
        public List<Match> GetMatches() => Matches;

        /// Teams ordered by current ranking.
        public List<Team> GetTeams() => ranked.ToList();
        #endregion
    }
}
