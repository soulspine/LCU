namespace WildRune.DTOs.LOL
{
    public class AllGameData
    {
        public ActivePlayer activePlayer { get; set; }
        public List<Player> allPlayers { get; set; }
        public EventContainer events { get; set; }
        public GameData gameData { get; set; }
    }
}
