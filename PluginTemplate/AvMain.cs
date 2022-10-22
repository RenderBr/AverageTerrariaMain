using AverageTerrariaMain;
using AverageTerrariaSurvival;
using System;
using Terraria.ObjectData;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.ID;
using System.Timers;
using Microsoft.Xna.Framework;
using TShockAPI.Localization;
using Terraria.Localization;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.IO;
using System.Text;
using System.Data;
using System.Linq.Expressions;

namespace PluginTemplate
{
    /// <summary>
    /// The main plugin class should always be decorated with an ApiVersion attribute. The current API Version is 1.25
    /// </summary>
    [ApiVersion(2, 1)]
    public class AvMain : TerrariaPlugin
    {
		internal static readonly AvPlayers Players = new AvPlayers();

		public Timer bcTimer;
        private IDbConnection _db;
        public static Database dbManager;

        public static DonatedItems _donatedItems = new DonatedItems();
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public override string Name => "Average's Survival";

        /// <summary>
        /// The version of the plugin in its current state.
        /// </summary>
        public override Version Version => new Version(1, 0, 0);

        public Config Config { get; private set; }

        /// <summary>
        /// The author(s) of the plugin.
        /// </summary>
        public override string Author => "Average";

        /// <summary>
        /// A short, one-line, description of the plugin's purpose.
        /// </summary>
        public override string Description => "Provides some functionality for Average's Survival server.";

        /// <summary>
        /// The plugin's constructor
        /// Set your plugin's order (optional) and any other constructor logic here
        /// </summary>
        public AvMain(Terraria.Main game) : base(game)
        {
            Order = 1;
        }

        /// <summary>
        /// Performs plugin initialization logic.
        /// Add your hooks, config file read/writes, etc here
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.ServerChat.Register(this, onChat);
			ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
            ServerApi.Hooks.ServerLeave.Register(this, onLeave);
            ServerApi.Hooks.NetSendData.Register(this, NetHooks_SendData);
            ServerApi.Hooks.NpcSpawn.Register(this, onBossSpawn);
            ServerApi.Hooks.NpcKilled.Register(this, onBossDeath);
            TShockAPI.GetDataHandlers.PlayerDamage += onPlayerDeath;
            TShockAPI.GetDataHandlers.TileEdit += onTileEdit;
            TShockAPI.GetDataHandlers.NPCStrike += strikeNPC;
			TShockAPI.Hooks.RegionHooks.RegionEntered += onRegionEnter;
			TShockAPI.Hooks.RegionHooks.RegionLeft += onRegionLeave;
			Console.WriteLine("Average Survival LOADED");

            switch (TShock.Config.Settings.StorageType.ToLower())
            {
                case "sqlite":
                    _db = new SqliteConnection(string.Format("uri=file://{0},Version=3",
                        Path.Combine(TShock.SavePath, "AvSurvival.sqlite")));
                    break;
                default:
                    throw new Exception("Invalid storage type.");
            }

            DonatedItems items = new DonatedItems();
            
            dbManager = new Database(_db);
        }

		public void broadcastMessage(Object source, ElapsedEventArgs args)
        {
			Random rnd = new Random();
			TSPlayer.All.SendMessage("[" + Config.serverName + "] " + Config.broadcastMessages[rnd.Next(0, Config.broadcastMessages.Count)], Microsoft.Xna.Framework.Color.Aquamarine);
        }

        void onBossDeath(NpcKilledEventArgs args)
        {
            NPC npc = Main.npc[args.npc.whoAmI];

            if (npc.netID == NPCID.EyeofCthulhu) { 

            }
        }

