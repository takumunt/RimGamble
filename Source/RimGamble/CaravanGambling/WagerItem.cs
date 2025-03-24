using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimGamble
{

    /**
     * Object used to store item information for the dictionaries< "colonyItemsWagered" and "traderItemsWagered"
     */
    public class WagerItem
    {
        public int numItems { get; set; }
        public String numItemsBuffer { get; set; }

        public WagerItem(int numItems)
        {
            this.numItems = numItems;
            this.numItemsBuffer = "";
        }
    }
}
