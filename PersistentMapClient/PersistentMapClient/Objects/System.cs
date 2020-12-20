using BattleTech;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapClient.Objects
{
    public class System {
        public List<FactionControl> factions;
        public string name;
        public int Players;
        public string owner;
        public bool immuneFromWar;
        public bool insurrect;
        public bool generatesItems = false;
        public List<string> itemsGenerated = new List<string>();
        public bool markSystem = false;


        public FactionControl FindFactionControlByFaction(string faction) {
            if(factions == null) {
                factions = new List<FactionControl>();
            }
            FactionControl result = factions.Find(x => x.Name == faction);
            if(result == null) {
                result = new FactionControl();
                result.Name = faction;
                result.control = 0;
                factions.Add(result);
            }
            return result;
        }

        public FactionControl FindHighestControl() {
            if (factions == null) {
                factions = new List<FactionControl>();
            }
            FactionControl result = factions.OrderByDescending(x => x.control).First();
            return result;
        }
    }
}
