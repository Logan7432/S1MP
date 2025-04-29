using System;
using Newtonsoft.Json;

namespace SteamGameServerMod.Client.Models
{
    public class ServerData
    {
        public string servername { get; set; }
        public bool whitelisted { get; set; }
        public int players { get; set; }
        public string steamid { get; set; }
        public int maxplayers { get; set; }
        public bool steam { get; set; }
        public int ingametime { get; set; }
        public int days { get; set; }
        public string ip { get; set; }
        public string port { get; set; }
        public long lastUpdate { get; set; }
        public string partnertext { get; set; }

        [JsonIgnore]
        public string FormattedTime
        {
            get
            {
                string timeString = this.ingametime.ToString().PadLeft(4, '0');
                int hours = int.Parse(timeString.Substring(0, 2));
                int minutes = int.Parse(timeString.Substring(2, 2));
                string timeOfDay = (hours >= 6 && hours < 18) ? "Day" : "Night";
                return string.Format("{0} {1:D2}:{2:D2}", timeOfDay, hours, minutes);
            }
        }
    }
}