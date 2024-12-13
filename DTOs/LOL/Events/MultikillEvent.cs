namespace WildRune.DTOs.LOL.Events
{
    public class MultikillEvent : BaseEvent
    {
        public string KillerName { get; set; }
        public int KillStreak { get; set; }
    }
}
