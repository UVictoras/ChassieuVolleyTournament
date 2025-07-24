using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ChassieuVolleyTournament
{
    internal class PoolPhase : Phase
    {
        Pool[] pools;

        int[] field1MatchsIndexes;
        int[] field1PoolsIndexes;

        int[] field2MatchsIndexes;
        int[] field2PoolsIndexes;

        int[] field3MatchsIndexes;
        int[] field3PoolsIndexes;

        int currentIndex;

        public PoolPhase(Pool pool1, Pool pool2, Pool pool3, Pool pool4) 
        {
            pools = new Pool[4];

            pools[0] = pool1;
            pools[1] = pool2;
            pools[2] = pool3;
            pools[3] = pool4;

            field1MatchsIndexes = new int[8] { 0, 0, 1, 2, 3, 3, 4, 5};
            field1PoolsIndexes =  new int[8] { 0, 3, 0, 0, 0, 3, 0, 0};

            field2MatchsIndexes = new int[8] { 0, 1, 1, 2, 3, 4, 4, 5};
            field2PoolsIndexes =  new int[8] { 1, 1, 3, 1, 1, 1, 3, 1};

            field3MatchsIndexes = new int[8] { 0, 1, 2, 2, 3, 4, 5, 5};
            field3PoolsIndexes =  new int[8] { 2, 2, 2, 3, 2, 2, 2, 3};

            currentIndex = 0;

            CycleMatches();
        }

        public void CycleMatches()
        {
            CurrentMatchField1 = pools[field1PoolsIndexes[currentIndex]].GetMatches()[field1MatchsIndexes[currentIndex]];
            CurrentMatchField2 = pools[field2PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];
            CurrentMatchField3 = pools[field3PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];

            currentIndex++;

            if (currentIndex >= 8)
                return;

            NextMatchField1 = pools[field1PoolsIndexes[currentIndex]].GetMatches()[field1MatchsIndexes[currentIndex]];
            NextMatchField2 = pools[field2PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];
            NextMatchField3 = pools[field3PoolsIndexes[currentIndex]].GetMatches()[field3MatchsIndexes[currentIndex]];
        }

        public Pool[] GetPools() => pools;
    }
}
