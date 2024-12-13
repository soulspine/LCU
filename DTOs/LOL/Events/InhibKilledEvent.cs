using WildRune.DTOs.LOL.Events;

namespace WildRune.DTOs.LOL
{
    public class InhibKilledEvent : BaseEvent
    {
        public List<string> Assisters { get; set; }
        public string InhibKilled { get; set; }
        public string KillerName { get; set; }
    }
}
