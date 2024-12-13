using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using WildRune.DTOs.LCU;

namespace WildRune
{
    public static class LCU
    {
        /// <summary>
        /// Represents the connection status to the API and Websocket.
        /// </summary>
        public static bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Represents the current gameflow phase.
        /// </summary>
        public static GameflowPhase CurrentGameflowPhase { get; private set; } = GameflowPhase.None;

        /// <summary>
        /// Represents the local summoner info.
        /// </summary>
        public static Summoner? LocalSummoner { get; private set; } = null;

        /// <summary>
        /// Event invoked when the connection to the API and Websocket is established.
        /// </summary>
        public static event Action? OnConnected = null;

        /// <summary>
        /// Event invoked when the connection to the API and Websocket is lost.
        /// </summary>
        public static event Action? OnDisconnected = null;

        /// <summary>
        /// Event invoked when the gameflow phase changes.
        /// </summary>
        public static event Action? OnGameflowPhaseChanged = null;

        /// <summary>
        /// Invoked when the local summoner info changes.
        /// </summary>
        public static event Action? OnLocalSummonerInfoChanged = null;

        private static int? port = null;
        private static string? region = null;
        private static string? locale = null;
        private static string? token = null;

        private static ConcurrentDictionary<string, List<Action<SubscriptionMessage>>> subscriptions = new();

        // http client is in Utils because it's used in both LCU2 and LOL
        private static ClientWebSocket? socketConnection = null;
        private static CancellationTokenSource? socketCancellationSource = null;

        //config-ish
        /// <summary>
        /// Whether to keep subscriptions after disconnecting. Defaults to false.
        /// </summary>
        public static bool PreserveSubscriptions { get; set; } = true;

        /// <summary>
        /// Whether to write all incoming events to console. Defaults to false.
        /// </summary>
        public static bool WriteAllEventsToConsole { get; set; } = false;

        private static bool connecting = false;
        private static bool disconnecting = false;
        private const string processName = "LeagueClientUx";

        static LCU()
        {
        }

