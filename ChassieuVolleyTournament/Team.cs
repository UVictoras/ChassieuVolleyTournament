using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    public class Team
    {
        public string Name { get; set; }
        public TeamStatistics MorningStatistics;
        public TeamStatistics LevelStatistics;
        public TeamStatistics TreeStatistics; 

        public Team(string name)
        {
            this.Name = name;

            this.MorningStatistics = new TeamStatistics();
            this.LevelStatistics = new TeamStatistics();
            this.TreeStatistics = new TeamStatistics();

            InitializeStatistics();
        }

        private void InitializeStatistics()
        {
            this.MorningStatistics.TournamentPoints = 0;
            this.MorningStatistics.ScoredPoints = 0;
            this.MorningStatistics.TakenPoints = 0;
            this.MorningStatistics.Difference = 0;

            this.LevelStatistics.TournamentPoints = 0;
            this.LevelStatistics.ScoredPoints = 0;
            this.LevelStatistics.TakenPoints = 0;
            this.LevelStatistics.Difference = 0;

            this.TreeStatistics.TournamentPoints = 0;
            this.TreeStatistics.ScoredPoints = 0;
            this.TreeStatistics.TakenPoints = 0;
            this.TreeStatistics.Difference = 0;
        }
    }
}
