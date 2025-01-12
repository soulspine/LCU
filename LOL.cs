using Newtonsoft.Json;
using WildRune.DTOs.LOL;
using WildRune.DTOs.LOL.Events;

namespace WildRune
{
    /// <summary>
    /// Interface for all League of Legends in-game data retrieval methods.
    /// </summary>
    public static class LOL
    {
        private const int port = 2999;

        /// <summary>
        /// Auto-retriever for all game data from currently running instance.
        /// You can bind <see cref="Action"/>s to <see cref="Listener.OnGameEvent"/> and <see cref="Listener.OnGameUpdate"/> events.
        /// When <see cref="Listener.Start"/> is called, it will start a loop that will retrieve all game data every <paramref name="sleepTime"/> milliseconds.
        /// Using <see cref="Listener.Stop"/> will stop the loop.
        /// Specifying <see cref="Listener.StopAtLostConnection"/> as <c>true</c> will stop the loop when data retrieval fails.
        /// </summary>
        public static class Listener
        {
            // config
            /// <summary>
            /// If set to <c>true</c>, the listener will stop when data retrieval fails.
            /// If set to <c>false</c>, the listener will keep trying to retrieve data and repeatedly invoke <see cref="OnFailedConnection"/> event."/>
            /// </summary>
            public static bool StopAtLostConnection = true;

            /// <summary>
            /// Represents the current state of the listener.
            /// </summary>
            public static bool IsRunning { get; private set; }
            private static Task? listenerTask = null;
            private static CancellationTokenSource? cts = null;

            /// <summary>
            /// Invoked when a new game event is detected. Passed methods get a <see cref="BaseEvent"/> object as an argument.
            /// </summary>
            public static event Action<BaseEvent>? OnGameEvent = null;

            /// <summary>
            /// Invoked when data is successfully retrieved from the game.
            /// </summary>
            public static event Action? OnGameUpdate = null;

            /// <summary>
            /// Invoked when data retrieval fails.
            /// </summary>
            public static event Action? OnFailedConnection = null;

            /// <summary>
            /// Read-only property that contains all game data from the currently running instance.
            /// </summary>
            public static AllGameData? AllGameData { get; private set; } = null;

            /// <summary>
            /// Read-only property that contains all game events from the currently running instance.
            /// </summary>
            public static List<BaseEvent>? Events => AllGameData?.events.Events;

            /// <summary>
            /// Starts the loop that retrieves all game data every <paramref name="sleepTime"/> milliseconds.
            /// Can throw <see cref="InvalidOperationException"/> if the listener is already running.
            /// </summary>
            /// <param name="sleepTime"></param>
            /// <exception cref="InvalidOperationException"></exception>
            public static void Start(uint? sleepTime = null)
            {
                if (IsRunning) throw new InvalidOperationException("Tried to start the listener but it is already running");
                cts = new CancellationTokenSource();
                listenerTask = Task.Run(async () => await Loop(sleepTime, cts.Token));
                IsRunning = true;
            }

            /// <summary>
            /// Stops the loop that retrieves all game data and resets <see cref="AllGameData"/> and <see cref="Events"/>.
            /// Can throw <see cref="InvalidOperationException"/> if the listener is not running.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            public static void Stop()
            {
                if (!IsRunning) throw new InvalidOperationException("Tried to stop the listener but it is not running");
                cts!.Cancel();
                cts = null;
                listenerTask = null;
                IsRunning = false;
                AllGameData = null;
            }

            /// <summary>
            /// Loop retrieving all game data after sleeping for <paramref name="sleepTime"/> milliseconds.
            /// Requires <paramref name="ct"/> as a parameter to be able to cancel the loop.
            /// Throws generic <see cref="Exception"/> if the loop fails unexpectedly.
            /// </summary>
            /// <param name="sleepTime"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            private static async Task Loop(uint? sleepTime, CancellationToken ct)
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        if (sleepTime != null) await Task.Delay(Convert.ToInt32(sleepTime), ct);

                        List<BaseEvent>? previousEvents = Events;
                        AllGameData = await AllGameData();

                        if ((AllGameData == null))
                        {
                            OnFailedConnection?.Invoke();
                            if (StopAtLostConnection) Stop();
                            continue;
                        }

