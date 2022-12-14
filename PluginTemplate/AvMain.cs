#region using
using AverageTerrariaMain;
using System;
using Terraria.ObjectData;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Terraria.ID;
using System.Timers;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using TShockAPI.Localization;
using Terraria.Localization;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;
using System.Data;
using System.Linq.Expressions;
using Terraria.GameContent.Creative;
using Terraria.GameContent.NetModules;
using Terraria.Net;
using ClientApi.Networking;
using TShockAPI.Hooks;
using System.Linq;

#endregion

namespace AverageTerrariaMain
{
    [ApiVersion(2, 1)]
    public class AvMain : TerrariaPlugin
    {
        #region define variables
        internal static readonly AvPlayers Players = new AvPlayers();

		public static List<TSPlayer> frozenPlayers = new List<TSPlayer>();
		public static List<Topic> TopicList = new List<Topic>();
		public static List<Element> ElementList = new List<Element>();
		public Timer bcTimer;
        private IDbConnection _db;
        public static Database dbManager;
		const int MaxItems = 5456;

        public Timer cgTimer;

		public static chatGame cg = new chatGame();

		public class chatGame
        {
			public bool Occuring = false;
			public int answer = 0;
			public string wordAnswer = "";
        }
		public override string Name => "Average's Terraria";

        public override Version Version => new Version(1, 0, 0);
		private DateTime Last;

		public Config Config { get; private set; }

        public override string Author => "Average";
        public override string Description => "Provide's some functionality for Average's Terraria server.";
        public AvMain(Terraria.Main game) : base(game)
        {
            base.Order = 3;
        }
        #endregion
        #region initPlugin & hooks
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
			ServerApi.Hooks.ServerChat.Register(this, onChat);
			ServerApi.Hooks.NetGreetPlayer.Register(this, onGreet);
			ServerApi.Hooks.GameUpdate.Register(this, onUpdate);
			
			On.Terraria.NPC.DoDeathEvents_CelebrateBossDeath += CelebrateBossDeath;
			TShockAPI.Hooks.RegionHooks.RegionEntered += onRegionEnter;
			TShockAPI.Hooks.RegionHooks.RegionLeft += onRegionLeave;
			TShockAPI.GetDataHandlers.TileEdit += onTileEdit;
      switch (TShock.Config.Settings.StorageType.ToLower())
      {
          case "sqlite":
              _db = new SqliteConnection(string.Format("Data Source={0}",
                  Path.Combine(TShock.SavePath, "AvSurvival.sqlite")));
              break;
          default:
              throw new Exception("Invalid storage type.");
      }

      dbManager = new Database(_db);
			Last = DateTime.UtcNow;
		}

        private void CelebrateBossDeath(On.Terraria.NPC.orig_DoDeathEvents_CelebrateBossDeath orig, NPC self)
        {
			return;
        }
        #endregion

        #region AntiGrief

        void updateTilesForAll(GetDataHandlers.TileEditEventArgs tile)
        {
            foreach (Player player in Main.player)
            {
                if (player == null)
                {
                    continue;
                }

                TSPlayer.FindByNameOrID(player.whoAmI.ToString())[0].SendTileSquareCentered(tile.Player.TileX, tile.Player.TileY, 32);
            }
        }

        public void onTileEdit(object sender, GetDataHandlers.TileEditEventArgs tile)
		{
			if (tile.Player.HasPermission("cool"))
			{
				return;
			}
            AvPlayer p = Players.GetByUsername(tile.Player.Name);

            //block broken
            if (tile.Action == GetDataHandlers.EditAction.KillTile && tile.EditData == 0)
			{

                p.blocksBroken.Add(new BrokenBlock());
			}
			else
			{

                return;
			}
			
			if(p.blocksBroken.Count < 250)
			{
				return;
            }

			if(p.blocksBroken.Count(x => DateTime.Now.Subtract(x.brokenAt).TotalSeconds < 30) > 250)
			{
				TShock.UserAccounts.SetUserGroup(tile.Player.Account, "possiblegriefer");
                TSPlayer.All.SendMessage($"{tile.Player.Name} has destroyed a lot of blocks in the last 30 seconds and has been marked as a griefer! Please contact a staff member if you believe this was a false detection! (/discord)", Color.Red);
				return;
			}

			return;	
		}

        #endregion

        #region onWorldLoad / Command Init

