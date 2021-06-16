using BattleTech;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace PersistentMapClient.Objects
{
    public class System {
        public List<FactionControl> factions;
        public string name;
        public int Players;
        public string owner;
        public bool immuneFromWar;
        public bool generatesItems = false;
        public List<string> itemsGenerated = new List<string>();
        public int markerType = 0;
        public List<string> mercStrings = new List<string>();


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

        private bool checkFlag(EMarkerTypes flag)
        {
            // Either I'm missing something or BitVector is just broken, if for example markerType is 4, then bit 2 of the 32 bits should be set and no other bits,
            // but for some reason bitvector32 tells me bits 0 & 4 are set and bit 2 is not set.....wtf
            //BitVector32 flags = new BitVector32(4);
            //return flags[(int)flag];
            return (markerType & (1 << (int)flag)) != 0;
        }

        public bool isInsurrect()
        {
            return checkFlag(EMarkerTypes.InsurrectSystem);
        }

        public bool hasOnlineEvent()
        {
            return checkFlag(EMarkerTypes.OnlineEvent);
        }

        public bool isMercTarget()
        {
            return checkFlag(EMarkerTypes.MercTarget);
        }

        public EMarkerTypes getMarkerType()
        {
            if (markerType != 0)
            {
                if (isMercTarget())
                {
                    return EMarkerTypes.MercTarget;
                }
                if (hasOnlineEvent())
                {
                    return EMarkerTypes.OnlineEvent;
                }
                if (isInsurrect())
                {
                    return EMarkerTypes.InsurrectSystem;
                }
            }

            return EMarkerTypes.NoMarker;
        }
    }
}
