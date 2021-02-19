using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentMapClient.Objects
{
    public class PurchasedItem
    {
        public string ID;
        public int Count;
        public int Cost = 0;
        public string TransactionId = "";
        public int remainingFunds = 0;
    }
}