        void onInitialize(EventArgs e)
		{
			Config = Config.Read();
			Commands.ChatCommands.Add(new Command("av.info", infoCommand, "info"));
			Commands.ChatCommands.Add(new Command("av.lastonline", LastOnline, "lastonline", "lo"));
			Commands.ChatCommands.Add(new Command("av.boss", fightCommand, "boss"));
			Commands.ChatCommands.Add(new Command("av.apply", applyStaffCommand, "apply", "applyforstaff"));
			Commands.ChatCommands.Add(new Command("av.pvp", tpToPvpCommand, "pvparena", "parena"));
			Commands.ChatCommands.Add(new Command("av.boss", tpToArena, "bossarena", "arena", "barena"));
			Commands.ChatCommands.Add(new Command("av.discord", discordInvite, "discord"));
			Commands.ChatCommands.Add(new Command("av.admin", reloadCommand, "avreload"));
			Commands.ChatCommands.Add(new Command("av.stuck", stuckCommand, "stuck", "imstuck"));
			Commands.ChatCommands.Add(new Command("av.vanish", vanishCommand, "vanish", "invis"));
			Commands.ChatCommands.Add(new Command("av.stoprain", stopRainCommand, "stoprain", "sr"));
			Commands.ChatCommands.Add(new Command("av.tpAverage", tpToAverage, "average", "av", "tpav"));
			Commands.ChatCommands.Add(new Command("av.boss", killBosses, "killbosses", "kb"));
			Commands.ChatCommands.Add(new Command("av.admin", adminAbout, "ab", "adminab", "adminabout"));
			Commands.ChatCommands.Add(new Command("av.helper", Freeze, "f", "freeze"));
			Commands.ChatCommands.Add(new Command("av.jmunlock", journeyUnlockAll, "researchall", "jmunlock"));
            Commands.ChatCommands.Add(new Command("av.info", aboutCommand, "about"));

			Commands.ChatCommands.Add(new Command("av.admin", triggerCg, "chatgame", "cg"));
			cg.Occuring = false;
			cg.answer = 0;

			//Broadcasts
			bcTimer = new Timer(Config.bcInterval * 1000 * 60); //minutes

			bcTimer.Elapsed += broadcastMessage;
			bcTimer.AutoReset = true;
			bcTimer.Enabled = true;

			//Chat games
			cgTimer = new Timer(Config.cgInterval * 1000 * 60); //minutes

			cgTimer.Elapsed += chatGames;
			cgTimer.AutoReset = true;
			cgTimer.Enabled = true;

			dbManager.InitialSync();
		}

		#endregion

		#region checkFrozen
		private void onUpdate(EventArgs args)
        {
			if((DateTime.UtcNow - Last).TotalSeconds > 5)
            {
				if(TShock.Utils.GetActivePlayerCount() > 0)
					foreach(var plr in TShock.Players)
                    {
                        if (frozenPlayers.Contains(plr))
                        {
							plr.Disable("Frozen");
                        }
                    }
            }
        }
        #endregion
        #region journeyUnlock Command
		//courtesy of Sergei / xtpkt AKA my favorite auroraiiaanasjjana (whatever shut up)
        public void journeyUnlockAll(CommandArgs args)
        {

            if (!args.Player.ContainsData("ResearchUnlocked"))
                for (short i = 0; i <= MaxItems; i++)
                {
                    var packet = new PacketFactory()
                  .SetType((byte)PacketTypes.LoadNetModule)
                  .PackUInt16(5)
                  .PackInt16(i)
                  .PackInt16(999)
                  .GetByteData();
                    args.Player.SetData("ResearchUnlocked", true);
                    args.Player.SendRawData(packet);
                }

        }
        #endregion

        #region lastOnline Command

		public void LastOnline(CommandArgs args)
        {
			TSPlayer player = args.Player;

			if(args.Parameters.Count == 0)
            {
				player.SendErrorMessage("Please enter a player name. /lastonline <player>");
				return;
            }

			if (TSPlayer.FindByNameOrID(args.Parameters[0]).Count > 0) {
				player.SendInfoMessage(args.Parameters[0] + " is online now!");
				return;
			}

			var lastOnlinePlayer = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);

			if(lastOnlinePlayer == null)
            {
				player.SendErrorMessage("Please enter a valid player. /lastonline <player>");
				return;
			}

