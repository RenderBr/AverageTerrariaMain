using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaSurvival
{
    public class Clan
    {
        public int dbId { get; set; }
        public string name{ get; set; }
        public ClanMembers members { get; set; }

        public string owner { get; set; }

        public Clan(string name, ClanMembers members, string owner)
        {
            this.name = name;
            this.members = members;
            this.owner = owner;
        }

        public Clan(int dbId, string name, ClanMembers members, string owner)
        {
            this.name = name;
            this.members = members;
            this.owner = owner;
            this.dbId = dbId;
        }

    }

    public class ClanMember
    {
        public int id { get; set; }

        public string clanName { get; set; }
        public string playerName { get; set; }
        public int role { get; set; }

        public DateTime joined { get; set; }

        public ClanMember(int id, string cname, string pname, int role, DateTime joined)
        {
            this.id = id;
            this.clanName = cname;
            this.playerName = pname;
            this.role = role;
            this.joined = joined;
        }


        public ClanMember(string cname, string pname, int role, DateTime joined)
        {
            this.clanName = cname;
            this.playerName = pname;
            this.role = role;
            this.joined = joined;
        }

    }

    public class ClanMembers
    {
        public List<ClanMember> members { get; set; }

        public ClanMember FindMember(string name)
        {
            return members.Find(x => x.playerName == name);
        }
    }

    public class Clans
    {

        public List<Clan> allClans = new List<Clan>();

        public Clan FindClan(string name)
        {
            Clan tempclan = allClans.Find(x => x.name == name);

            return tempclan;
        }

    }

}
