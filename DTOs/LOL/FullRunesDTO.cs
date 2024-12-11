namespace WildRune.DTOs.LOL
{
    public class FullRunesDTO
    {
        public List<GeneralRuneDTO> generalRunes { get; set; }
        public GeneralRuneDTO keystone { get; set; }
        public GeneralRuneDTO primaryRuneTree { get; set; }
        public GeneralRuneDTO secondaryRuneTree { get; set; }
        public List<StatRuneDTO> statRunes { get; set; }
    }
}
