﻿using AverageTerrariaMain;
using System;
using Terraria.ObjectData;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.ID;
using System.Timers;
using Microsoft.Xna.Framework;
using TShockAPI.Localization;

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
            ServerApi.Hooks.NetSendData.Register(this, NetHooks_SendData);
            ServerApi.Hooks.NpcSpawn.Register(this, onBossSpawn);
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

        void onBossSpawn(NpcSpawnEventArgs args)
        {
            NPC npc = Main.npc[args.NpcId];

            if(npc.netID == NPCID.EyeofCthulhu)
            {
                
                TSPlayer.All.SendMessage("A suspicious looking eye is upon us!", Color.Red);
                npc.lifeMax = 3600;
                npc.life = 3600;
                npc.GivenName = "Suspicious Eye";
                npc.scale = 500;
                TSPlayer.All.Sen(PacketTypes.Npc, "", args.NpcId);
                args.Handled = true;
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

			bcTimer = new Timer(Config.bcInterval*1000*60); //minutes

			bcTimer.Elapsed += broadcastMessage;
			bcTimer.AutoReset = true;
			bcTimer.Enabled = true;
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
                    tile.Handled = true;
                }
                //Tin behaviour
                if (Main.tile[tile.X, tile.Y].type == TileID.Tin)
                {
                    tile.Player.GiveItem(ItemID.TinBar, 10);
                    Main.tile[tile.X, tile.Y].type = TileID.Hellstone;
                    Main.tile[tile.X, tile.Y].active(true);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
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

                        handleTree(tile.X, tile.Y, tile.Player);
                        
                    

                        tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                        tile.Handled = true;
                    

                }

                //Silver behaviour
                if(Main.tile[tile.X, tile.Y].type == TileID.Silver){
                    tile.Player.GiveItem(ItemID.SilverBar, 20);
                    Main.tile[tile.X, tile.Y].active(false);
                    tile.Player.SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
                    int p = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(tile.Player.X, tile.Player.Y), new Vector2(0, 0), ProjectileID.BombSkeletronPrime, 100, 10);
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
