using WildRune.DTOs.LOL.Events;

namespace WildRune.DTOs.LOL
{
    public class ChampionKillEvent : BaseEvent
    {
        public List<string> Assisters { get; set; }
        public string KillerName { get; set; }
        public string VictimName { get; set; }
    }
}