        /// <summary>
        /// Looks up all methods bound to <see cref="OnConnected"/>, <see cref="OnDisconnected"/>, <see cref="OnGameflowPhaseChanged"/> and <see cref="OnLocalSummonerInfoChanged"/>.
        /// Also looks up all methods bound to subscribed events coming from the websocket.
        /// </summary>
        /// <returns>
        /// Dictionary with <see cref="string"/> keys being the endpoint or event names and values being a list of <see cref="string"/>s with method's names.
        /// </returns>
        public static Dictionary<string, List<string>> GetEventMethods()
        {
            Dictionary<string, List<string>> eventMethods = new();

            // methods bound to internal delegates
            foreach (var eventInfo in typeof(LCU).GetEvents())
            {
                Action? eventAction = typeof(LCU).GetField(eventInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!.GetValue(null) as Action;

                var invocations = eventAction?.GetInvocationList();

                List<string> invocationsNames = new List<string>();

                foreach (var invocation in invocations!)
                {
                    invocationsNames.Add($"{invocation.Method.DeclaringType!.FullName}.{invocation.Method.Name}");
                }

                eventMethods.Add(eventInfo.Name, invocationsNames);
            }

            // methods bound to subscribed events
            foreach (string endpoint in subscriptions.Keys)
            {
                var actions = subscriptions[endpoint];

                List<string> actionsNames = new List<string>();

                foreach (var action in actions)
                {
                    actionsNames.Add($"{action.Method.DeclaringType!.FullName}.{action.Method.Name}");
                }

                eventMethods.Add(endpoint, actionsNames);
            }
            return eventMethods;
        }

        /// <summary>
        /// Connects to LCU's API and Websocket. It will repeatedly invoke <see cref="TryConnect"/>, blocking the thread until it succeeds.
        /// Sleeps for <paramref name="sleepTime"/> milliseconds between retries.
        /// </summary>
        /// <param name="sleepTime">Ttime (in milliseconds) to sleep between connection attempts. Defaults to 1000.</param>
        public static void ForceConnect(int sleepTime = 1000)
        {
            while (!IsConnected)
            {
                TryConnect();
                Thread.Sleep(sleepTime);
            }
        }

        /// <summary>
        /// Tries to connect to LCU's API and Websocket once.
        /// If connection is already established / LCU process not running / API not ready, it will return immediately.
        /// Upon successful connection, it will set <see cref="IsConnected"/> to true.
        /// </summary>
        public static void TryConnect()
        {
            // checking if it's already connecting or connected
            if (connecting || IsConnected) return;

            // checking if the process is running
            if (!Utils.Process.IsRunning(processName)) return;

            Dictionary<string, string> cmdArgs = Utils.Process.GetCmdArgs(processName)!;

            if (cmdArgs == null) { connecting = false; return; }

            // getting necessary data from the command line arguments
            port = Convert.ToInt32(cmdArgs["app-port"]);
            region = cmdArgs["region"];
            locale = cmdArgs["locale"];
            string rawToken = cmdArgs["remoting-auth-token"]; // needed later for authorization in websocket connection
            token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("riot:" + rawToken));

            // checking if the API is ready

            HttpResponseMessage apiCheckResponse;
            try
            {
                apiCheckResponse = Request(RequestMethod.GET, "/lol-gameflow/v1/availability", ignoreReady: true).Result;
            }
            catch
            {
                { connecting = false; return; }
            }
            
            if (apiCheckResponse.StatusCode != HttpStatusCode.OK) { connecting = false; return; }
            
            JObject apiData = JObject.Parse(apiCheckResponse.Content.ReadAsStringAsync().Result);
            if (apiData == null) { connecting = false; return; }

            if (!Convert.ToBoolean(apiData!["isAvailable"]))
            {
                connecting = false;
                return;
            }

            // cheking what is the current gameflow phase and setting it
            var gameflowStateRequest = Request(RequestMethod.GET, "/lol-gameflow/v1/gameflow-phase", ignoreReady: true).Result;
            if (Enum.TryParse(gameflowStateRequest.Content.ReadAsStringAsync().Result.Replace("\"", ""), true, out GameflowPhase result))
            {
                CurrentGameflowPhase = result;
            }
            else return;

            // getting local summoner data
            var summonerRequest = Request(RequestMethod.GET, "/lol-summoner/v1/current-summoner", ignoreReady: true).Result;
            if (summonerRequest.IsSuccessStatusCode)
            {
                LocalSummoner = JsonConvert.DeserializeObject<Summoner>(summonerRequest.Content.ReadAsStringAsync().Result);
            }
            else return;

            // creating a socket and connecting to it
            if (socketConnection != null) socketConnection.Dispose();
            if (socketCancellationSource != null) socketCancellationSource.Dispose();

            socketConnection = new ClientWebSocket();
            socketCancellationSource = new CancellationTokenSource();

            socketConnection.Options.AddSubProtocol("wamp"); // set protocol
            socketConnection.Options.Credentials = new NetworkCredential("riot", rawToken); // set credentials
            socketConnection.Options.RemoteCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true); // ignore certificate errors
            try
            {
                socketConnection.ConnectAsync(new Uri($"wss://127.0.0.1:{port}/"), socketCancellationSource.Token).Wait();
                SocketMessageHandler(); // runs until socket closes
                SendSubscriptionMessage("OnJsonApiEvent", 5, true); // subscribe to OnJsonApiEvent - all events
            }
            catch
            {
                socketConnection.Dispose();
                socketConnection = null;
                socketCancellationSource.Dispose();
                socketCancellationSource = null;
                connecting = false;
                return;
            }

            // end of checks, setting IsConnected to true
            IsConnected = true;

            connecting = false;
            OnConnected?.Invoke();
        }

        /// <summary>
        /// Disconnects from the API and Websocket. Resets all variables to null and sets <see cref="IsConnected"/> to false.
        /// </summary>
        public static void Disconnect()
        {
            if (!IsConnected || disconnecting) return;
            disconnecting = true;

            // close the socket and dispose of it
            socketConnection!.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None).Wait();
            socketCancellationSource!.Cancel();

