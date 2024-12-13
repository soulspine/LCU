namespace WildRune.DTOs.LOL.Events
{
    public class TurretKilledEvent : BaseEvent
    {
        public List<string> Assisters { get; set; }
        public string KillerName { get; set; }
        public string TurretKilled { get; set; }
    }
}
