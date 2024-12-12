namespace WildRune.DTOs.LOL
{
    public class ChampionKillEventDTO : BaseEventDTO
    {
        public List<string> Assisters { get; set; }
        public string KillerName { get; set; }
        public string VictimName { get; set; }
    }
}
