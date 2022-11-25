using AverageTerrariaMain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaSurvival
{
    public static class ChallengeMaster
    {
        public static Challenge GetChallengeInfo(Config cfg, int cid)
        {
            var x = cfg.challenges.FirstOrDefault(x => x.internalId == cid);

            return x;
        }

        public static Challenge GetMostRecentChallenge(Config cfg)
        {
            var x = cfg.challenges[cfg.challenges.Count - 1];

            return x;
        }
    }

    public class Reward
    {
        public string name { get; set; }
        public string command { get; set; }
        public int value { get; set; }
        public Reward(string name, string command, int value)
        {
            this.name = name;
            this.command = command;
            this.value = value;
        }
    }

    public class Challenge
    {
        public string name { get; set; }
        public int internalId { get; set; }

        //description
        public string desc { get; set; }

        public List<Reward> rewards = new List<Reward> { new Reward("Testing", "", 1) };

        public int totalValue { get
            {
                var val = 0;
                foreach(Reward r in rewards)
                {
                    val += r.value;
                }
                return val;
            } }

        public Challenge(string n, int iid, string d, List<Reward> r) {
            this.name = n;
            this.internalId = iid;
            this.desc = d;
            this.rewards = r;
        
        }


    }
}