        void bossProgression(CommandArgs args)
        {
            Dictionary<string, Color> bossProg = new Dictionary<string, Color> { };
            int bossesKilled = 0;

            if (NPC.downedBoss1)
            {
                bossProg.Add("Eye Of Cthulhu - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Eye Of Cthulhu - Not Defeated", Color.Red);
            }

            if (NPC.downedBoss2)
            {
                bossProg.Add("Eater of Worlds - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Eater of Worlds - Not Defeated", Color.Red);
            }

            if (NPC.downedBoss3)
            {
                bossProg.Add("Skeletron - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Skeletron - Not Defeated", Color.Red);
            }
            if (Main.hardMode == true)
            {
                bossProg.Add("Wall of Flesh - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Wall of Flesh - Not Defeated", Color.Red);
            }
            if (NPC.downedMechBoss1)
            {
                bossProg.Add("Destroyer - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Destroyer - Not Defeated", Color.Red);
            }
            if (NPC.downedMechBoss2)
            {
                bossProg.Add("The Twins - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("The Twins - Not Defeated", Color.Red);
            }
            if (NPC.downedMechBoss3)
            {
                bossProg.Add("Skeletron Prime - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Skeletron Prime - Not Defeated", Color.Red);
            }
            if (NPC.downedPlantBoss)
            {
                bossProg.Add("Plantera - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Plantera - Not Defeated", Color.Red);
            }
            if (NPC.downedGolemBoss)
            {
                bossProg.Add("Golem - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Golem - Not Defeated", Color.Red);
            }
            if (NPC.downedFishron)
            {
                bossProg.Add("Duke Fishron - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Duke Fishron - Not Defeated", Color.Red);
            }
            if (NPC.downedEmpressOfLight)
            {
                bossProg.Add("Empress of Light - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Empress of Light - Not Defeated", Color.Red);
            }
            if (NPC.downedAncientCultist)
            {
                bossProg.Add("Lunatic Cultist - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Lunatic Cultist - Not Defeated", Color.Red);
            }
            if (NPC.downedMoonlord)
            {
                bossProg.Add("Moon Lord - Defeated", Color.Green);
                bossesKilled++;
            }
            else
            {
                bossProg.Add("Moon Lord - Not Defeated", Color.Red);
            }

            if(bossesKilled <= 0)
            {
                args.Player.SendMessage("No bosses have been defeated yet!", Color.Red);
            }

            foreach(KeyValuePair<string,Color> boss in bossProg)
            {
                args.Player.SendMessage(boss.Key, boss.Value);
            }
        }

            void onPlayerDeath(object sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            TSPlayer player = TSPlayer.FindByNameOrID(args.PlayerDeathReason._sourcePlayerIndex.ToString())[0];

            if(Players.GetByUsername(player.Name).isBountied == true && Main.player[player.Index].statLife <= 0)
            {
                TimeRanks.TimeRanks.Players.GetByUsername(player.Name).totalCurrency += Players.GetByUsername(player.Name).bountyPrice;
                Players.GetByUsername(player.Name).isBountied = false;
                TSPlayer.All.SendMessage(player.Name + " has claimed the bounty on " + args.Player.Name + " and won " + Players.GetByUsername(player.Name).bountyPrice + " dollas!", Color.IndianRed);
            }
            
        }

        private List<int> ItemList
        {
            get
            {
                List<int> list = new List<int>();
                var items = typeof(Terraria.ID.ItemID).GetFields();
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add((int)items[i].GetValue(items[i]));
                }
                list.Sort();
                return list;
            }
        }

