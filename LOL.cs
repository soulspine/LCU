using Newtonsoft.Json;
using WildRune.DTOs.LOL;
using WildRune.DTOs.LOL.Events;

namespace WildRune
{
    public static class LOL
    {
        private const int port = 2999;

        // GAME DATA
        public static async Task<AllGameDataDTO?> AllGameData() => await Request<AllGameDataDTO>("liveclientdata/allgamedata");
        public static async Task<EventContainerDTO?> EventData(int? eventId = null) => await Request<EventContainerDTO>("liveclientdata/eventdata" + ((eventId == null) ? string.Empty : $"?eventId={eventId}"));

        public static async Task<List<PlayerDTO>?> PlayerList() => await Request<List<PlayerDTO>>("liveclientdata/playerlist");

        // ACTIVE PLAYER
        public static async Task<ActivePlayerDTO?> ActivePlayer() => await Request<ActivePlayerDTO>("liveclientdata/activeplayer");

        public static async Task<AbilitiesDTO?> ActivePlayerAbilities() => await Request<AbilitiesDTO>("liveclientdata/activeplayerabilities");

        public static async Task<string?> ActivePlayerName() => await Request<string>("liveclientdata/activeplayername");

        public static async Task<FullRunesDTO?> ActivePlayerRunes() => await Request<FullRunesDTO>("liveclientdata/activeplayerrunes");

        // SPECIFIC PLAYER
        public static async Task<MainRunesDTO?> PlayerMainRunes(string playerName) => await Request<MainRunesDTO>($"liveclientdata/playermainrunes?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");

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
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), new EventConverter());
            }
            catch (HttpRequestException)
            {
                return default;
            }
        }
    }
}
