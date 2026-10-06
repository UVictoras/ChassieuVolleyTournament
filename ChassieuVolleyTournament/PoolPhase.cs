using System;
using System.Linq;

namespace ChassieuVolleyTournament
{
    /// -------------------------------------------------------
    /// Represents the pool phase of the tournament,
    /// managing four pools and cycling matches across three
    /// fields (8 rounds x 3 fields = 24 matches).
    /// Standings are updated when a match is *committed*; committing
    /// is idempotent, so a match is never counted twice and a late
    /// score correction simply replaces the previous result.
    /// -------------------------------------------------------
    internal class PoolPhase : Phase
    {
        #region ---- Properties ----
        Pool[] pools;

        int[] field1MatchsIndexes;
        int[] field1PoolsIndexes;

        int[] field2MatchsIndexes;
        int[] field2PoolsIndexes;

        int[] field3MatchsIndexes;
        int[] field3PoolsIndexes;

        int round;

        public const int RoundCount = 8;

        /// <summary>Zero-based index of the round currently played.</summary>
        public int CurrentRound => round;

        /// <summary>True once the last round has ended.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>True if another round follows the current one.</summary>
        public bool HasNextRound => round + 1 < RoundCount;

        #endregion

        #region ---- Constructor ----

        public PoolPhase(Pool pool1, Pool pool2, Pool pool3, Pool pool4)
        {
            pools = new Pool[4];

            pools[0] = pool1;
            pools[1] = pool2;
            pools[2] = pool3;
            pools[3] = pool4;

            field1MatchsIndexes = new int[8] { 0, 0, 1, 2, 3, 3, 4, 5 };
            field1PoolsIndexes = new int[8] { 0, 3, 0, 0, 0, 3, 0, 0 };

            field2MatchsIndexes = new int[8] { 0, 1, 1, 2, 3, 4, 4, 5 };
            field2PoolsIndexes = new int[8] { 1, 1, 3, 1, 1, 1, 3, 1 };

            field3MatchsIndexes = new int[8] { 0, 1, 2, 2, 3, 4, 5, 5 };
            field3PoolsIndexes = new int[8] { 2, 2, 2, 3, 2, 2, 2, 3 };

            round = 0;
            AssignRound();
        }

        #endregion

        #region ---- Methods ----

        private Match MatchAt(int[] poolIdx, int[] matchIdx, int r)
        {
            return pools[poolIdx[r]].GetMatches()[matchIdx[r]];
        }

        /// ------------------------------------------------------------
        /// Sets current matches to the current round, and next matches
        /// to the following round (or to the current ones if none).
        /// ------------------------------------------------------------
        private void AssignRound()
        {
            CurrentMatchField1 = MatchAt(field1PoolsIndexes, field1MatchsIndexes, round);
            CurrentMatchField2 = MatchAt(field2PoolsIndexes, field2MatchsIndexes, round);
            CurrentMatchField3 = MatchAt(field3PoolsIndexes, field3MatchsIndexes, round);

            if (HasNextRound)
            {
                NextMatchField1 = MatchAt(field1PoolsIndexes, field1MatchsIndexes, round + 1);
                NextMatchField2 = MatchAt(field2PoolsIndexes, field2MatchsIndexes, round + 1);
                NextMatchField3 = MatchAt(field3PoolsIndexes, field3MatchsIndexes, round + 1);
            }
            else
            {
                NextMatchField1 = CurrentMatchField1;
                NextMatchField2 = CurrentMatchField2;
                NextMatchField3 = CurrentMatchField3;
            }
        }

        /// ------------------------------------------------------------
        /// Moves to the next round. After the last round, the phase is
        /// marked as finished (current matches stay displayed).
        /// ------------------------------------------------------------
        public void AdvanceRound()
        {
            if (HasNextRound)
            {
                round++;
                AssignRound();
            }
            else
            {
                IsFinished = true;
            }
        }

        /// -----------------------------------------------
        /// Adds the three current matches to the standings.
        /// -----------------------------------------------
        public void CommitCurrentMatches()
        {
            CommitMatch(CurrentMatchField1);
            CommitMatch(CurrentMatchField2);
            CommitMatch(CurrentMatchField3);
        }

        /// ---------------------------------------------------------------
        /// Adds one match to the standings. If the match was already
        /// committed, its previous result is removed first, so calling
        /// this twice (or after a score correction) is always safe.
        /// ---------------------------------------------------------------
        public void CommitMatch(Match match)
        {
            if (match == null) return;

            if (match.Committed)
                ApplyResult(match, -1, match.CommittedScore1, match.CommittedScore2);

            ApplyResult(match, +1, match.ScoreTeam1, match.ScoreTeam2);

            // A finished match is locked for referees (first time only: a match the
            // staff reopened for a correction stays open until the staff locks it again).
            if (!match.Committed) match.Locked = true;

            match.Committed = true;
            match.CommittedScore1 = match.ScoreTeam1;
            match.CommittedScore2 = match.ScoreTeam2;
        }

        /// -------------------------------------------------------------
        /// Adds (sign = +1) or removes (sign = -1) a result: tournament
        /// points (win 3 / loss 1 / draw 1 each), scored/taken points.
        /// -------------------------------------------------------------
        private static void ApplyResult(Match match, int sign, int score1, int score2)
        {
            int points1, points2;

            if (score1 > score2) { points1 = 3; points2 = 1; }
            else if (score1 < score2) { points1 = 1; points2 = 3; }
            else { points1 = 1; points2 = 1; }

            match.Team1.Statistics.TournamentPoints += sign * points1;
            match.Team2.Statistics.TournamentPoints += sign * points2;

            match.Team1.Statistics.ScoredPoints += sign * score1;
            match.Team1.Statistics.TakenPoints += sign * score2;
            match.Team1.Statistics.Difference = match.Team1.Statistics.ScoredPoints - match.Team1.Statistics.TakenPoints;

            match.Team2.Statistics.ScoredPoints += sign * score2;
            match.Team2.Statistics.TakenPoints += sign * score1;
            match.Team2.Statistics.Difference = match.Team2.Statistics.ScoredPoints - match.Team2.Statistics.TakenPoints;
        }

        /// -------------------------------------
        /// Re-sorts every pool by current stats.
        /// -------------------------------------
        public void UpdateAllRankings()
        {
            foreach (Pool pool in pools)
                pool.UpdateRanking();
        }

        /// ----------------------------------------------------------------
        /// Referees may change the score of a match that is on a field.
        /// Finished matches are locked (the staff can reopen them), and
        /// matches of later rounds are not open yet.
        /// ----------------------------------------------------------------
        public override bool IsMatchActive(Match match)
        {
            if (match == null) return false;

            if (match.Locked) return false;

            return match.Committed
                || match == CurrentMatchField1
                || match == CurrentMatchField2
                || match == CurrentMatchField3;
        }

        /// <summary>Field number (1-3) the match is currently played on, or 0.</summary>
        public int GetFieldOf(Match match)
        {
            if (match == null) return 0;
            if (match == CurrentMatchField1) return 1;
            if (match == CurrentMatchField2) return 2;
            if (match == CurrentMatchField3) return 3;
            return 0;
        }

        #endregion

        #region ---- Getters & Setters ----
        public Pool[] GetPools() => pools;

        #endregion
    }
}
