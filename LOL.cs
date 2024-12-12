using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WildRune.DTOs.LOL;

namespace WildRune
{
    public class LOL
    {
        private const int port = 2999;

        public async Task<AllGameDataDTO?> AllGameData()
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, "liveclientdata/allgamedata");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<AllGameDataDTO>(await response.Content.ReadAsStringAsync(), new EventConverter());
        }

        public async Task<ActivePlayerDTO?> ActivePlayer()
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, "liveclientdata/activeplayer");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<ActivePlayerDTO>(await response.Content.ReadAsStringAsync());
        }

        public async Task<AbilitiesDTO?> ActivePlayerAbilities()
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, "liveclientdata/activeplayerabilities");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<AbilitiesDTO>(await response.Content.ReadAsStringAsync());
        }

        public async Task<string?> ActivePlayerName()
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, "liveclientdata/activeplayername");
            if (response == null) return "";
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<FullRunesDTO?> ActivePlayerRunes()
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, "liveclientdata/activeplayerrunes");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<FullRunesDTO>(await response.Content.ReadAsStringAsync());
        }

        public async Task<MainRunesDTO?> PlayerMainRunes(string playerName)
        {
            HttpResponseMessage? response = await Request(RequestMethod.GET, $"liveclientdata/playermainrunes?riotId={System.Web.HttpUtility.UrlEncode(playerName)}");
            if (response == null) return null;
            return JsonConvert.DeserializeObject<MainRunesDTO>(await response.Content.ReadAsStringAsync());
        }

        private async Task<HttpResponseMessage?> Request(RequestMethod method, string endpoint)
        {
            Utils.Endpoint.CleanUp(ref endpoint);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = new HttpMethod(method.ToString()),
                RequestUri = new Uri($"https://127.0.0.1:{port}{endpoint}")
            };

            try
            {
                return await Utils.insecureHttpClient.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}
