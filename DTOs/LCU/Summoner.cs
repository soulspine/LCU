namespace WildRune.DTOs.LCU
{
    public class Summoner
    {
        public Int64 accountId { get; set; }
        public string displayName { get; set; }
        public string gameName { get; set; }
        public string internalName { get; set; }
        public bool nameChangeFlag { get; set; }
        public int percentCompleteForNextLevel { get; set; }
        public string privacy { get; set; }
        public int profileIconId { get; set; }
        public string puuid { get; set; }
        public RerollPoints rerollPoints { get; set; }
        public Int64 summonerId { get; set; }
        public int summonerLevel { get; set; }
        public string tagLine { get; set; }
        public bool unnamed { get; set; }
        public Int64 xpSinceLastLevel { get; set; }
        public Int64 xpUntilNextLevel { get; set; }
    }
}
