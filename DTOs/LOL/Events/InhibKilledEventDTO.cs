using WildRune.DTOs.LOL.Events;

namespace WildRune.DTOs.LOL
{
    public class InhibKilledEventDTO : BaseEventDTO
    {
        public List<string> Assisters { get; set; }
        public string InhibKilled { get; set; }
        public string KillerName { get; set; }
    }
}
