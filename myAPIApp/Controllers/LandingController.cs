using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using myAPIApp.Models;
using Newtonsoft.Json.Linq;
using System.Threading;
using FileHelpers;
using System.Web.Hosting;

namespace myAPIApp.Controllers
{
    public class LandingController : Controller
    {
        private const string apiKey = "api_key=91a00623-5397-4358-9481-608740057501";
        // GET: Landing
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(FormCollection collection)
        {try
            {
                Dictionary<int, int> indicies = championIdMapping();
                Dictionary<int, int> reverseMap = indexToIdMap();
                List<ChampionMastery> champsUsed = getPlayerMasteries(getPlayerID(collection["SummonerID"]));
                int threshold = collection["onlyZeroPoints"].Contains("true") ? 1 : 6000;
                bool[] diffs = new bool[10];
                string s = "diff";
                for(int i = 0; i < 10; ++i)
                {
                    diffs[i] = collection[s + (i+1).ToString()].Contains("true");
                }



                int[] top3champs = new int[3];
                top3champs[0] = champsUsed[0].championId;
                top3champs[1] = champsUsed[1].championId;
                top3champs[2] = champsUsed[2].championId;

                float[] correlations = new float[130];

                float[][] f = loadCorrelationArray();

                for (int i = 0; i < indicies.Keys.Count; ++i)
                {
                    int index0, index1, index2;
                    indicies.TryGetValue(top3champs[0], out index0);
                    indicies.TryGetValue(top3champs[1], out index1);
                    indicies.TryGetValue(top3champs[2], out index2);
                    //correlations holds the information of what champs our user will likely enjoy, based on his top 3 champs.
                    correlations[i] = f[index0][i] + f[index1][i] + f[index2][0];
                }

                //make sure not to suggest champions that are above the threshold!
                foreach (ChampionMastery champ in champsUsed)
                {
                    if (champ.championPoints > threshold)
                    {
                        int id;
                        indicies.TryGetValue(champ.championId, out id);
                        correlations[id] = float.MinValue;
                    }
                }

                //filter difficulties
                foreach(JToken child in getChampionNodes())
                {
                    if( !diffs[(int) child.First.SelectToken("info").SelectToken("difficulty") - 1])
                    {
                        int id;
                        indicies.TryGetValue((int) child.First.SelectToken("key"), out id);
                        correlations[id] = float.MinValue;
                    }
                }
                

                //find top 3 values in correlation array
                List<int> suggestedChamps = new List<int>();
                ViewBag.ChampNames = new string[3];
                ViewBag.ChampRealNames = new string[3];

                for (int i = 0; i < 3; ++i)
                {
                    float maxValue = correlations.Max();
                    int index = correlations.ToList().IndexOf(maxValue);
                    int temp;
                    reverseMap.TryGetValue(index, out temp);
                    suggestedChamps.Add(temp);
                    ViewBag.ChampNames[i] = (string)getChampionNode(temp).SelectToken("id");
                    ViewBag.ChampRealNames[i] = (string)getChampionNode(temp).SelectToken("name");
                    correlations[index] = float.MinValue;
                }

                

                ViewBag.Message = "Based on the champs that you've done well with and enjoyed, we recommend you try: ";


                ViewBag.Title = "Detail";

                return View("Detail");
            }
            catch (WebException e)
            {
                ViewBag.Title = e.Message;
                return View("ErrorScreen");
            }
        }

        public static float[][] loadCorrelationArray()
        {
            float[][] f = new float[130][];
            //string base_url = AppDomain.CurrentDomain.BaseDirectory;
            //string full_url = base_url + "/App_Data/CorrelationTable.txt";
            var full_url = HostingEnvironment.MapPath("~/App_Data/CorrelationTable.txt");
            string[] data = System.IO.File.ReadAllLines(full_url);
            for(int i = 0; i < data.Length; ++i)
            {
                f[i] = data[i].Split(',').Select(s => float.Parse(s)).ToArray();
            }
            return f;
        }

        public int getPlayerID(string summonerID)
        {
            string sURL = "https://na.api.pvp.net/api/lol/na/v1.4/summoner/by-name/" + summonerID + "?" + apiKey;

            WebRequest wrGET = WebRequest.Create(sURL);

            WebResponse jsonObj = wrGET.GetResponse();
            //Thread.Sleep(1000);

            StreamReader sr = new StreamReader(jsonObj.GetResponseStream());
            string jsonStr = sr.ReadToEnd();

            Dictionary<string, Summoner> dict = JsonConvert.DeserializeObject<Dictionary<string,Summoner>>(jsonStr);

            return dict[summonerID.Replace(" ","").ToLower()].id;
                      
        }

