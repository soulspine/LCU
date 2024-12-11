namespace WildRune
{
    /// <summary>
    /// Main wrapper class for the WildRune library. It contains all necessary stuff to interact with Client, Game and even Riot's API.
    /// </summary>
    public class WildRune
    {
        public readonly LCU Client;
        public readonly LOL Game;
        public readonly RiotAPI RiotApi;

        public WildRune(string apiKey = "")
        {
            Client = new LCU();
            Game = new LOL();
            RiotApi = new RiotAPI(apiKey);
        }
    }
}
