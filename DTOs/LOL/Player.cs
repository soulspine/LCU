namespace WildRune.DTOs.LOL
{
    public class Player
    {
        public string championName { get; set; }
        public bool isBot { get; set; }
        public bool isDead { get; set; }
        public List<Item> items { get; set; }
        public short level { get; set; }
        public string position { get; set; }
        public string rawChampionName { get; set; }
        public string rawSkinName { get; set; }
        public double respawnTimer { get; set; }
        public string riotId { get; set; }
        public string riotIdGameName { get; set; }
        public string riotIdTagLine { get; set; }
        public MainRunes runes { get; set; }
        public Scores scores { get; set; }
        public int skinID { get; set; }
        public string skinName { get; set; }
        public string summonerName { get; set; }
        public SummonerSpellsContainer summonerSpells { get; set; }
        public TeamID team { get; set; }
    }
}
