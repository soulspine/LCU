namespace WildRune.DTOs.LOL.Events
{
    public class MultikillEventDTO : BaseEventDTO
    {
        public string KillerName { get; set; }
        public int KillStreak { get; set; }
    }
}
