using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        public Topic(string name)
        {
            this.name = name;
        }

        public static Topic GetByName(string name)
        {

            return AvMain.TopicList.FirstOrDefault(p => p.name.ToLower() == name.ToLower());
        }

        public static List<Element> GetAllElementsFromTopicName(string name)
        {
            var topic = Topic.GetByName(name);
            List<Element> _elements = new List<Element>();

            foreach(Element element in AvMain.ElementList)
            {
                if(element.topic == topic.dbId)
                {
                    _elements.Add(element);
                }
            }

            return _elements;
        }
    }

    public class Element
    {
        public int dbId { get; set; }
        public string name { get; set; }
        public string message { get; set; }

        public int topic { get  ; set; }
        
        public Element(int dbId, string name, string message, int topic)
        {
            this.dbId = dbId;
            this.name = name;
            this.message = message;
            this.topic = topic;
        }

        public Element(string name, string message, int topic)
        {
            this.name = name;
            this.message = message;
            this.topic = topic;
        }

        public static Element GetByName(string name)
        {
            return AvMain.ElementList.FirstOrDefault(e => e.name.ToLower() == name.ToLower());

        }
    }
}
