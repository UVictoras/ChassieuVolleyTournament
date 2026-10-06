namespace ChassieuVolleyTournament
{
    /// -----------------------------------------------
    /// Represents a generic phase in the tournament,
    /// holding references to current and next matches
    /// on three separate fields.
    /// -----------------------------------------------
    public class Phase
    {
        #region ---- Properties ----
        public Match CurrentMatchField1;
        public Match CurrentMatchField2;
        public Match CurrentMatchField3;

        public Match NextMatchField1;
        public Match NextMatchField2;
        public Match NextMatchField3;

        #endregion

        #region ---- Constructor ----
        public Phase() { }

        #endregion

        #region ---- Methods ----
        /// ---------------------------------------------------------
        /// True when referees may change the score of this match
        /// (i.e. the match is being played, or has been played).
        /// ---------------------------------------------------------
        public virtual bool IsMatchActive(Match match)
        {
            return true;
        }

        #endregion
    }
}
