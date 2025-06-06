using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    internal class Team
    {
        public string Name { get; set; }
        public TeamStatistics MorningStatistics { get; set; }
        public TeamStatistics LevelStatistics { get; set; }
        public TeamStatistics TreeStatistics { get; set; }

        public Team(string name)
        {
            this.Name = name; 
        }
    }
}