            socketConnection.Dispose();
            socketConnection = null;
            socketCancellationSource.Dispose();
            socketCancellationSource = null;

            // set variables accordingly
            IsConnected = false;
            disconnecting = false;
            port = null;
            region = null;
            locale = null;
            token = null;
            CurrentGameflowPhase = GameflowPhase.None;
            LocalSummoner = null;
            if (!PreserveSubscriptions) subscriptions.Clear();

            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Subscribes to an event that will come from <paramref name="endpoint"/>. When the event is received, it will invoke the action passed as <paramref name="func"/>.
        /// </summary>
        /// <param name="endpoint"/>
        /// <param name="func"/>
        public static void Subscribe(string endpoint, Action<SubscriptionMessage> func)
        {
            bool hadActionsBefore = subscriptions.TryGetValue(endpoint, out _);

            Utils.Endpoint.CleanUp(ref endpoint);

            if (subscriptions.TryGetValue(endpoint, out _)) // there are actions binded to this endpoint
            {
                //check if the action is already binded and throw an exception if it is
                if (subscriptions[endpoint].Contains(func)) throw new InvalidOperationException($"This action ({func.Method.Name}) is already binded to this endpoint ({endpoint}).");
                else subscriptions[endpoint].Add(func); // add it to the list
            }
            else // no actions binded to this endpoint
            {
                subscriptions.TryAdd(endpoint, new List<Action<SubscriptionMessage>>() { func }); // create a new list with the action
            }
        }

        /// <summary>
        /// Unsubscribes from an event that comes from <paramref name="endpoint"/>.
        /// If <paramref name="func"/> is passed, it will only remove that specific action.
        /// Otherwise, it will remove all actions binded to that endpoint.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="func"></param>
        public static void Unsubscribe(string endpoint, Action<SubscriptionMessage>? func = null)
        {
            Utils.Endpoint.CleanUp(ref endpoint);

            if (func == null) subscriptions.TryRemove(endpoint, out _); // remove all actions if no action specified
            else if (subscriptions.TryGetValue(endpoint, out List<Action<SubscriptionMessage>>? actions))
            {
                actions.Remove(func); // remove specific action
                if (actions.Count == 0) subscriptions.TryRemove(endpoint, out _); // remove endpoint if no actions binded
            }
        }

        /// <summary>
        /// Invokes <see cref="SendSubscriptionMessageAsync(string, int, bool)"/> with the same arguments.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="opcode"></param>
        /// <param name="isEvent"></param>
        private static void SendSubscriptionMessage(string endpoint, int opcode, bool isEvent = false)
        {
            Task.Run(async () => await SendSubscriptionMessageAsync(endpoint, opcode, isEvent));
        }

