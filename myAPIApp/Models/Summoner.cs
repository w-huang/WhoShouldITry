using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace myAPIApp.Models
{

    public class Summoner
    {
        public int id { get; set; }
        public string name { get; set; }
        public int profileIconId { get; set; }
        public int summonerLevel { get; set; }
        public long revisionDate { get; set; }
    }

    public class ChampionMastery
    {
        public string highestGrade { get; set; }
        public int championPoints { get; set; }
        public int playerId { get; set; }
        public int championPointsUntilNextLevel { get; set; }
        public bool chestGranted { get; set; }
        public int championId { get; set; }
        public int championLevel { get; set; }
        public int championPointsSinceLastLevel { get; set; }
        public long lastPlayTime { get; set; }
    }

    
}