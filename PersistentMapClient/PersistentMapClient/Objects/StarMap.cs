using System;
using System.Collections.Generic;
using System.Linq;

namespace PersistentMapClient.Objects
{
    public class StarMap : ICloneable {

        public List<System> starsystems = new List<System>();

        public System FindSystemByName(string name) {
            System result = null;
            if (starsystems != null && starsystems.Count > 0) {
                result = starsystems.FirstOrDefault(x => x.name.Equals(name));
            }
            return result;
        }

        // Do a deep clone of all members
        public object Clone() {
            return MemberwiseClone();
        }

    }
}
