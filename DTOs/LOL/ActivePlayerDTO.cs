namespace WildRune.DTOs.LOL
{
    public class ActivePlayerDTO
    {
        public AbilitiesDTO abilities { get; set; }
        public ChampionStatsDTO championStats { get; set; }
        public double currentGold { get; set; }
        public FullRunesDTO fullRunes { get; set; }
        public short level { get; set; }
        public string riotId { get; set; }
        public string riotIdGameName { get; set; }
        public string riotIdTagLine { get; set; }
        public string summonerName { get; set; }
        public bool teamRelativeColors { get; set; }
    }
}
