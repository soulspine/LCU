namespace WildRune.DTOs.LOL.Events
{
    public class EpicMonsterKillEvent : BaseEvent
    {
        public List<string> Assisters { get; set; }
        public string KillerName { get; set; }
        public bool Stolen { get; set; }
    }
}
