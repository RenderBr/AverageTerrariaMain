using AverageTerrariaMain;
using IL.Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;

namespace AverageTerrariaSurvival.Challenges
{
    public class FindDiamonds : IChallenge
    {
        public string Name { get; set; }
        public int InternalId { get; set; }
        public string Description { get; set; }
        public FindDiamonds()
        {
            Name = "Find Diamonds!";
            InternalId = 1;
            Description = "Find a diamond! - First challenge ever o:";
        }

        public void CheckCompleted(TSPlayer p)
        {
            bool userCompleted = AvMain.dbManager.HasUserCompletedChallenge(InternalId, p);
            if(userCompleted == false)
            {

                foreach (Item i in p.TPlayer.inventory)
                {
                    if (i.netID == Terraria.ID.ItemID.Diamond)
                    {
                        AvMain.dbManager.InsertChallenge(1, p);
                        RewardOnComplete(p);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            else
            {
                return;
            }


        }

        public void RewardOnComplete(TSPlayer p)
        {
            p.SendMessage("You have successfully completed the challenge: 'Find Diamonds'! Congratz...", Color.LightGreen);
            p.SendMessage("You will now receive your reward:", Color.LightYellow);
            p.SendMessage("+ 1000 XP / Dollas", Color.LightGray);
            p.SendMessage("+ Cool Sunglasses :D", Color.LightGray);
            p.GiveItem(2862, 1);
            SimpleEcon.PlayerManager.GetPlayer(p.Name).balance += 1000;
            return;
        }
    }
}