        /// <summary>
        /// Tries to send a subscription message to the websocket. If there's no connection, it will throw an <see cref="InvalidOperationException"/>.
        /// Available <paramref name="opcode"/> values are 5 (subscribe) and 6 (unsubscribe).
        /// If you want to pass an event <c>(OnJsonApiEvent_something)</c> rather than an endpoint <c>(/something/)</c>, set <paramref name="isEvent"/> to true.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="opcode"></param>
        /// <param name="isEvent"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static async Task SendSubscriptionMessageAsync(string endpoint, int opcode, bool isEvent = false)
        {
            if (!IsConnected || socketConnection == null)
            {
                throw new InvalidOperationException($"Tried sending a subscription message to {endpoint} with opcode {opcode} but there's no connection to API.");
            }

            if (!isEvent) Utils.Endpoint.CleanUp(ref endpoint);

            string eventName = isEvent ? endpoint : Utils.Endpoint.GetEventFromEndpoint(endpoint);

            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes($"[{opcode}, \"{eventName}\"]");

            await socketConnection.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Loop listening for messages and invoking actions binded to specific endpoints.
        /// Do not invoke anywhere else than after initializing the socket, only one of them should be running at a time.
        /// </summary>
        private static async void SocketMessageHandler()
        {
            var buffer = new byte[1024];
            System.Text.StringBuilder messageBuffer = new System.Text.StringBuilder();

            while (socketConnection != null && socketConnection.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await socketConnection.ReceiveAsync(new ArraySegment<byte>(buffer), socketCancellationSource!.Token);
                }
                catch
                {
                    return;
                }


                if (result.MessageType == WebSocketMessageType.Close) Disconnect();
                else
                {
                    messageBuffer.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (!result.EndOfMessage) continue;

                    string message = messageBuffer.ToString();
                    messageBuffer.Clear();

                    if (WriteAllEventsToConsole) Console.WriteLine(message);

                    JArray arr = JArray.Parse(message);

                    if (!arr[1].ToString().StartsWith("OnJsonApiEvent")) continue; // not an event, probably welcome status message

                    string messageEndpoint = arr[2]["uri"]!.ToString();

                    Utils.Endpoint.CleanUp(ref messageEndpoint);

                    string messageType = arr[2]["eventType"]!.ToString();
                    JToken messageToken = arr[2]["data"]!;

                    // SPECIAL CASE FOR EXITING THE CLIENT
                    if (messageEndpoint == "/process-control/v1/process"){
                        if (messageToken["status"]!.ToString() == "Stopping")
                        {
                            Disconnect();
                            return;
                        }
                    }
                    // SPECIAL CASE FOR GAMEFLOW PHASE
                    else if (messageEndpoint == "/lol-gameflow/v1/gameflow-phase")
                    {
                        CurrentGameflowPhase = (GameflowPhase)Enum.Parse(typeof(GameflowPhase), messageToken.ToString());
                        OnGameflowPhaseChanged?.Invoke();
                    }
                    // SPECIAL CASE FOR LOCAL SUMMONER
                    else if (messageEndpoint == "/lol-summoner/v1/current-summoner")
                    {
                        LocalSummoner = JsonConvert.DeserializeObject<Summoner>(messageToken.ToString());
                        OnLocalSummonerInfoChanged?.Invoke();
                    }

                    if (subscriptions.ContainsKey(messageEndpoint))
                    {
                        foreach (var action in subscriptions[messageEndpoint])
                        {

                            action.Invoke(new SubscriptionMessage(messageEndpoint, messageType, messageToken));
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Sends a request to the API. If <paramref name="data"/> is passed, it will get serialized automatically. It will throw an <see cref="InvalidOperationException"/> if you try to make a request before properly connecting.
        /// You can disable the check for connection by setting <paramref name="ignoreReady"/> to true.
        /// When the request fails unexpectedly, it will throw an <see cref="HttpRequestException"/>.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <param name="data"></param>
        /// <param name="ignoreReady"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="HttpRequestException"></exception>
        public static async Task<HttpResponseMessage> Request(RequestMethod method, string endpoint, dynamic? data = null, bool ignoreReady = false)
        {
            if (!IsConnected && !ignoreReady)
            {
                throw new InvalidOperationException($"Tried sending a request to {endpoint} but there's no connection to API.");
            }

            Utils.Endpoint.CleanUp(ref endpoint);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = new HttpMethod(method.ToString()),
                RequestUri = new Uri($"https://127.0.0.1:{port}{endpoint}"),
                Headers =
                    {
                        { HttpRequestHeader.Authorization.ToString(), $"Basic {token}" },
                        { HttpRequestHeader.Accept.ToString(), "application/json" }
                    },
            };

            if (data != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            }

            try
            {
                return await Utils.insecureHttpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                throw new HttpRequestException($"Failed when sending a request to {endpoint}. - {e.Message}");
            }
        }

        /// <summary>
        /// Class representing a message received from the websocket. All methods binded to an endpoint will receive this object.
        /// </summary>
        public class SubscriptionMessage
        {
            public string Endpoint;
            public string Type;
            public JToken Data;
            public SubscriptionMessage(string endpoint, string type, JToken data) { Endpoint = endpoint; Type = type; Data = data; }
        }

    }
}
