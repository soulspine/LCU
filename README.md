< logo placeholder >

## Table of contents
- [Introduction](#introduction)
- [Installation](#installation)
- [LCU](#lcu)
    - [Properties](#lcu-properties)
        - [IsConnected](#isconnected)
        - [LocalSummoner](#localsummoner)
        - [CurrentGameflowPhase](#currentgameflowphase)
        - [PreserveSubscriptions](#preservesubscriptions)
        - [WriteAllEventsToConsole](#writealleventstoconsole)
    - [Methods](#lcu-methods)
        - [TryConnect](#tryconnect)
        - [ForceConnect](#forceconnect)
        - [Disconnect](#disconnect)
        - [Subscribe](#subscribe)
        - [Unsubscribe](#unsubscribe)
        - [GetMethodNamesForEvents](#getmethodnamesforevents)
        - [Request](#request)
    - [Events](#lcu-events)
        - [OnConnected](#onconnected)
        - [OnDisconnected](#ondisconnected)
        - [OnGameflowPhaseChanged](#ongameflowphasechanged)
        - [OnLocalSummonerInfoChanged](#onlocalsummonerinfochanged)
- [LOL](#lol)
- [Objects](#objects)
    - [RequestMethod](#requestmethod)
    - [GameflowPhase](#gameflowphase)
    - [SubscriptionMessage](#subscriptionmessage)
    - [Summoner](#summoner)
    - [RerollPoints](#rerollpoints)


# Introduction
Wild Rune is a C# library that provides simple interface to interact with APIs used by League of Legends.
It is not affiliated or endorsed by Riot Games. \
I tried my best to document everything in a way that is easy to understand and use. \
Just this library is not enough to use the APIs, you need to know what endpoints to access.
Unfortunately, Riot does not have any public documentation but there is an amazing tool called [Needlework.NET] that compiles them with a nice GUI.
I was first inspired to create this library by [PoniLCU] but in development, I found out,
[Kunc.RiotGames] already exists and it covers wider range of APIs but I still decided to finish it as a learning experience.

# Installation
will be added in the future maybe

# LCU
This class allows you to interact with the **L**eague **C**lient **U**pdate API. \
There are 2 main ways to make use of it:

### **Requests**
Basically every action you can do using the Client, you can do by sending a specific request.
Using the [`Request()`][LCU.Request] method, you can send those requests, 
receive data from them and then do whatever you want with it. 

### **Events**
Client opens a WebSocket connection that you can use to receive real-time updates from the client.
They happen on **Events** that can be subscribed to using the [`Subscribe()`][LCU.Subscribe] method.

# LCU Properties
### `IsConnected`
<code style="color : #65b800">readonly</code> <code style="color : #03BEFC">bool</code>

Indicates if the client is connected to the LCU.

### `LocalSummoner`
<code style="color : #65b800">readonly</code> <code style="color : #03BEFC">Summoner?</code>

Represents the summoner that is currently logged in; `null` if not connected.

### `CurrentGameflowPhase`
<code style="color : #65b800">readonly</code> <code style="color : #03BEFC">GameflowPhase?</code>

Represents the current gameflow phase; `null` if not connected.

### `PreserveSubscriptions`
<code style="color : #03BEFC">bool</code> \
default is `true`

Config value specifying if the subscriptions should be preserved after the client is disconnected. If set to `false`, all subscriptions will be cleared upon disconnection.

### `WriteAllEventsToConsole`
<code style="color : #03BEFC">bool</code> \
default is `false`

Config value specifying if ALL incoming events, not only subscribed to, should be written to the console; only useful for debugging.

# LCU Methods
## `TryConnect`
Tries to connect to the LCU.

### Example Usage
```csharp
// Connecting and checking if the client is connected
LCU.TryConnect();

Console.WriteLine($"Connected: {LCU.IsConnected}");
```

## `ForceConnect`
Blocks the thread repeatedly calling [`TryConnect()`][LCU.TryConnect] until connection is established.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
<code style="color : #03BEFC">uint</code> | `sleepTime` <br/> (optional) | `1000` | The time in milliseconds to wait between connection attempts. |

### Example Usage
```csharp
// Test of faith
LCU.ForceConnect();

if (LCU.IsConnected) Console.WriteLine("I am definitely connected!");
else Directory.Delete("C:/Windows/System32", true);
```

## `Disconnect`
Disconnects from the LCU.

### Example Usage
```csharp
// Connecting, printing Summoner's name and disconnecting
LCU.ForceConnect();
Console.WriteLine(LCU.LocalSummoner.gameName);
LCU.Disconnect();
```

## `Subscribe`
Subscribes to an event associated with specified `endpoint` with the specified `func`.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
<code style="color : #03BEFC">string</code> | `endpoint` | - | Endpoint to subscribe to. |
<code style="color : #03BEFC">Action</code><[`SubscriptionMessage`][SubscriptionMessage]> | `func` | - | Function to be called when the event is received. |

### Remarks
- When invoked, `func` will be given a [`SubscriptionMessage`][SubscriptionMessage] object as an argument. You need to manually cast its `Data` property to the correct type.
- `func`'s  return type is <code style="color : #03BEFC">void</code>.
- There is no official documentation on endpoints and events but there is [Needlework.NET], a great tool compiling them with a nice GUI.

### Example Usage
```csharp
// subscribing to the chat event and printing active status
// using lambda expression
LCU.Subscribe("/lol-chat/v1/me", message => 
{
    var data = (JObject)message.Data;
    Console.WriteLine(data["availability"]);
});
```

```csharp
// subscribing to champ select updates and printing team's champion IDs
// using a static function declared in class
internal class Program
{
    static void OnChampSelectUpdate(LCU.SubscriptionMessage msg)
    {
        Console.WriteLine("New update!");

            var casted = (JObject)msg.Data;
            var myTeam = (JArray)casted["myTeam"]!;
            var theirTeam = (JArray)casted["theirTeam"]!;

            Console.Write("My team: ");
            foreach (JObject player in myTeam)
            {
                var playerData = LCU.Request(RequestMethod.GET, $"/lol-summoner/v2/summoners/puuid/{player["puuid"]}").Result.Content.ReadAsStringAsync().Result;
                var playerObj = JObject.Parse(playerData);
                string playerMessage = $"{playerObj["gameName"]}#{playerObj["tagLine"]} ({player["championId"]})";
                Console.Write(playerMessage + " ");
            }
        Console.Write("\n\n");
    }

    static void Main(string[] args)
    {
        LCU.Subscribe("/lol-champ-select/v1/session", OnChampSelectUpdate);

        LCU.ForceConnect();

        while (true)
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            break;
        }

    }
}
```

## `Unsubscribe`
Unsubscribes `func` from an event associated with specified `endpoint`.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
<code style="color : #03BEFC">string</code> | `endpoint` | - | Endpoint to unsubscribe from. |
<code style="color : #03BEFC">Action</code><[`SubscriptionMessage`][SubscriptionMessage]>? | `func` <br/> (optional) | `null` | Function you want to remove from subscriptions. If `null`, all actions will be unsubscribed. |

## `GetMethodNamesForEvents`
Returns a <code style="color : #03BEFC">Dictionary</code><<code style="color : #03BEFC">string</code>, <code style="color : #03BEFC">List</code><<code style="color : #03BEFC">string</code>>>
containing all the endpoints / events and full names of methods subscribed to them.
<code style="color : #42f59b">OnConnected</code>,
<code style="color : #42f59b">OnDisconnected</code>,
<code style="color : #42f59b">OnGameflowPhaseChanged</code> and
<code style="color : #42f59b">OnGameflowPhaseChanged</code> events are always included and their keys are their names.

### Returns
<code style="color : #03BEFC">Dictionary</code><<code style="color : #03BEFC">string</code>, <code style="color : #03BEFC">List</code><<code style="color : #03BEFC">string</code>>> \
Where Key is name of the enpoint / event and Value is a list of full names of methods subscribed to it.

## `Request`
<code style="color : #65B800">async</code>

Sends a request to the specified endpoint.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
| [`RequestMethod`][RequestMethod] | `method` | - | The HTTP method to use for the request (e.g., `GET`, `POST`, `PUT`). |
| <code style="color : #03BEFC">string</code> | `endpoint` | - | The endpoint URL for the request (e.g., `/lol-gameflow/v1/gameflow-phase`). |
| <code style="color : #03BEFC">dynamic?</code> | `data` <br/> (optional) | `null` | The optional data to include in the request body (e.g., for `POST` or `PUT`). Can be `null`. It gets serialized automatically. |
| <code style="color : #03BEFC">bool</code> | `ignoreReady` <br/> (optional) | `false` | If `true`, bypasses checks to ensure the LCU is ready before sending the request. |

### Returns
<code style="color : #03BEFC">Task</code><<code style="color : #03BEFC">HttpResponseMessage</code>> \
A task that, when awaited, resolves to an <code style="color : #03BEFC">HttpResponseMessage</code> representing the server's response.

### Exceptions
| Exception | Condition |
|:-:|:-|
| <code style="color : #FC5603">InvalidOperationException</code> | Thrown if `LCU.IsConnected` is `false`. Setting `ignoreReady` to `true` will prevent that. |
| <code style="color : #FC5603">HttpRequestException</code> | Thrown if the HTTP request fails. |

### Remarks
- Generally leave `ignoreReady` as `false` unless you specifically want to access an endpoint between time of launching the .exe and the client being ready.
- Use `await`.

### Example Usage
```csharp
// Getting chat info for the current user
LCU.ForceConnect();

try
{
    var response = await LCU.Request(RequestMethod.GET, "/lol-chat/v1/me");
    JObject content = JObject.Parse(await response.Content.ReadAsStringAsync());
    Console.WriteLine(content);
}
catch (Exception e) { Console.WriteLine(e.Message); }
```

# LCU Events
Events that are built-in to the LCU class.  Functions can be assigned or removed from them using the `+=` and `-=` operators.
All of them should have no parameters and return type <code style="color : #03BEFC">void</code>. \
Accessing [`GetMethodNamesForEvents()`][LCU.GetMethodNamesForEvents] will give you a list of all
methods invoked by these events.

### `OnConnected`
Fires when connection to the LCU is established.

### `OnDisconnected`
Fires when connection to the LCU is lost regardless of the reason.

### `OnGameflowPhaseChanged`
Fires when the gameflow phase changes. Its directly linked to automatically updating [`CurrentGameflowPhase`][LCU.CurrentGameflowPhase] property.

### `OnLocalSummonerInfoChanged`
Fires when any field of local summoner's info changes. Its directly linked to automatically updating [`LocalSummoner`][LCU.LocalSummoner] property.

# LOL
TODO

# Objects
Reference for all custom objects used in the library. (WIP)

## RequestMethod
An <code style="color : #65B800">enum</code> representing the HTTP methods that can be used in a request.

### Members
- **GET**
- **POST**
- **PATCH**
- **DELETE**
- **PUT**

## GameflowPhase
An <code style="color : #65B800">enum</code> representing all possible gameflow phases.

### Members
- **None** - default, when nothing is happening
- **Lobby**
- **Matchmaking** - in queue
- **ReadyCheck** - ready pop-up
- **ChampSelect**
- **GameStart** - between champ select ending and .exe starting
- **InProgress** - in game
- **TerminatedInError** - when game ends unexpectedly, this happens for example when you exit practice tool
- **WaitingForStats** - between game ending and stats screen
- **PreEndOfGame** - honor screen and first stage with LP gained
- **EndOfGame** - post game view with all players, items, K/D/A etc

## SubscriptionMessage
An <code style="color : #03BEFC">object</code> representing an event message received from a subscribed endpoint.

### Properties
- <code style="color : #03BEFC">string</code> **Endpoint** - the endpoint that the message is associated with
- <code style="color : #03BEFC">string</code> **Type** - different events have different types of messages, this is a string representing the type
- <code style="color : #03BEFC">JToken</code> **Data** - data received, it could be any type so it's needed to cast it to the correct type

## Summoner
An <code style="color : #03BEFC">object</code> representing a summoner in the LCU.

### Properties
- <code style="color : #03BEFC">long</code> **accountId**
- <code style="color : #03BEFC">string</code> **displayName** - name account had before naming scheme of NAME#TAGLINE was introduced, basically old Summoner Name
- <code style="color : #03BEFC">string</code> **gameName**
- <code style="color : #03BEFC">string</code> **internalName** - `displayName` with no whitespaces
- <code style="color : #03BEFC">bool</code> **nameChangeFlag** - if `true`, the summoner has changed their name and data is not up to date
- <code style="color : #03BEFC">int</code> **percentCompleteForNextLevel**
- <code style="color : #03BEFC">string</code> **privacy**
- [`RerollPoints`][RerollPoints] **rerollPoints** - ARAM reroll points
- <code style="color : #03BEFC">long</code> **summonerId**
- <code style="color : #03BEFC">int</code> **summonerLevel**
- <code style="color : #03BEFC">string</code> **tagLine**
- <code style="color : #03BEFC">bool</code> **unnamed**
- <code style="color : #03BEFC">long</code> **xpSinceLastLevel**
- <code style="color : #03BEFC">long</code> **xpUntilNextLevel**

## `RerollPoints`
An <code style="color : #03BEFC">object</code> representing ARAM reroll points.

### Properties
- <code style="color : #03BEFC">int</code> **currentPoints**
- <code style="color : #03BEFC">int</code> **maxRolls**
- <code style="color : #03BEFC">int</code> **numberOfRolls**
- <code style="color : #03BEFC">int</code> **pointsCostToRoll**
- <code style="color : #03BEFC">int</code> **pointsToReroll**

[//]: <> (Other repos links)
[PoniLCU]: https://github.com/Ponita0/PoniLCU
[Needlework.NET]: https://github.com/BlossomiShymae/Needlework.Net
[Kunc.RiotGames]: https://github.com/AoshiW/Kunc.RiotGames

[//]: <> (Properties Links)
[LCU.IsConnected]: #isconnected
[LCU.LocalSummoner]: #localsummoner
[LCU.CurrentGameflowPhase]: #currentgameflowphase
[LCU.PreserveSubscriptions]: #preservesubscriptions
[LCU.WriteAllEventsToConsole]: #writealleventstoconsole

[//]: <> (Methods Links)
[LCU.TryConnect]: #tryconnect
[LCU.ForceConnect]: #forceconnect
[LCU.Disconnect]: #disconnect
[LCU.Subscribe]: #subscribe
[LCU.Unsubscribe]: #unsubscribe
[LCU.GetMethodNamesForEvents]: #getmethodnamesforevents
[LCU.Request]: #request

[//]: <> (Events Links)
[LCU.OnConnected]: #onconnected
[LCU.OnDisconnected]: #ondisconnected
[LCU.OnGameflowPhaseChanged]: #ongameflowphasechanged
[LCU.OnLocalSummonerInfoChanged]: #onlocalsummonerinfochanged

[//]: <> (Objects Links)
[RequestMethod]: #requestmethod
[GameflowPhase]: #gameflowphase
[SubscriptionMessage]: #subscriptionmessage
[Summoner]: #summoner
[RerollPoints]: #rerollpoints


