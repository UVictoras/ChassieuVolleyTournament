using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ChassieuVolleyTournament
{
    /// -----------------------------------------------------------
    /// Singleton class representing the Volleyball Tournament.
    /// Manages all tournament phases, teams, matches, and timers.
    /// Responsible for updating the display and staff windows.
    /// 
    /// Threading: everything here runs on the UI thread (the timer
    /// ticks on a WinForms timer, and the web server marshals its
    /// calls with Invoke). Windows may be null (headless tests).
    /// -----------------------------------------------------------
    public class Tournament
    {
        #region ---- Properties ----

        private static readonly Lazy<Tournament> _instance =
            new Lazy<Tournament>(() => new Tournament());

        /// <summary>
        /// When true, statistics restart from zero for the "level" phase (second pool stage),
        /// so the ranking inside a level pool only reflects the level matches.
        /// Set to false to carry the morning points over.
        /// </summary>
        public static bool ResetStatsForLevelPhase = true;

        /// <summary>Participating teams (16).</summary>
        public Team[] Teams;

        private readonly ChassieuVolleyTournament.Timer _timer;
        private DisplayWindow _displayWindow;
        private StaffWindow _staffWindow;

        private PoolPhase _morningPhase;
        private PoolPhase _levelPhase;
        private TreePhase _finalBracket;

        private Phase _currentPhase;

        private readonly HashSet<string> _validKeys = new HashSet<string>();
        private readonly object _keysLock = new object();

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastTickMs;

        private static readonly string[] DefaultTeamNames =
        {
            "Aliexpress", "Les 4 Fantasques", "Aymard", "Mojito",
            "Bounniz", "Les fratés", "PanOx", "Les Vollaylles",
            "Zimbra", "Éclatés au sol", "Les pipous", "Namasté",
            "Fixouille", "Black Mamba", "Les Crazy Dinos", "Ti-Punch"
        };

        #endregion

        #region ---- Constructor ----

        private Tournament()
        {
            _timer = new ChassieuVolleyTournament.Timer();

            // Subscribed once, for the whole life of the app. Only a MATCH timer
            // reaching zero ends a match; a warm-up ending does nothing here.
            _timer.OnMatchEnd += HandleMatchEnd;
        }

        #endregion

        #region ---- Phases ----

        /// --------------------------------------------------------
        /// Creates the 16 teams, distributes them into 4 pools and
        /// starts the morning pool phase.
        /// --------------------------------------------------------
        public void InitializeMorningPhase()
        {
            string[] names = LoadTeamNames();

            Teams = new Team[16];
            for (int i = 0; i < Teams.Length; i++)
                Teams[i] = new Team(names[i]);

            Pool[] tempPools = new Pool[4];
            for (int j = 0; j < 4; j++)
                tempPools[j] = new Pool(Teams[j * 4], Teams[j * 4 + 1], Teams[j * 4 + 2], Teams[j * 4 + 3]);

            _levelPhase = null;
            _finalBracket = null;

            _morningPhase = new PoolPhase(tempPools[0], tempPools[1], tempPools[2], tempPools[3]);
            _currentPhase = _morningPhase;

            RefreshAll();
        }

        /// ---------------------------------------------------------------
        /// Team names: first 16 non-empty lines of teams.txt (next to the
        /// executable or in the app data folder) if present, else defaults.
        /// ---------------------------------------------------------------
        private static string[] LoadTeamNames()
        {
            try
            {
                string[] candidates =
                {
                    Path.Combine(AppPaths.BaseDir, "teams.txt"),
                    Path.Combine(AppPaths.DataDir, "teams.txt")
                };

                foreach (string path in candidates)
                {
                    if (!File.Exists(path)) continue;

                    string[] lines = File.ReadAllLines(path)
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0)
                        .ToArray();

                    if (lines.Length >= 16)
                        return lines.Take(16).ToArray();

                    AppPaths.Log("teams.txt ignoré : " + lines.Length + " noms au lieu de 16 (" + path + ")");
                }
            }
            catch (Exception ex)
            {
                AppPaths.Log("Lecture teams.txt impossible : " + ex.Message);
            }

            return (string[])DefaultTeamNames.Clone();
        }

        /// -------------------------------------------------------------
        /// Returns why the next phase cannot start now, or null if it can.
        /// -------------------------------------------------------------
        public string GetNextPhaseProblem()
        {
            if (_currentPhase == null) return "Le tournoi n'est pas initialisé.";
            if (_timer.GetTimerIsEnabled()) return "Un timer est en cours : terminez-le ou annulez-le d'abord.";
            if (_currentPhase == _finalBracket) return "La phase finale est la dernière phase.";
            return null;
        }

        /// ------------------------------------------------------------
        /// Moves morning -> level -> final. Returns false if not possible.
        /// ------------------------------------------------------------
        public bool GoToNextPhase()
        {
            if (GetNextPhaseProblem() != null) return false;

            if (_currentPhase == _morningPhase)
            {
                InitializeLevelPhase();
                return true;
            }

            if (_currentPhase == _levelPhase)
            {
                InitializeFinalPhase();
                return true;
            }

            return false;
        }

        /// ------------------------------------------------------
        /// Level phase (second pool stage): pool N contains the
        /// teams ranked N-th in each morning pool.
        /// ------------------------------------------------------
        private void InitializeLevelPhase()
        {
            _morningPhase.UpdateAllRankings();
            Pool[] morningPools = _morningPhase.GetPools();

            Pool[] levelPools = new Pool[4];
            for (int rank = 0; rank < 4; rank++)
            {
                levelPools[rank] = new Pool(
                    morningPools[0].GetTeams()[rank],
                    morningPools[1].GetTeams()[rank],
                    morningPools[2].GetTeams()[rank],
                    morningPools[3].GetTeams()[rank]);
            }

            if (ResetStatsForLevelPhase)
            {
                foreach (Team team in Teams)
                    team.Statistics = new TeamStatistics();
            }

            _levelPhase = new PoolPhase(levelPools[0], levelPools[1], levelPools[2], levelPools[3]);
            _currentPhase = _levelPhase;

            _displayWindow?.PoolLayout();
            RefreshAll();
        }

        /// ------------------------------------------------------
        /// Final knockout phase with the 8+8 team brackets.
        /// ------------------------------------------------------
        private void InitializeFinalPhase()
        {
            _levelPhase.UpdateAllRankings();
            Pool[] pools = _levelPhase.GetPools();

            _displayWindow?.FinalLayout();

            _finalBracket = new TreePhase();
            _finalBracket.GenerateMatches(pools[0], pools[1], pools[2], pools[3]);

            _currentPhase = _finalBracket;

            _staffWindow?.RebuildForTreePhase();
            RefreshAll();
        }

        /// -------------------------------------------------------------
        /// Puts the normal display back (after the pause screen was shown).
        /// -------------------------------------------------------------
        public void RestoreDisplay()
        {
            if (_displayWindow == null) return;

            if (_currentPhase is PoolPhase)
            {
                _displayWindow.PoolLayout();
                RefreshAll();
            }
            else if (_currentPhase is TreePhase)
            {
                _displayWindow.FinalLayout();
                _finalBracket.DisplayMatchesOnWindow();
            }
        }

        #endregion

        #region ---- Timer ----

        /// -------------------------------------------------------------
        /// A match timer just reached zero (or was ended by the staff):
        /// add the three matches to the standings and move to the next
        /// round. Committing is idempotent, so this can never count a
        /// match twice.
        /// -------------------------------------------------------------
        private void HandleMatchEnd(object sender, EventArgs e)
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;

            if (poolPhase != null && !poolPhase.IsFinished)
            {
                poolPhase.CommitCurrentMatches();
                poolPhase.UpdateAllRankings();
                poolPhase.AdvanceRound();
            }

            _displayWindow?.SetTimerText(0);
            RefreshAll();
        }

        /// ---------------------------------------------------------
        /// Returns why a timer cannot start now, or null if it can.
        /// ---------------------------------------------------------
        public string GetStartTimerProblem(bool isMatch)
        {
            if (_currentPhase == null) return "Le tournoi n'est pas initialisé.";

            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (isMatch && poolPhase != null && poolPhase.IsFinished)
                return "Tous les matchs de cette phase sont terminés.\nPassez à la phase suivante.";

            return null;
        }

        public void StartTimer(bool isMatch = true)
        {
            if (GetStartTimerProblem(isMatch) != null) return;

            _lastTickMs = _clock.ElapsedMilliseconds;
            _timer.StartTimer(isMatch);
            _displayWindow?.SetTimerText(_timer.GetCurrentTime());
            RefreshStatus();
        }

        /// <summary>Ends the running match now (counts the matches and moves on).</summary>
        public void EndMatchNow()
        {
            if (_timer.IsMatchTimerRunning)
                _timer.StopTimer();
        }

        /// <summary>Stops the running timer without any effect on the standings.</summary>
        public void CancelTimer()
        {
            _timer.CancelTimer();
            _displayWindow?.SetTimerText(0);
            RefreshStatus();
        }

        public bool IsTimerRunning => _timer.GetTimerIsEnabled();
        public bool IsMatchTimerRunning => _timer.IsMatchTimerRunning;

        public void StopTimer() => _timer.StopTimer();

        /// -------------------------------------------------------------
        /// Called regularly by the UI timer: decrements the countdown by
        /// the real elapsed time and refreshes the timer display.
        /// -------------------------------------------------------------
        public void CycleTimer()
        {
            long now = _clock.ElapsedMilliseconds;
            float deltaTime = (now - _lastTickMs) / 1000f;
            _lastTickMs = now;

            if (!_timer.GetTimerIsEnabled())
                return;

            _timer.DecrementTimer(deltaTime);

            _displayWindow?.SetTimerText(_timer.GetCurrentTime());
            RefreshStatus();
        }

        #endregion

        #region ---- Display refresh ----

        /// <summary>Pushes the whole current state to both windows.</summary>
        public void RefreshAll()
        {
            RefreshRankings();
            RefreshFields();
            RefreshKeys();
            RefreshBracket();
            RefreshStatus();
        }

        /// <summary>Re-sorts the pools after a manual edit of the statistics, then refreshes.</summary>
        public void RefreshAllPoolsRanking()
        {
            (_currentPhase as PoolPhase)?.UpdateAllRankings();
            RefreshAll();
        }

        /// <summary>Alias kept for team renames (Team objects are shared, only the labels need a refresh).</summary>
        public void RefreshNames() => RefreshAll();

        private static string FormatDiff(int diff) => diff >= 0 ? "+" + diff : diff.ToString();

        /// -------------------------------------------------------------
        /// Ranking tables of all pools, in display and staff windows.
        /// -------------------------------------------------------------
        public void RefreshRankings()
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (poolPhase == null) return;

            Pool[] pools = poolPhase.GetPools();

            for (int i = 0; i < pools.Length; i++)
            {
                List<Team> ordered = pools[i].GetTeams();

                string[] names = ordered.Select(t => t.Name).ToArray();
                string[] diffs = ordered.Select(t => FormatDiff(t.Statistics.Difference)).ToArray();
                string[] points = ordered.Select(t => t.Statistics.TournamentPoints.ToString()).ToArray();

                _displayWindow?.SetRankingText(i, names, diffs, points);
                _staffWindow?.SetRankingText(i, ordered);
            }
        }

        /// -------------------------------------------------------------
        /// Court panels (teams, score, referee) and "next match" panels.
        /// -------------------------------------------------------------
        public void RefreshFields()
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (_displayWindow == null || poolPhase == null) return;

            Match[] current = { poolPhase.CurrentMatchField1, poolPhase.CurrentMatchField2, poolPhase.CurrentMatchField3 };
            Match[] next = { poolPhase.NextMatchField1, poolPhase.NextMatchField2, poolPhase.NextMatchField3 };

            for (int i = 0; i < 3; i++)
            {
                _displayWindow.SetFieldText(i,
                    current[i].GetTeam1Name(),
                    current[i].GetTeam2Name(),
                    current[i].GetScoreTeamOne(),
                    current[i].GetScoreTeamTwo());

                _displayWindow.UpdateReferee(i, current[i].RefereeTeam?.Name);

                if (poolPhase.HasNextRound)
                {
                    _displayWindow.SetNextMatchText(i,
                        next[i].GetTeam1Name(),
                        next[i].GetTeam2Name(),
                        next[i].RefereeTeam?.Name);
                }
                else
                {
                    _displayWindow.SetNextMatchText(i, "---", "---", "Arbitre");
                }
            }
        }

        /// -----------------------------------------------------
        /// Current/next match keys shown in the staff window.
        /// -----------------------------------------------------
        public void RefreshKeys()
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (_staffWindow == null || poolPhase == null) return;

            Match[] current = { poolPhase.CurrentMatchField1, poolPhase.CurrentMatchField2, poolPhase.CurrentMatchField3 };
            Match[] next = { poolPhase.NextMatchField1, poolPhase.NextMatchField2, poolPhase.NextMatchField3 };

            for (int i = 0; i < 3; i++)
            {
                _staffWindow.SetCurrentMatchKey(i, current[i].GetMatchKey());
                _staffWindow.SetNextMatchKey(i, poolPhase.HasNextRound ? next[i].GetMatchKey() : "-");
            }
        }

        /// <summary>Bracket blocks of the final phase.</summary>
        public void RefreshBracket()
        {
            TreePhase treePhase = _currentPhase as TreePhase;
            if (_displayWindow == null || treePhase == null) return;

            foreach (string slot in TreePhase.Slots)
            {
                Match m = treePhase.GetMatchBySlot(slot);
                if (m == null) continue;

                _displayWindow.UpdateBracketBlock(slot, m.Team1.Name, m.ScoreTeam1, m.ScoreTeam2, m.Team2.Name);
            }
        }

        /// <summary>Phase / round / timer line of the staff window.</summary>
        public void RefreshStatus()
        {
            _staffWindow?.SetStatus(GetStatusText());
        }

        public string GetStatusText()
        {
            string text;

            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (poolPhase != null)
            {
                string phaseName = poolPhase == _morningPhase ? "Poules du matin" : "Poules de niveau";
                text = poolPhase.IsFinished
                    ? phaseName + " : tous les matchs sont terminés"
                    : phaseName + " : tour " + (poolPhase.CurrentRound + 1) + "/" + PoolPhase.RoundCount;
            }
            else if (_currentPhase is TreePhase)
            {
                text = "Phase finale";
            }
            else
            {
                text = "En attente";
            }

            if (_timer.GetTimerIsEnabled())
            {
                int seconds = (int)Math.Ceiling(_timer.GetCurrentTime());
                text += "   |   " + (_timer.IsMatchTimerRunning ? "Match" : "Échauffement")
                      + " " + (seconds / 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
            }

            return text;
        }

        /// -------------------------------------------------------------
        /// Updates live scores on the display (called after each point
        /// scored by a referee).
        /// -------------------------------------------------------------
        public void UpdateLiveScores()
        {
            if (_displayWindow == null || _currentPhase == null) return;

            if (_currentPhase is PoolPhase)
                RefreshFields();
            else if (_currentPhase is TreePhase)
                RefreshBracket();
        }

        #endregion

        #region ---- Referee (web) access ----

        /// ----------------------------------------------------------------
        /// A score was changed by a referee. If the match is already in the
        /// standings, its result is replaced; then screens are refreshed.
        /// ----------------------------------------------------------------
        public void OnMatchScoreChanged(Match match)
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;

            if (poolPhase != null && match != null && match.Committed)
            {
                poolPhase.CommitMatch(match);
                poolPhase.UpdateAllRankings();
                RefreshRankings();
            }

            UpdateLiveScores();
        }

        /// -----------------------------------------------------------
        /// Gets a match of the CURRENT phase by its key (case-insensitive).
        /// Returns null if the key is unknown, or belongs to a past phase.
        /// -----------------------------------------------------------
        public Match GetMatchByKey(string key)
        {
            key = PrivateKeyGenerator.Normalize(key);
            if (!IsValidKey(key))
                return null;

            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (poolPhase != null)
            {
                foreach (Pool pool in poolPhase.GetPools())
                {
                    foreach (Match match in pool.GetMatches())
                    {
                        if (match.Key == key)
                            return match;
                    }
                }
                return null;
            }

            TreePhase treePhase = _currentPhase as TreePhase;
            if (treePhase != null)
            {
                foreach (Match match in treePhase.GetMatches())
                {
                    if (match.Key == key)
                        return match;
                }
            }

            return null;
        }

        /// <summary>True if referees may currently change this match's score.</summary>
        public bool IsMatchActive(Match match)
        {
            return _currentPhase != null && _currentPhase.IsMatchActive(match);
        }

        /// <summary>True if the match was locked (finished) and referees can't change it.</summary>
        public bool IsMatchLocked(Match match)
        {
            return match != null && match.Locked;
        }

        /// -------------------------------------------------------------
        /// Matches the staff can lock/unlock: finished matches of a pool
        /// phase, or every bracket match in the final phase.
        /// -------------------------------------------------------------
        public List<KeyValuePair<string, Match>> GetLockableMatches()
        {
            var list = new List<KeyValuePair<string, Match>>();

            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (poolPhase != null)
            {
                foreach (Pool pool in poolPhase.GetPools())
                {
                    foreach (Match m in pool.GetMatches())
                    {
                        if (!m.Committed) continue;
                        list.Add(new KeyValuePair<string, Match>(
                            m.Team1.Name + " " + m.ScoreTeam1 + " - " + m.ScoreTeam2 + " " + m.Team2.Name + "   [" + m.Key + "]", m));
                    }
                }
            }

            TreePhase treePhase = _currentPhase as TreePhase;
            if (treePhase != null)
            {
                foreach (string slot in TreePhase.Slots)
                {
                    Match m = treePhase.GetMatchBySlot(slot);
                    if (m == null) continue;
                    list.Add(new KeyValuePair<string, Match>(
                        slot + " : " + m.Team1.Name + " " + m.ScoreTeam1 + " - " + m.ScoreTeam2 + " " + m.Team2.Name + "   [" + m.Key + "]", m));
                }
            }

            return list;
        }

        /// <summary>Short text shown to referees: field number, "upcoming", "finished"...</summary>
        public string DescribeMatch(Match match)
        {
            PoolPhase poolPhase = _currentPhase as PoolPhase;
            if (poolPhase != null)
            {
                int field = poolPhase.GetFieldOf(match);
                if (field > 0) return "Terrain " + field;
                if (match.Locked) return "Match terminé (verrouillé)";
                return match.Committed ? "Match terminé (corrections ouvertes)" : "Match à venir";
            }

            TreePhase treePhase = _currentPhase as TreePhase;
            if (treePhase != null)
                return treePhase.DescribeMatch(match) + (match.Locked ? " (verrouillé)" : "");

            return "Match";
        }

        public void AddValidKey(string key)
        {
            lock (_keysLock) { _validKeys.Add(key); }
        }

        public bool IsValidKey(string key)
        {
            lock (_keysLock) { return key != null && _validKeys.Contains(key); }
        }

        #endregion

        #region ---- Getters & Setters ----

        public static Tournament Instance => _instance.Value;

        public void SetWindows(DisplayWindow display, StaffWindow staff)
        {
            _displayWindow = display;
            _staffWindow = staff;
        }

        public DisplayWindow GetDisplayWindow() => _displayWindow;
        public StaffWindow GetStaffWindow() => _staffWindow;
        public Phase GetCurrentPhase() => _currentPhase;
        public ChassieuVolleyTournament.Timer GetTimer() => _timer;

        #endregion
    }
}
