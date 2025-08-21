using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    internal class TreePhase : Phase
    {
        #region ---- Properties ----

        List<Match> matchesPrincipal;
        List<Match> matchesConsolante;

        Team placeHolder1;
        Team placeHolder2;

        #endregion

        #region ---- Constructor ----

        public TreePhase()
        {
            matchesPrincipal = new List<Match>();
            matchesConsolante = new List<Match>();

            placeHolder1 = new Team("Équipe 1");
            placeHolder2 = new Team("Équipe 2");
        }

        #endregion

        #region ---- Methods ----

        public void GenerateMatches(Pool pool1, Pool pool2, Pool pool3, Pool pool4)
        {
            for (int i = 0; i < 4; i++)
            {
                matchesPrincipal.Add(new Match(pool1.GetTeams()[i], pool2.GetTeams()[3 - i]));
                matchesConsolante.Add(new Match(pool3.GetTeams()[i], pool4.GetTeams()[3 - i]));
            }

            Match tempMatch = new Match(placeHolder1, placeHolder2);

            for (int i = 0; i < 4; i++)
            {
                matchesPrincipal.Add(tempMatch);
                matchesConsolante.Add(tempMatch);
            }

            DisplayMatchesOnWindow();
        }

        private void DisplayMatchesOnWindow()
        {
            var display = Tournament.Instance.GetDisplayWindow();

            if (display == null) return;
            
            for (int i = 0; i < 4; i++)
            {
                display.UpdateBracketBlock("P_QF" + (i + 1).ToString(), matchesPrincipal[i].Team1.Name, matchesPrincipal[i].ScoreTeam1, matchesPrincipal[i].ScoreTeam2, matchesPrincipal[i].Team2.Name);
                display.UpdateBracketBlock("C_QF" + (i + 1).ToString(), matchesConsolante[i].Team1.Name, matchesConsolante[i].ScoreTeam1, matchesConsolante[i].ScoreTeam2, matchesConsolante[i].Team2.Name);
            }

            for (int i = 4; i < 6; i++)
            {
                display.UpdateBracketBlock("P_SF" + (i - 3).ToString(), matchesPrincipal[i].Team1.Name, matchesPrincipal[i].ScoreTeam1, matchesPrincipal[i].ScoreTeam2, matchesPrincipal[i].Team2.Name);
                display.UpdateBracketBlock("C_SF" + (i - 3).ToString(), matchesConsolante[i].Team1.Name, matchesConsolante[i].ScoreTeam1, matchesConsolante[i].ScoreTeam2, matchesConsolante[i].Team2.Name);
            }

            display.UpdateBracketBlock("P_FINAL", matchesPrincipal[6].Team1.Name, matchesPrincipal[6].ScoreTeam1, matchesPrincipal[6].ScoreTeam2, matchesPrincipal[6].Team2.Name);
            display.UpdateBracketBlock("C_FINAL", matchesConsolante[6].Team1.Name, matchesConsolante[6].ScoreTeam1, matchesConsolante[6].ScoreTeam2, matchesConsolante[6].Team2.Name);

            display.UpdateBracketBlock("P_3RD", matchesPrincipal[7].Team1.Name, matchesPrincipal[7].ScoreTeam1, matchesPrincipal[7].ScoreTeam2, matchesPrincipal[7].Team2.Name);
            display.UpdateBracketBlock("C_3RD", matchesConsolante[7].Team1.Name, matchesConsolante[7].ScoreTeam1, matchesConsolante[7].ScoreTeam2, matchesConsolante[7].Team2.Name);

            display.Update();
        }

        #endregion
    }
}