                        OnGameUpdate?.Invoke();
                        if ((previousEvents != null) && (previousEvents.Count != Events!.Count))
                        {
                            for (int i = Events.Count - 1; i >= previousEvents.Count; i--) OnGameEvent?.Invoke(Events[i]);
                        }
                    }
                }
                catch (TaskCanceledException) // when .Stop() is called
                {

                }
                catch (Exception e)
                {
                    throw new Exception("Listener loop failed unexpectedly", e);
                }
            }

            public static void ClearOnGameEvent() => OnGameEvent = null;
            public static void ClearOnGameUpdate() => OnGameUpdate = null;
            public static void ClearOnFailedConnection() => OnFailedConnection = null;

            public static void StopIfNoActions()
            {
                if ((IsRunning) && (OnGameEvent == null) && (OnGameUpdate == null) && (OnFailedConnection == null)) Stop();
            }
        }


        // GAME DATA

        /// <summary>
        /// Retrieves all game data from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="DTOs.LOL.AllGameData"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<AllGameData?> AllGameData() => await Request<AllGameData>("liveclientdata/allgamedata");

        /// <summary>
        /// Retrieves all event data from currently running instance. Optionally, you can specify an <paramref name="eventId"/> to get only that event.
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns>
        /// <see cref="DTOs.LOL.EventContainer"/> object or <c>null</c> if the request fails
        /// </returns>
        public static async Task<EventContainer?> EventData(int? eventId = null) => await Request<EventContainer>("liveclientdata/eventdata" + ((eventId == null) ? string.Empty : $"?eventId={eventId}"));

        /// <summary>
        /// Retrieves basic game stats from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="DTOs.LOL.GameData"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<GameData?> GameStats() => await Request<GameData>("liveclientdata/gamestats");

        /// <summary>
        /// Retrieves all player data from currently running instance.
        /// </summary>
        /// <returns>
        /// List with <see cref="DTOs.LOL.Player"/> objects or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<List<Player>?> PlayerList() => await Request<List<Player>>("liveclientdata/playerlist");

        // ACTIVE PLAYER
        /// <summary>
        /// Retrieves active player's data from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="DTOs.LOL.ActivePlayer"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<ActivePlayer?> ActivePlayer() => await Request<ActivePlayer>("liveclientdata/activeplayer");

        /// <summary>
        /// Retrieves active player's abilities data from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="DTOs.LOL.Abilities"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<Abilities?> ActivePlayerAbilities() => await Request<Abilities>("liveclientdata/activeplayerabilities");

        /// <summary>
        /// Retrieves active player's name from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="string"/> or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<string?> ActivePlayerName() => await Request<string>("liveclientdata/activeplayername");

        /// <summary>
        /// Retrieves active player's runes data from currently running instance.
        /// </summary>
        /// <returns>
        /// <see cref="DTOs.LOL.FullRunes"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<FullRunes?> ActivePlayerRunes() => await Request<FullRunes>("liveclientdata/activeplayerrunes");

        // SPECIFIC PLAYER
        /// <summary>
        /// Retrieves specific player's main runes data from currently running instance. Requires <paramref name="riotId"/> as a parameter. Those can be obtained from <see cref="PlayerList"/> method.
        /// </summary>
        /// <param name="riotId"></param>
        /// <returns>
        /// <see cref="DTOs.LOL.MainRunes"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<MainRunes?> PlayerMainRunes(string riotId) => await Request<MainRunes>($"liveclientdata/playermainrunes?riotId={System.Web.HttpUtility.UrlEncode(riotId)}");

        /// <summary>
        /// Retrieves specific player's items data from currently running instance. Requires <paramref name="riotId"/> as a parameter. Those can be obtained from <see cref="PlayerList"/> method.
        /// </summary>
        /// <param name="riotId"></param>
        /// <returns>
        /// List of <see cref="DTOs.LOL.Item"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<List<Item>?> PlayerItems(string riotId) => await Request<List<Item>>($"liveclientdata/playeritems?riotId={System.Web.HttpUtility.UrlEncode(riotId)}");

        /// <summary>
        /// Retrieves specific player's scores data from currently running instance. Requires <paramref name="riotId"/> as a parameter. Those can be obtained from <see cref="PlayerList"/> method.
        /// Important to note, <see cref="DTOs.LOL.Scores.creepScore"/> displays only multiples of 10 so if a player has 123 CS, it will display 120.
        /// </summary>
        /// <param name="riotId"></param>
        /// <returns>
        /// <see cref="DTOs.LOL.Scores"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<Scores?> PlayerScores(string riotId) => await Request<Scores>($"liveclientdata/playerscores?riotId={System.Web.HttpUtility.UrlEncode(riotId)}");

        /// <summary>
        /// Retrieves specific player's summoner spells data from currently running instance. Requires <paramref name="riotId"/> as a parameter. Those can be obtained from <see cref="PlayerList"/> method.
        /// </summary>
        /// <param name="riotId"></param>
        /// <returns>
        /// <see cref="DTOs.LOL.SummonerSpellsContainer"/> object or <c>null</c> if the request fails.
        /// </returns>
        public static async Task<SummonerSpellsContainer?> PlayerSummonerSpells(string riotId) => await Request<SummonerSpellsContainer>($"liveclientdata/playersummonerspells?riotId={System.Web.HttpUtility.UrlEncode(riotId)}");

        /// <summary>
        /// Tempate method for making requests to the LCU API. It is used by all other methods in this class.
        /// Specify return type with <typeparamref name="T"/> parameter. Specify endpoint with <paramref name="endpoint"/> parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <returns>
        /// Specified <typeparamref name="T"/> object or <c>null</c> if the request fails.
        /// </returns>
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
