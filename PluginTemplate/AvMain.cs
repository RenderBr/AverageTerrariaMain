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
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;
using System.Data;
using System.Linq.Expressions;
using System.Linq;
using TShockAPI.Hooks;
using Steamworks;

namespace AverageTerrariaMain
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
        public DateTime lastChecked = DateTime.Now;

        public static DonatedItems _donatedItems = new DonatedItems();
        public static Clans _clans = new Clans();

        #region server challenges

        #endregion

        #region restaurant variables
        public static bool restaurantOpen = false;
        public static string restaurantName = "The Chef's Diner";

        public static List<Tuple<TSPlayer, string, int>> orders = new List<Tuple<TSPlayer, string, int>>();
        public static List<TSPlayer> chefs = new List<TSPlayer>();
        public static List<TSPlayer> chefApplicants = new List<TSPlayer>();
        public static TSPlayer HeadChef = null;
        #endregion

        #region anti-rush variables
        public int totalDays;
        public bool canSummonWOF = false;
        public bool canSummonEOC = false;
        public bool canSummonEvilBoss = false;
        public bool canSummonSkeletron = false;
        #endregion
        public override string Name => "Average's Survival";
        public override System.Version Version => new System.Version(1, 0, 0);

        public Config Config { get; private set; }

        public override string Author => "Average";

        public override string Description => "Provides some functionality for Average's Survival server.";
        public AvMain(Terraria.Main game) : base(game)
        {
            Order = 1;
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.ServerChat.Register(this, onChat);
            ServerApi.Hooks.GameUpdate.Register(this, onUpdate);
			ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
            ServerApi.Hooks.ServerLeave.Register(this, onLeave);
            ServerApi.Hooks.NetSendData.Register(this, NetHooks_SendData);
            ServerApi.Hooks.NpcSpawn.Register(this, onBossSpawn);
            ServerApi.Hooks.NpcKilled.Register(this, onBossDeath);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PlayerLogin;
            TShockAPI.Hooks.PlayerHooks.PlayerChat += PlayerChat;
            TShockAPI.GetDataHandlers.KillMe += KillMeEvent;
            TShockAPI.GetDataHandlers.TileEdit += onTileEdit;
            TShockAPI.GetDataHandlers.PlayerDamage += onPlayerDamage;
			TShockAPI.Hooks.RegionHooks.RegionEntered += onRegionEnter;
			TShockAPI.Hooks.RegionHooks.RegionLeft += onRegionLeave;

            switch (TShock.Config.Settings.StorageType.ToLower())
            {
                case "sqlite":
                    _db = new SqliteConnection(string.Format("Data Source={0}",
                        Path.Combine(TShock.SavePath, "AvSurvival.sqlite")));
                    break;
                default:
                    throw new Exception("Invalid storage type.");
            }

            DonatedItems items = new DonatedItems();
            _clans.allClans = new List<Clan>();

            dbManager = new Database(_db);
        }

        private void PlayerChat(PlayerChatEventArgs e)
        {
            var c = Utilities.IntToColor((int)Players.GetByUsername(e.Player.Name).level).Hex3();

            e.TShockFormattedText = $"[c/{c}:[{Players.GetByUsername(e.Player.Name).level}][c/{c}:]] {e.TShockFormattedText}";
        }

        private void onUpdate(EventArgs args)
        {
            var dateTime = DateTime.Now;

            if(dateTime.Subtract(lastChecked).Seconds > 4)
            {
                lastChecked = DateTime.Now;
                foreach(TSPlayer p in TShock.Players)
                {
                    if(p == null)
                    {
                        continue;
                    }
                    if(Players.GetByUsername(p.Name) == null)
                    {
                        continue;
                    }
                    Redo:
                    if (Players.GetByUsername(p.Name).level >= 400)
                    {
                        continue;
                    }

                    if (SimpleEcon.PlayerManager.GetPlayer(p.Name).balance >= 1500*Players.GetByUsername(p.Name).level+(Players.GetByUsername(p.Name).level* Players.GetByUsername(p.Name).level))
                    {
                        
                        dbManager.LevelUp(p.Name);
                        var ply = p;
                        var newLevel = Players.LevelUp(p.Name);
                        var player = Players.GetByUsername(p.Name);
                        p.SendMessage($"You have leveled up! You're now level: [c/90EE90:{player.level}]", Color.Orange);

                        applyStats(p);
                        goto Redo;
                    }
                }
            }


        }

        public void PlayerLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {

        }

        public void onPlayerDamage(Object sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            var PVPedPlayer = args.Player;
            var player = TShock.Players[args.ID];
            if(_clans.FindClan(Players.GetByUsername(player.Name).clan) == _clans.FindClan(Players.GetByUsername(PVPedPlayer.Name).clan)){
                args.Handled = true;
            }
            else
            {
                return;
            }

        }

        public void KillMeEvent(Object sender, GetDataHandlers.KillMeEventArgs args)
        {
            short damage = args.Damage;
            short id = args.PlayerId;
            var deathReason = args.PlayerDeathReason;
            TSPlayer enemyPlayer = TShock.Players[deathReason._sourcePlayerIndex];

            if (enemyPlayer == null)
            {
                return;
            }

            if (Players.GetByUsername(args.Player.Name).isBountied == true)
            {
                SimpleEcon.PlayerManager.GetPlayer(enemyPlayer.Name).balance += Players.GetByUsername(args.Player.Name).bountyPrice;
                Players.GetByUsername(args.Player.Name).isBountied = false;
                TSPlayer.All.SendMessage(enemyPlayer.Name + " has claimed the bounty on " + args.Player.Name + " and won " + Players.GetByUsername(args.Player.Name).bountyPrice + " dollas!", Color.IndianRed);
            }
        }

        public void openRestaurant(CommandArgs args)
        {
            if(restaurantOpen == false)
            {
                var lobbyRegion = TShock.Regions.GetRegionByName("lobby").Area;
                var regionSize = (lobbyRegion.Width * lobbyRegion.Height);
                restaurantOpen = true;


                for(var i = 0; i < regionSize; i++)
                {
                //    Main.tile[lobbyRegion.BottomRight().X+i, lobbyRegion.BottomRight().Y].
                }


                TSPlayer.All.SendMessage($"[{restaurantName}] The restaurant is now open for business! Head to /spawn to get some food :D", Color.LightBlue);
            }
            else
            {
                restaurantOpen = false;
                TSPlayer.All.SendMessage($"[{restaurantName}] The restaurant has closed!", Color.LightBlue);

            }
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

        #region InformPlayers
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
        #endregion
        // /givecurrency <player> <quantity)
        void adminGiveDollas(CommandArgs args)
        {
            if(args.Parameters.Count <= 0)
            {
                args.Player.SendErrorMessage("Invalid arguments! Use this as an example: /givecurrency <player> <quantity>");
                return;
            }

            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Invalid arguments! Please enter a quantity: /givecurrency {args.Parameters[0]} <quantity>");
                return;
            }

            var player = args.Parameters[0];
            var quantity = args.Parameters[1];
            var Nplayer = SimpleEcon.PlayerManager.GetPlayer(player);

            if (Nplayer == null)
            {
                args.Player.SendErrorMessage("This player does not exist in the TimeRanks database!");
                return;
            }

            if (int.Parse(quantity) == 0 || quantity == null)
            {
                args.Player.SendErrorMessage($"Please enter a quantity as the second parameter. Ex. /givecurrency {player} 1000");
                return;
            }

            Nplayer.balance += int.Parse(quantity);
            args.Player.SendMessage($"You have given {player} {quantity} {SimpleEcon.SimpleEcon.config.currencyNamePlural}!", Color.LightGreen);
            TSPlayer.FindByNameOrID(player)[0].SendMessage($"You have been given {quantity} {SimpleEcon.SimpleEcon.config.currencyNamePlural} by {args.Player.Name}!", Color.LightGreen);

        }

        void adminSetDollas(CommandArgs args)
        {
            if (args.Parameters.Count <= 0)
            {
                args.Player.SendErrorMessage("Invalid arguments! Use this as an example: /setcurrency <player> <quantity>");
                return;
            }

            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Invalid arguments! Please enter a quantity: /setcurrency {args.Parameters[0]} <quantity>");
                return;
            }

            var player = args.Parameters[0];
            var quantity = args.Parameters[1];
            var Nplayer = SimpleEcon.PlayerManager.GetPlayer(player);

            if (Nplayer == null)
            {
                args.Player.SendErrorMessage("This player does not exist in the SimpleEcon database!");
                return;
            }

            if (int.Parse(quantity) == 0 || quantity == null)
            {
                args.Player.SendErrorMessage($"Please enter a quantity as the second parameter. Ex. /setcurrency {player} 1000");
                return;
            }

            Nplayer.balance = int.Parse(quantity);
            args.Player.SendMessage($"You have set {player}'s balance to {quantity} {SimpleEcon.SimpleEcon.config.currencyNamePlural}!", Color.LightGreen);
            TSPlayer.FindByNameOrID(player)[0].SendMessage($"Your balance has been set to {quantity} {SimpleEcon.SimpleEcon.config.currencyNamePlural} by {args.Player.Name}!", Color.LightGreen);
            return;
        }

        void onBossSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];

            //if(npc.netID == NPCID.EyeofCthulhu)
            //{

            //    TSPlayer.All.SendMessage("A suspicious eye is upon us!", Color.Red);
            //    npc.lifeMax = 10000+(TShock.Players.Length*1000);
            //    npc.life = 10000+(TShock.Players.Length * 1000);
            //    npc.height = 100;
            //    npc.width = 100;
            //    NetMessage.SendData(23, -1, -1, (NetworkText) null, args.NpcId);
            //    TSPlayer.Server.StrikeNPC(args.NpcId, 1, 0, 0);
            //    TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", args.NpcId);
            //    TSPlayer.All.SendData(PacketTypes.UpdateNPCName, npc.GivenName);
            //    //update on client side immediately
            //    args.Handled = true;
            //}
            //if (npc.netID == NPCID.SkeletronHead)
            //{

            //    TSPlayer.All.SendMessage("A suspicious eye is upon us!", Color.Red);
            //    npc.lifeMax = 20000 + (TShock.Players.Length * 2000);
            //    npc.life = 20000 + (TShock.Players.Length * 2000);
            //    npc.height = 100;
            //    npc.width = 100;
            //    NetMessage.SendData(23, -1, -1, (NetworkText)null, args.NpcId);
            //    TSPlayer.Server.StrikeNPC(args.NpcId, 1, 0, 0);
            //    TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", args.NpcId);
            //    TSPlayer.All.SendData(PacketTypes.UpdateNPCName, npc.GivenName);
            //    //update on client side immediately
            //    args.Handled = true;
            //}


            //anti rush
            if(canSummonEOC == true)
            {

            }
            else if(canSummonEOC == false && npc.netID == NPCID.EyeofCthulhu)
            {
                NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, NetworkText.Empty, args.NpcId);
                TSPlayer.All.SendErrorMessage("Due to anti-rush, the Eye of Cthulhu can not be summoned yet!");
            }

            //anti rush
            if (canSummonEvilBoss == true)
            {

            }
            else if (canSummonEvilBoss == false && (npc.netID == NPCID.BrainofCthulhu || npc.netID == NPCID.EaterofWorldsBody))
            {
                NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, NetworkText.Empty, args.NpcId);
                TSPlayer.All.SendErrorMessage("Due to anti-rush, the evil boss can not be summoned yet!");
            }

            if (canSummonSkeletron == true)
            {

            }
            else if (canSummonSkeletron == false && npc.netID == NPCID.SkeletronHead)
            {
                NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, NetworkText.Empty, args.NpcId);
                TSPlayer.All.SendErrorMessage("Due to anti-rush, Skeletron can not be summoned yet!");
            }


            if (canSummonWOF == true)
            {

            }
            else if (canSummonWOF == false && npc.netID == NPCID.WallofFlesh)
            {
                NetMessage.SendData((int)PacketTypes.NpcUpdate, -1, -1, NetworkText.Empty, args.NpcId);
                TSPlayer.All.SendErrorMessage("Due to anti-rush, the Wall of Flesh can not be summoned yet!");
            }

        }

        void onLeave(LeaveEventArgs args)
        {
            TSPlayer player = TSPlayer.FindByNameOrID(args.Who.ToString())[0];
            if(Players.GetByUsername(player.Name).isBountied == true)
            {
                SimpleEcon.PlayerManager.GetPlayer(Players.GetByUsername(player.Name).bountiedBy.Name).balance += Players.GetByUsername(player.Name).bountyPrice;
                TSPlayer.FindByNameOrID(Players.GetByUsername(player.Name).bountiedBy.Name)[0].SendMessage("You have been repayed for your bounty because the the bountied player has left the game.", Color.Aquamarine);
            }
        }

        void SendToChefs(int type, Tuple<TSPlayer, string, int> order)
        {

            if(type == 0)
            {
                foreach (TSPlayer player in chefs)
                {
                    player.SendInfoMessage($"A new order (num. {orders.IndexOf(order)}) has been placed by {order.Item1.Name} for a {order.Item2}. Use /chef prep {orders.IndexOf(order)}");
                }
            }

        }

        void SendToChefs(int type, Tuple<TSPlayer, string, int> order, TSPlayer playerTakingOrder)
        {

            if (type == 1)
            {
                foreach (TSPlayer player in chefs)
                {
                    player.SendInfoMessage($"{playerTakingOrder.Name} is preparing order number {orders.IndexOf(order)}! Please do not prep this order unless you wanna break the game!)");
                }
            }

            if (type == 2)
            {
                foreach (TSPlayer player in chefs)
                {
                    player.SendInfoMessage($"{playerTakingOrder.Name} has finished the order {orders.IndexOf(order)}!");
                }
            }

        }

        void Prep(CommandArgs args)
        {
            var Player = args.Player;
            var Chef = Players.GetByUsername(Player.Name);
            if(Chef.isChef == false)
            {
                Player.SendMessage("You aren't even a chef!", Color.Orange);
                return;
            }
            if(Chef.prepping == false)
            {
                Player.SendMessage("You aren't prepping a meal!", Color.Orange);
                return;
            }

            var Order = orders[Chef.order];

            if (Order == null)
            {
                Player.SendMessage("Something went wrong with this order! You probably won't be able to continue... :(", Color.Orange);
                return;
            }

            Player.SendMessage($"(Order Num. {Chef.order}) You successfully prepared the {Order.Item2}. Next step: /cook!", Color.Orange);
            Chef.prepping = false;
            Chef.cooking = true;
            return;

        }


        void Cook(CommandArgs args)
        {
            var Player = args.Player;
            var Chef = Players.GetByUsername(Player.Name);
            if (Chef.isChef == false)
            {
                Player.SendMessage("You aren't even a chef!", Color.Orange);
                return;
            }
            if (Chef.cooking == false)
            {
                Player.SendMessage("You aren't cooking a meal!", Color.Orange);
                return;
            }

            var Order = orders[Chef.order];

            if (Order == null)
            {
                Player.SendMessage("Something went wrong with this order! You probably won't be able to continue... :(", Color.Orange);
                return;
            }

            Player.SendMessage($"(Order Num. {Chef.order}) You successfully cooked the {Order.Item2}. Next step: /plate!", Color.Orange);
            Chef.cooking = false;
            Chef.plating = true;
            return;

        }

        void Plate(CommandArgs args)
        {
            var Player = args.Player;
            var Chef = Players.GetByUsername(Player.Name);
            if (Chef.isChef == false)
            {
                Player.SendMessage("You aren't even a chef!", Color.Orange);
                return;
            }
            if (Chef.plating == false)
            {
                Player.SendMessage("You aren't plating a meal!", Color.Orange);
                return;
            }

            var Order = orders[Chef.order];

            if (Order == null)
            {
                Player.SendMessage("Something went wrong with this order! You probably won't be able to continue... :(", Color.Orange);
                return;
            }

            Player.SendMessage($"(Order Num. {Chef.order}) You successfully plated the {Order.Item2}. Next step: /serve!", Color.Orange);
            Chef.plating = false;
            Chef.serving = true;
            return;

        }

        void Serve(CommandArgs args)
        {
            var Player = args.Player;
            var Chef = Players.GetByUsername(Player.Name);
            if (Chef.isChef == false)
            {
                Player.SendMessage("You aren't even a chef!", Color.Orange);
                return;
            }
            if (Chef.serving == false)
            {
                Player.SendMessage("You aren't serving a meal!", Color.Orange);
                return;
            }

            var Order = orders[Chef.order];

            if (Order == null)
            {
                Player.SendMessage("Something went wrong with this order! You probably won't be able to continue... :(", Color.Orange);
                return;
            }
            var dolla = Convert.ToInt32((TShock.Utils.GetItemByName(Order.Item2)[0].GetStoreValue() / 5000) / 1.5);
            Player.SendMessage($"(Order Num. {Chef.order}) You successfully served the {Order.Item2} to {Order.Item1.Name}. You've earned {dolla} dollas!", Color.Orange);
            Chef.order = 0;
            Chef.serving = false;
            Order.Item1.GiveItem(TShock.Utils.GetItemByName(Order.Item2)[0].netID, Order.Item3);
            Order.Item1.SendMessage($"Your food has been served by {Chef.name}! Enjoy :D! If you're feeling generous, tip them for their service with /pay!", Color.Green);
            SendToChefs(2, Order, Player);
            orders.RemoveAt(Chef.order);
            return;

        }

        void Menu(CommandArgs args)
        {
            TSPlayer Player = args.Player;

            Player.SendInfoMessage("Menu for The Chef's Diner");
            foreach(Food food in Config.Menu)
            {
                Player.SendInfoMessage($"{food.terrariaName} - ${food.price}");
            }

            if (restaurantOpen == true)
            {
                Player.SendInfoMessage("Use /chef order <food> to order something! Ex. /chef order HotDog (You must be in the restaurant!)");
            }
            else
            {
                Player.SendInfoMessage("The restaurant is not currently open so you are unable to order food at this moment!");
            }

            return;


        }

        void Chef(CommandArgs args)
        {
            if(restaurantOpen != true)
            {
                args.Player.SendErrorMessage("The restaurant is not open at the moment!");
                return;
            }

            if(args.Parameters.Count <= 0)
            {
                args.Player.SendErrorMessage("Invalid arguments. Use /chef help to get a list of commands!");
                return;
            }

            HeadChef = TSPlayer.FindByNameOrID("Evauation")[0];
            var subcommand = args.Parameters[0];
            var Player = args.Player;

            switch (subcommand)
            {
                case "help":
                    Player.SendInfoMessage("Chef Commands");
                    Player.SendInfoMessage("/chef apply - puts your application in as a chef!");
                    Player.SendInfoMessage("/chef order <food> - puts in an order for your food!");
                    Player.SendInfoMessage("/chef prepare <order num> - used by chefs to prepare food!!");
                    Player.SendInfoMessage("/menu - see a list of foods!");
                    return;
                case "apply":
                    chefApplicants.Add(Player);
                    HeadChef.SendMessage(Player.Name + " has applied for the sous-chef position! Use /chef (a)ccept " + Player.Name + " or /chef (d)eny " + Player.Name, Color.LightGreen);
                    Player.SendInfoMessage("Your application has been submitted and is awaiting a response!");
                    return;
                case "a":
                case "accept":
                    if (args.Parameters[1] == null)
                    {
                        HeadChef.SendMessage("You must enter a valid player name to accept.", Color.Orange);
                        return;
                    }
                    var acceptedPlayer = TSPlayer.FindByNameOrID(args.Parameters[1])[0];

                    chefs.Add(acceptedPlayer);
                    chefApplicants.Remove(acceptedPlayer);
                    Players.GetByUsername(acceptedPlayer.Name).isChef = true;
                    TSPlayer.All.SendMessage($"[{restaurantName}] {acceptedPlayer.Name} is the latest addition to the restaurant!", Color.Aqua);
                    return;
                case "d":
                case "deny":
                    if (args.Parameters[1] == null)
                    {
                        HeadChef.SendMessage("You must enter a valid player name to deny.", Color.Orange);
                        return;
                    }
                    var deniedPlayer = TSPlayer.FindByNameOrID(args.Parameters[1])[0];

                    chefApplicants.Remove(deniedPlayer);
                    deniedPlayer.SendMessage("You have been denied for the position of chef!", Color.IndianRed);
                    HeadChef.SendMessage($"You have denied {deniedPlayer}'s application!", Color.LightBlue);
                    return;
                case "request":
                case "order":
                    if(args.Parameters.Count == 2 && args.Parameters[1] == "list")
                    {
                        if (Players.GetByUsername(args.Player.Name).isChef == true) {
                            args.Player.SendMessage("List of Active Orders: ", Color.Orange);

                            foreach (Tuple<TSPlayer,string,int> t in orders)
                            {
                                args.Player.SendMessage($"[ID: {orders.IndexOf(t)}] {t.Item1.Name} ordered {t.Item2}", Color.LightGreen);
                            }
                            return;
                        }
                        else
                        {
                            args.Player.SendErrorMessage("You are not a chef!");
                            return;
                        }
                    }

                    int quantity = 1;
                    if(args.Parameters.Count == 3)
                    {
                        var succeeded = int.TryParse(args.Parameters[2], out quantity);
                        if(succeeded == false)
                        {
                            quantity = 1;
                        }

                    }

                    if (Player == HeadChef || Players.GetByUsername(Player.Name).isChef == true)
                    {
                        Player.SendErrorMessage("You cannot order from the restaurant and be working at the restaurant!");
                        return;
                    }
                    if (args.Parameters[1] == null)
                    {
                        Player.SendErrorMessage("Check out the /menu to see a list of the foods you can order!");
                        return;
                    }
                    if (Player.CurrentRegion.Name != "lobby")
                    {
                        Player.SendErrorMessage("You must be in the restaurant to order food!");
                        return;
                    }
                    if (orders.Any(x => x.Item1.Name == Player.Name))
                    {
                        Player.SendErrorMessage("You cannot make two orders at the same time! Wait for your current food!");
                        return;
                    }
                    string requestedFood = args.Parameters[1].ToLower();
                    var price = 0;

                    foreach (Food item in Config.Menu)
                    {
                        if (item.aliases.Contains(requestedFood) == true)
                        {
                            price = item.price * quantity;
                            requestedFood = item.terrariaName;

                            if (SimpleEcon.PlayerManager.GetPlayer(Player.Name).balance < item.price)
                            {
                                Player.SendErrorMessage($"You do not have enough dollas to order this food! ({item.price} $ needed)");
                                return;
                            }

                            SimpleEcon.PlayerManager.GetPlayer(Player.Name).balance -= item.price;
                            var order = new Tuple<TSPlayer, string, int>(Player, requestedFood, quantity);
                            orders.Add(order);
                            SendToChefs(0, order);
                            Player.SendInfoMessage($"Your order has placed for a {requestedFood}! Please seat yourself and wait for a chef to prepare your food!");
                            return;

                        }
                    }
                    args.Player.SendErrorMessage("That food item does not exist! Check out our menu with /menu");
                    return;
                case "make":
                case "prep":
                case "prepare":
                    if(Player.CurrentRegion.Name != "kitchen")
                    {
                        if (args.Parameters[2] == "-test")
                        {

                        }
                        else
                        {
                            Player.SendErrorMessage("You must be in the kitchen to prepare food!");
                            return;
                        }

                    }
                    if (args.Parameters[1] == null)
                    {
                        Player.SendErrorMessage("Please specify an order number to prepare! Ex. /chef prepare 5");
                        return;
                    }
                    var o = int.Parse(args.Parameters[1]);
                    var porder = orders[o];

                    SendToChefs(1, porder, Player);
                    Players.GetByUsername(Player.Name).order = o;
                    Player.SendInfoMessage($"You have just started prepping order number {o} for {porder.Item1.Name}. Begin with /prep");
                    Players.GetByUsername(Player.Name).prepping = true;
                    return;
                case "quit":
                    if (Players.GetByUsername(Player.Name).isChef == false && chefs.Contains(Player) == false)
                    {
                        Player.SendErrorMessage("You are not a chef!");
                        return;
                    }

                    var quittingPlayer = Players.GetByUsername(Player.Name);
                    quittingPlayer.isChef = false;
                    chefs.Remove(Player);
                    TSPlayer.All.SendMessage($"[{restaurantName}] {Player.Name} has quit their job at The Chef's Diner!", Color.IndianRed);
                    return;
                case "fire":
                case "kick":
                    var kickedPlayer = args.Parameters[1];

                    if(Players.GetByUsername(args.Parameters[1]).isChef == false && chefs.Contains(TSPlayer.FindByNameOrID(args.Parameters[1])[0]) == false)
                    {
                        Player.SendErrorMessage("Not a valid chef!");
                        return;
                    }


                    var kickedPlayerValid = Players.GetByUsername(kickedPlayer);
                    kickedPlayerValid.isChef = false;
                    chefs.Remove(Player);
                    TSPlayer.All.SendMessage($"[{restaurantName}] {kickedPlayerValid.name} has been fired from The Chef's Diner!", Color.IndianRed);
                    return;
                default:
                    Player.SendErrorMessage("Invalid arguments. Use /chef help to get a list of commands!");
                    return;
            }
        }

        void onInitialize(EventArgs e)
        {
            Config = Config.Read();
            totalDays = (int)DateTime.UtcNow.Subtract(Config.startDate).TotalDays;
            Commands.ChatCommands.Add(new Command("av.info", infoCommand, "info"));
			Commands.ChatCommands.Add(new Command("av.vote", VoteCommand, "tvote", "tv"));
			Commands.ChatCommands.Add(new Command("av.apply", applyStaffCommand, "apply", "applyforstaff"));
			Commands.ChatCommands.Add(new Command("av.discord", discordInvite, "discord"));
            Commands.ChatCommands.Add(new Command("av.reload", reloadCommand, "avreload"));
            Commands.ChatCommands.Add(new Command("av.bounty", Bounty, "bounty"));
            Commands.ChatCommands.Add(new Command("av.bounty", bossProgression, "bosses", "progression", "far"));
            Commands.ChatCommands.Add(new Command("av.donate", Donate, "donate", "ditem"));
            Commands.ChatCommands.Add(new Command("av.donate", Sell, "sell", "itemtodollas", "convitem"));
            Commands.ChatCommands.Add(new Command("clan.chat", ClanChat, "c", "ditem"));
            Commands.ChatCommands.Add(new Command("clan.list", ClansList, "clist", "clans"));
            Commands.ChatCommands.Add(new Command("clan.use", Clan, "clan"));
            Commands.ChatCommands.Add(new Command("av.admin", openRestaurant, "openres", "resopen", "restaurant"));
            Commands.ChatCommands.Add(new Command("av.bounty", Menu, "menu"));
            Commands.ChatCommands.Add(new Command("av.bounty", Level, "level"));
            Commands.ChatCommands.Add(new Command("av.admin", SetLevel, "setlevel"));

            Commands.ChatCommands.Add(new Command("av.bounty", Challenge, "challenges"));
            Commands.ChatCommands.Add(new Command("av.bounty", CurrentChallenge, "currentchallenge"));

            Commands.ChatCommands.Add(new Command("av.bounty", OrderFood, "order"));
            Commands.ChatCommands.Add(new Command("av.bounty", Prep, "prep", "prepare"));
            Commands.ChatCommands.Add(new Command("av.bounty", Cook, "cook"));
            Commands.ChatCommands.Add(new Command("av.bounty", Plate, "plate"));
            Commands.ChatCommands.Add(new Command("av.bounty", Serve, "serve"));
            Commands.ChatCommands.Add(new Command("av.bounty", Chef, "chef"));
            Commands.ChatCommands.Add(new Command("clan.use", MyClan, "myclan", "cinfo", "claninfo"));
            Commands.ChatCommands.Add(new Command("av.donate", Conv, "conv", "convert", "dollastogold"));

            Commands.ChatCommands.Add(new Command("av.admin", adminGiveDollas, "givecurrency", "gc", "givebal", "baladd")); Commands.ChatCommands.Add(new Command("av.admin", adminGiveDollas, "givecurrency", "gc", "givebal", "baladd"));
            Commands.ChatCommands.Add(new Command("av.admin", adminSetDollas, "setcurrency", "sc", "setbal", "setbalance"));

            Commands.ChatCommands.Add(new Command("av.receive", ReceiveDonation, "beg", "receive", "plz"));

            bcTimer = new Timer(Config.bcInterval*1000*60); //minutes

			bcTimer.Elapsed += broadcastMessage;
			bcTimer.AutoReset = true;
			bcTimer.Enabled = true;

            dbManager.InitialSync();
            AntiRush();

        }

        private void OrderFood(CommandArgs args)
        {
            args.Parameters.Insert(0, "order");
            Chef(args);
        }
        private void SetLevel(CommandArgs args)
        {
            if(args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Enter a player name. Ex. /setlevel <player> <level>");
                return;
            }

            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage($"Enter a level. Ex. /setlevel {args.Parameters[0]} <level>");
                return;
            }

            var p = Players.GetByUsername(args.Parameters[0]);
            var lvl = int.Parse(args.Parameters[1]);
            Players.ManipulateLevel(p.name, lvl);
            dbManager.ManipulateLevel(p.name, lvl);
            args.Player.SendSuccessMessage($"You have set {p.name}'s level to {lvl}!");
            return;
        }

        private void CurrentChallenge(CommandArgs args)
        {
            if(args.Player.IsLoggedIn == false)
            {
                args.Player.SendErrorMessage("You must be logged-in to use this command! :>");
                return;
            }
            var chal = ChallengeMaster.GetMostRecentChallenge(Config);
            
            
            if(dbManager.HasUserCompletedChallenge(chal.internalId, args.Player))
            {
                args.Player.SendMessage("You have already completed the latest challenge! Good job! :>", Color.LightGreen);
                args.Player.SendMessage($"{chal.name} - {chal.desc} ✓ (${chal.totalValue} value)", Color.Goldenrod);
                return;
            }
            else
            {
                args.Player.SendMessage($"New Challenge: {chal.name}", Color.Goldenrod);
                args.Player.SendMessage($"{chal.desc}", Color.LightGray);
                args.Player.SendMessage($"If completed you will get: {chal.totalValue} dollas worth of rewards!", Color.Goldenrod);
                return;
            }
        }

        private void Challenge(CommandArgs args)
        {

            if(Config.challenges.Count > 0)
            {
                args.Player.SendMessage("List of Challenges!", Color.Orange);
                foreach (Challenge chal in Config.challenges)
                {
                    var userCompleted = dbManager.HasUserCompletedChallenge(chal.internalId, args.Player);

                    if(userCompleted == true)
                    {
                        args.Player.SendMessage($"{chal.name} - {chal.desc} ✓", Color.LightGreen);
                    }
                    else
                    {
                        args.Player.SendMessage($"{chal.name} - {chal.desc} -", Color.OrangeRed);
                    }

                }
                args.Player.SendMessage($"Use /currentchallenge to view the current challenge!", Color.Goldenrod);
                return;
            }
            else
            {
                args.Player.SendErrorMessage("No challenges are implemented yet!");
                return;
            }

        }
        #region Donate & ItemToDollas Command
        void Donate(CommandArgs args)
        {
            var avp = Players.GetByUsername(args.Player.Name);

            if (avp.donateBeg.Enabled == true)
            {
                if(args.Player.HasPermission("cooldownbypass") == false)
                {
                    args.Player.SendMessage("You must wait to use this command again!", Color.IndianRed);
                    return;
                }
            }

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


            args.Player.SendMessage("You have inserted a " + item.Name + " into the donation pool. Other players can receive this with /beg! What a charitable citizen!", Color.LightGreen);
            dbManager.InsertItem(new DonatedItem(item.netID, item.stack, item.prefix));
            avp.donateBeg.Start();
            return;
        }

        void Sell(CommandArgs args)
        {
            var avp = Players.GetByUsername(args.Player.Name);

            Item item = args.Player.SelectedItem;
            if (item.IsAir)
            {
                args.Player.SendMessage("You must be holding an item!", Color.IndianRed);
                return;
            }

            var e = 0;

            foreach (Item i in Main.player[args.Player.Index].inventory)
            {
                if (i == item)
                {
                    break;
                }
                e++;
            }
            args.Player.TPlayer.inventory[e] = new Item();
            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, new NetworkText(Main.player[args.Player.Index].inventory[e].Name, NetworkText.Mode.Literal), args.Player.Index, e, 0);
            NetMessage.SendData((int)PacketTypes.PlayerSlot, args.Player.Index, -1, new NetworkText(Main.player[args.Player.Index].inventory[e].Name, NetworkText.Mode.Literal), args.Player.Index, e, 0);


            var dolla = (item.GetStoreValue() / 5000)* item.stack;

            if (dolla == 0)
            {
                dolla++;
            }

            SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance += dolla;
            args.Player.SendMessage($"You have sold a {item.Name} and received {dolla} {SimpleEcon.SimpleEcon.config.currencyNamePlural}!", Color.LightGreen);
            return;
        }
        #endregion


    #region Clans List Command
    void ClansList(CommandArgs args)
        {
            var page = 1;
            var i = 0;
            if(args.Parameters.Count > 0)
            {
                if (args.Parameters[0] == "list")
                {
                    args.Parameters.Clear();
                }
                else
                {
                    args.Parameters[0] = "" + 1;
                    page = int.Parse(args.Parameters[0]);
                }
            }

            if(_clans.allClans.Count == 0)
            {
                args.Player.SendMessage("There are no clans!", Color.LightCyan);
                return;
            }

            args.Player.SendMessage($"Clan List - Page {page}", Color.Gold);

            foreach (Clan clan in _clans.allClans) {

                args.Player.SendMessage(clan.name + " - Member Count: " + clan.members.members.Count + "", Color.LightYellow);
            }



        }
        #endregion

        #region MyClan Command
        void MyClan(CommandArgs args)
        {
            var p = args.Player;
            if(p.Account == null)
            {
                p.SendMessage("You must be logged into use this command!", Color.Orange);
                return;
            }

            var ply = Players.GetByUsername(args.Player.Name);
            if(ply.clan != "")
            {
                Clan clan = _clans.FindClan(ply.clan);

                var memberCount = clan.members.members.Count;
                var name = clan.name;
                var yourTempRole = clan.members.FindMember(ply.name).role;
                var yourRole = "";
                var owner = clan.owner;

                if(yourTempRole == 0)
                {
                    yourRole = "Rookie";
                }
                if(yourTempRole == 1)
                {
                    yourRole = "Member";
                }
                if (yourTempRole == 2)
                {
                    yourRole = "Admin";
                }
                if (yourTempRole == 3)
                {
                    yourRole = "Owner";
                }

                p.SendMessage("Clan Info for " + name, Color.Gold);
                p.SendMessage("Clan Owner: " + owner, Color.LightBlue);
                p.SendMessage("Your role in clan: " + yourRole, Color.LightBlue);
                p.SendMessage("Member Count: " + memberCount, Color.LightBlue);
                return;

            }
            else
            {
                p.SendMessage("You are not in a clan!", Color.Orange);
                return;
            }
        }
        #endregion

        #region ReceiveDonation Command
        void ReceiveDonation(CommandArgs args)
        {
            var avp = Players.GetByUsername(args.Player.Name);

            if (_donatedItems.donations.Count <= 0)
            {
                args.Player.SendMessage("There are currently no items in the donation pool! Use /donate to insert an item!", Color.IndianRed);
                return;
            }


            if (avp.donateBeg.Enabled == true)
            {
                if (args.Player.HasPermission("cooldownbypass"))
                {

                }
                else
                {
                    args.Player.SendMessage("You must wait to use this command again!", Color.IndianRed);
                    return;
                }

            }

            Players.GetByUsername(args.Player.Name).donateBeg.Start();

            if (args.Player.InventorySlotAvailable)
            {
                Random r = new Random();
                var item = _donatedItems.donations[r.Next(0, _donatedItems.donations.Count)];

                args.Player.GiveItem(item.id, item.quantity, item.prefix);
                dbManager.DeleteItem(item);
                _donatedItems.donations.Remove(item);
                args.Player.SendMessage("You have received a " + EnglishLanguage.GetItemNameById(item.id) + " from the donation pool!", Color.LightGreen);
                avp.donateBeg.Start();
                return;
            }
            else
            {
                args.Player.SendMessage("You do not have any available inventory slots!", Color.IndianRed);
                return;
            }

        }
        #endregion

        #region Bounty
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

                if (SimpleEcon.PlayerManager.GetPlayer(player.Name.ToString()).balance >= bountyPrice)
                {
                    SimpleEcon.PlayerManager.GetPlayer(player.Name.ToString()).balance -= bountyPrice;
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
        #endregion
        void noMoreCoolDown(Object obj, ElapsedEventArgs args)
        {

        }

        void banItem(int item)
        {
            TShock.ItemBans.DataModel.AddNewBan(EnglishLanguage.GetItemNameById(item));
        }

        void UnbanItem(int item)
        {
            TShock.ItemBans.DataModel.RemoveBan(EnglishLanguage.GetItemNameById(item));
        }

        public void Level(CommandArgs args)
        {
            if(args.Player.IsLoggedIn == false)
            {
                args.Player.SendErrorMessage("You must be logged in to use this command!");
                return;
            }

            var lvl = Players.GetByUsername(args.Player.Name).level;

            args.Player.SendMessage($"You are level [c/90EE90:{lvl}!] You need [c/90EE90:{1500*Players.GetByUsername(args.Player.Name).level+Players.GetByUsername(args.Player.Name).level* Players.GetByUsername(args.Player.Name).level-SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance} XP] to level up!", Color.LightGoldenrodYellow);
        }

        void AntiRush()
        {
            // reset has only been around for less than a day
            if (totalDays < 1)
            {
                banItem(ItemID.SuspiciousLookingEye);
                banItem(ItemID.WormFood);
                banItem(ItemID.BloodySpine);
                banItem(ItemID.GuideVoodooDoll);
                banItem(4988);
                banItem(ItemID.MechanicalEye);
                banItem(ItemID.MechanicalWorm);
                banItem(ItemID.MechanicalSkull);
                banItem(ItemID.LihzahrdPowerCell);
                banItem(ItemID.CelestialSigil);


            }
            if (totalDays > 0)
            {
                UnbanItem(ItemID.SuspiciousLookingEye);
                canSummonEOC = true;
            }
            if (totalDays >= 2)
            {
                UnbanItem(ItemID.WormFood);
                UnbanItem(ItemID.BloodySpine);
                canSummonEvilBoss = true;

            }
            if (totalDays >= 3)
            {
                canSummonSkeletron = true;

            }
            if (totalDays >= 5)
            {
                canSummonWOF = true;
                UnbanItem(ItemID.GuideVoodooDoll);


            }
            if (totalDays >= 7)
            {
                UnbanItem(ItemID.MechanicalEye);

            }

            if (totalDays >= 9)
            {
                UnbanItem(ItemID.MechanicalWorm);

            }

            if (totalDays >= 10)
            {
                UnbanItem(ItemID.MechanicalSkull);

            }
            if (totalDays >= 12)
            {
                UnbanItem(ItemID.LihzahrdPowerCell);

            }
            if (totalDays >= 14)
            {
                UnbanItem(ItemID.CelestialSigil);

            }
        }


        void applyStats(TSPlayer ply)
        {
            var player = Players.GetByUsername(ply.Name);

            ply.TPlayer.statLifeMax += (int)Math.Round(player.level * 1.4);
            ply.TPlayer.statManaMax += (int)Math.Round(player.level * 2.5);
            ply.TPlayer.lifeRegen += (int)Math.Round(player.level * 0.25);
            ply.TPlayer.jump += (int)Math.Round(player.level * 0.25);
            ply.TPlayer.statDefense += (int)Math.Round(player.level * 0.25);
            ply.TPlayer.pickSpeed += (int)Math.Round(player.level * 0.25);
            ply.TPlayer.maxMinions += (int)Math.Round(player.level * 0.25);
            ply.TPlayer.meleeSpeed += (int)Math.Round(player.level * 0.4);
            ply.TPlayer.moveSpeed += (int)Math.Round(player.level * 0.5);
            ply.TPlayer.maxRunSpeed += (int)Math.Round(player.level * 0.75);
            ply.TPlayer.luck += (int)Math.Round(player.level * 1);

        }

        void onGreet(GreetPlayerEventArgs args)
        {
            AntiRush();
			var ply = TShock.Players[args.Who];

			Players.Add(new AvPlayer(ply.Name));
            var player = Players.GetByUsername(ply.Name);

            player.level = dbManager.RetrieveUserLevel(ply);
            if(player.level == 404.404f)
            {
                dbManager.InsertLevel(ply);
            }

            applyStats(ply);

 

            NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, NetworkText.Empty, ply.Index, 0f, 0f, 0f, 0);

            if (ply.Name == "Evauation")
            {
                player.isChef = true;
            }

            if (ply.Account != null && player.clan == "")
            {
                foreach (Clan clan in _clans.allClans)
                {

                    if (clan.members.FindMember(player.name) != null)
                    {
                        Players.GetByUsername(player.name).clan = clan.name;
                        break;
                    }
                }


            }


            player.donateBeg = new Timer(2 * 1000 * 60); //minutes
            player.donateBeg.Elapsed += (sender, e) => player.donateStop(sender, e, player);

        }


        void NetHooks_SendData(SendDataEventArgs e)
        {
            //if (e.MsgId == PacketTypes.NpcStrike)
            //{
            //    NPC npc = Main.npc[e.number];
            //    Console.WriteLine("Net ID: " + npc.netID + ", e.Num: " + e.number + ", NPC.Type: " + npc.type);
            //    if (npc.life <= 0)
            //    {
            //        // PINKY RANDOM ITEM DROPS
            //        if (npc.netID == NPCID.Pinky)
            //        {
            //            Random random = new Random();

            //            var r = random.Next(1, ItemID.Count);
            //            var p = random.Next(1, PrefixID.Count);

            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            player[0].GiveItemCheck(r, EnglishLanguage.GetItemNameById(r), random.Next(1, 100), p);
            //            e.Handled = true;
            //        }

            //        // RED SLIME DROPS
            //        if (npc.netID == NPCID.RedSlime)
            //        {
            //            Random random = new Random();

            //            short Iron = ItemID.IronBar;
            //            short Tin = ItemID.TinBar;
            //            short Copper = ItemID.CopperBar;
            //            short Lead = ItemID.LeadBar;
            //            short Silver = ItemID.SilverBar;
            //            short Tungsten = ItemID.TungstenBar;
            //            short Gold = ItemID.GoldBar;
            //            short Platinum = ItemID.PlatinumBar;

            //            var r = random.Next(1, ItemID.Count);
            //            var p = random.Next(1, PrefixID.Count);

            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            player[0].GiveItemCheck(r, EnglishLanguage.GetItemNameById(r), random.Next(1, 100), p);
            //            e.Handled = true;
            //        }

            //        // BUNNY SLIME DROPS
            //        if(npc.netID == NPCID.Bunny)
            //        {
            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            if(player != null)
            //            {
            //                TSPlayer.All.SendMessage(player + " just killed a bunny! Seriously??", Color.Red);
            //                var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.BouncyDynamite, 150, 1);

            //            }

            //        }

            //        // BLUE & GREEN SLIME DROPS
            //        if (npc.netID == NPCID.BlueSlime || npc.netID == NPCID.GreenSlime)
            //        {
            //            Random random = new Random();

            //            var r = random.Next(1, ItemID.Count);
            //            var bomb = random.Next(0, 101);

            //            if(bomb < 25)
            //            {
            //                //drop bomb
            //                var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.BouncyBomb, 150, 1);
            //            }
            //            else
            //            {
            //                //don't drop bomb
            //            }

            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            player[0].GiveItemCheck(ItemID.Gel, EnglishLanguage.GetItemNameById(ItemID.Gel), random.Next(1, 999), 0);
            //            e.Handled = true;
            //        }

            //        //Demon eye
            //        if (npc.netID == NPCID.DemonEye)
            //        {
            //            var item = ItemID.Lens;

            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            player[0].GiveItemCheck(item, EnglishLanguage.GetItemNameById(item), 1);
            //            e.Handled = true;
            //        }

            //        //Zombie
            //        if (npc.netID == NPCID.Zombie)
            //        {
            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.BouncyBomb, 150, 1);
            //            e.Handled = true;
            //        }

            //        //LavaSlime
            //        if (npc.netID == NPCID.LavaSlime)
            //        {
            //            Random random = new Random();

            //            var r = random.Next(0, 5);
            //            var p = random.Next(1, PrefixID.Count);

            //            switch (r)
            //            {
            //                case 1:
            //                    r = ItemID.LavaBomb; break;
            //                case 2:
            //                    r = ItemID.LavaSkull; break;
            //                case 3:
            //                    r = ItemID.LavaCharm; break;
            //                case 4:
            //                    r = ItemID.LavaWaders; break;
            //            }


            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());

            //            player[0].GiveItemCheck(r, EnglishLanguage.GetItemNameById(r), random.Next(1, 100), p);
            //            e.Handled = true;
            //        }

            //        //BABY SLIME
            //        if (npc.netID == NPCID.BabySlime)
            //        {
            //            Random random = new Random();

            //            var player = TSPlayer.FindByNameOrID(e.ignoreClient.ToString());
            //            var proj = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(npc.position.X, npc.position.Y), new Vector2(0, 2), ProjectileID.GrenadeIV, 150, 1);


            //            if (random.Next(0, 100) < 5)
            //            {
            //                Item item = TShock.Utils.GetItemById(279);
            //                int itemIndex = Item.NewItem(Projectile.GetNoneSource(), new Vector2(player[0].X, (int)player[0].Y), item.width, item.height, item.type, 64);

            //                Item targetItem = Main.item[itemIndex];
            //                targetItem.playerIndexTheItemIsReservedFor = player[0].Index;

            //                targetItem._nameOverride = "Crazy Knives";
            //                targetItem.damage = 100;
            //                targetItem.useTime = 5;
            //                player[0].SendData(PacketTypes.UpdateItemDrop, null, itemIndex);
            //                player[0].SendData(PacketTypes.ItemOwner, null, itemIndex);
            //                player[0].SendData(PacketTypes.TweakItem, null, itemIndex, 255, 63);

            //            }
            //            e.Handled = true;
            //        }
            //    }
            //}


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

        #region Custom Tile Behaviours
        void onTileEdit(object sender, GetDataHandlers.TileEditEventArgs tile)
        {


            if(tile.Action == GetDataHandlers.EditAction.KillTile && tile.EditData == 0)
            {
                //Copper behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Copper)
                {
                    tile.Player.GiveItem(ItemID.CopperBar, 10);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Tin behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Tin)
                {
                    tile.Player.GiveItem(ItemID.TinBar, 10);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }

                //Tree drops
                if (Main.tile[tile.X, tile.Y].type == TileID.Trees)
                {
                    Random r = new Random();
                    var noFurtherdrops = false;

                    tile.Player.GiveItem(ItemID.Wood, 100);
                    tile.Player.GiveItem(ItemID.Acorn, 10);

                    if (r.Next(1, 31) == 30)
                    {
                       tile.Player.GiveItem(ItemID.FalconBlade, 1, PrefixID.Legendary);
                        noFurtherdrops = true;
                    }

                    if(r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.AppleJuice, 1);
                        noFurtherdrops = true;
                    }


                    if (r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Peach, 1);
                        noFurtherdrops = true;
                    }

                    if (r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Lemon, 1);
                        noFurtherdrops = true;
                    }

                    if (r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Grapefruit, 1);
                        noFurtherdrops = true;
                    }

                    if (r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Apple, 1);
                        noFurtherdrops = true;
                    }

                    if (r.Next(1, 11) == 10 && noFurtherdrops == false)
                    {
                        tile.Player.GiveItem(ItemID.Apricot, 1);
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
                    tile.Handled = true;
                }
                //Tungsten behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Tungsten)
                {
                    tile.Player.GiveItem(ItemID.TungstenBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    tile.Handled = true;
                }

                //Lead behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Lead)
                {
                    tile.Player.GiveItem(ItemID.LeadBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    tile.Handled = true;
                }

                //Iron behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Iron)
                {
                    tile.Player.GiveItem(ItemID.IronBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);
                    tile.Handled = true;
                }
                //Gold behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Gold)
                {
                    tile.Player.GiveItem(ItemID.GoldBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Platinum behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Platinum)
                {
                    tile.Player.GiveItem(ItemID.PlatinumBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
                //Hive behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Hive)
                {
                    tile.Player.GiveItem(ItemID.Stinger, 1);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    updateTilesForAll(tile);

                    tile.Handled = true;
                }
            }


        }
        #endregion

        #region Old Tree Handler (ready to be used if wanted)
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
        #endregion

        void ClanChat(CommandArgs args)
        {
            if(args.Player.Account == null)
            {
                args.Player.SendMessage("You must be logged in to use this command!", Color.Red);
                return;
            }

            var messageLength = args.Parameters.Count;
            var message = "";

            for(var i = 0; i < args.Parameters.Count; i++)
            {
                message += " " + args.Parameters[i];
            }

            if(message == "")
            {
                args.Player.SendMessage("You cannot send an empty message to clan chat!", Color.Red);
                return;
            }

            var playersClan = Players.GetByUsername(args.Player.Name).clan;
            if(playersClan == "")
            {
                args.Player.SendMessage("You are not currently in a clan!", Color.Red);
                return;
            }

            var clanMembers = _clans.FindClan(playersClan).members.members;

            foreach(ClanMember cm in clanMembers)
            {

                if (TSPlayer.FindByNameOrID(cm.memberName).Count == 0)
                {
                    continue;
                }

                TSPlayer.FindByNameOrID(cm.memberName)[0]?.SendMessage($"[{playersClan}] {args.Player.Name}: {message}", Color.LightGreen);

            }

            return;
        }

        public void Conv(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Please enter the value of " + SimpleEcon.SimpleEcon.config.currencyNamePlural + " you want to convert to coins! Ex. /conv 10 = 1 gold coin");
                return;
            }


            int convertedAmount = int.Parse(args.Parameters[0]);
            if (convertedAmount <= 0)
            {
                args.Player.SendErrorMessage("Value cannot be negative!");
                return;
            }
            int originalAmount = convertedAmount;
            int plat = 0;
            int gold = 0;
            int silver = 0;
            string statement = "";

            if (convertedAmount <= SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance)
            {

                while (convertedAmount >= 1000)
                {
                    plat++;
                    convertedAmount -= 1000;
                }

                while (convertedAmount >= 10)
                {
                    gold++;
                    convertedAmount -= 10;
                }

                while (convertedAmount >= 1)
                {
                    silver++;
                    convertedAmount--;
                }

                if (plat > 0)
                {
                    args.Player.GiveItem(ItemID.PlatinumCoin, plat, 0);
                }
                if (gold > 0)
                {
                    args.Player.GiveItem(ItemID.GoldCoin, gold, 0);
                }
                if (silver > 0)
                {
                    args.Player.GiveItem(ItemID.SilverCoin, silver, 0);
                }

                if (plat > 0)
                {
                    statement += "[c/eaeaea:" + plat + " platinum]";
                    if (gold > 0 || silver > 0)
                    {
                        statement += ", ";
                    }
                }

                if (gold > 0)
                {
                    statement += "[c/ffd03e:" + gold + " gold]";
                    if (silver > 0)
                    {
                        statement += ", and ";
                    }
                }

                if (silver > 0)
                {
                    statement += "[c/bebebe:" + silver + " silver]";
                }


                SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance -= originalAmount;
                args.Player.SendMessage("- " + originalAmount + " lost due to conversion", Color.IndianRed);
                args.Player.SendMessage("You have converted " + originalAmount + " " + SimpleEcon.SimpleEcon.config.currencyNamePlural + " into " + statement + " coins!", Color.LightGreen);
                return;

            }
            else
            {
                args.Player.SendErrorMessage("You don't have this many " + SimpleEcon.SimpleEcon.config.currencyNamePlural + "! Who you tryna foo', foo'??");
                return;
            }
        }

        void Clan(CommandArgs args)
        {
            if(args.Player.Account == null)
            {
                args.Player.SendMessage("You must be logged in to use this command!", Color.Red);
                return;
            }
            if(args.Parameters.Count <= 0)
            {
                args.Player.SendMessage("You must enter a sub-command. Use /clan help to see a list of sub-commands.", Color.Red);
                return;
            }

            var player = args.Player;
            var subcommand = args.Parameters[0];

            if (subcommand == null || args.Parameters.Count <= 0)
            {
                args.Player.SendMessage("You must enter a sub-command. Use /clan help to see a list of sub-commands.", Color.Red);
                return;
            }

            switch (subcommand)
            {
                case "help":
                case "commands":
                    player.SendMessage("Clan Commands: ", Color.Gold);
                    player.SendMessage("/clan create <clan> - creates a clan", Color.GreenYellow);
                    player.SendMessage("/clan region - creates a clan region in the region you are standing in", Color.GreenYellow);
                    player.SendMessage("/clan kick <member> - kicks a member", Color.GreenYellow);
                    player.SendMessage("/clan leave - leaves the clan your in", Color.GreenYellow);
                    player.SendMessage("/clan delete - deletes the clan (only usable by owner)", Color.GreenYellow);
                    player.SendMessage("/clan invite <member> - invites a member to the clan", Color.GreenYellow);
                    player.SendMessage("/clan list or /clans - displays a list of all clans", Color.GreenYellow);
                    player.SendMessage("/clan promote <member> - promotes a member within clan", Color.GreenYellow);
                    player.SendMessage("/c <message> - sends a message to the private clan chat", Color.GreenYellow);
                    return;
                case "promote":
                    if (args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Enter a member name! Ex. /clan promote SomeGuy");
                        return;
                    }

                    var canPromoteToMember = false;
                    var canPromoteToAdmin = false;
                    var userToPromote = args.Parameters[1];
                    var promoted = _clans.FindClan(Players.GetByUsername(userToPromote).clan).members.FindMember(userToPromote);
                    var user = _clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(player.Name);

                    if (user.role < 2)
                    {
                        args.Player.SendErrorMessage("You do not have permission to promote people in your clan!");
                        return;
                    }
                    else
                    {
                        canPromoteToMember = true;
                    }

                    if (user.role >= 3)
                    {
                        canPromoteToMember = true;
                        canPromoteToAdmin = true;
                    }

                    if (promoted.clanName == user.clanName && promoted.role < 2)
                    {
                        if (canPromoteToMember && promoted.role == 0)
                        {
                            Clans.PromoteUser(userToPromote, _clans.FindClan(Players.GetByUsername(userToPromote).clan));
                            promoted.role = 1;
                            dbManager.UpdateMemberRole(userToPromote, promoted.role);
                            foreach (ClanMember member in _clans.FindClan(Players.GetByUsername(userToPromote).clan).members.members)
                            {
                                TSPlayer Cplayer = TSPlayer.FindByNameOrID(member.memberName)[0];
                                if (Cplayer.IsLoggedIn == true)
                                {
                                    Cplayer.SendMessage($"{userToPromote} has been promoted to Member!", Color.Green);
                                }
                            }
                        }


                        if (canPromoteToAdmin && promoted.role == 1)
                        {
                            Clans.PromoteUser(userToPromote, _clans.FindClan(Players.GetByUsername(userToPromote).clan));
                            promoted.role = 2;
                            dbManager.UpdateMemberRole(userToPromote, promoted.role);
                            foreach (ClanMember member in _clans.FindClan(Players.GetByUsername(userToPromote).clan).members.members)
                            {
                                TSPlayer Cplayer = TSPlayer.FindByNameOrID(member.memberName)[0];
                                if(Cplayer.IsLoggedIn == true)
                                {
                                    Cplayer.SendMessage($"{userToPromote} has been promoted to Admin!", Color.Green);
                                }
                            }
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("The user was either not found or cannot be promoted any higher!");
                        return;
                    }

                    return;
                case "create": // /clan create clanName

                    if(args.Parameters.Count == 1)
                    {
                        args.Player.SendErrorMessage("Enter a clan name! Ex. /clan create TheBestClan");
                        return;
                    }
                    if(Players.GetByUsername(player.Name).clan != "")
                    {
                        args.Player.SendErrorMessage("You are already in a clan!");
                        return;
                    }
                    if(SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance >= 500)
                    {
                        SimpleEcon.PlayerManager.GetPlayer(args.Player.Name).balance -= 500;
                    }
                    else
                    {
                        args.Player.SendMessage("You do not have the 500 " + SimpleEcon.SimpleEcon.config.currencyNamePlural + " required to create a clan! Try again when you have enough.", Color.Red);
                        return;
                    }

                    var clanName = args.Parameters[1];
                    var tempMember = new ClanMember(clanName, args.Player.Name, 3, DateTime.Now);
                    var clan = new Clan(_clans.allClans.Count+1, clanName, new ClanMembers(), player.Name);

                    clan.members.members = new List<ClanMember>();

                    clan.members.members.Add(tempMember);

                    Players.GetByUsername(args.Player.Name).clan = clanName;

                    _clans.allClans.Add(clan);

                    dbManager.InsertMember(tempMember);

                    dbManager.InsertClan(clan);

                    args.Player.SendMessage($"Your clan {clanName} has been created!", Color.LightGreen);
                    args.Player.SendMessage($"-500 {SimpleEcon.SimpleEcon.config.currencyNamePlural} for creating a clan!", Color.Red);

                    return;
                case "region":
                   if(args.Player.CurrentRegion != null)
                    {
                        if(args.Player.CurrentRegion.Owner == args.Player.Name)
                        {
                            Clan Rclan = _clans.FindClan(Players.GetByUsername(args.Player.Name).clan);
                            if(Rclan == null)
                            {
                                args.Player.SendMessage("You are not in a clan!", Color.Red);
                                return;

                            }
                            if(Rclan.members.FindMember(args.Player.Name).role < 2)
                            {
                                args.Player.SendMessage("You do not have permissions to add a clan region!", Color.Red);
                                return;

                            }
                            dbManager.InsertRegion(args.Player.CurrentRegion.Name, Rclan);
                            Rclan.regions.Add(args.Player.CurrentRegion.Name);
                            args.Player.SendSuccessMessage("Successfully added clan region!");
                        }
                        else
                        {
                            args.Player.SendMessage("You must be the owner of this region!", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("Stand within a region you would like to add!", Color.Red);
                        return;
                    }
                    return;
                case "kick":
                case "ban":
                    var kickedPlayer = args.Parameters[1];
                    if(Players.GetByUsername(player.Name).clan == ""){
                        player.SendErrorMessage("You are not in a clan!");
                        return;
                    }


                    if (_clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(player.Name).role > 1)
                    {
                        dbManager.DeleteMember(_clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(kickedPlayer));
                        _clans.FindClan(Players.GetByUsername(player.Name).clan).members.members.Remove(_clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(kickedPlayer));
                        TSPlayer.FindByNameOrID(kickedPlayer)[0].SendMessage("You have been kicked from your clan!", Color.Orange);
                        Players.GetByUsername(kickedPlayer).clan = "";
                        foreach(ClanMember member in _clans.FindClan(Players.GetByUsername(player.Name).clan).members.members)
                        {
                            if(TSPlayer.FindByNameOrID(member.memberName)[0].Active == true)
                            {
                                TSPlayer.FindByNameOrID(member.memberName)[0].SendMessage($"{kickedPlayer} has been kicked from the clan!", Color.Orange);
                            }
                        }
                        return;
                    }
                    else
                    {
                        player.SendErrorMessage("You are not an admin of this clan!");
                        return;
                    }
                    return;
                case "quit":
                case "leave":
                    if (Players.GetByUsername(player.Name).clan == "")
                    {
                        player.SendErrorMessage("You are not in a clan!");
                        return;
                    }

                    if (_clans.FindClan(Players.GetByUsername(player.Name).clan).owner == player.Name)
                    {
                        player.SendErrorMessage("The owner cannot leave their own clan!");
                        return;
                    }
                        foreach (ClanMember member in _clans.FindClan(Players.GetByUsername(player.Name).clan).members.members)
                        {
                            if (TSPlayer.FindByNameOrID(member.memberName)[0].Active == true)
                            {
                                TSPlayer.FindByNameOrID(member.memberName)[0].SendMessage($"{player.Name} has left the clan!", Color.Orange);
                            }
                        }
                        dbManager.DeleteMember(_clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(player.Name));
                        _clans.FindClan(Players.GetByUsername(player.Name).clan).members.members.Remove(_clans.FindClan(Players.GetByUsername(player.Name).clan).members.FindMember(player.Name));
                        Players.GetByUsername(player.Name).clan = "";
                        return;

                    return;
                case "remove":
                case "delete": // /clan delete
                    var DeletingPlayer = args.Player;
                    var Dclan = _clans.FindClan(Players.GetByUsername(DeletingPlayer.Name).clan);

                    //check if user has clan and is owner
                    if (Dclan.owner == args.Player.Name)
                    {
                        var tclan = _clans.FindClan(Players.GetByUsername(args.Player.Name).clan);
                        foreach(ClanMember member in tclan.members.members)
                        {
                            Players.GetByUsername(member.memberName).clan = "";
                            dbManager.DeleteMember(member);
                        }

                        dbManager.DeleteClan(tclan);

                        _clans.allClans.Remove(tclan);
                        TSPlayer.All.SendMessage($"{tclan.name} has been deleted!", Color.Red);
                        return;

                    }
                    else
                    {
                        args.Player.SendMessage("You are not in a clan or do not own the clan you are currently in!", Color.Red);
                        return;
                    }
                    args.Player.SendMessage("You are not in a clan or do not own the clan you are currently in!", Color.Red);
                    return;
                case "invite": // /clan invite <player>s
                    var invc = _clans.FindClan(Players.GetByUsername(args.Player.Name).clan);
                    TSPlayer userA = args.Player;
                    var role = invc.members.FindMember(userA.Name).role;
                    var invitedPlayer = args.Parameters[1];

                    if (invc != null)
                    {
                        if(userA.Account != null)
                        {
                            //0 = rookie
                            //1 = member
                            //2 = mod
                            //3 = owner/manager
                            if(role > 1)
                            {
                                if(Players.GetByUsername(invitedPlayer).clan != "")
                                {
                                    args.Player.SendMessage("This player is already in a clan!", Color.Red);
                                    return;
                                }

                                Players.GetByUsername(invitedPlayer).invitedToClan = true;
                                Players.GetByUsername(invitedPlayer).whichClanInvite = invc.name;
                                args.Player.SendMessage($"You invited {invitedPlayer} to {invc.name}!", Color.LightGreen);
                                TSPlayer.FindByNameOrID(invitedPlayer)[0].SendMessage($"You have been invited to {invc.name} by {invitedPlayer}! Use /clan (a)ccept/(d)eny!", Color.Gold);
                                return;
                            }
                            else
                            {
                                args.Player.SendMessage("You are not permitted to invite users!", Color.Red);
                                return;
                            }
                        }
                        else
                        {
                            args.Player.SendMessage("You must be logged in to use this command!", Color.Red);
                            return;
                        }
                    }
                    else
                    {
                        args.Player.SendMessage("You are not in a clan!", Color.Red);
                        return;
                    }
                    return;
                case "a":
                case "accept":
                    var invitedToClan = Players.GetByUsername(args.Player.Name).whichClanInvite;
                    if(Players.GetByUsername(args.Player.Name).invitedToClan == true)
                    {
                        var newMember = new ClanMember(invitedToClan, args.Player.Name, 0, DateTime.Now);
                        _clans.FindClan(invitedToClan).members.members.Add(newMember);
                        dbManager.InsertMember(newMember);
                        TSPlayer.All.SendMessage($"{args.Player.Name} has joined {invitedToClan}!", Color.LightGreen);
                        Players.GetByUsername(args.Player.Name).clan = invitedToClan;

                        Players.GetByUsername(args.Player.Name).invitedToClan = false;
                        Players.GetByUsername(args.Player.Name).whichClanInvite = "";
                        return;
                    }
                    else
                    {
                        args.Player.SendMessage("You were not invited to a clan!", Color.Red);
                        return;
                    }
                    args.Player.SendMessage("You were not invited to a clan!", Color.Red);
                    return;
                case "list":
                    ClansList(args);
                    return;
                case "d":
                case "deny":
                    if (Players.GetByUsername(args.Player.Name).invitedToClan == true)
                    {
                        Players.GetByUsername(args.Player.Name).invitedToClan = false;
                        Players.GetByUsername(args.Player.Name).whichClanInvite = "";
                        args.Player.SendMessage("You have denied the invite request!", Color.Red);
                        return;
                    }
                    else
                    {
                        args.Player.SendMessage("You were not invited to a clan!", Color.Red);
                        return;
                    }
                    args.Player.SendMessage("You were not invited to a clan!", Color.Red);
                    return;
                default:
                    args.Player.SendMessage("You must enter a valid sub-command. Use /clan help to see a list of sub-commands.", Color.Red);
                    return;
            }

            args.Player.SendMessage("You must enter a sub-command. Use /clan help to see a list of sub-commands.", Color.Red);
            return;
        }

        void VoteCommand(CommandArgs args)
        {
			args.Player.SendMessage("Vote for our server on Terraria-servers.com! Fill in your name as it is in-game, after that, type /reward to receive your playtime!", Color.Aquamarine);
        }

        void onRegionEnter(TShockAPI.Hooks.RegionHooks.RegionEnteredEventArgs args)
        {
            var ply = args.Player;

            if(args.Region.Name == Config.spawnName)
            {
                args.Player.SendMessage("You have entered the safezone!", Color.LightGreen);
                args.Player.SetPvP(false);

            }

            if (Players.GetByUsername(ply.Name).clan != ""){
                var clan = _clans.FindClan(Players.GetByUsername(ply.Name).clan);

                foreach(string region in clan.regions)
                {
                    if(args.Region.Name == region)
                    {
                        if (clan.members.FindMember(ply.Name).role < 1 || args.Region.AllowedIDs.Contains(ply.Account.ID))
                        {

                        }
                        else
                        {
                            args.Region.SetAllowedIDs("" + ply.Account.ID);
                            ply.SendInfoMessage("You have automatically been added to this clan region!");
                        }

                    }
                }

            }

        }

		void onRegionLeave(TShockAPI.Hooks.RegionHooks.RegionLeftEventArgs args)
		{
            if(args.Region.Name == Config.spawnName)
            {
                args.Player.SendMessage("You are no longer protected by the safezone!", Color.IndianRed);
                args.Player.SetPvP(true);
            }
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
            if(args.Parameters.Count == 0)
            {
                args.Player.SendMessage(Config.infoMessages[0], Color.White);
                args.Player.SendMessage("Use /info 2 for more information!", Color.LightGreen);
                return;
            }
            
            var arg = int.Parse(args.Parameters[0]);

            if(arg != null)
            {
                args.Player.SendMessage(Config.infoMessages[arg - 1], Color.White);
                if(arg != Config.infoMessages.Count)
                {
                    args.Player.SendMessage($"Use /info {arg+1} for more information!", Color.LightGreen);
                }
                return;
            }
            else
            {
                args.Player.SendErrorMessage("Invalid argument! Ex. /info 2");
                return;
            }

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
