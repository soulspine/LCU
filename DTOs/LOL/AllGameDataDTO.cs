namespace WildRune.DTOs.LOL
{
    public class AllGameDataDTO
    {
        public ActivePlayerDTO activePlayer { get; set; }
        public List<PlayerDTO> allPlayers { get; set; }
        public EventContainerDTO events { get; set; }
        public GameDataDTO gameData { get; set; }
    }
}
