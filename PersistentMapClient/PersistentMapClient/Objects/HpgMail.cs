using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistentMapClient.Objects
{
    public class HpgMail
    {
        public string factionMessage = "";
        public List<EventMessage> events = new List<EventMessage>();
        public List<EventMessage> news = new List<EventMessage>();
        public List<ClientReward> rewards = new List<ClientReward>();
    }
}
