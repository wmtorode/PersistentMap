﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentMapClient.Objects
{
    public class ServerSettings
    {
        public bool CanPostSoldItems = false;
        public List<string> SupportBypass = new List<string>();
        public bool BlackMarketAvailable = false;
        public Dictionary<string, string> Capitals = new Dictionary<string, string>();
    }
}
