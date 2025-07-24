using System;
using System.Collections.Generic;

namespace ChassieuVolleyTournament
{
    public class Tournament
    {
        private static readonly Lazy<Tournament> _instance =
            new Lazy<Tournament>(() => new Tournament());

        public static Tournament Instance => _instance.Value;

        public Team[] Teams;

        private Timer _timer;
        private DisplayWindow _displayWindow;
        private StaffWindow _staffWindow;

        private PoolPhase _morningPhase;
        private PoolPhase _levelPhase;
        private TreePhase _finalBracket;

        private Phase _currentPhase;

        private HashSet<string> _validKeys;

        private Tournament()
        {
            _timer = new Timer();
            _timer.OnTimerStop += UpdateWindowTexts;

            _validKeys = new HashSet<string>();
        }

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


        public void AddValidKey(string key)
        {
            _validKeys.Add(key);
        }

        public bool IsValidKey(string key)
        {
            return _validKeys.Contains(key);
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
                    foreach(Match match in tempPool.GetMatches())
                    {
                        if (match.Key == key)
                            return match;
                    }
                }
            }

            return null;
        }

        public void StartTimer(bool reset = false) => _timer.StartTimer(reset);
        public void StopTimer() => _timer.StopTimer();

        public DisplayWindow GetDisplayWindow() => _displayWindow;

        public StaffWindow GetStaffWindow() => _staffWindow;

        public Phase GetCurrentPhase() => _currentPhase;
    }
}
