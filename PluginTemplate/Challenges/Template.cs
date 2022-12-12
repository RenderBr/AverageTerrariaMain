using Challenges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace AverageTerrariaSurvival.Challenges
{
    public class Template : IChallenge
    {
        public string Name { get; set; }
        public int InternalId { get; set; }
        public string Description { get; set; }

        public void CheckCompleted(TSPlayer p)
        {
            throw new NotImplementedException();
        }

        public void RewardOnComplete(TSPlayer p)
        {
            throw new NotImplementedException();
        }
    }
}
