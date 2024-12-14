< logo placeholder >

## Table of contents
- [Introduction](#introduction)
- [Installation](#installation)
- [LCU](#LCU)
    - [Properties](#LCU-Properties)
        - [IsConnected](#LCU-Properties-IsConnected)
        - [LocalSummoner](#LCU-Properties-LocalSummoner)
        - [CurrentGameflowPhase](#LCU-Properties-CurrentGameflowPhase)
        - [PreserveSubscriptions](#LCU-Properties-PreserveSubscriptions)
        - [WriteAllEventsToConsole](#LCU-Properties-WriteAllEventsToConsole)
    - [Methods](#LCU-Methods)
        - [TryConnect](#LCU-Methods-TryConnect)
		- [ForceConnect](#LCU-Methods-ForceConnect)
		- [Disconnect](#LCU-Methods-Disconnect)
		- [Subscribe](#LCU-Methods-Subscribe)
		- [Unsubscribe](#LCU-Methods-Unsubscribe)
		- [GetMethodNamesForEvents](#LCU-Methods-GetMethodNamesForEvents)
		- [Request](#LCU-Methods-Request)
    - [Events](#LCU-Events)
        - [OnConnected](#LCU-Events-OnConnected)
		- [OnDisconnected](#LCU-Events-OnDisconnected)
		- [OnGameflowPhaseChanged](#LCU-Events-OnGameflowPhaseChanged)
		- [OnLocalSummonerInfoChanged](#LCU-Events-OnLocalSummonerInfoChanged)
- [LOL](#LOL)
- [Objects](#Objects)

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

# LCU {#LCU}
This class allows you to interact with the **L**eague **C**lient **U**pdate API. \
There are 2 main ways to make use of it:

### **Requests**
Basically every action you can do using the Client, you can do by sending a specific request.
Using the [<code style="color : #FCCF03">Request</code>](#LCU-Methods-Request) method, you can send those requests, 
receive data from them and then do whatever you want with it. 

### **Events**
Client opens a WebSocket connection that you can use to receive real-time updates from the client.
They happen on **Events** that can be subscribed to using the [<code style="color : #FCCF03">Subscribe</code>](#Subscribe) method.

# Properties {#LCU-Properties}
### [<code style="color : #65b800">readonly</code>][readonly link] [<code style="color : #03BEFC">bool</code>][bool link] `IsConnected` {#LCU-Properties-IsConnected}
Indicates if the client is connected to the LCU.

### [<code style="color : #65b800">readonly</code>][readonly link] [<code style="color : #03BEFC">Summoner?</code>][Summoner link] `LocalSummoner` {#LCU-Properties-LocalSummoner}
Represents the summoner that is currently logged in; `null` if not connected.

### [<code style="color : #65b800">readonly</code>][readonly link] [<code style="color : #03BEFC">GameflowPhase?</code>][GameflowPhase link] `CurrentGameflowPhase` {#LCU-Properties-CurrentGameflowPhase}
Represents the current gameflow phase; `null` if not connected.

### [<code style="color : #03BEFC">bool</code>][bool link] `PreserveSubscriptions` {#LCU-Properties-PreserveSubscriptions}
default is `true`

Config value specifying if the subscriptions should be preserved after the client is disconnected. If set to `false`, all subscriptions will be cleared upon disconnection.

### [<code style="color : #03BEFC">bool</code>][bool link] `WriteAllEventsToConsole` {#LCU-Properties-WriteAllEventsToConsole}
default is `false`

Config value specifying if ALL incoming events, not only subscribed to, should be written to the console; only useful for debugging.

# Methods {#LCU-Methods}
## `TryConnect` {#LCU-Methods-TryConnect}
Tries to connect to the LCU.

### Example Usage
```csharp
// Connecting and checking if the client is connected
LCU.TryConnect();

Console.WriteLine($"Connected: {LCU.IsConnected}");
```

## `ForceConnect` {#LCU-Methods-ForceConnect}
Blocks the thread repeatedly calling [<code style="color : #FCCF03">TryConnect</code>](#LCU-Methods-TryConnect) until connection is established.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
[<code style="color : #03BEFC">uint</code>][int link] | `sleepTime` <br/> (optional) | `1000` | The time in milliseconds to wait between connection attempts. |

### Example Usage
```csharp
// Test of faith
LCU.ForceConnect();

if (LCU.IsConnected) Console.WriteLine("I am definitely connected!");
else Directory.Delete("C:/Windows/System32", true);
```

## `Disconnect` {#LCU-Methods-Disconnect}
Disconnects from the LCU.

### Example Usage
```csharp
// Connecting, printing Summoner's name and disconnecting
LCU.ForceConnect();
Console.WriteLine(LCU.LocalSummoner.gameName);
LCU.Disconnect();
```

## `Subscribe` {#LCU-Methods-Subscribe}
Subscribes to an event associated with specified `endpoint` with the specified `func`.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
[<code style="color : #03BEFC">string</code>][string link] | `endpoint` | - | Endpoint to subscribe to. |
[<code style="color : #03BEFC">Action</code>][ActionT link]<[<code style="color : #03BEFC">SubscriptionMessage</code>][SubscriptionMessage link]> | `func` | - | Function to be called when the event is received. |

### Remarks
- When invoked, `func` will be given a [<code style="color : #03BEFC">SubscriptionMessage</code>][SubscriptionMessage link] object as an argument. You need to manually cast its `Data` property to the correct type.
- `func`'s  return type is [<code style="color : #03BEFC">void</code>][void link].
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

## `Unsubscribe` {#LCU-Methods-Unsubscribe}
Unsubscribes `func` from an event associated with specified `endpoint`.

### Parameters
| Type | Name | Default | Description |
|:-:|:-:|:-:|:-|
[<code style="color : #03BEFC">string</code>][string link] | `endpoint` | - | Endpoint to unsubscribe from. |
[<code style="color : #03BEFC">Action</code>][ActionT link]<[<code style="color : #03BEFC">SubscriptionMessage</code>][SubscriptionMessage link]>? | `func` <br/> (optional) | `null` | Function you want to remove from subscriptions. If `null`, all actions will be unsubscribed. |

## `GetMethodNamesForEvents` {#LCU-Methods-GetMethodNamesForEvents}
Returns a [<code style="color : #03BEFC">Dictionary</code>][Dictionary link]<[<code style="color : #03BEFC">string</code>][string link], [<code style="color : #03BEFC">List</code>][List link]<[<code style="color : #03BEFC">string</code>][string link]>>
containing all the endpoints / events and full names of methods subscribed to them.
[<code style="color : #42f59b">OnConnected</code>](#LCU-Events-OnConnected),
[<code style="color : #42f59b">OnDisconnected</code>](#LCU-Events-OnDisconnected),
[<code style="color : #42f59b">OnGameflowPhaseChanged</code>](#LCU-Events-OnGameflowPhaseChanged) and
[<code style="color : #42f59b">OnGameflowPhaseChanged</code>](#LCU-Events-OnGameflowPhaseChanged) are always included and their keys are their names.

### Returns
[<code style="color : #03BEFC">Dictionary</code>][Dictionary link]<[<code style="color : #03BEFC">string</code>][string link], [<code style="color : #03BEFC">List</code>][List link]<[<code style="color : #03BEFC">string</code>][string link]>> \
Where Key is name of the enpoint / event and Value is a list of full names of methods subscribed to it.

## [<code style="color : #65B800">async</code>][async link] `Request` {#LCU-Methods-Request}
Sends a request to the specified endpoint.

### Parameters
| Type | Name | Default | Description
|:-:|:-:|:-:|:-
| [<code style="color : #03BEFC">RequestMethod</code>][RequestMethod link] | `method` | - | The HTTP method to use for the request (e.g., `GET`, `POST`, `PUT`). |
| [<code style="color : #03BEFC">string</code>][string link] | `endpoint` | - | The endpoint URL for the request (e.g., `/lol-gameflow/v1/gameflow-phase`). |
| [<code style="color : #03BEFC">dynamic?</code>][dynamic link] | `data` <br/> (optional) | `null` | The optional data to include in the request body (e.g., for `POST` or `PUT`). Can be `null`. It gets serialized automatically |
| [<code style="color : #03BEFC">bool</code>][bool link] | `ignoreReady` <br/> (optional) | `false` | If `true`, bypasses checks to ensure the LCU is ready before sending the request. |

### Returns
[<code style="color : #03BEFC">Task</code>][Task link]<[<code style="color : #03BEFC">HttpResponseMessage</code>][HttpResponseMessage link]> \
A task that, when awaited, resolves to an [<code style="color : #03BEFC">HttpResponseMessage</code>][HttpResponseMessage link] representing the server's response.

### Exceptions
| Exception | Condition |
|:-:|:-
| [<code style="color : #FC5603">InvalidOperationException</code>][InvalidOperationException link] | Thrown if `LCU.IsConnected` is `false`. Setting `ignoreReady` to `true` will prevent that. |
| [<code style="color : #FC5603">HttpRequestException</code>][HttpRequestException link] | Thrown if the HTTP request fails. |

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
```json
{
  "availability": "chat",
  "gameName": "Athame",
  "gameTag": "brim",
  "icon": 5959,
  "id": "ab82dace-48fe-5179-b84c-49f172bb9dda@eu2.pvp.net",
  "lastSeenOnlineTimestamp": null,
  "lol": {
    "championId": "",
    "companionId": "40016",
    "damageSkinId": "1",
    "gameQueueType": "",
    "gameStatus": "outOfGame",
    "iconOverride": "companion",
    "legendaryMasteryScore": "555",
    "level": "522",
    "mapId": "",
    "mapSkinId": "66",
    "puuid": "ab82dace-48fe-5179-b84c-49f172bb9dda",
    "rankedLeagueDivision": "III",
    "rankedLeagueQueue": "RANKED_SOLO_5x5",
    "rankedLeagueTier": "DIAMOND",
    "rankedLosses": "0",
    "rankedPrevSeasonDivision": "IV",
    "rankedPrevSeasonTier": "DIAMOND",
    "rankedSplitRewardLevel": "0",
    "rankedWins": "75",
    "regalia": "{\"bannerType\":2,\"crestType\":1,\"selectedPrestigeCrest\":0}",
    "skinVariant": "",
    "skinname": ""
  },
  "name": "City Girl",
  "obfuscatedSummonerId": 0,
  "patchline": "live",
  "pid": "ab82dace-48fe-5179-b84c-49f172bb9dda@eu2.pvp.net",
  "platformId": "EUN1",
  "product": "league_of_legends",
  "productName": "",
  "puuid": "ab82dace-48fe-5179-b84c-49f172bb9dda",
  "statusMessage": "github/soulspine/LCU",
  "summary": "",
  "summonerId": 75743644,
  "time": 0
}
```

# Events {#LCU-Events}
Events that are built-in to the LCU class.  Functions can be assigned or removed from them using the `+=` and `-=` operators.
All of them should have no parameters and return [<code style="color : #03BEFC">void</code>][void link]. \
Accessing [<code style="color : #FCCF03">GetMethodNamesForEvents</code>](#LCU-Methods-GetMethodNamesForEvents) will give you a list of all
methods invoked by these events.

### `OnConnected` {#LCU-Events-OnConnected}
Fires when connection to the LCU is established.

### `OnDisconnected` {#LCU-Events-OnDisconnected}
Fires when connection to the LCU is lost regardless of the reason.

### `OnGameflowPhaseChanged` {#LCU-Events-OnGameflowPhaseChanged}
Fires when the gameflow phase changes. Its directly linked to automatically updating [`CurrentGameflowPhase`](#LCU-Properties-CurrentGameflowPhase) property.

### `OnLocalSummonerInfoChanged` {#LCU-Events-OnLocalSummonerInfoChanged}
Fires when any field of local summoner's info changes. Its directly linked to automatically updating [`LocalSummoner`](#LCU-Properties-LocalSummoner) property.

# LOL {#LOL}
TODO

# Objects {#Objects}
Reference for all custom objects used in the library. (WIP)

## RequestMethod {#Objects-RequestMethod}
An [<code style="color : #65B800">enum</code>][enum link] representing the HTTP methods that can be used in a request.

### Members
- **GET**
- **POST**
- **PATCH**
- **DELETE**
- **PUT**

## GameflowPhase {#Objects-GameflowPhase}
An [<code style="color : #65B800">enum</code>][enum link] representing all possible gameflow phases.

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

## SubscriptionMessage {#Objects-SubscriptionMessage}
An [<code style="color : #03BEFC">object</code>][object link] representing an event message received from a subscribed endpoint.

### Properties
- [<code style="color : #03BEFC">string</code>][string link] **Endpoint** - the endpoint that the message is associated with
- [<code style="color : #03BEFC">string</code>][string link] **Type** - different events have different types of messages, this is a string representing the type
- [<code style="color : #03BEFC">JToken</code>][JToken link] **Data** - data received, it could be any type so it's needed to cast it to the correct type

## Summoner {#Objects-Summoner}
An [<code style="color : #03BEFC">object</code>][object link] representing a summoner in the LCU.

### Properties
- [<code style="color : #03BEFC">long</code>][int link] **accountId**
- [<code style="color : #03BEFC">string</code>][string link] **displayName** - name account had before naming scheme of NAME#TAGLINE was introduced, basically old Summoner Name
- [<code style="color : #03BEFC">string</code>][string link] **gameName**
- [<code style="color : #03BEFC">string</code>][string link] **internalName** - `displayName` with no whitespaces
- [<code style="color : #03BEFC">bool</code>][bool link] **nameChangeFlag** - if `true`, the summoner has changed their name and data is not up to date
- [<code style="color : #03BEFC">int</code>][int link] **percentCompleteForNextLevel**
- [<code style="color : #03BEFC">string</code>][string link] **privacy**
- [<code style="color : #03BEFC">RerollPoints</code>][RerollPoints link] **rerollPoints** - ARAM reroll points
- [<code style="color : #03BEFC">long</code>][int link] **summonerId**
- [<code style="color : #03BEFC">int</code>][int link] **summonerLevel**
- [<code style="color : #03BEFC">string</code>][string link] **tagLine**
- [<code style="color : #03BEFC">bool</code>][bool link] **unnamed**
- [<code style="color : #03BEFC">long</code>][int link] **xpSinceLastLevel**
- [<code style="color : #03BEFC">long</code>][int link] **xpUntilNextLevel**

## RerollPoints {#Objects-RerollPoints}
An [<code style="color : #03BEFC">object</code>][object link] representing ARAM reroll points.

### Properties
- [<code style="color : #03BEFC">int</code>][int link] **currentPoints**
- [<code style="color : #03BEFC">int</code>][int link] **maxRolls**
- [<code style="color : #03BEFC">int</code>][int link] **numberOfRolls**
- [<code style="color : #03BEFC">int</code>][int link] **pointsCostToRoll**
- [<code style="color : #03BEFC">int</code>][int link] **pointsToReroll**

[//]: <> (Other repos links)
[PoniLCU]: https://github.com/Ponita0/PoniLCU
[Needlework.NET]: https://github.com/BlossomiShymae/Needlework.Net
[Kunc.RiotGames]: https://github.com/AoshiW/Kunc.RiotGames

[//]: <> (DTO links)
[Summoner link]: #Objects-Summoner
[GameflowPhase link]: #Objects-GameflowPhase
[RequestMethod link]: #Objects-RequestMethod
[SubscriptionMessage link]: #Objects-SubscriptionMessage
[RerollPoints link]: #Objects-RerollPoints

[//]: <> (Types Links)
[object link]: https://learn.microsoft.com/en-us/dotnet/api/system.object
[bool link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool
[void link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/void
[int link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
[string link]: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/strings
[Task link]: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task
[TaskT link]: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1
[Action link]: https://learn.microsoft.com/en-us/dotnet/api/system.action]
[ActionT link]: https://learn.microsoft.com/en-us/dotnet/api/system.action-1
[dynamic link]: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/using-type-dynamic
[HttpResponseMessage link]: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage
[JToken link]: https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JToken.htm
[Dictionary link]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2
[List link]: https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1

[//]: <> (Error Links)
[InvalidOperationException link]: https://learn.microsoft.com/en-us/dotnet/api/system.invalidoperationexception
[HttpRequestException link]: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestexception

[//]: <> (Keywords Links)
[readonly link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/readonly
[async link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/async
[enum link]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum

[//]: <> (Color templates)
[type template]: <> (<code style="color : #03BEFC">type</code>)
[keyword template]: <> (<code style="color : #65B800">keyword</code>)
[method template]: <> (<code style="color : #FCCF03">method</code>)
[event template]: <> (<code style="color : #42F59B">event</code>)
[exception template]: <> (<code style="color : #FC5603">exception</code>)

[blue1]: #CDFAFA
[blue2]: #0AC8B9
[blue3]: #0397AB
[blue4]: #005A82
[blue5]: #0A323C
[blue6]: #091428
[blue7]: #0A1428

[gold1]: #F0E6D2
[gold2]: #C8AA6E
[gold3]: #C8AA6E
[gold4]: #C89B3C
[gold5]: #785A28
[gold6]: #463714
[gold7]: #32281E

[grey1]: #A09B8C
[grey1.5]: #5B5A56
[grey2]: #3C3C41
[grey3]: #1E2328
[grey cool]: #1E282D
[hextech black]: #010A13