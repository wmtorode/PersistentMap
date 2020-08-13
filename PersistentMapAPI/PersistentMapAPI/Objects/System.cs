using BattleTech;
using PersistentMapAPI.Objects;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapAPI {
    public class System {
        public List<FactionControl> factions;
        public string name;
        public int Players;
        public string owner;
        public bool immuneFromWar;

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
