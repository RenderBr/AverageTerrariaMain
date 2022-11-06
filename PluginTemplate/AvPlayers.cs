using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;
using Terraria;
using System.Timers;

namespace AverageTerrariaMain
{
    public class AvPlayers
    {
        private readonly List<AvPlayer> _players = new List<AvPlayer>();

        public void Add(string name)
        {
            _players.Add(new AvPlayer(name));
        }

        public void Add(AvPlayer player)
        {
            _players.Add(player);
        }

        public AvPlayer GetByUsername(string username)
        {
            return _players.FirstOrDefault(p => p.name == username);
        }

        public IEnumerable<AvPlayer> GetListByUsername(string username)
        {
            return _players.Where(p => p.name.ToLowerInvariant().Contains(username.ToLowerInvariant()));
        }

        public IEnumerable<AvPlayer> Players { get { return _players; } }

        public IEnumerable<AvPlayer> Offline { get { return _players.Where(p => !p.Online); } }
        public IEnumerable<AvPlayer> Online { get { return _players.Where(p => p.Online); } }


    }

    public class AvPlayer
    {
        public TSPlayer tsPlayer;
        public bool Online { get { return tsPlayer != null; } }
        public readonly string name;
        public bool isVanished = false;
        public bool isBountied = false;
        public int bountyPrice = 0;
        public TSPlayer bountiedBy;
        public Timer donateBeg;
        public string clan = "";
        public bool invitedToClan = false;
        public string whichClanInvite;
        public bool isChef = false;
        public int order = 0;
        public bool prepping = false;
        public bool cooking = false;
        public bool plating = false;
        public bool serving = false;

        public string Group
        {
            get { return !Online ? TShock.UserAccounts.GetUserAccountByName(name).Group : tsPlayer.Group.Name; }
        }

        public void donateStop(object sender, ElapsedEventArgs e, AvPlayer av)
        {
            donateBeg.Stop();
        }

        public AvPlayer(string name)
        {
            this.name = name;
            this.isVanished = false;
            this.clan = "";
        }

    }
}
