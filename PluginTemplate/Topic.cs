using System;
using System.Collections.Generic;
using System.Text;

namespace AverageTerrariaMain
{
    public class Topic
    {
        public int dbId { get; set; }
        public string name { get; set; }
        
        public Topic(int dbId, string name)
        {
            this.dbId = dbId; 
            this.name = name;
        }
    }

    public class Element
    {
        public int dbId { get; set; }
        public string name { get; set; }
        public string message { get; set; }

        public Element(int dbId, string name, string message)
        {
            this.dbId = dbId;
            this.name = name;
            this.message = message;
        }
    }
}
