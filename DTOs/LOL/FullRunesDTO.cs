namespace WildRune.DTOs.LOL
{
    public class FullRunesDTO : MainRunesDTO
    {
        public List<GeneralRuneDTO> generalRunes { get; set; }
        public List<StatRuneDTO> statRunes { get; set; }
    }
}
