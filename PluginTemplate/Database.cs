using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.DB;

namespace AverageTerrariaSurvival
{
    public class Database
    {
        private readonly IDbConnection _db;

        public Database(IDbConnection db)
        {
            _db = db;

            var sqlCreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            var donatedItems = new SqlTable("DonatedItems",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ItemID", MySqlDbType.Int32),
                new SqlColumn("Quantity", MySqlDbType.Int32, 50),
                new SqlColumn("Prefix", MySqlDbType.Int32)
                );
            sqlCreator.EnsureTableStructure(donatedItems);


            var playerStats = new SqlTable("PlayerStats",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("PlayerName", MySqlDbType.String),
                new SqlColumn("Level", MySqlDbType.Float),
                new SqlColumn("Xp", MySqlDbType.Float)
                );
            sqlCreator.EnsureTableStructure(playerStats);

            var clans = new SqlTable("Clans",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ClanName", MySqlDbType.String),
                new SqlColumn("Owner", MySqlDbType.String)
                );
            sqlCreator.EnsureTableStructure(clans);

            var clanRegions = new SqlTable("ClanRegions",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ClanName", MySqlDbType.String),
                new SqlColumn("RegionName", MySqlDbType.String)
                );
            sqlCreator.EnsureTableStructure(clanRegions);

            var challenges = new SqlTable("Challenges",
            new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
            new SqlColumn("ChallengeId", MySqlDbType.Int32),
            new SqlColumn("PlayerName", MySqlDbType.String)
            );
            sqlCreator.EnsureTableStructure(challenges);

            var clanMembers = new SqlTable("ClanMembers",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ClanName", MySqlDbType.String),
                new SqlColumn("MemberName", MySqlDbType.String),
                new SqlColumn("Role", MySqlDbType.Int32),
                new SqlColumn("JoinDate", MySqlDbType.DateTime)
                );

            sqlCreator.EnsureTableStructure(clanMembers);

        }
        public bool InsertItem(DonatedItem item)
        {
            return _db.Query("INSERT INTO DonatedItems (ItemID, Quantity, Prefix)" + "VALUES (@0, @1, @2)", item.id, item.quantity, item.prefix) != 0;
        }

        public bool InsertClan(Clan clan)
        {
            return _db.Query("INSERT INTO Clans (ClanName, Owner) VALUES (@0, @1)", clan.name, clan.owner) != 0;
        }

        public bool InsertRegion(string regionName, Clan clan)
        {
            return _db.Query("INSERT INTO ClanRegions (ClanName, RegionName) VALUES (@0, @1)", clan.name, regionName) != 0;
        }

        public bool InsertChallenge(int challengeId, TSPlayer Player)
        {
            return _db.Query("INSERT INTO Challenges (ChallengeId, PlayerName) VALUES (@0, @1)", challengeId, Player.Name) != 0;
        }

        public bool InsertMember(ClanMember cm)
        {
            return _db.Query("INSERT INTO ClanMembers (ClanName, MemberName, Role, JoinDate) VALUES (@3, @0, @1, @2)", cm.memberName, cm.role, cm.joined, cm.clanName) != 0;
        }

        public bool InsertLevel(TSPlayer tp)
        {
            return _db.Query("INSERT INTO PlayerStats (PlayerName, Level) VALUES (@0, 0)", tp.Name) != 0;
        }


        public bool UpdateMemberRole(string memberName, int role)
        {
            return _db.Query("UPDATE ClanMembers SET Role=@0 WHERE MemberName=@1", memberName, role) != 0;
        }

        public bool LevelUp(string memberName)
        {
            return _db.Query("UPDATE PlayerStats SET Level=Level+1 WHERE PlayerName=@0", memberName) != 0;
        }
        public bool ManipulateLevel(string memberName, int level)
        {
            return _db.Query("UPDATE PlayerStats SET Level=@1 WHERE PlayerName=@0", memberName, level) != 0;
        }
        public bool UpdateXP(string memberName, float xp)
        {
            return _db.Query("UPDATE PlayerStats SET xp=@1 WHERE PlayerName=@0", memberName, xp) != 0;
        }

        public bool DeleteItem(DonatedItem item)
        {
            return _db.Query("DELETE FROM DonatedItems WHERE Id = @0", item.dbId) != 0;
        }

        public bool DeleteMember(ClanMember cm)
        {
            return _db.Query("DELETE FROM ClanMembers WHERE Id = @0", cm.id) != 0;
        }

        public bool DeleteClan(Clan clan)
        {
            foreach(ClanMember member in clan.members.members)
            {
                _db.Query("DELETE FROM ClanMembers WHERE Id = @0", member.id);

            }
            return _db.Query("DELETE FROM Clans WHERE Id = @0", clan.dbId) != 0;
        }

        public bool HasUserCompletedChallenge(int c, TSPlayer p)
        {
            using (var reader = _db.QueryReader($"SELECT * FROM Challenges WHERE playerName={p.Name}"))
            {
                while (reader.Read())
                {
                    var challengeId = reader.Get<int>("ChallengeId");
                    var playerName = reader.Get<string>("PlayerName");


                    return true;

                }
            }
            return false;
        }


        public float RetrieveUserLevel(TSPlayer p)
        {
            using (var reader = _db.QueryReader("SELECT * FROM PlayerStats WHERE PlayerName=@0", p.Name))
            {
                while (reader.Read())
                {
                    var level = reader.Get<float>("Level");

                    return level;

                }
            }
            return 404.404f;
        }


        //not just for players anymore
        public void InitialSync()
        {
            using (var reader = _db.QueryReader("SELECT * FROM DonatedItems"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var itemID = reader.Get<int>("ItemID");
                    var Quantity = reader.Get<int>("Quantity");
                    var prefix = reader.Get<int>("Prefix");


                    AverageTerrariaMain.AvMain._donatedItems.donations.Add(new DonatedItem(actualId, itemID, Quantity, prefix));

                    
                }
            }

            using (var reader = _db.QueryReader("SELECT * FROM Clans"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("ClanName");
                    var owner = reader.Get<string>("Owner");


                    AverageTerrariaMain.AvMain._clans.allClans.Add(new Clan(actualId, name, new ClanMembers(), owner));


                }
            }

            using (var reader = _db.QueryReader("SELECT * FROM ClanRegions"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("ClanName");
                    var rname = reader.Get<string>("RegionName");


                    AverageTerrariaMain.AvMain._clans.FindClan(name).regions.Add(rname);


                }
            }

            using (var reader = _db.QueryReader("SELECT * FROM ClanMembers"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var clanName = reader.Get<string>("ClanName");
                    var memberName = reader.Get<string>("MemberName");
                    var role = reader.Get<int>("Role");
                    var joined = reader.Get<DateTime>("JoinDate");

                    AverageTerrariaMain.AvMain._clans.FindClan(clanName).members.members.Add(new ClanMember(actualId, clanName, memberName, role, joined));

                    if (AverageTerrariaMain.AvMain.Players.GetByUsername(memberName) != null)
                    {
                        AverageTerrariaMain.AvMain.Players.GetByUsername(memberName).clan = clanName;
                    }
                }
            }
        }
    }
}
