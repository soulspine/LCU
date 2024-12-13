using System.Collections.Specialized;
using System.Windows.Markup;

namespace WildRune.DTOs.LOL.Events
{
    public class DragonKillEvent : EpicMonsterKillEvent
    {
        public DragonType DragonType { get; set; }
    }
}


