using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.WebSockets;

namespace LCUcore
{
    public enum RequestMethod
    {
        GET, POST, PATCH, DELETE, PUT
    }

    public class LCU2 : ILCU2
    {
        public bool IsConnected { get; private set; } = false;

        private int? port = null;
        private string? region = null;
        private string? locale = null;
        private string? token = null;

        // can be static, no reason to  create different handlers across different instances
        private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler()
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
        });

        // this cannot be static
        private ClientWebSocket? socketConnection = null;
        private CancellationTokenSource? socketCancellationSource = null;

        private Dictionary<string, string>? cmdArgs = null;
        private bool connecting = false;
        private bool disconnecting = false;
        private const string processName = "LeagueClientUx";

        public LCU2()
        {

        }

        /// <summary>
        /// Connects to LCU's API and Websocket. It will repeatedly invoke <see cref="TryConnect"/>, blocking the thread until it succeeds.
        /// Sleeps for <paramref name="sleepTime"/> milliseconds between retries.
        /// </summary>
        /// <param name="sleepTime">Ttime (in milliseconds) to sleep between connection attempts. Defaults to 1000.</param>
        public void ForceConnect(int sleepTime = 1000)
        {
            while (!IsConnected)
            {
                TryConnect();
                Thread.Sleep(sleepTime);
            }
        }


        /// <summary>
        /// Tries to connect to LCU's API and Websocket once.
        /// If connection is already established (<see cref="IsConnected"/> == true) / process not running / API not ready, it will return immediately.
        /// Upon successful connection, it will set <see cref="IsConnected"/> to true.
        /// </summary>
        public void TryConnect()
        {
            // checking if it's already connecting or connected
            if (connecting || IsConnected) return;

            // checking if the process is running
            if (!Utils.Process.IsRunning(processName)) return;

            Utils.Process.GetCmdArgs(processName, ref cmdArgs);

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
                SendSubscriptionMessage("OnJsonApiEvent", 5, true); // subscribe to OnJsonApiEvent
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
        }

        public void Disconnect()
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
        }

        public void Subscribe(string endpoint)
        {
            SendSubscriptionMessage(endpoint, 5);
        }

        public void Unsubscribe(string endpoint)
        {
            SendSubscriptionMessage(endpoint, 6);
        }

        /// <summary>
        /// Invokes <see cref="SendSubscriptionMessageAsync(string, int, bool)"/> with the same arguments.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="opcode"></param>
        /// <param name="isEvent"></param>
        private void SendSubscriptionMessage(string endpoint, int opcode, bool isEvent = false)
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
        private async Task SendSubscriptionMessageAsync(string endpoint, int opcode, bool isEvent = false)
        {
            if (!IsConnected || socketConnection == null)
            {
                throw new InvalidOperationException($"Tried sending a subscription message to {endpoint} with opcode {opcode} but there's no connection to API.");
            }

            string eventName = isEvent ? endpoint : Utils.Endpoint.GetEventFromEndpoint(endpoint);

            byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes($"[{opcode}, \"{eventName}\"]");

            await socketConnection.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async void SocketMessageHandler()
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
                catch (System.Threading.Tasks.TaskCanceledException)
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

                    JArray arr = JArray.Parse(message);

                    string messageEvent = arr[1].ToString();

                    if (!messageEvent.StartsWith("OnJsonApiEvent")) continue; // not an event, probably welcome status message

                    JToken dataToken = arr[2]["data"]!;
                    Console.WriteLine(dataToken);

                }
            }
        }

        public async Task<HttpResponseMessage> Request(RequestMethod method, string endpoint, JObject? data = null, bool ignoreReady = false)
        {
            if (!IsConnected && !ignoreReady)
            {
                throw new InvalidOperationException($"Tried sending a request to {endpoint} but there's no connection to API.");
            }

            if (endpoint.StartsWith("/")) endpoint = endpoint.Substring(1);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Method = new HttpMethod(method.ToString()),
                RequestUri = new Uri($"https://127.0.0.1:{port}/{endpoint}"),
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
                return await httpClient.SendAsync(request);
            }
            catch (Exception e)
            {
                throw new HttpRequestException($"Failed when sending a request to {endpoint}. - {e.Message}");
            }
        }
    }
}
