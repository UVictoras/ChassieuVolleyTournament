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

            Ranking.Add("1er", Team1);
            Ranking.Add("2eme", Team2);
            Ranking.Add("3eme", Team3);
            Ranking.Add("4eme", Team4);

            GenerateMatches();
        }

        private void GenerateMatches()
        {
            List<Team> Teams = Ranking.Values.ToList();

            string Team1 = Teams[0].Name;
            string Team2 = Teams[1].Name;
            string Team3 = Teams[2].Name;
            string Team4 = Teams[3].Name;

            Matches.Add(new Match(Team1, Team2));
            Matches.Add(new Match(Team3, Team4));
            Matches.Add(new Match(Team1, Team3));
            Matches.Add(new Match(Team2, Team4));
            Matches.Add(new Match(Team1, Team4));
            Matches.Add(new Match(Team2, Team3));
        }

        public void UpdateRanking()
        {

        }
    }
}