        #region GenChests Command
        private void DoChests(CommandArgs args)
        {
            if (args.Parameters.Count == 0 || args.Parameters.Count > 2)
            {
                args.Player.SendInfoMessage("Usage: /genchests <amount> [gen mode: default/easy/all]");
            }
            int empty = 0;
            int tmpEmpty = 0;
            int chests = 0;
            int maxChests = 1000;

            string setting = "default";
            if (args.Parameters.Count > 1)
            {
                setting = args.Parameters[1];
            }
            const int maxtries = 100000;
            Int32.TryParse(args.Parameters[0], out chests);
            const int threshold = 100;
                for (int x = 0; x < maxChests; x++)
                {
                    if (Main.chest[x] != null)
                    {
                        tmpEmpty++;
                        bool found = false;
                        foreach (Item itm in Main.chest[x].item)
                            if (itm.netID != 0)
                                found = true;
                        if (found == false)
                        {
                            empty++;
                            //      TShock.Utils.Broadcast(string.Format("Found chest {0} empty at x {1} y {2}", x, Main.chest[x].x,
                            //                                           Main.chest[x].y));

                            // destroying
                            WorldGen.KillTile(Main.chest[x].x, Main.chest[x].y, false, false, false);
                            Main.chest[x] = null;

                        }

                    }

                }
                args.Player.SendSuccessMessage("Uprooted {0} empty out of {1} chests.", empty, tmpEmpty);
            
            if (chests + tmpEmpty + threshold > maxChests)
                chests = maxChests - tmpEmpty - threshold;
            if (chests > 0)
            {
                int chestcount = 0;
                chestcount = tmpEmpty;
                int tries = 0;
                int newcount = 0;
                while (newcount < chests)
                {
                    int contain;
                    if (setting == "default")
                    {
                        // Moved item list into a separate .txt file
                        int[] itemID = Config.DefaultChestIDs;
                        contain = itemID[WorldGen.genRand.Next(0, itemID.Length)];
                    }
                    else if (setting == "all")
                    {
                        // Updated item list to 1.2.4.1
                        contain = WorldGen.genRand.Next(ItemList[0], ItemList.Count + 1);
                    }
                    else if (setting == "easy")
                    {
                        contain = WorldGen.genRand.Next(-24, 364);
                    }
                    else
                    {
                        args.Player.SendWarningMessage("Warning! Typo in second argument: {0}", args.Parameters[1]);
                        return;
                    }
                    int tryX = WorldGen.genRand.Next(20, Main.maxTilesX - 20);
                    int tryY = WorldGen.genRand.Next((int)Main.worldSurface, Main.maxTilesY - 200);
                    while (!Main.tile[tryX, tryY].active())
                    {
                        tryY++;
                    }
                    tryY--;
                    WorldGen.KillTile(tryX, tryY, false, false, false);
                    WorldGen.KillTile(tryX + 1, tryY, false, false, false);
                    WorldGen.KillTile(tryX, tryY + 1, false, false, false);
                    WorldGen.KillTile(tryX + 1, tryY, false, false, false);

                    if (WorldGen.AddBuriedChest(tryX, tryY, contain, true, 1))
                    {
                        chestcount++;
                        newcount++;

                            StringBuilder items = new StringBuilder();
                            Terraria.Chest c = Main.chest[0];
                            if (c != null)
                            {
                                for (int j = 0; j < 40; j++)
                                {
                                    items.Append(c.item[j].netID + "," + c.item[j].stack + "," + c.item[j].prefix);
                                    if (j != 39)
                                    {
                                        items.Append(",");
                                    }
                                }
                                items.Clear();
                                Main.chest[0] = null;
                            }
                        

                    }
                    if (tries + 1 >= maxtries)
                        break;

                    tries++;
                }
                args.Player.SendSuccessMessage("Generated {0} new chests - {1} total", newcount, chestcount);
                InformPlayers();
            }
        }
        #endregion

        public static void InformPlayers(bool hard = false)
		{
			foreach (TSPlayer person in TShock.Players)
			{
				if ((person != null) && (person.Active))
				{
					for (int i = 0; i < 255; i++)
					{
						for (int j = 0; j < Main.maxSectionsX; j++)
						{
							for (int k = 0; k < Main.maxSectionsY; k++)
							{
                                NetMessage.SendSection(i, j, k);
							}
						}
					}
				}
			}

		}

