using Newtonsoft.Json;
using WildRune.DTOs.LOL;
using WildRune.DTOs.LOL.Events;

namespace WildRune
{
    public static class LOL
    {
        private const int port = 2999;

        // GAME DATA
        public static async Task<AllGameData?> AllGameData() => await Request<AllGameData>("liveclientdata/allgamedata");
        public static async Task<EventContainer?> EventData(int? eventId = null) => await Request<EventContainer>("liveclientdata/eventdata" + ((eventId == null) ? string.Empty : $"?eventId={eventId}"));
        public static async Task<GameData?> GameStats() => await Request<GameData>("liveclientdata/gamestats");
        public static async Task<List<Player>?> PlayerList() => await Request<List<Player>>("liveclientdata/playerlist");

        // ACTIVE PLAYER
        public static async Task<ActivePlayer?> ActivePlayer() => await Request<ActivePlayer>("liveclientdata/activeplayer");
        public static async Task<Abilities?> ActivePlayerAbilities() => await Request<Abilities>("liveclientdata/activeplayerabilities");
        public static async Task<string?> ActivePlayerName() => await Request<string>("liveclientdata/activeplayername");
        public static async Task<FullRunes?> ActivePlayerRunes() => await Request<FullRunes>("liveclientdata/activeplayerrunes");

        // SPECIFIC PLAYER
        public static async Task<MainRunes?> PlayerMainRunes(string playerName) => await Request<MainRunes>($"liveclientdata/playermainrunes?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");
        public static async Task<List<Item>?> PlayerItems(string playerName) => await Request<List<Item>>($"liveclientdata/playeritems?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");
        public static async Task<Scores?> PlayerScores(string playerName) => await Request<Scores>($"liveclientdata/playerscores?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");
        public static async Task<SummonerSpellsContainer?> PlayerSummonerSpells(string playerName) => await Request<SummonerSpellsContainer>($"liveclientdata/playersummonerspells?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");

        private static async Task<T?> Request<T>(string endpoint)
        {
            Utils.Endpoint.CleanUp(ref endpoint);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri($"https://127.0.0.1:{port}{endpoint}")
            };

            try
            {
                HttpResponseMessage response = await Utils.insecureHttpClient.SendAsync(request);
                if (response.StatusCode != System.Net.HttpStatusCode.OK) return default;
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), new EventConverter());
            }
            catch (HttpRequestException)
            {
                return default;
            }
        }
    }
}
