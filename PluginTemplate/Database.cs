using System;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
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

            var table = new SqlTable("DonatedItems",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("ItemID", MySqlDbType.Int32),
                new SqlColumn("Quantity", MySqlDbType.Int32, 50),
                new SqlColumn("Prefix", MySqlDbType.Int32)
                );
            sqlCreator.EnsureTableStructure(table);
        }
        public bool InsertItem(DonatedItem item)
        {
            return _db.Query("INSERT INTO DonatedItems (ItemID, Quantity, Prefix)" + "VALUES (@0, @1, @2)", item.id, item.quantity, item.prefix) != 0;
        }

        public bool DeleteItem(DonatedItem item)
        {
            return _db.Query("DELETE FROM DonatedItems WHERE Id = @0", item.dbId) != 0;
        }


        public void InitialSyncPlayers()
        {
            using (var reader = _db.QueryReader("SELECT * FROM DonatedItems"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var itemID = reader.Get<int>("ItemID");
                    var Quantity = reader.Get<int>("Quantity");
                    var prefix = reader.Get<int>("Prefix");


                    PluginTemplate.AvMain._donatedItems.donations.Add(new DonatedItem(actualId, itemID, Quantity, prefix));

                    
                }
            }
        }
    }
}
