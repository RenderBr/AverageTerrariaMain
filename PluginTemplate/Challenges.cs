using AverageTerrariaMain;
using AverageTerrariaSurvival.Challenges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace Challenges
{
    public static class Challenge
    {
        public static List<IChallenge> ChallengeList = new List<IChallenge>()
        {
            FindDiamonds
        };

        public static FindDiamonds FindDiamonds = new FindDiamonds();

        public static IChallenge GetMostRecentChallenge()
        {
            return ChallengeList.Last();
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



    public interface IChallenge
    {
        public string Name { get; set; }
        public int InternalId { get; set; }
        public string Description { get; set; }
        public abstract void CheckCompleted(TSPlayer p);
        public abstract void RewardOnComplete(TSPlayer p);

    }
}
