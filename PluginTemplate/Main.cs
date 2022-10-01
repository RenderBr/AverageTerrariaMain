using AverageTerrariaMain;
using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PluginTemplate
{
    /// <summary>
    /// The main plugin class should always be decorated with an ApiVersion attribute. The current API Version is 1.25
    /// </summary>
    [ApiVersion(2, 1)]
    public class Main : TerrariaPlugin
    {
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
        public Main(Terraria.Main game) : base(game)
        {

        }

        /// <summary>
        /// Performs plugin initialization logic.
        /// Add your hooks, config file read/writes, etc here
        /// </summary>
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, onInitialize);
            Console.WriteLine("Average Main LOADED");
        }

        void onInitialize(EventArgs e)
        {
            Config = Config.Read();
            Commands.ChatCommands.Add(new Command("av.info", infoCommand, "info"));
            Commands.ChatCommands.Add(new Command("av.reload", reloadCommand, "avreload"));
            Commands.ChatCommands.Add(new Command("av.rps", rockPaperScissorsCommand, "avrps"));

        }

        void infoCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage(Config.infoMessage);
        }

        void rockPaperScissorsCommand(CommandArgs args)
        {
            var yourself = args.Player;
            TSPlayer player = null;
            Console.WriteLine(yourself.Name);

            if (args.Parameters.Count > 0)
            {
                player = TSPlayer.FindByNameOrID(args.Parameters[0])[0];
                Console.WriteLine("Got " + player);
            }


            if (args.Parameters.Count == 1 && yourself.GetData<bool>("invited") == true)
            {
                //rock = 1 , paper = 2, scissors = 3 , Y v P
                var enemyPlay = yourself.GetData<TSPlayer>("playerInvitedBy").GetData<int>("play");

                if (enemyPlay == int.Parse(args.Parameters[0]))
                {  // same
                    args.Player.SendInfoMessage("You both chose the same thing! Tie game!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage(args.Player + " chose the same thing as you! They tied the game!");
                    return;
                }

                if (enemyPlay == 1 && int.Parse(args.Parameters[0]) == 2)
                {  // P v R
                    args.Player.SendInfoMessage("You chose paper, and your opponent chose rock. You win!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage(args.Player + " won by choosing paper!");
                    return;
                }

                if (enemyPlay == 2 && int.Parse(args.Parameters[0]) == 1) {  // R v P 
                    args.Player.SendInfoMessage("You chose rock, and your opponent chose paper. You lose!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage("You won by choosing paper!");
                    return;
                }

                if (enemyPlay == 2 && int.Parse(args.Parameters[0]) == 3)
                {  // P v S
                    args.Player.SendInfoMessage("You chose scissors, and your opponent chose paper. You win!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage(args.Player + " won by choosing scissors!");
                    return;
                }

                if (enemyPlay == 3 && int.Parse(args.Parameters[0]) == 1)
                { // S v P
                    args.Player.SendInfoMessage("You chose paper, and your opponent chose scissors. You lose!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage("You won by choosing scissors!");
                    return;
                }

                if (enemyPlay == 3 && int.Parse(args.Parameters[0]) == 1)
                { // R v S
                    args.Player.SendInfoMessage("You chose scissors, and your opponent chose rock. You lose!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage("You won by choosing rock!");
                    return;
                }

                if (enemyPlay == 3 && int.Parse(args.Parameters[0]) == 1)
                { // S v R
                    args.Player.SendInfoMessage("You chose rock, and your opponent chose scissors. You win!");
                    yourself.GetData<TSPlayer>("playerInvitedBy").SendInfoMessage(args.Player + " won by choosing rock!");
                    return;
                }

            }



            if (args.Parameters.Count == 1)
            {
                args.Player.SendErrorMessage("You must enter a play! (r)ock, (p)aper, or (s)cissors.");
                return;
            }

            if (args.Parameters[0] == null)
            {
                args.Player.SendErrorMessage("You must enter a player name!");
                return;
            }

            if (player == null)
            {
                args.Player.SendErrorMessage("You must enter a valid player name!");
                return;
            }

            int you = 0;
            int enemy = 0;
            

            if (args.Parameters[1] == "r")
            {
                you = 1;
            }
            if (args.Parameters[1] == "p")
            {
                you = 2;
            }
            if (args.Parameters[1] == "s")
            {
                you = 3;
            }

            player.SendInfoMessage("You have been challenged to rock-paper-scissors. Fight back with /rps (r,p,s)");
            player.SetData<bool>("invited", true);
            player.SetData<TSPlayer>("playerInvitedBy", yourself);
            yourself.SetData<int>("play", you);
            yourself.SendInfoMessage("You have challenged " + player.Name);




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
                //unhook
                //dispose child objects
                //set large objects to null
            }
            base.Dispose(disposing);
        }
    }
}
