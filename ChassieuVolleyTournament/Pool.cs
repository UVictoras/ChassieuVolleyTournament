using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    internal class Pool
    {
        public Dictionary<String, Team> Ranking {  get; set; }
        public List<Match> Matches { get; set; }

        public Pool (Team Team1, Team Team2, Team Team3, Team Team4)
        {
            Ranking = new Dictionary<String, Team> ();
            Matches = new List<Match> ();

            Ranking.Add("1er", Team1);
            Ranking.Add("2eme", Team2);
            Ranking.Add("3eme", Team3);
            Ranking.Add("4eme", Team4);

            GenerateMatches();
        }

        private void GenerateMatches()
        {
            List<Team> Teams = Ranking.Values.ToList();

            Matches.Add(new Match(Teams[0], Teams[1]));
            Matches.Add(new Match(Teams[2], Teams[3]));
            Matches.Add(new Match(Teams[0], Teams[2]));
            Matches.Add(new Match(Teams[1], Teams[3]));
            Matches.Add(new Match(Teams[0], Teams[3]));
            Matches.Add(new Match(Teams[1], Teams[2]));
        }

        public void UpdateRanking()
        {
            List<Team> teams = Ranking.Values.ToList();

            Ranking.Clear();

            List<Team> orderedTeams = teams
                .OrderByDescending(t => t.LevelStatistics.TournamentPoints)
                .ThenByDescending(t => t.LevelStatistics.Difference)
                .ToList();

            bool allEmpty = orderedTeams.All(t =>
                t.LevelStatistics.TournamentPoints == 0 &&
                t.LevelStatistics.Difference == 0);

            if (allEmpty)
            {
                foreach (Team team in orderedTeams)
                {
                    Console.WriteLine($"{team.Name}: Non Classé");
                }
            }
            else
            {
                Ranking["1er"] = orderedTeams[0];
                Ranking["2ème"] = orderedTeams[1];
                Ranking["3ème"] = orderedTeams[2];
                Ranking["4ème"] = orderedTeams[3];
            }
        }

        public List<Match> GetMatches() => Matches;
    }
}
