using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AverageTerrariaMain
{
    public class BrokenBlock
    {
        public DateTime brokenAt;

        public BrokenBlock() { 
            this.brokenAt= DateTime.Now;
        }
    }
}
