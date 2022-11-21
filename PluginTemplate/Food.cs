using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaSurvival
{
    public class Food
    {
        public int price;
        public string terrariaName;
        public List<string> aliases = new List<string>();

        public Food(int price, string terrariaName, List<string> aliases)
        {
            this.price = price;
            this.terrariaName = terrariaName;
            this.aliases = aliases;
        }
    }
}
