#region ---- Includes ----
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Windows.Forms;
#endregion

namespace ChassieuVolleyTournament
{
    public class Tournament
    {
        #region ---- Properties ----
        private static readonly Lazy<Tournament> _instance =
            new Lazy<Tournament>(() => new Tournament());

        public static Tournament Instance => _instance.Value;

        public Team[] Teams;

        private ChassieuVolleyTournament.Timer _timer;
        private DisplayWindow _displayWindow;
        private StaffWindow _staffWindow;

        private PoolPhase _morningPhase;
        private PoolPhase _levelPhase;
        private TreePhase _finalBracket;

        private Phase _currentPhase;

        private HashSet<string> _validKeys;

        private long _lastTimerTime;

        public event EventHandler EndPhase;

        #endregion

        #region ---- Constructor ----
        private Tournament()
        {
            _timer = new ChassieuVolleyTournament.Timer();

            _validKeys = new HashSet<string>();
        }

        #endregion

        #region ---- Methods ----
        public void InitializeMorningPhase()
        {
            Teams = new Team[16];
            Team tempTeam;

            Pool[] tempPools = new Pool[4];
            Pool tempPool;

            for (int i = 0; i < Teams.Length; i++)
            {
                tempTeam = new Team("Équipe " + i.ToString());
                Teams[i] = tempTeam;
            }

            for (int j = 0; j < 4; j++)
            {
                tempPool = new Pool(Teams[j * 4], Teams[j * 4 + 1], Teams[j * 4 + 2], Teams[j * 4 + 3]);
                tempPools[j] = tempPool;
            }

            _morningPhase = new PoolPhase(tempPools[0], tempPools[1], tempPools[2], tempPools[3]);
            _currentPhase = _morningPhase;

            // ensure only one subscription to the event
            _timer.OnTimerStop -= _morningPhase.IncrementTeamsScores;
            _timer.OnTimerStop += _morningPhase.IncrementTeamsScores;

            _timer.OnTimerStop -= _morningPhase.CycleMatches;
            _timer.OnTimerStop += _morningPhase.CycleMatches;

            _timer.OnTimerStop -= UpdateWindowTexts;
            _timer.OnTimerStop += UpdateWindowTexts;

            _timer.OnTimerStop -= UpdateKeys;
            _timer.OnTimerStop += UpdateKeys;

            string[] TeamNames = new string[16];
            for (int k = 0; k < 16; k++)
            {
                TeamNames[k] = Teams[k].Name;
            }

            _staffWindow.SetTeamNames(TeamNames);

            _staffWindow.SetCurrentMatchKey(0, (_currentPhase as PoolPhase).CurrentMatchField1.Key);
            _staffWindow.SetCurrentMatchKey(1, (_currentPhase as PoolPhase).CurrentMatchField2.Key);
            _staffWindow.SetCurrentMatchKey(2, (_currentPhase as PoolPhase).CurrentMatchField3.Key);
            _staffWindow.SetNextMatchKey(0, (_currentPhase as PoolPhase).NextMatchField1.Key);
            _staffWindow.SetNextMatchKey(1, (_currentPhase as PoolPhase).NextMatchField2.Key);
            _staffWindow.SetNextMatchKey(2, (_currentPhase as PoolPhase).NextMatchField3.Key);

            UpdateLiveScores();
            UpdateWindowTexts(null, EventArgs.Empty);

            EndPhase -= InitializeLevelPhase;
            EndPhase += InitializeLevelPhase;
        }