        void onBossSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];

            if(npc.netID == NPCID.EyeofCthulhu)
            {
                
                TSPlayer.All.SendMessage("A suspicious eye is upon us!", Color.Red);
                npc.lifeMax = 10000+(TShock.Players.Length*2000);
                npc.life = 10000+(TShock.Players.Length * 2000);
                npc.height = 100;
                npc.width = 100;
                NetMessage.SendData(23, -1, -1, (NetworkText) null, args.NpcId);
                TSPlayer.Server.StrikeNPC(args.NpcId, 1, 0, 0);
                TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", args.NpcId);
                TSPlayer.All.SendData(PacketTypes.UpdateNPCName, npc.GivenName);
                //update on client side immediately
                args.Handled = true;
            }
            if (npc.netID == NPCID.SkeletronHead)
            {

                TSPlayer.All.SendMessage("A suspicious eye is upon us!", Color.Red);
                npc.lifeMax = 20000 + (TShock.Players.Length * 2000);
                npc.life = 20000 + (TShock.Players.Length * 2000);
                npc.height = 100;
                npc.width = 100;
                NetMessage.SendData(23, -1, -1, (NetworkText)null, args.NpcId);
                TSPlayer.Server.StrikeNPC(args.NpcId, 1, 0, 0);
                TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", args.NpcId);
                TSPlayer.All.SendData(PacketTypes.UpdateNPCName, npc.GivenName);
                //update on client side immediately
                args.Handled = true;
            }
        }

        void onLeave(LeaveEventArgs args)
        {
            TSPlayer player = TSPlayer.FindByNameOrID(args.Who.ToString())[0];
            if(Players.GetByUsername(player.Name).isBountied == true)
            {
                TimeRanks.TimeRanks.Players.GetByUsername(Players.GetByUsername(player.Name).bountiedBy.Name).totaltime += Players.GetByUsername(player.Name).bountyPrice;
                TSPlayer.FindByNameOrID(Players.GetByUsername(player.Name).bountiedBy.Name)[0].SendMessage("You have been repayed for your bounty because the the bountied player has left the game.", Color.Aquamarine);
            }
        }

        void onInitialize(EventArgs e)
        {
            Config = Config.Read();
            Commands.ChatCommands.Add(new Command("av.info", infoCommand, "info"));
			Commands.ChatCommands.Add(new Command("av.vote", VoteCommand, "tvote", "tv"));
			Commands.ChatCommands.Add(new Command("av.apply", applyStaffCommand, "apply", "applyforstaff"));
			Commands.ChatCommands.Add(new Command("av.discord", discordInvite, "discord"));
            Commands.ChatCommands.Add(new Command("av.reload", reloadCommand, "avreload"));
            Commands.ChatCommands.Add(new Command("av.bounty", Bounty, "bounty"));
            Commands.ChatCommands.Add(new Command("av.bounty", bossProgression, "bosses", "progression", "far"));
            Commands.ChatCommands.Add(new Command("av.chests", DoChests, "chests", "genChests"));
            Commands.ChatCommands.Add(new Command("av.donate", Donate, "donate", "ditem"));
            Commands.ChatCommands.Add(new Command("av.receive", ReceiveDonation, "beg", "receive", "plz"));

            bcTimer = new Timer(Config.bcInterval*1000*60); //minutes

			bcTimer.Elapsed += broadcastMessage;
			bcTimer.AutoReset = true;
			bcTimer.Enabled = true;

            dbManager.InitialSyncPlayers();


        }
        void Donate(CommandArgs args)
        {
            Item item = args.Player.SelectedItem;
            if(item == null)
            {
                args.Player.SendMessage("You must be holding an item!", Color.IndianRed);
                return;
            }
            _donatedItems.donations.Add(new DonatedItem(item.netID, item.stack, item.prefix));
            var e = 0;

            foreach(Item i in Main.player[args.Player.Index].inventory)
            {
                if(i == item)
                {
                    break;
                }
                e++;
            }
            args.Player.TPlayer.inventory[e] = new Item();
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, new NetworkText(Main.player[args.Player.Index].inventory[e].Name, NetworkText.Mode.Literal), args.Player.Index, e, 0);
            NetMessage.SendData((int)PacketTypes.PlayerSlot, args.Player.Index, -1, new NetworkText(Main.player[args.Player.Index].inventory[e].Name, NetworkText.Mode.Literal), args.Player.Index, e, 0);

            var dolla = item.GetStoreValue() * item.stack / 5000;

            if(dolla == 0)
            {
                dolla++;
            }

            TimeRanks.TimeRanks.Players.GetByUsername(args.Player.Name).totalCurrency += dolla;
            args.Player.SendMessage("You have inserted a " + item.Name + " into the donation pool and received " + dolla + " dollas!", Color.LightGreen);
            dbManager.InsertItem(new DonatedItem(item.netID, item.stack, item.prefix));
        }

        void ReceiveDonation(CommandArgs args)
        {
            if(_donatedItems.donations.Count <= 0)
            {
                args.Player.SendMessage("There are currently no items in the donation pool! Use /donate to insert an item!", Color.IndianRed);
                return;
            }

            if (args.Player.InventorySlotAvailable)
            {
                Random r = new Random();
                var item = _donatedItems.donations[r.Next(0, _donatedItems.donations.Count)];

                args.Player.GiveItem(item.id, item.quantity, item.prefix);
                dbManager.DeleteItem(item);
                _donatedItems.donations.Remove(item);
                args.Player.SendMessage("You have received a " + EnglishLanguage.GetItemNameById(item.id) + " from the donation pool!", Color.LightGreen);
            }
            else
            {
                args.Player.SendMessage("You do not have any available inventory slots!", Color.IndianRed);
                return;
            }

        }


        void Bounty(CommandArgs b)
        {
            if(b.Parameters.Count == 0) {
                b.Player.SendMessage("Insert a player's name to put a bounty on them. Ex: /bounty Average 100 (dollas)", Color.LightBlue);
                return;
            }

            if (b.Parameters.Count == 1)
            {
                b.Player.SendMessage("Please insert a value of dollas after the user's name. Ex: /bounty " + b.Parameters[0] + " 100", Color.LightBlue);
                return;
            }

            var bountyPrice = int.Parse(b.Parameters[1]);
            var bountied = b.Parameters[0];
            TSPlayer player;


            if(TSPlayer.FindByNameOrID(bountied)[0] != null)
            {
                player = TSPlayer.FindByNameOrID(bountied)[0];

                if (TimeRanks.TimeRanks.Players.GetByUsername(player.Name.ToString()).totalCurrency >= bountyPrice)
                {
                    TimeRanks.TimeRanks.Players.GetByUsername(player.Name.ToString()).totalCurrency -= bountyPrice;
                    TSPlayer.All.SendMessage("A bounty has been placed on " + player.Name + " for " + bountyPrice + " dollas! Kill them to get this reward!", Color.LightGreen);
                    Players.GetByUsername(player.Name.ToString()).bountyPrice = bountyPrice;
                    Players.GetByUsername(player.Name.ToString()).isBountied = true;
                    Players.GetByUsername(player.Name.ToString()).bountiedBy = b.Player;
                }
                else
                {
                    b.Player.SendMessage("You did not have the amount specified! Get sum' mo' money befo' you come back here biotch!", Color.LightCyan);
                    return;
                }
            }
            else
            {
                b.Player.SendMessage("The player name you entered was invalid!", Color.LightCyan);
                return;
            }


        }

		void onGreet(GreetPlayerEventArgs args)
        {
			var ply = TShock.Players[args.Who];

			Players.Add(new AvPlayer(ply.Name));
        }

        void NetHooks_SendData(SendDataEventArgs e)
        {
            if(e.MsgId == PacketTypes.NpcStrike)
            {
                NPC npc = Main.npc[e.number];
                Console.WriteLine("Net ID: " + npc.netID + ", e.Num: " + e.number + ", NPC.Type: " + npc.type);
                if(npc.life <= 0)
                {
                    // BLUE OR GREEN SLIME
                    if (npc.netID == NPCID.BlueSlime || npc.netID == NPCID.GreenSlime)
                    {
                        Random random = new Random();

                        var r = random.Next(1, ItemID.Count);
                        var p = random.Next(1, PrefixID.Count);

                        var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

                        player[0].GiveItemCheck(r, EnglishLanguage.GetItemNameById(r), random.Next(1, 100), p);
                        e.Handled = true;
                    }

                    //Demon eye
                    if(npc.netID == NPCID.DemonEye)
                    {
                        var item = ItemID.Lens;

                        var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

                        player[0].GiveItemCheck(item, EnglishLanguage.GetItemNameById(item), 1);
                        e.Handled = true;
                    }

                    //Zombie
                    if(npc.netID == NPCID.Zombie)
                    {
                        var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

                        var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.BouncyBomb, 150, 1);
                        e.Handled=true;
                    }

                    //LavaSlime
                    if (npc.netID == NPCID.LavaSlime)
                    {
                        Random random = new Random();

                        var r = random.Next(0, 5);
                        var p = random.Next(1, PrefixID.Count);

                        switch(r) {
                            case 1:
                                r = ItemID.LavaBomb; break;
                            case 2:
                                r = ItemID.LavaSkull; break;
                            case 3:
                                r = ItemID.LavaCharm; break;
                            case 4:
                                r = ItemID.LavaWaders; break;
                        }
                                

                        var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

                        player[0].GiveItemCheck(r, EnglishLanguage.GetItemNameById(r), random.Next(1, 100), p);
                        e.Handled = true;
                    }

                    //BABY SLIME
                    if (npc.netID == NPCID.BabySlime)
                    {
                        Random random = new Random();

                        var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());
                        var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.GrenadeIV, 150, 1);


                        if (random.Next(0, 100) < 5)
                        {
                            Item item = TShock.Utils.GetItemById(279);
                            int itemIndex = Item.NewItem(Projectile.GetNoneSource(), new Vector2(player[0].X, (int)player[0].Y), item.width, item.height, item.type, 64);

                            Item targetItem = Main.item[itemIndex];
                            targetItem.playerIndexTheItemIsReservedFor = player[0].Index;

                            targetItem._nameOverride = "Crazy Knives";
                            targetItem.damage = 100;
                            targetItem.useTime = 5;
                            player[0].SendData(PacketTypes.UpdateItemDrop, null, itemIndex);
                            player[0].SendData(PacketTypes.ItemOwner, null, itemIndex);
                            player[0].SendData(PacketTypes.TweakItem, null, itemIndex, 255, 63);

                        }
                        e.Handled = true;
                    }
                }
            }

            
        }

        void strikeNPC(object sender, GetDataHandlers.NPCStrikeEventArgs args)
        {



        }

        void updateTilesForAll(GetDataHandlers.TileEditEventArgs tile)
        {
            foreach(Player player in Main.player)
            {
                if(player == null)
                {
                    continue;
                }

                TSPlayer.FindByNameOrID(player.whoAmI.ToString())[0].SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
            }
        }

        void onTileEdit(object sender, GetDataHandlers.TileEditEventArgs tile)
        {
            if(tile.Action == GetDataHandlers.EditAction.KillTile && Main.tile[tile.X, tile.Y].type == 21)
            {
                var chest = Main.chest[Chest.FindChest(tile.X, tile.Y)];

                    foreach(Item item in chest.item)
                    {
                        if (tile.Player.InventorySlotAvailable == true)
                        {
                            tile.Player.GiveItem(item.netID, item.stack, item.prefix);
                            item.active = false;
                            item.UpdateItem(0);
                            NetMessage.SendData((int)PacketTypes.ChestGetContents, -1, -1, null, item.netID);
                        }
                    }
                
            }


            if(tile.Action == GetDataHandlers.EditAction.KillTile && tile.EditData == 0)
            {
                //Copper behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Copper)
                {
                    tile.Player.GiveItem(ItemID.CopperBar, 10);
                    Main.tile[tile.X, tile.Y].type = TileID.Hellstone;
                    Main.tile[tile.X, tile.Y].active(true);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Tin behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Tin)
                {
                    tile.Player.GiveItem(ItemID.TinBar, 10);
                    Main.tile[tile.X, tile.Y].type = TileID.Hellstone;
                    Main.tile[tile.X, tile.Y].active(true);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }

                //Tree drops
                if (Main.tile[tile.X, tile.Y].type == TileID.Trees)
                {
                    Random r = new Random();
                    var noFurtherdrops = false;
                    
                    tile.Player.GiveItem(ItemID.Wood, 250);
                    tile.Player.GiveItem(ItemID.Acorn, 25);

                    if (r.Next(1, 26) == 25)
                    {
                       tile.Player.GiveItem(ItemID.PearlwoodSword, 1, PrefixID.Legendary);
                        noFurtherdrops = true;
                    }

                    if(r.Next(1, 6) == 5 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.AppleJuice, 1);
                        noFurtherdrops = true;
                    }


                    if (r.Next(1, 6) == 5 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Peach, 1);
                        noFurtherdrops = true;
                    }

                    if (r.Next(1, 6) == 5 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Grapes, 1);
                        noFurtherdrops = true;
                    }

                        // handleTree(tile.X, tile.Y, tile.Player);
                        
                    

                        tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                        updateTilesForAll(tile);
                        tile.Handled = true;
                    

                }

                //Silver behaviour
                if(Main.tile[tile.X, tile.Y].type == TileID.Silver){
                    tile.Player.GiveItem(ItemID.SilverBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    int p = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(tile.Player.X, tile.Player.Y), new Vector2(0, 0), ProjectileID.BombSkeletronPrime, 100, 10);
                    tile.Handled = true;
                }
                //Tungsten behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Tungsten)
                {
                    tile.Player.GiveItem(ItemID.TungstenBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    int p = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(tile.Player.X, tile.Player.Y), new Vector2(0, 0), ProjectileID.BombSkeletronPrime, 100, 10);
                    tile.Handled = true;
                }

                //Lead behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Lead)
                {
                    tile.Player.GiveItem(ItemID.LeadBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    int p = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(tile.Player.X, tile.Player.Y), new Vector2(0, 0), ProjectileID.BouncyBoulder, 100, 10);
                    tile.Handled = true;
                }

                //Iron behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Iron)
                {
                    tile.Player.GiveItem(ItemID.IronBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    int p = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(tile.Player.X, tile.Player.Y), new Vector2(0, 0), ProjectileID.BouncyBoulder, 100, 10);
                    tile.Handled = true;
                }
                //Gold behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Gold)
                {
                    tile.Player.GiveItem(ItemID.GoldBar, 20);
                    Main.tile[tile.X, tile.Y].type = TileID.Spikes;
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Platinum behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Platinum)
                {
                    tile.Player.GiveItem(ItemID.PlatinumBar, 20);
                    Main.tile[tile.X, tile.Y].type = TileID.Spikes;
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Hive behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Hive)
                {
                    tile.Player.GiveItem(ItemID.GoldBar, 20);
                    Main.tile[tile.X, tile.Y].type = TileID.CrispyHoneyBlock;
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
            }


        }

        void handleTree(int x, int y, TSPlayer Player)
        {
            WorldGen.KillTile(x, y);
            if(Main.tile[x, y+1].type == TileID.Trees)
            {
                handleTree(x, y+1, Player);
                Player.SendTileSquareCentered(Player.TileX, Player.TileY, 32);
            }
            else
            {
                Player.SendTileSquareCentered(Player.TileX, Player.TileY, 32);

            }
        }

        void VoteCommand(CommandArgs args)
        {
			args.Player.SendMessage("Vote for our server on Terraria-servers.com! Fill in your name as it is in-game, after that, type /reward to receive your playtime!", Color.Aquamarine);
        }

        void onRegionEnter(TShockAPI.Hooks.RegionHooks.RegionEnteredEventArgs args)
        {


        }

		void onRegionLeave(TShockAPI.Hooks.RegionHooks.RegionLeftEventArgs args)
		{

		}

		void applyStaffCommand(CommandArgs args)
        {
			args.Player.SendInfoMessage("Head to averageterraria.lol, register an account, and fill out the staff template under the 'Staff Applications' tag! Thanks for considering applying :)");
		}


		void onChat(ServerChatEventArgs args)
        {

            
        }

        void infoCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage(Config.infoMessage);
        }

        void discordInvite(CommandArgs args)
        {
            args.Player.SendSuccessMessage(Config.discordMessage);
        }


        void reloadCommand(CommandArgs args)
        {
            Config = Config.Read();

            args.Player.SendSuccessMessage("Average's Terraria plugin config has been reloaded!");
        }
        
        /// <summary>
        /// Performs plugin cleanup logic
        /// Remove your hooks and perform general cleanup here
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
				ServerApi.Hooks.ServerChat.Deregister(this, onChat);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, onGreet);
                ServerApi.Hooks.NetSendData.Deregister(this, NetHooks_SendData);
				TShockAPI.Hooks.RegionHooks.RegionEntered -= onRegionEnter;
				TShockAPI.Hooks.RegionHooks.RegionLeft -= onRegionLeave;

			}
            base.Dispose(disposing);
        }


        
    }
}
