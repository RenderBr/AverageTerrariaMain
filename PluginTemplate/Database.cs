using System;
using System.Data;
using System.Linq;
using AverageTerrariaMain;
using MySql.Data.MySqlClient;
using TShockAPI.DB;

namespace AverageTerrariaMain
{
    public class Database
    {
        private readonly IDbConnection _db;

        public Database(IDbConnection db)
        {
            _db = db;

            var sqlCreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            var topics = new SqlTable("Topics",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.String)
                );
            sqlCreator.EnsureTableStructure(topics);

            var elements = new SqlTable("Elements",
            new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
            new SqlColumn("Name", MySqlDbType.String),
            new SqlColumn("Topic", MySqlDbType.Int32),
            new SqlColumn("Message", MySqlDbType.String)
                );
            sqlCreator.EnsureTableStructure(elements);
        }
        public bool InsertTopic(Topic topic)
        {
            bool pass = _db.Query("INSERT INTO Topics (Name)" + "VALUES (@0)", topic.name) != 0;
            updateStructure();
            return pass;

        }

        public bool DeleteTopic(Topic topic)
        {
            bool pass = _db.Query("DELETE FROM Topics WHERE Name = @0", topic.name) != 0;
            updateStructure();
            return pass;
        }

        public bool InsertElement(Element element)
        {
            
            bool pass = _db.Query("INSERT INTO Elements (Name, Topic, Message)" + "VALUES (@0, @1, @2)", element.name, element.topic, element.message) != 0;
            updateStructure();
            return pass;
        }

        public bool UpdateElementTopic(Element element, int newTopicID)
        {
            bool pass = _db.Query("UPDATE Elements SET Topic = @0 WHERE Id = @1", newTopicID, element.dbId) != 0;
            return pass;
        }

        public bool UpdateElementMessage(Element element, string newMessage)
        {
            bool pass = _db.Query("UPDATE Elements SET Message = @0 WHERE Id = @1", newMessage, element.dbId) != 0;
            return pass;
        }


        public bool DeleteElement(Element element)
        {
            bool pass = _db.Query("DELETE FROM Elements WHERE Name = @0", element.name) != 0;

            updateStructure();
            return pass;


        }

        public void updateStructure()
        {
            using (var reader = _db.QueryReader("SELECT * FROM Topics"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("Name");

                    Console.WriteLine(Topic.GetByName(name).name + " " + actualId + " ");
                    if(Topic.GetByName(name) == null)
                    {
                        continue;
                    }

                    Topic.GetByName(name).dbId = actualId;


                }
            }

            using (var reader = _db.QueryReader("SELECT * FROM Elements"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("Name");
                    var topic = reader.Get<int>("Topic");
                    var message = reader.Get<string>("Message");

                    if (Element.GetByName(name) == null)
                    {
                        continue;
                    }

                    Element.GetByName(name).dbId = actualId;
                    Element.GetByName(name).topic = topic;


                }
            }
        }


        public void InitialSync()
        {
            using (var reader = _db.QueryReader("SELECT * FROM Topics"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("Name");

                    PluginTemplate.AvMain.TopicList.Add(new Topic(actualId, name));

                    
                }
            }

            using (var reader = _db.QueryReader("SELECT * FROM Elements"))
            {
                while (reader.Read())
                {
                    var actualId = reader.Get<int>("Id");
                    var name = reader.Get<string>("Name");
                    var topic = reader.Get<int>("Topic");
                    var message = reader.Get<string>("Message");


                    PluginTemplate.AvMain.ElementList.Add(new Element(actualId, name, message, topic));


                }
            }
        }
    }
}
