namespace WildRune.DTOs.LOL
{
    public class ActivePlayer
    {
        public Abilities abilities { get; set; }
        public ChampionStats championStats { get; set; }
        public double currentGold { get; set; }
        public FullRunes fullRunes { get; set; }
        public short level { get; set; }
        public string riotId { get; set; }
        public string riotIdGameName { get; set; }
        public string riotIdTagLine { get; set; }
        public string summonerName { get; set; }
        public bool teamRelativeColors { get; set; }
    }
}
