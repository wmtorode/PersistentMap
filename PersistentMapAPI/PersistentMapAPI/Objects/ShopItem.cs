using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace PersistentMapAPI
{
    public class ShopItem
    {
        public int Count;
        public string UiName;

        public string ID { get; set; }

        public ShopItemType Type { get; set; }
    }
}
