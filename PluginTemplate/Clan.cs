using Newtonsoft.Json;
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
        [JsonProperty("id")]

        public int id { get; set; }

        [JsonProperty("clanName")]
        public string clanName { get; set; }
        
        [JsonProperty("memberName")]
        public string memberName { get; set; }

        [JsonProperty("role")]
        public int role { get; set; }

        [JsonProperty("joined")]
        public DateTime joined { get; set; }

        public ClanMember(int id, string cname, string pname, int role, DateTime joined)
        {
            this.id = id;
            this.clanName = cname;
            this.memberName = pname;
            this.role = role;
            this.joined = joined;
        }


        public ClanMember(string cname, string pname, int role, DateTime joined)
        {
            this.clanName = cname;
            this.memberName = pname;
            this.role = role;
            this.joined = joined;
        }

    }

    public class ClanMembers
    {
        [JsonProperty("members")]
        public List<ClanMember> members { get; set; }

        public ClanMember FindMember(string name)
        {
            return members.Find(x => x.memberName == name);
        }
    }

    public class Clans
    {

        public List<Clan> allClans = new List<Clan>();

        public Clan FindClan(string name)
        {
            Clan tempclan = allClans.FirstOrDefault(x => x.name == name);

            return tempclan;
        }

    }

}
