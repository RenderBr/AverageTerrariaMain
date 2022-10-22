using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaSurvival
{
    public class DonatedItem
    {
        public int dbId { get; set; }
        public int id { get; set; }
        public int quantity { get; set; }

        public int prefix { get; set; }

        public DonatedItem(int actualId, int id, int quantity, int prefix)
        {
            this.dbId = actualId;
            this.id = id;
            this.quantity = quantity;
            this.prefix = prefix;
        }

        public DonatedItem(int id, int quantity, int prefix)
        {
            this.id = id;
            this.quantity = quantity;
            this.prefix = prefix;
        }

    }

    public class DonatedItems
    {

        public List<DonatedItem> donations = new List<DonatedItem>();

        public List<DonatedItem> GetDonations()
        {
            return donations;
        }

        public static void Add(DonatedItem item, List<DonatedItem> donations) {
            donations.Add(item);
        }


    }

}