        public List<ChampionMastery> getPlayerMasteries(int playerID)
        {

            string sURL = "https://na.api.pvp.net/championmastery/location/NA1/player/" + playerID.ToString() + "/champions?" + apiKey;
            WebRequest wrGET = WebRequest.Create(sURL);
            WebResponse resp = wrGET.GetResponse();
            //Thread.Sleep(1000);

            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string jsonStr = sr.ReadToEnd();

            List<ChampionMastery> mastery = JsonConvert.DeserializeObject<List<ChampionMastery>>(jsonStr);
            return mastery;
        }

        public static Dictionary<int, int> championIdMapping()
        {
            List<int> champList = getAllChampIDs();
            Dictionary<int, int> idToArrayIndex = new Dictionary<int, int>();
            Dictionary<int, int> arrayIndexToId = new Dictionary<int, int>();

            for (int i = 0; i < champList.Count; ++i)
            {
                idToArrayIndex.Add(champList[i], i);
                arrayIndexToId.Add(i, champList[i]);
            }

            return idToArrayIndex;
        }
        public static List<int> getAllChampIDs()
        {
            List<int> champs = new List<int>();
            int[] champsArr = new int[130];

            string sURL = "https://na.api.pvp.net/api/lol/na/v1.2/champion?freeToPlay=false&api_key=91a00623-5397-4358-9481-608740057501";
            WebRequest wrGET = WebRequest.Create(sURL);
            WebResponse resp = wrGET.GetResponse();
            //Thread.Sleep(1000);


            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string jsonStr = sr.ReadToEnd();

            JObject jsonObj = JObject.Parse(jsonStr);

            JToken root = jsonObj.SelectToken("champions");
            int max = root.Count();
            root = root.First;
            for (int i = 0; i < max; ++i)
            {
                champs.Add((int)root.SelectToken("id"));
                champsArr[i] = (int)root.SelectToken("id");
                root = root.Next;
            }

            champs.Sort();
            return champs;

        }

        public static Dictionary<int, int> indexToIdMap()
        {
            List<int> champList = getAllChampIDs();
            Dictionary<int, int> arrayIndexToId = new Dictionary<int, int>();

            for (int i = 0; i < champList.Count; ++i)
            {
                arrayIndexToId.Add(i, champList[i]);
            }

            return arrayIndexToId;
        }

        public string getChampionName(int champID)
        {
            //string base_url = AppDomain.CurrentDomain.BaseDirectory;
            //string full_url = base_url + "/App_Data/staticChampData.json";
            var full_url = HostingEnvironment.MapPath("~/App_Data/staticChampData.json");
            StreamReader sr = new StreamReader(full_url);
            JToken root = JObject.Parse(sr.ReadToEnd());
            root = root.SelectToken("data");
            foreach(JToken child in root.Children())
            {
                if(champID == (int)child.First.SelectToken("key"))
                {
                    return (string)child.First.SelectToken("id");
                }
            }

            return "default_value";
        }



        public JToken getChampionNode(int champID)
        {
            string base_url = AppDomain.CurrentDomain.BaseDirectory;
            string full_url = base_url + "/App_Data/staticChampData.json";
            StreamReader sr = new StreamReader(full_url);
            JToken root = JObject.Parse(sr.ReadToEnd());
            root = root.SelectToken("data");
            foreach (JToken child in root.Children())
            {
                if (champID == (int)child.First.SelectToken("key"))
                {
                    return child.First;
                }
            }

            return root;
        }

        public JEnumerable<JToken> getChampionNodes()
        {
            string base_url = AppDomain.CurrentDomain.BaseDirectory;
            string full_url = base_url + "/App_Data/staticChampData.json";
            StreamReader sr = new StreamReader(full_url);
            JToken root = JObject.Parse(sr.ReadToEnd());
            root = root.SelectToken("data");

            return root.Children();

            /*
            foreach (JToken child in root.Children())
            {
                if (champID == (int)child.First.SelectToken("key"))
                {
                    return child.First;
                }
            }

            return root;*/
        }
    }
}