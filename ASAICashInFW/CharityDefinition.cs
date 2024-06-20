using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ProTopas.Impl.ASAICashInFW
{
    public class Charity
    {
        public string name { get; set; }
        public string value { get; set; }
        public Boolean enabled { get; set; }
        public string bOServerID { get; set; }
        public string sIcon { get; set; }
    }

    public class CharityList
    {       
        public List<Charity> charityList { get; set; }

        public CharityList DeserializeJSON<CharityList>(string json)
        {
            return (JsonConvert.DeserializeObject<CharityList>(json));          
        }

        public string SerializeJSON<CharityList>(CharityList obj)
        {
            JObject o = (JObject)JToken.FromObject(obj);
            return o.ToString();
        }
    }
}
