using System.Collections.Specialized;
using System.Windows.Markup;

namespace WildRune.DTOs.LOL.Events
{
    public class DragonKillEventDTO : EpicMonsterKillEventDTO
    {
        public DragonTypeDTO DragonType { get; set; }
    }
}


