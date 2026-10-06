using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Every bracket slot id, in display order.</summary>
        public static readonly string[] Slots =
        {
            "P_QF1", "P_QF2", "P_QF3", "P_QF4",
            "C_QF1", "C_QF2", "C_QF3", "C_QF4",
            "P_SF1", "P_SF2",
            "C_SF1", "C_SF2",
            "P_FINAL", "C_FINAL",
            "P_3RD", "C_3RD"
        };

        #endregion

        #region ---- Constructor ----

        public TreePhase()
        {
            matchesPrincipal = new List<Match>();
            matchesConsolante = new List<Match>();
        }

        #endregion

        #region ---- Methods ----

        /// -----------------------------------------------------------
        /// Generates all matches for the tree phase from the four level
        /// pools (rankings must be up to date). Quarter-finals are real
        /// matches; semi-finals, finals and 3rd place matches get their
        /// own placeholder teams, so renaming one slot never renames
        /// the others (the old code shared two Team objects between
        /// all of them).
        /// -----------------------------------------------------------
        public void GenerateMatches(Pool pool1, Pool pool2, Pool pool3, Pool pool4)
        {
            for (int i = 0; i < 4; i++)
            {
                matchesPrincipal.Add(new Match(pool1.GetTeams()[i], pool2.GetTeams()[3 - i]));
                matchesConsolante.Add(new Match(pool3.GetTeams()[i], pool4.GetTeams()[3 - i]));
            }

            // SF1, SF2, FINAL, 3RD for each bracket
            for (int i = 0; i < 4; i++)
            {
                matchesPrincipal.Add(new Match(new Team("Équipe 1"), new Team("Équipe 2")));
                matchesConsolante.Add(new Match(new Team("Équipe 1"), new Team("Équipe 2")));
            }

            DisplayMatchesOnWindow();
        }

        /// ------------------------------------------------
        /// Updates the display window to show all matches
        /// in both the principal and consolation brackets.
        /// ------------------------------------------------
        public void DisplayMatchesOnWindow()
        {
            var display = Tournament.Instance.GetDisplayWindow();

            if (display == null) return;

            foreach (string slot in Slots)
            {
                Match m = GetMatchBySlot(slot);
                if (m == null) continue;

                display.UpdateBracketBlock(slot, m.Team1.Name, m.ScoreTeam1, m.ScoreTeam2, m.Team2.Name);
            }

            display.Update();
        }

        /// <summary>Referees can score every bracket match that the staff has not locked.</summary>
        public override bool IsMatchActive(Match match)
        {
            return match != null && !match.Locked;
        }

        /// <summary>Human readable name of a bracket match, e.g. "Quart de finale (principale)".</summary>
        public string DescribeMatch(Match match)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (GetMatchBySlot(Slots[i]) != match) continue;

                string slot = Slots[i];
                string bracket = slot.StartsWith("P_") ? "principale" : "consolante";
                string kind = slot.Substring(2);

                if (kind.StartsWith("QF")) return "Quart de finale " + kind.Substring(2) + " (" + bracket + ")";
                if (kind.StartsWith("SF")) return "Demi-finale " + kind.Substring(2) + " (" + bracket + ")";
                if (kind == "FINAL") return "Finale (" + bracket + ")";
                if (kind == "3RD") return "Match 3ème place (" + bracket + ")";
            }
            return "Match";
        }

        #endregion

        #region ---- Getters & Setters ----

        /// -------------------------------------------------
        /// Returns the match in a bracket slot ("P_QF1", "C_FINAL"...)
        /// or null when the slot id is unknown.
        /// -------------------------------------------------
        public Match GetMatchBySlot(string slot)
        {
            if (string.IsNullOrEmpty(slot) || slot.Length < 4) return null;

            List<Match> list = slot.StartsWith("P_") ? matchesPrincipal
                             : slot.StartsWith("C_") ? matchesConsolante
                             : null;
            if (list == null || list.Count < 8) return null;

            string kind = slot.Substring(2);
            int n;

            if (kind.StartsWith("QF") && int.TryParse(kind.Substring(2), out n) && n >= 1 && n <= 4)
                return list[n - 1];
            if (kind.StartsWith("SF") && int.TryParse(kind.Substring(2), out n) && n >= 1 && n <= 2)
                return list[3 + n];
            if (kind == "FINAL") return list[6];
            if (kind == "3RD") return list[7];

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
