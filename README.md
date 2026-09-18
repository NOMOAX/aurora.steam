# Aurora Steam

![license](https://img.shields.io/github/license/NOMOAX/aurora.steam)
![version](https://img.shields.io/badge/version-1.0.2-blue)
![lowest Unity version](https://img.shields.io/badge/Unity-2019.4%2B-blue)

Task-based asynchronous pattern (TAP) wrapper for Steamworks.NET's async callback model.

English | [中文](README.zh.md)

## Dependencies

- [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net)

## Installation

1. Open Unity package manager.
2. Click the `+` button in the upper-left corner, then select `Add package from git URL...`.
3. Input `https://github.com/NOMOAX/aurora.steam.git` and then click the `Add` button.

## Steam Callbacks

Steamworks.NET reports an event through a `Callback<TResult>` object: the event is only received while that object is registered and kept alive, and it is delivered by `SteamAPI.RunCallbacks()`. `SteamTasks.WhenCallback<TResult>` wraps that lifecycle and returns a `Task<TResult>` that completes when the event fires.

```csharp
using Steamworks;
using Aurora.Steam.Threading.Tasks;

// Waits until the user tries to join the game from the friends list
var joinRequest = await SteamTasks.WhenCallback<GameRichPresenceJoinRequested_t>();
Debug.Log($"Join requested with the connect string: {joinRequest.m_rgchConnect}");
```

`<TResult>` is the `SteamXxx_t` event struct to listen for (see the `SteamCallbacks.cs` file of Steamworks.NET). The `Callback<TResult>` is registered right away, when `WhenCallback` is called:

- an event that fired before that moment is not received, there is no buffering;
- the `Callback<TResult>` is unregistered as soon as the task completes or is canceled.

## Steam API Call Results

Methods such as `SteamUserStats.RequestUserStats` return a `SteamAPICall_t` handle instead of a result; Steamworks.NET delivers the result through a `CallResult<TResult>` object created for that handle. `SteamTasks.WhenCallResult<TResult>` wraps that lifecycle and returns a `Task<SteamApiCallResult<TResult>>`.

```csharp
using Steamworks;
using Aurora.Steam.Threading.Tasks;

var call = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
var callResult = await SteamTasks.WhenCallResult<UserStatsReceived_t>(call);

if (callResult.IOFailure)
{
    // The request never reached the Steam servers; retry the call
}
else if (callResult.Result.m_eResult != EResult.k_EResultOK)
{
    // The request reached Steam, and Steam reported a failure
}
```

`<TResult>` is the callback type that the documentation comment of the corresponding `SteamXxx.RequestXxx` method names as the received result — in the example above, `UserStatsReceived_t`.

### SteamApiCallResult\<TResult\>

`SteamApiCallResult<TResult>` is a `readonly struct` that carries both halves of what the `CallResult<TResult>.APIDispatchDelegate` reports: the `param` and `bIOFailure` parameters become the `Result` and `IOFailure` fields.

| Field       | Meaning                                                                          |
|-------------|----------------------------------------------------------------------------------|
| `Result`    | The result of the Steam API call; invalid when `IOFailure` is `true`             |
| `IOFailure` | Whether the request failed to reach the Steam server (a transport-layer failure) |

`IOFailure` is `true` when the call never got to the Steam servers, so retrying it is meaningful; the detailed reason can be queried through `SteamUtils.GetAPICallFailureReason` (mainly for debugging).

## Cancellation

Both methods take an optional `CancellationToken`. When it is canceled before the event fires or the result arrives, the task turns canceled (it throws `OperationCanceledException` when awaited), and the underlying `Callback<TResult>` / `CallResult<TResult>` is unregistered, so nothing keeps waiting for it. A token that is already canceled at the moment of the call returns an already canceled task.

```csharp
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    var callResult = await SteamTasks.WhenCallResult<UserStatsReceived_t>(call, cancellationTokenSource.Token);
    // ...
}
catch (OperationCanceledException)
{
    // Timed out; the CallResult has been unregistered
}
```

## Steam Initialization and Callback Dispatch

This package only turns Steam callbacks and call results into tasks. Initializing Steam and dispatching its callbacks stay the caller's responsibility:

- `SteamAPI.Init()` (or `SteamAPI.RestartAppIfNecessary`) must have succeeded before any of these calls, otherwise the waiting tasks are never reached;
- `SteamAPI.RunCallbacks()` must be called every frame, because only its dispatch loop delivers the callbacks and the call results that complete the waiting tasks.

The waiting tasks are backed by `TaskCompletionSource<TResult>` with its default behavior: a continuation that captured a synchronization context is posted back to that context, while any other continuation runs synchronously on the thread that called `RunCallbacks()`. Do not perform long-running work on that thread — in a Unity game, dispatch callbacks on the main thread only.

Only the client-side registrations (`Callback<T>.Create` and `CallResult<T>.Create`) are used, which `SteamAPI.RunCallbacks()` dispatches; game server callbacks, dispatched by `GameServer.RunCallbacks()`, are not covered.