        public void InitializeLevelPhase(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => UpdateWindowTexts(sender, e)));
                    return;
                }
            }

            if (_displayWindow == null || _currentPhase == null)
                return;

            EndPhase -= InitializeLevelPhase;

            Team[] newPool1Teams = new Team[4];
            Team[] newPool2Teams = new Team[4];
            Team[] newPool3Teams = new Team[4];
            Team[] newPool4Teams = new Team[4];

            for (int i = 0; i < 4; i++)
            {
                newPool1Teams[i] = _morningPhase.GetPools()[i].GetTeams()[0];
                newPool2Teams[i] = _morningPhase.GetPools()[i].GetTeams()[1];
                newPool3Teams[i] = _morningPhase.GetPools()[i].GetTeams()[2];
                newPool4Teams[i] = _morningPhase.GetPools()[i].GetTeams()[3];
            }

            Pool tempPool1 = new Pool(newPool1Teams[0], newPool1Teams[1], newPool1Teams[2], newPool1Teams[3]);
            Pool tempPool2 = new Pool(newPool2Teams[0], newPool2Teams[1], newPool2Teams[2], newPool2Teams[3]);
            Pool tempPool3 = new Pool(newPool3Teams[0], newPool3Teams[1], newPool3Teams[2], newPool3Teams[3]);
            Pool tempPool4 = new Pool(newPool4Teams[0], newPool4Teams[1], newPool4Teams[2], newPool4Teams[3]);

            _levelPhase = new PoolPhase(tempPool1, tempPool2, tempPool3, tempPool4); 
            _currentPhase = _levelPhase;

            _timer.OnTimerStop -= _morningPhase.IncrementTeamsScores;
            _timer.OnTimerStop -= _levelPhase.IncrementTeamsScores;
            _timer.OnTimerStop += _levelPhase.IncrementTeamsScores;

            _timer.OnTimerStop -= _morningPhase.CycleMatches;
            _timer.OnTimerStop -= _levelPhase.CycleMatches;
            _timer.OnTimerStop += _levelPhase.CycleMatches;

            _timer.OnTimerStop -= UpdateWindowTexts;
            _timer.OnTimerStop += UpdateWindowTexts;

            _timer.OnTimerStop -= UpdateKeys;
            _timer.OnTimerStop += UpdateKeys;

            string[] TeamNames = new string[16];
            for (int k = 0; k < 16; k++)
            {
                TeamNames[k] = Teams[k].Name;
            }

            _staffWindow.SetTeamNames(TeamNames);

            _staffWindow.SetCurrentMatchKey(0, (_currentPhase as PoolPhase).CurrentMatchField1.Key);
            _staffWindow.SetCurrentMatchKey(1, (_currentPhase as PoolPhase).CurrentMatchField2.Key);
            _staffWindow.SetCurrentMatchKey(2, (_currentPhase as PoolPhase).CurrentMatchField3.Key);
            _staffWindow.SetNextMatchKey(0, (_currentPhase as PoolPhase).NextMatchField1.Key);
            _staffWindow.SetNextMatchKey(1, (_currentPhase as PoolPhase).NextMatchField2.Key);
            _staffWindow.SetNextMatchKey(2, (_currentPhase as PoolPhase).NextMatchField3.Key);

            // Build pool texts
            for (int i = 0; i < 4; i++)
            {
                var pool = _levelPhase.GetPools()[i];
                string[] names = pool.GetTeams().Select(t => t.Name).ToArray();
                string[] diffs = pool.GetTeams().Select(t => t.Statistics.Difference.ToString()).ToArray();
                string[] points = pool.GetTeams().Select(t => t.Statistics.TournamentPoints.ToString()).ToArray();

                _displayWindow.SetRankingText(i, names, diffs, points);
            }

            _displayWindow.SetFieldText(0,
                _levelPhase.CurrentMatchField1.Team1.Name,
                _levelPhase.CurrentMatchField1.Team2.Name,
                _levelPhase.CurrentMatchField1.ScoreTeam1.ToString(),
                _levelPhase.CurrentMatchField1.ScoreTeam2.ToString());

            _displayWindow.SetFieldText(1,
                _levelPhase.CurrentMatchField2.Team1.Name,
                _levelPhase.CurrentMatchField2.Team2.Name,
                _levelPhase.CurrentMatchField2.ScoreTeam1.ToString(),
                _levelPhase.CurrentMatchField2.ScoreTeam2.ToString());

            _displayWindow.SetFieldText(2,
                _levelPhase.CurrentMatchField3.Team1.Name,
                _levelPhase.CurrentMatchField3.Team2.Name,
                _levelPhase.CurrentMatchField3.ScoreTeam1.ToString(),
                _levelPhase.CurrentMatchField3.ScoreTeam2.ToString());


            _displayWindow.SetNextMatchText(0,
                _levelPhase.NextMatchField1.Team1.Name,
                _levelPhase.NextMatchField1.Team2.Name,
                "Referee");

            _displayWindow.SetNextMatchText(1,
                _levelPhase.NextMatchField2.Team1.Name,
                _levelPhase.NextMatchField2.Team2.Name,
                "Referee");

            _displayWindow.SetNextMatchText(2,
                _levelPhase.NextMatchField3.Team1.Name,
                _levelPhase.NextMatchField3.Team2.Name,
                "Referee");


            UpdateLiveScores();
            UpdateWindowTexts(null, EventArgs.Empty);

            EndPhase -= InitializeFinalPhase;
            EndPhase += InitializeFinalPhase;

            Program.SetKeysInfo();
            Program.GiveWebServerKeysInfo();
            Program.GiveWebServerKeys();
        }

        public void InitializeFinalPhase(object sender, EventArgs e)
        {
            _displayWindow.FinalLayout();

            _finalBracket = new TreePhase();
            _finalBracket.GenerateMatches(_levelPhase.GetPools()[0], _levelPhase.GetPools()[1], _levelPhase.GetPools()[2], _levelPhase.GetPools()[3]);

            _currentPhase = _finalBracket;

            _displayWindow.Update();

            Program.SetKeysInfo();
            Program.GiveWebServerKeysInfo();
            Program.GiveWebServerKeys();
        }

        public void SetWindows(DisplayWindow display, StaffWindow staff)
        {
            _displayWindow = display;
            _staffWindow = staff;
        }

        public void SetCurrentPhase(Phase phase)
        {
            _currentPhase = phase;
            UpdateWindowTexts(null, EventArgs.Empty);
        }

        private void UpdateWindowTexts(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count > 0)
            {
                var mainForm = Application.OpenForms[0];
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => UpdateWindowTexts(sender, e)));
                    return;
                }
            }

            if (_displayWindow == null || _currentPhase == null)
                return;

            _displayWindow.SetFieldText(0,
                _currentPhase.CurrentMatchField1.GetTeam1Name(),
                _currentPhase.CurrentMatchField1.GetTeam2Name(),
                _currentPhase.CurrentMatchField1.GetScoreTeamOne(),
                _currentPhase.CurrentMatchField1.GetScoreTeamTwo());

            _displayWindow.SetFieldText(1,
                _currentPhase.CurrentMatchField2.GetTeam1Name(),
                _currentPhase.CurrentMatchField2.GetTeam2Name(),
                _currentPhase.CurrentMatchField2.GetScoreTeamOne(),
                _currentPhase.CurrentMatchField2.GetScoreTeamTwo());

            _displayWindow.SetFieldText(2,
                _currentPhase.CurrentMatchField3.GetTeam1Name(),
                _currentPhase.CurrentMatchField3.GetTeam2Name(),
                _currentPhase.CurrentMatchField3.GetScoreTeamOne(),
                _currentPhase.CurrentMatchField3.GetScoreTeamTwo());

            if (_currentPhase.CurrentMatchField1 != _currentPhase.NextMatchField1)
            {
                _displayWindow.SetNextMatchText(0,
                    _currentPhase.NextMatchField1.GetTeam1Name(),
                    _currentPhase.NextMatchField1.GetTeam2Name(),
                    "Arbitre");

                _displayWindow.SetNextMatchText(1,
                    _currentPhase.NextMatchField2.GetTeam1Name(),
                    _currentPhase.NextMatchField2.GetTeam2Name(),
                    "Arbitre");

                _displayWindow.SetNextMatchText(2,
                    _currentPhase.NextMatchField3.GetTeam1Name(),
                    _currentPhase.NextMatchField3.GetTeam2Name(),
                    "Arbitre");
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    _displayWindow.SetNextMatchText(i, "---", "---", "Arbitre");
                }
            }
        }

        public void UpdateLiveScores()
        {
            if (_displayWindow == null || _currentPhase == null)
                return;

            if (!(_currentPhase is PoolPhase poolPhase))
                return;

            Match[] currentMatches = new Match[]
            {
                poolPhase.CurrentMatchField1,
                poolPhase.CurrentMatchField2,
                poolPhase.CurrentMatchField3
            };

            for (int i = 0; i < currentMatches.Length; i++)
            {
                Match match = currentMatches[i];
                if (match == null)
                    continue;

                var labels = _displayWindow.CourtLabels[i];

                if (labels == null || labels.Length < 4)
                    continue;

                labels[0].Text = match.GetTeam1Name();
                labels[1].Text = match.GetTeam2Name();
                labels[2].Text = match.GetScoreTeamOne().ToString();
                labels[3].Text = match.GetScoreTeamTwo().ToString();
            }
        }

        private void UpdateKeys(object sender, EventArgs e)
        {
            if (Application.OpenForms.Count > 0)
            {
                var staffForm = Application.OpenForms[1];
                if (staffForm.InvokeRequired)
                {
                    staffForm.BeginInvoke(new Action(() => UpdateKeys(sender, e)));
                    return;
                }
            }

            string[] currentKeys = new string[3] { _currentPhase.CurrentMatchField1.GetMatchKey(), _currentPhase.CurrentMatchField2.GetMatchKey(), _currentPhase.CurrentMatchField3.GetMatchKey() };
            string[] nextKeys = new string[3] {_currentPhase.NextMatchField1.GetMatchKey(), _currentPhase.NextMatchField2.GetMatchKey(), _currentPhase.NextMatchField3.GetMatchKey()};
            _staffWindow.UpdateKeysLabels(currentKeys, nextKeys);
        }
        public void RefreshAllPoolsRanking()
        {
            if (_currentPhase is PoolPhase poolPhase)
            {
                var display = GetDisplayWindow();
                var pools = poolPhase.GetPools();

                for (int poolIndex = 0; poolIndex < pools.Length; poolIndex++)
                {
                    var pool = pools[poolIndex];

                    // Recalculate standings
                    pool.UpdateRanking();

                    string[] teamNames = new string[pool.GetTeams().Count];
                    string[] teamDiffs = new string[pool.GetTeams().Count];
                    string[] teamPoints = new string[pool.GetTeams().Count];

                    var orderedTeams = pool.Ranking.Values.ToList();

                    for (int i = 0; i < orderedTeams.Count; i++)
                    {
                        var team = orderedTeams[i];
                        teamNames[i] = team.Name;
                        teamDiffs[i] = team.Statistics.Difference >= 0
                            ? $"+{team.Statistics.Difference}"
                            : team.Statistics.Difference.ToString();
                        teamPoints[i] = team.Statistics.TournamentPoints.ToString();
                    }

                    display?.SetRankingText(poolIndex, teamNames, teamDiffs, teamPoints);
                }

                display?.Update();
            }
        }

        public void UpdateTeamNameInAllMatches(string oldName, string newName)
        {
            foreach(var pool in (_currentPhase as PoolPhase).GetPools())
            {
                foreach (var match in pool.GetMatches())
                {
                    var teams = match.GetTeams();

                    if (teams[0].Name == oldName)
                    {
                        teams[0].Name = newName;
                    }
                    if (teams[1].Name == oldName)
                    {
                        teams[1].Name = newName;
                    }
                }
            }

        }


        public void AddValidKey(string key)
        {
            _validKeys.Add(key);
        }

        public bool IsValidKey(string key)
        {
            return _validKeys.Contains(key);
        }

        public void RaiseEndPhase()
        {
            EndPhase?.Invoke(this, EventArgs.Empty);
        }
        
        public void CycleTimer(Stopwatch Stopwatch)
        {
            // establish baseline on first call to avoid huge delta (and instant stop)
            long currentTime = Stopwatch.ElapsedMilliseconds;
            if (_lastTimerTime == 0)
            {
                _lastTimerTime = currentTime;
                return;
            }

            float deltaTime = (currentTime - _lastTimerTime) / 1000f;
            _lastTimerTime = currentTime;

            if (!_timer.GetTimerIsEnabled())
                return;

            _timer.DecrementTimer(deltaTime);

            if (GetDisplayWindow() != null)
            {
                GetDisplayWindow().Invoke((MethodInvoker)delegate
                {
                    GetDisplayWindow().SetTimerText(_timer.GetCurrentTime());
                });
            }
        }

        #endregion

        #region ---- Getters & Setters ----
        public void StartTimer(bool IsMatch = true)
        {
            _lastTimerTime = 0; // reset baseline
            _timer.StartTimer(IsMatch);
        }

        public Match GetMatchByKey(string key)
        {
            if (!IsValidKey(key))
                return null;

            if (_currentPhase != _finalBracket)
            {
                PoolPhase currentPoolPhase = _currentPhase as PoolPhase;

                foreach (Pool tempPool in currentPoolPhase.GetPools())
                {
                    foreach (Match match in tempPool.GetMatches())
                    {
                        if (match.Key == key)
                            return match;
                    }
                }
            }

            return null;
        }

        public void StopTimer() => _timer.StopTimer();
        public DisplayWindow GetDisplayWindow() => _displayWindow;
        public StaffWindow GetStaffWindow() => _staffWindow;
        public Phase GetCurrentPhase() => _currentPhase;
        public ChassieuVolleyTournament.Timer GetTimer() => _timer;

        #endregion
    }
}
