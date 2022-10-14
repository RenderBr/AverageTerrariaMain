using AverageTerrariaMain;
using System;
using Terraria.ObjectData;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.ID;
using System.Timers;
using Microsoft.Xna.Framework;

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
        public override string Name => "Average's Terraria";

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
        public override string Description => "Provide's some functionality for Average's Terraria server.";

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
			TShockAPI.Hooks.RegionHooks.RegionEntered += onRegionEnter;
			TShockAPI.Hooks.RegionHooks.RegionLeft += onRegionLeave;
			Console.WriteLine("Average Main LOADED");
        }

		public void broadcastMessage(Object source, ElapsedEventArgs args)
        {
			Random rnd = new Random();
			TSPlayer.All.SendMessage("[" + Config.serverName + "] " + Config.broadcastMessages[rnd.Next(0, Config.broadcastMessages.Count)], Microsoft.Xna.Framework.Color.Aquamarine);
        }

        void onInitialize(EventArgs e)
        {
            Config = Config.Read();
            Commands.ChatCommands.Add(new Command("av.info", infoCommand, "info"));
            Commands.ChatCommands.Add(new Command("av.boss", fightCommand, "boss"));
			Commands.ChatCommands.Add(new Command("av.apply", applyStaffCommand, "apply", "applyforstaff"));
			Commands.ChatCommands.Add(new Command("av.pvp", tpToPvpCommand, "pvparena", "parena"));
			Commands.ChatCommands.Add(new Command("av.boss", tpToPvpCommand, "bossarena", "arena", "barena"));
			Commands.ChatCommands.Add(new Command("av.discord", discordInvite, "discord"));
            Commands.ChatCommands.Add(new Command("av.reload", reloadCommand, "avreload"));
			Commands.ChatCommands.Add(new Command("av.stuck", stuckCommand, "stuck", "imstuck"));
			Commands.ChatCommands.Add(new Command("av.vanish", vanishCommand, "vanish", "invis"));
			Commands.ChatCommands.Add(new Command("av.stoprain", stopRainCommand, "stoprain", "sr"));
			Commands.ChatCommands.Add(new Command("av.tpAverage", tpToAverage, "average", "av", "tpav"));



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

		void stopRainCommand(CommandArgs args)
        {
			if(Main.IsItRaining == true)
            {
				Main.StopRain();
				TSPlayer.All.SendData(PacketTypes.WorldInfo);
				TSPlayer.All.SendInfoMessage("{0} stopped the rain!", args.Player.Account.Name);
            }
            else
            {
				args.Player.SendInfoMessage("It's not currently raining?");
            }
        }

		void tpToAverage(CommandArgs args)
        {
			TSPlayer average = TSPlayer.FindByNameOrID("Average")[0];

            if (!average.TPAllow)
            {
				args.Player.SendMessage("Average has currently disabled TPs! :<", Color.OrangeRed);
				return;
			}

			if (average.ConnectionAlive == true)
			{
				args.Player.Teleport(average.LastNetPosition.X, average.LastNetPosition.Y);
				args.Player.SendMessage("You have been teleported to Average! :>", Color.Aquamarine);
			}
			else
            {
				args.Player.SendMessage("Average is not currently online! :<", Color.OrangeRed);
            }
        }

		void onRegionEnter(TShockAPI.Hooks.RegionHooks.RegionEnteredEventArgs args)
        {

			if(args.Region.Name == Config.pvpArena)
            {
				args.Player.SetPvP(true);
				args.Player.SendInfoMessage("Your PvP has been auto-turned on!");
            }
        }

		void onRegionLeave(TShockAPI.Hooks.RegionHooks.RegionLeftEventArgs args)
		{
			if (args.Region.Name == Config.pvpArena)
			{
				args.Player.SetPvP(false);
				args.Player.SendInfoMessage("Your PvP has been auto-turned off!");
			}
		}

		void applyStaffCommand(CommandArgs args)
        {
			args.Player.SendInfoMessage("Head to averageterraria.lol, register an account, and fill out the staff template under the 'Staff Applications' tag! Thanks for considering applying :)");
		}

		void tpToPvpCommand(CommandArgs args)
        {
			var warp = TShock.Warps.Find(Config.pvpArena);
			var player = args.Player;

			player.Teleport(warp.Position.X * 16, warp.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent to the PvP arena!");
		}

		void tpToArena(CommandArgs args)
		{
			var warp = TShock.Warps.Find(Config.arenaRegionName);
			var player = args.Player;

			player.Teleport(warp.Position.X * 16, warp.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent to the boss arena!");
		}

		//coming soon-ish? if i can figure out how to implement
		void vanishCommand(CommandArgs args)
        {
			var player = Players.GetByUsername(args.Player.Name);


			//if (player.isVanished == true)
   //         {

   //         }
   //         else
   //         {
			//	TSPlayer.All.SendMessage(player.name + " has left.", Microsoft.Xna.Framework.Color.LightYellow);
			//	player.tsPlayer.SetBuff(BuffID.Invisibility, 360000);
				
				
   //         }
        }

		void stuckCommand(CommandArgs args)
        {
			var player = args.Player;
			var spawn = TShock.Warps.Find(Config.spawnName);

			player.Teleport(spawn.Position.X * 16, spawn.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent back to spawn! Unstuck :>");
		}

		void onChat(ServerChatEventArgs args)
        {

            
        }

        void infoCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage(Config.infoMessage);
        }

        void fightCommand(CommandArgs args)
        {
            if(args.Player.CurrentRegion == null)
            {
                args.Player.SendWarningMessage("You aren't in the arena! Go to /warp arena to summon bosses!");
                return;
            }

            if (args.Player.CurrentRegion.Name == Config.arenaRegionName)
            {
                if (args.Parameters.Count > 0)
                {
                    NPC npc = new NPC();
                    int amount = 1;
                    string spawnName;
					if(EssentialsPlus.Commands.FreezeTimer.Enabled == true)
                    {
						EssentialsPlus.Commands.FreezeTimer.Stop();
                    }
                    switch (args.Parameters[0].ToLower())
                    {
                        case "*":
                        case "all":
                                int[] npcIds = { 4, 13, 35, 50, 125, 126, 127, 134, 222, 245, 262, 266, 370, 398, 439, 636, 657 };
                                TSPlayer.Server.SetTime(false, 0.0);
                                foreach (int i in npcIds)
                                {
                                    npc.SetDefaults(i);
                                    TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                                }
                                spawnName = "all bosses";
                                break;
                        case "brain":
                        case "brain of cthulhu":
                        case "boc":
                                npc.SetDefaults(266);
                                TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
                                spawnName = "Brain of Cthulhu";
                                break;
						case "destroyer":
							npc.SetDefaults(134);
							TSPlayer.Server.SetTime(false, 0.0);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Destroyer";
							break;
						case "duke":
						case "duke fishron":
						case "fishron":
							npc.SetDefaults(370);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Duke Fishron";
							break;
						case "eater":
						case "eater of worlds":
						case "eow":
							npc.SetDefaults(13);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Eater of Worlds";
							break;
						case "eye":
						case "eye of cthulhu":
						case "eoc":
							npc.SetDefaults(4);
							TSPlayer.Server.SetTime(false, 0.0);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Eye of Cthulhu";
							break;
						case "golem":
							npc.SetDefaults(245);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "the Golem";
							break;
						case "king":
						case "king slime":
						case "ks":
							npc.SetDefaults(50);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "King Slime";
							break;
						case "plantera":
							npc.SetDefaults(262);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Plantera";
							break;
						case "prime":
						case "skeletron prime":
							npc.SetDefaults(127);
							TSPlayer.Server.SetTime(false, 0.0);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Skeletron Prime";
							break;
						case "queen bee":
						case "qb":
							npc.SetDefaults(222);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Queen Bee";
							break;
						case "skeletron":
							npc.SetDefaults(35);
							TSPlayer.Server.SetTime(false, 0.0);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Skeletron";
							break;
						case "twins":
							TSPlayer.Server.SetTime(false, 0.0);
							npc.SetDefaults(125);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							npc.SetDefaults(126);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Twins";
							break;
						case "moon":
						case "moon lord":
						case "ml":
							npc.SetDefaults(398);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Moon Lord";
							break;
						case "empress":
						case "empress of light":
						case "eol":
							npc.SetDefaults(636);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Empress of Light";
							break;
						case "queen slime":
						case "qs":
							npc.SetDefaults(657);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Queen Slime";
							break;
						case "lunatic":
						case "lunatic cultist":
						case "cultist":
						case "lc":
							npc.SetDefaults(439);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Lunatic Cultist";
							break;
						case "betsy":
							npc.SetDefaults(551);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Betsy";
							break;
						case "flying dutchman":
						case "flying":
						case "dutchman":
							npc.SetDefaults(491);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "The Flying Dutchman";
							break;
						case "mourning wood":
							npc.SetDefaults(325);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Mourning Wood";
							break;
						case "pumpking":
							npc.SetDefaults(327);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Pumpking";
							break;
						case "everscream":
							npc.SetDefaults(344);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Everscream";
							break;
						case "santa-nk1":
						case "santa":
							npc.SetDefaults(346);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Santa-NK1";
							break;
						case "ice queen":
							npc.SetDefaults(345);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Ice Queen";
							break;
						case "martian saucer":
							npc.SetDefaults(392);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "A Martian Saucer";
							break;
						case "deerclops":
							npc.SetDefaults(668);
							TSPlayer.Server.SpawnNPC(npc.type, npc.FullName, amount, args.Player.TileX, args.Player.TileY);
							spawnName = "Deerclops";
							break;
						default:
                            args.Player.SendErrorMessage("Invalid boss!");
                            return;

                    }
					TSPlayer.All.SendSuccessMessage("{0} has been summoned at /warp arena!", spawnName);
				}
                else
                {
                    args.Player.SendWarningMessage("Type in a boss name after the command! Ex. /boss king slime");
                }
            }
            else
            {
                args.Player.SendWarningMessage("You aren't in the arena! Go to /warp arena to summon bosses!");

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
				TShockAPI.Hooks.RegionHooks.RegionEntered -= onRegionEnter;
				TShockAPI.Hooks.RegionHooks.RegionLeft -= onRegionLeave;

			}
            base.Dispose(disposing);
        }


        
    }
}
