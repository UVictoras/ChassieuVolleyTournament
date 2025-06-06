using System;

namespace ChassieuVolleyTournament
{
    internal class Match
    {
        public string Key { get; set; }
        public string Team1 { get; set; }
        public string Team2 { get; set; }
        public int ScoreTeam1 { get; set; }
        public int ScoreTeam2 { get; set; }

        public Match(string team1, string team2)
        {
            Team1 = team1;
            Team2 = team2;
            ScoreTeam1 = 0;
            ScoreTeam2 = 0;
        }
    }
}
