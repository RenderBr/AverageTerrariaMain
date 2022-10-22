using AverageTerrariaMain;
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

        void onPlayerDeath(object sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            TSPlayer player = TSPlayer.FindByNameOrID(args.PlayerDeathReason._sourcePlayerIndex.ToString())[0];

            if(Players.GetByUsername(player.Name).isBountied == true)
            {
                TimeRanks.TimeRanks.Players.GetByUsername(player.Name).totalCurrency += Players.GetByUsername(player.Name).bountyPrice;
                Players.GetByUsername(player.Name).isBountied = false;
                TSPlayer.All.SendMessage(player.Name + " has claimed the bounty on " + args.Player.Name + " and won " + Players.GetByUsername(player.Name).bountyPrice + " dollas!", Color.IndianRed);
            }
            
        }

        void onBossSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];

            if(npc.netID == NPCID.EyeofCthulhu)
            {
                
                TSPlayer.All.SendMessage("A suspicious eye is upon us!", Color.Red);
                npc.lifeMax = 10000;
                npc.life = 10000;
                npc.height = 100;
                npc.width = 100;
                npc.GivenName = "Suspicious Eye";
                NetMessage.SendData(23, -1, -1, (NetworkText) null, args.NpcId);
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


            bcTimer = new Timer(Config.bcInterval*1000*60); //minutes

			bcTimer.Elapsed += broadcastMessage;
			bcTimer.AutoReset = true;
			bcTimer.Enabled = true;
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
