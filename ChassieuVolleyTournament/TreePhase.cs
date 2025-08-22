#region ----Includes ----
using System.Collections.Generic;
using System.Linq;

#endregion

namespace ChassieuVolleyTournament
{
    /// ----------------------------------------------------
    /// Represents the elimination phase of the tournament,
    /// including both the principal bracket and the
    /// consolation bracket. Generates matches and displays
    /// them on the display window.
    /// ----------------------------------------------------
    internal class TreePhase : Phase
    {
        #region ---- Properties ----

        private List<Match> matchesPrincipal;
        private List<Match> matchesConsolante;

        /// --------------------------------------------------
        /// Placeholder teams used for initial empty matches.
        /// --------------------------------------------------
        Team placeHolder1;
        Team placeHolder2;

        #endregion

        #region ---- Constructor ----

        /// -----------------------------------------------------
        /// Initializes a new instance of the TreePhase class
        /// and sets up placeholder teams and empty match lists.
        /// -----------------------------------------------------
        public TreePhase()
        {
            matchesPrincipal = new List<Match>();
            matchesConsolante = new List<Match>();

            placeHolder1 = new Team("Équipe 1");
            placeHolder2 = new Team("Équipe 2");
        }

        #endregion

        #region ---- Methods ----

        /// -----------------------------------------------------------
        /// Generates all matches for the tree phase based on
        /// the provided pools for principal and consolation brackets.
        /// Also displays the initial matches on the display window.
        /// -----------------------------------------------------------
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

                tempMatch = new Match(placeHolder1, placeHolder2);

                matchesConsolante.Add(tempMatch);

                tempMatch = new Match(placeHolder1, placeHolder2);
            }

            DisplayMatchesOnWindow();
        }

        /// ------------------------------------------------
        /// Updates the display window to show all matches
        /// in both the principal and consolation brackets.
        /// ------------------------------------------------
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

        #region ---- Getters & Setters ----

        /// -------------------------------------------------
        /// Returns the match corresponding to the given key
        /// in either the principal or consolation bracket.
        /// -------------------------------------------------
        public Match GetMatchByKey(string key)
        {
            if (key.StartsWith("P_QF"))
                return matchesPrincipal[int.Parse(key.Substring(4)) - 1];
            if (key.StartsWith("C_QF"))
                return matchesConsolante[int.Parse(key.Substring(4)) - 1];

            if (key.StartsWith("P_SF"))
                return matchesPrincipal[3 + int.Parse(key.Substring(4))];
            if (key.StartsWith("C_SF"))
                return matchesConsolante[3 + int.Parse(key.Substring(4))];

            if (key == "P_FINAL") return matchesPrincipal[6];
            if (key == "C_FINAL") return matchesConsolante[6];
            if (key == "P_3RD") return matchesPrincipal[7];
            if (key == "C_3RD") return matchesConsolante[7];

            return null;
        }

        public List<Match> MatchsPrincipal => matchesPrincipal;
        public List<Match> MatchsConsolant => matchesConsolante;

        /// ----------------------------------------------------------------
        /// Returns all matches (principal + consolation) as a single list.
        /// ----------------------------------------------------------------
        public List<Match> GetMatches() => matchesPrincipal.Concat(matchesConsolante).ToList();

        #endregion
    }
}
