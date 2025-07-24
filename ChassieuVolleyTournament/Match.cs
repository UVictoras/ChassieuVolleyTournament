using System;

namespace ChassieuVolleyTournament
{
    public class Match
    {
        public string Key { get; set; }
        public Team Team1 { get; set; }
        public Team Team2 { get; set; }
        public Team RefereeTeam { get; set; }
        public int ScoreTeam1 { get; set; }
        public int ScoreTeam2 { get; set; }

        public Match(Team team1, Team team2)
        {
            Team1 = team1;
            Team2 = team2;
            ScoreTeam1 = 0;
            ScoreTeam2 = 0;

            Key = PrivateKeyGenerator.Instance.GenerateKey();
            Tournament.Instance.AddValidKey(Key);
        }

        public string GetTeam1Name() => Team1.Name;
        public string GetTeam2Name() => Team2.Name;
        public string GetRefereeName() => RefereeTeam.Name;
        public string GetScoreTeamOne() => ScoreTeam1.ToString();
        public string GetScoreTeamTwo() => ScoreTeam2.ToString();
        public string GetMatchKey() => Key;

        public void SetScores(string Team1Name, string Team2Name, int Score1, int Score2)
        {
            if (Team1Name == Team1.Name)
            {
                ScoreTeam1 = Score1;
                ScoreTeam2 = Score2;
            }
            else
            {
                ScoreTeam1 = Score2;
                ScoreTeam2 = Score1;
            }
        }
    }
}