			DateTime lastAccessed = DateTime.Parse(lastOnlinePlayer.LastAccessed).ToLocalTime();
			player.SendInfoMessage(lastOnlinePlayer.Name + " was last online " + Math.Round(DateTime.Now.Subtract(lastAccessed).TotalHours) + " hours ago (" + lastAccessed + ")");
			return;
        }

        #endregion

        #region autoBC
        public void broadcastMessage(Object source, ElapsedEventArgs args)
        {
			Random rnd = new Random();
			TSPlayer.All.SendMessage("[" + Config.serverName + "] " + Config.broadcastMessages[rnd.Next(0, Config.broadcastMessages.Count)], Microsoft.Xna.Framework.Color.Aquamarine);
			for (int i = 0; i < Main.maxItems; i++)
			{

				if (Main.item[i].active)
				{
					Main.item[i].active = false;
					TSPlayer.All.SendData(PacketTypes.ItemDrop, "", i);
				}
			}

			for (int i = 0; i < Main.maxProjectiles; i++)
			{

				if (Main.projectile[i].active)
				{
					Main.projectile[i].active = false;
					Main.projectile[i].type = 0;
					TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", i);
				}
			}
			TSPlayer.Server.SetTime(false, 0.0);
		}
        #endregion

        #region freeze Command
        public void Freeze(CommandArgs args)
        {
			TSPlayer Player = args.Player;

			if(args.Parameters.Count == 0)
            {
				Player.SendErrorMessage("Enter a user to freeze. Ex. /freeze <player>");
				return;
            }

			TSPlayer FrozenPlayer = TSPlayer.FindByNameOrID(args.Parameters[0])[0];

			if(FrozenPlayer == null)
            {
				Player.SendErrorMessage("Invalid user!");
				return;
			}

            if (frozenPlayers.Contains(FrozenPlayer)){
				frozenPlayers.Remove(FrozenPlayer);
				Player.SendSuccessMessage("You have un-frozen " + FrozenPlayer.Name);
				return;
            }
            else
            {
				frozenPlayers.Add(FrozenPlayer);
				Player.SendSuccessMessage("You have frozen " + FrozenPlayer.Name);
				FrozenPlayer.Disable($"{FrozenPlayer} has been frozen by {Player}");
				return;
			}
			return;
        }
        #endregion

        #region chatGames & cg cmd
        public void chatGames(Object source, ElapsedEventArgs args)
        {
			Random rand = new Random();
			string Oper = null;
			int gamemode = rand.Next(0, 5);
			string mathProblem = null;
			string wordProblem = null;
			int answer = 0;
			DataTable eval = new DataTable();
			
			switch (gamemode)
            {
				case 1:
					Oper = "-";
					mathProblem = "" + rand.Next(1, 100) + Oper + rand.Next(1, 150);
					answer = (int)eval.Compute(mathProblem,"");
					cg.answer = answer;
					
					break;
				case 2:
					Oper = "+";
					mathProblem = rand.Next(1, 100) + Oper + rand.Next(1, 150);
					answer = (int)eval.Compute(mathProblem, "");
					cg.answer = answer;

					break;
				case 3:
					Oper = "*";
					mathProblem = rand.Next(1, 12) + Oper + rand.Next(1, 12);
					answer = (int)eval.Compute(mathProblem, "");
					cg.answer = answer;
					break;
				case 4:
					wordProblem = "true";
					break;
				default:
					Oper= "+";
					mathProblem = rand.Next(1, 100) + Oper + rand.Next(1, 150);
					answer = (int)eval.Compute(mathProblem, "");
					cg.answer = answer;
					break;
			}
			cg.Occuring = true;


			if (wordProblem != null)
            {
				var problem = rand.Next(0, WordList.list.Length);
				wordProblem = WordList.list[problem];
				cg.wordAnswer = wordProblem;
				TSPlayer.All.SendMessage("[Chat Games] Unscramble this word problem and win 25 minutes of rank playtime: " + ScrambleWord(wordProblem), Color.LightGreen);

			}
			else
            {
				TSPlayer.All.SendMessage("[Chat Games] Answer this math problem and win 25 minutes of rank playtime: " + mathProblem, Color.LightGreen);
			}


        }

		void triggerCg(CommandArgs arsg)
		{
			chatGames(null, null);
		}

		public string ScrambleWord(string word)
		{
			char[] chars = new char[word.Length];
			Random rand = new Random(10000);

			int index = 0;

			while (word.Length > 0)
			{
				// Get a random number between 0 and the length of the word.
				int next = rand.Next(0, word.Length - 1);

				// Take the character from the random position and add to our char array.
				chars[index] = word[next];

				// Remove the character from the word.
				word = word.Substring(0, next) + word.Substring(next + 1);

				++index;
			}

			return new String(chars);
		}

		#endregion

        #region names manager
        void onGreet(GreetPlayerEventArgs args)
        {
			var ply = TShock.Players[args.Who];
			Random rand = new Random();

			if(Regex.IsMatch(ply.Name, "[^\x00-\x80]+")){
				ply.TPlayer.name = "NonEnglishUser" + rand.Next(0, 100000);
				NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, new Terraria.Localization.NetworkText(ply.TPlayer.name, Terraria.Localization.NetworkText.Mode.Literal), args.Who, 0, 0, 0, 0);
				ply.SendMessage("Your name has been temporarily changed to " + ply.TPlayer.name + " because it had non-English characters! Change it to something else with /nick (new name)!", Color.Gold);
			}
			if(ply.Name.ToLower().Contains("nigger") | ply.Name.ToLower().Contains("nigga") | ply.Name.ToLower().Contains("niga"))
            {
				TShock.Bans.InsertBan("acc:" + ply.Name, "offensive username", "AutoMod", DateTime.UtcNow, DateTime.UtcNow.AddDays(100));
            }


			Players.Add(new AvPlayer(ply.Name));

            if (ply.Group.Name == "guest") {
				ply.SendMessage("[c/d74a06:Y][c/d74a06:o][c/d74a06:u] [c/d74a06:a][c/d84b06:r][c/d84b06:e] [c/d84b06:a] [c/d94c07:g][c/d94c07:u][c/d94c07:e][c/d94d07:s][c/d94d07:t][c/da4d07:!] [c/da4e07:P][c/da4e08:l][c/da4e08:e][c/db4e08:a][c/db4f08:s][c/db4f08:e] [c/db4f08:/][c/dc5008:r][c/dc5008:e][c/dc5009:g][c/dc5009:i][c/dc5009:s][c/dd5109:t][c/dd5109:e][c/dd5109:r] [c/dd5209:<][c/de5209:p][c/de520a:a][c/de520a:s][c/de530a:s][c/de530a:w][c/df530a:o][c/df530a:r][c/df540a:d][c/df540a:>] [c/e0540a:a][c/e0550b:n][c/e0550b:d] [c/e0550b:/][c/e1560b:l][c/e1560b:o][c/e1560b:g][c/e1560b:i][c/e2570c:n] [c/e2570c:<][c/e2570c:s][c/e2570c:a][c/e3580c:m][c/e3580c:e][c/e3580c:_][c/e3580c:p][c/e3590c:a][c/e4590d:s][c/e4590d:s][c/e4590d:w][c/e45a0d:o][c/e45a0d:r][c/e55a0d:d][c/e55a0d:>] [c/e55b0e:t][c/e55b0e:o] [c/e65c0e:g][c/e65c0e:a][c/e65c0e:i][c/e65c0e:n] [c/e75d0e:m][c/e75d0f:o][c/e75d0f:r][c/e75d0f:e] [c/e85e0f:a][c/e85e0f:c][c/e85e0f:c][c/e85f0f:e][c/e95f0f:s][c/e95f0f:s] [c/e96010:t][c/e96010:o] [c/ea6010:t][c/ea6110:h][c/ea6110:e] [c/eb6111:s][c/eb6211:e][c/eb6211:r][c/eb6211:v][c/eb6211:e][c/ec6311:r] [c/ec6311::][c/ec6311:D]", Color.White);
			}


		}
        #endregion

        #region user utilities


		void fightCommand(CommandArgs args)
		{
			if (args.Player.CurrentRegion == null)
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
					if (EssentialsPlus.Commands.FreezeTimer.Enabled == true)
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
						case "average":
							args.Player.KillPlayer();
							spawnName = "Average";
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
						case "mechdusa":
							NPC.SpawnMechQueen(args.Player.Index);
							spawnName = "Mechdusa";
							return;
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
		void aboutCommand(CommandArgs args)
		{
			TSPlayer Player = args.Player;

			if (args.Parameters.Count <= 0)
			{
				var allTopics = "";

				foreach (Topic topic in TopicList)
				{
					if (TopicList.IndexOf(topic) == TopicList.Count - 1)
					{
						allTopics += topic.name;


					}
					else
					{
						allTopics += topic.name + ", ";
					}
				}

				Player.SendInfoMessage("Topics (/about <topic>): " + allTopics);
				return;
			}

			var infoAbout = args.Parameters[0];

			if (Topic.GetByName(infoAbout) != null)
			{
				var topic = Topic.GetByName(infoAbout);
				List<Element> elements = Topic.GetAllElementsFromTopicName(topic.name);
				var elementsString = "";
				foreach (Element element in elements)
				{
					if (elements.IndexOf(element) == elements.Count - 1)
					{
						elementsString += element.name;
					}
					else
					{
						elementsString += element.name + ", ";
					}
				}
				Player.SendInfoMessage($"Within the topic {topic.name}, these are the following topics you can find more about: {elementsString}");
				return;
			}

			if (Element.GetByName(infoAbout) != null)
			{
				var element = Element.GetByName(infoAbout);

				Player.SendInfoMessage($"{element.message}");
				return;
			}

			return;
		}
		void discordInvite(CommandArgs args)
		{
			args.Player.SendSuccessMessage(Config.discordMessage);
		}
		void killBosses(CommandArgs args)
        {
			var user = args.Player;
			int kills = 0;
			var npcId = 0;
			for (int i = 0; i < Main.npc.Length; i++)
			{
				if (Main.npc[i].active && ((npcId == 0 && !Main.npc[i].townNPC && Main.npc[i].netID != NPCID.TargetDummy) || Main.npc[i].netID == npcId))
				{
					TSPlayer.Server.StrikeNPC(i, (int)(Main.npc[i].life + (Main.npc[i].defense * 0.6)), 0, 0);
					kills++;
				}
			}

			if (args.Silent)
				user.SendSuccessMessage($"You butchered {kills} NPC{(kills > 1 ? "s" : "")}.");
			else
				TSPlayer.All.SendInfoMessage($"{user.Name} butchered {kills} NPC{(kills > 1 ? "s" : "")}.");
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

			if (TSPlayer.FindByNameOrID("Average")[0].TPlayer.active)
			{
				args.Player.Teleport(average.LastNetPosition.X, average.LastNetPosition.Y);
				args.Player.SendMessage("You have been teleported to Average! :>", Color.Aquamarine);
			}
			else
			{
				args.Player.SendMessage("Average is not currently online! :<", Color.OrangeRed);
				return;
			}
		}

		void infoCommand(CommandArgs args)
		{
			args.Player.SendSuccessMessage(Config.infoMessage);
			return;
		}

		void applyStaffCommand(CommandArgs args)
		{
			args.Player.SendInfoMessage("Head to averageterraria.lol, register an account, and fill out the staff template under the 'Staff Applications' tag! Thanks for considering applying :)");
			return;
		}

		void tpToPvpCommand(CommandArgs args)
		{
			var warp = TShock.Warps.Find(Config.pvpArena);
			var player = args.Player;

			player.Teleport(warp.Position.X * 16, warp.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent to the PvP arena!");
			return;
		}

		void tpToArena(CommandArgs args)
		{
			var warp = TShock.Warps.Find(Config.arenaRegionName);
			var player = args.Player;

			player.Teleport(warp.Position.X * 16, warp.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent to the boss arena!");
			return;
		}
		void stuckCommand(CommandArgs args)
		{
			var player = args.Player;
			var spawn = TShock.Warps.Find(Config.spawnName);

			player.Teleport(spawn.Position.X * 16, spawn.Position.Y * 16);
			args.Player.SendSuccessMessage("You have been sent back to spawn! Unstuck :>");
		}

		#endregion

		#region autoPvp
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
        #endregion

        #region vanish command
        void vanishCommand(CommandArgs args)
        {
            int num = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(args.Player.X, args.Player.Y), Vector2.Zero, 170, 0, 0f, 255, 0f, 0f);
            Main.projectile[num].timeLeft = 0;
            NetMessage.SendData(27, -1, -1, null, num, 0f, 0f, 0f, 0, 0, 0);
            args.TPlayer.active = !args.TPlayer.active;
            NetMessage.SendData(14, -1, args.Player.Index, null, args.Player.Index, (float)args.TPlayer.active.GetHashCode(), 0f, 0f, 0, 0, 0);
            bool active = args.TPlayer.active;
            if (active)
            {
                NetMessage.SendData(4, -1, args.Player.Index, null, args.Player.Index, 0f, 0f, 0f, 0, 0, 0);
                NetMessage.SendData(13, -1, args.Player.Index, null, args.Player.Index, 0f, 0f, 0f, 0, 0, 0);
            }
            args.Player.SendSuccessMessage(string.Format("{0}abled Vanish.", args.TPlayer.active ? "Dis" : "En"));
        }
        #endregion

        #region chat filtering
        void onChat(ServerChatEventArgs args)
        {

			var player = TSPlayer.FindByNameOrID(args.Who.ToString());

			if(player[0].Group.Name == "guest" && args.Text.Contains("help"))
            {
				player[0].SendMessage("[c/d74a06:Y][c/d74a06:o][c/d74a06:u] [c/d74a06:a][c/d84b06:r][c/d84b06:e] [c/d84b06:a] [c/d94c07:g][c/d94c07:u][c/d94c07:e][c/d94d07:s][c/d94d07:t][c/da4d07:!] [c/da4e07:P][c/da4e08:l][c/da4e08:e][c/db4e08:a][c/db4f08:s][c/db4f08:e] [c/db4f08:/][c/dc5008:r][c/dc5008:e][c/dc5009:g][c/dc5009:i][c/dc5009:s][c/dd5109:t][c/dd5109:e][c/dd5109:r] [c/dd5209:<][c/de5209:p][c/de520a:a][c/de520a:s][c/de530a:s][c/de530a:w][c/df530a:o][c/df530a:r][c/df540a:d][c/df540a:>] [c/e0540a:a][c/e0550b:n][c/e0550b:d] [c/e0550b:/][c/e1560b:l][c/e1560b:o][c/e1560b:g][c/e1560b:i][c/e2570c:n] [c/e2570c:<][c/e2570c:s][c/e2570c:a][c/e3580c:m][c/e3580c:e][c/e3580c:_][c/e3580c:p][c/e3590c:a][c/e4590d:s][c/e4590d:s][c/e4590d:w][c/e45a0d:o][c/e45a0d:r][c/e55a0d:d][c/e55a0d:>] [c/e55b0e:t][c/e55b0e:o] [c/e65c0e:g][c/e65c0e:a][c/e65c0e:i][c/e65c0e:n] [c/e75d0e:m][c/e75d0f:o][c/e75d0f:r][c/e75d0f:e] [c/e85e0f:a][c/e85e0f:c][c/e85e0f:c][c/e85f0f:e][c/e95f0f:s][c/e95f0f:s] [c/e96010:t][c/e96010:o] [c/ea6010:t][c/ea6110:h][c/ea6110:e] [c/eb6111:s][c/eb6211:e][c/eb6211:r][c/eb6211:v][c/eb6211:e][c/ec6311:r] [c/ec6311::][c/ec6311:D]", Color.White);
            }

			if(cg.Occuring == true)
            {
				if(args.Text == cg.answer.ToString())
                {
					RankSystem.PlayerManager.getPlayer(player[0].Name).totaltime += 1500;
					TSPlayer.All.SendMessage("[Chat Games] " + player[0].Name + " won the chat game (answer: " + cg.answer.ToString() + ") and has won 25 minutes of rank playtime! Whoot!", Color.Gold);
					cg.Occuring = false;
					cg.answer = 0;
                }
				if(args.Text == cg.wordAnswer.ToString())
                {
					RankSystem.PlayerManager.getPlayer(player[0].Name).totaltime += 1500;
					TSPlayer.All.SendMessage("[Chat Games] " + player[0].Name + " won the chat game (answer: " + cg.wordAnswer.ToString() + ") and has won 25 minutes of rank playtime! Whoot!", Color.Gold);
					cg.Occuring = false;
					cg.wordAnswer = null;
					cg.answer = 0;
				}
            }
			Console.WriteLine(args.Text);

			if(args.Text.Contains("chest") && args.Text.Contains("open"))
            {
				args.Handled = true;
				player[0].SendMessage("Chests can not be open on mobile currently! We are looking into a fix for this. In the meantime use /item <itemname>.", Color.LightGreen);
            }
			if (args.Text.Contains("how") && args.Text.Contains("get") && args.Text.Contains("to") && args.Text.Contains("survival"))
			{
				args.Handled = true;
				player[0].SendMessage("Use /survival to warp to our survival server! Use /servers to see a list of servers", Color.LightGreen);
			}

			if (args.Text.Contains("nigger") || args.Text.Contains("mong") || args.Text.Contains("nigga") || args.Text.Contains("chink") || args.Text.Contains("fag") || args.Text.Contains("faggot") || args.Text.Contains("suicide"))
            {
				args.Handled = true;
				player[0].Kick("Prohibited language used. This may be offensive to others!", true, false, "Average");
            }
			if (Regex.IsMatch(args.Text, "[^\x00-\x80]+"))
            {
				args.Handled = true;
				player[0].SendMessage("Please only use English characters! If you would like to speak to others in another language, do so with Discord/other private messaging! Sorry for the inconvenience.", Color.Beige);
			}
		}
        #endregion

        #region admin
        void adminAbout(CommandArgs args)
        {
			TSPlayer Player = args.Player;
			string secondSubCommand;

			if (args.Parameters.Count <= 0)
            {
				Player.SendErrorMessage("/ab topic (add/del/list)");
				Player.SendErrorMessage("/ab info (add/del/list/setTopic/setMessage)");
				return;
            }
			if(args.Parameters.Count == 1)
            {
				secondSubCommand = string.Empty;
            }
            else
            {
				secondSubCommand = args.Parameters[1];
			}

			var subcommand = args.Parameters[0];
			Console.WriteLine("Pre1");

			switch (subcommand)
            {
				case "t":
				case "topic":

					if (string.IsNullOrEmpty(secondSubCommand))
                    {
						Player.SendErrorMessage("/ab topic (add/del/list)");
								return;
					}
					switch (secondSubCommand)
                    {
						case "a":
						case "create":
						case "add":
							if (args.Parameters.Count == 2){
								Player.SendErrorMessage("/ab topic add <name>");
								return;
							}
							var addedTopic = args.Parameters[2];

							var newAddedTopic = new Topic(addedTopic);
							TopicList.Add(newAddedTopic);
							dbManager.InsertTopic(newAddedTopic);
							Player.SendSuccessMessage($"{newAddedTopic.name} has been added as a topic!");
							return;
						case "d":
						case "r":
						case "del":
						case "delete":
						case "remove":
							bool removeElementsToo = false;
							if (args.Parameters.Count == 2)
							{
								Player.SendErrorMessage("/ab topic del <name> <-r> (-r will delete all elements assigned to the topic!)");
								return;
							}
							if(args.Parameters.Count == 4)
                            {
								if (args.Parameters[3] == "-r")
								{
									removeElementsToo = true;
								}
							}
							
							var deleteTopic = args.Parameters[2];

							var deletedTopic = Topic.GetByName(deleteTopic);
							if (removeElementsToo == true)
                            {
								foreach(var element in ElementList)
                                {
									if(element.topic == deletedTopic.dbId)
                                    {
										ElementList.Remove(element);
                                    }
                                }
								Player.SendSuccessMessage($"The topic {deletedTopic.name} has been deleted along with all of it's elements!");

							}
							else
                            {
								Player.SendSuccessMessage($"The topic {deletedTopic.name} has been deleted!");
							}

							TopicList.Remove(deletedTopic);
							dbManager.DeleteTopic(deletedTopic);
							return;
						case "list":
						case "l":
							if(TopicList.Count == 0)
                            {
								Player.SendErrorMessage("There are no topics!");
								return;
                            }
							string topicListString = "";

                            foreach (Topic topic in TopicList)
                            {
								if(TopicList.IndexOf(topic) == TopicList.Count - 1)
                                {
									topicListString += topic.name;
                                }
                                else
                                {
									topicListString += topic.name + ", ";
                                }
                            }
							Player.SendInfoMessage("List of topics: " + topicListString);
							return;
						default:
							Player.SendErrorMessage("/ab topic (add/del/list)");
							return;

					}
					return;
				case "element":
				case "info":
				case "i":
				case "e":
					if (secondSubCommand == null)
					{
						Player.SendErrorMessage("/ab (e)lement/(i)nfo (add/del/list/setTopic/setMessage)");
						return;
					}
					switch (secondSubCommand)
					{
						case "a":
						case "create":
						case "add":
							if (args.Parameters.Count == 2)
							{
								Player.SendErrorMessage("/ab info add <name> <assignToTopicName>");
								return;
							}
							var addedElement = args.Parameters[2];

							if (args.Parameters.Count == 3)
							{
								Player.SendErrorMessage($"/ab info add {addedElement} <assignToTopicName>");
								return;
							}
							var assignedTopic = Topic.GetByName(args.Parameters[3]).dbId;

							var newAddedElement = new Element(addedElement, "", assignedTopic);
							ElementList.Add(newAddedElement);
							dbManager.InsertElement(newAddedElement);
							Player.SendSuccessMessage($"{newAddedElement.name} has been added as an element, assigned to {args.Parameters[3]}!");
							return;
						case "d":
						case "r":
						case "delete":
						case "del":
						case "remove":
							if (args.Parameters.Count == 2)
							{
								Player.SendErrorMessage("/ab info del <name>");
								return;
							}
			
							var deleteElement = args.Parameters[2];

							var deletedElement = Element.GetByName(deleteElement);
					
							Player.SendSuccessMessage($"The element {deletedElement.name} has been deleted!");
						
							ElementList.Remove(deletedElement);
							dbManager.DeleteElement(deletedElement);
							return;
						case "setTopic":
						case "st":
							if (args.Parameters.Count == 2)
                            {
								Player.SendErrorMessage("/ab info st <elementName> <newTopicName>");
								return;
							}
							Console.WriteLine("BEEP1");
							dbManager.updateStructure();
							Console.WriteLine("BEEP2");

							var setElement = Element.GetByName(args.Parameters[2]);
							Console.WriteLine("BEEP3");

							if (args.Parameters.Count == 3)
							{
								Console.WriteLine("BEEP4");

								Player.SendErrorMessage($"/ab info st {setElement.name} <newTopicName>");
								return;
							}
							Console.WriteLine("BEEP5");

							var newTopic = Topic.GetByName(args.Parameters[3]);
							Console.WriteLine("BEEP6 " + newTopic.dbId);

							dbManager.UpdateElementTopic(setElement, newTopic.dbId);
							Console.WriteLine("BEEP7");

							setElement.dbId = newTopic.dbId;
							Console.WriteLine("BEEP8");
							dbManager.updateStructure();
							Player.SendSuccessMessage($"The element {setElement.name} is now part of the topic: {newTopic.name}!");
							return;
						case "setMessage":
						case "sm":
							if (args.Parameters.Count == 2)
							{
								Player.SendErrorMessage("/ab info sm <elementName> <newMessage>");
								return;
							}
							var messageElement = Element.GetByName(args.Parameters[2]);

							if (args.Parameters.Count == 3)
							{
								Player.SendErrorMessage($"/ab info st {messageElement.name} <newMessage>");
								return;
							}
							var newMessage = "";
							foreach(string newM in args.Parameters)
                            {
								if(args.Parameters.IndexOf(newM) < 3)
                                {
									continue;
                                }

								newMessage += newM + " ";

                            }
							dbManager.UpdateElementMessage(messageElement, newMessage);
							messageElement.message = newMessage;
							Player.SendSuccessMessage($"You updated {messageElement.name}'s message!");
							return;
						case "list":
						case "l":
							if (ElementList.Count == 0)
							{
								Player.SendErrorMessage("There are no elements!");
								return;
							}
							string elementListString = "";

							foreach (Element element in ElementList)
							{
								if (ElementList.IndexOf(element) == ElementList.Count - 1)
								{
									elementListString += element.name;
								}
								else
								{
									elementListString += element.name + ", ";
								}
							}
							Player.SendInfoMessage("List of elements: " + elementListString);
							return;
						default:
							Player.SendErrorMessage("/ab info (add/del/list/setTopic/setMessage)");
							return;

					}
					return;
				case "default":
					Player.SendErrorMessage("/ab topic (add/del/list)");
					Player.SendErrorMessage("/ab info (add/del/list/setTopic/setMessage)");
					return;
			}
			Player.SendErrorMessage("/ab topic (add/del/list)");
			Player.SendErrorMessage("/ab info (add/del/list/setTopic/setMessage)");
			return;
        }
		void reloadCommand(CommandArgs args)
		{
			Config = Config.Read();
			dbManager.InitialSync();

			args.Player.SendSuccessMessage("Average's Terraria plugin config has been reloaded!");
		}
        #endregion

        #region cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
				ServerApi.Hooks.GameInitialize.Deregister(this, onInitialize);
				ServerApi.Hooks.ServerChat.Deregister(this, onChat);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, onGreet);
				TShockAPI.Hooks.RegionHooks.RegionEntered -= onRegionEnter;
				TShockAPI.Hooks.RegionHooks.RegionLeft -= onRegionLeave;
				ServerApi.Hooks.GameUpdate.Deregister(this, onUpdate);
                TShockAPI.GetDataHandlers.TileEdit -= onTileEdit;


            }
            base.Dispose(disposing);
        }
        #endregion



    }
}
