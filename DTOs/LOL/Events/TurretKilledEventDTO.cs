namespace WildRune.DTOs.LOL.Events
{
    public class TurretKilledEventDTO : BaseEventDTO
    {
        public List<string> Assisters { get; set; }
        public string KillerName { get; set; }
        public string TurretKilled { get; set; }
    }
}
