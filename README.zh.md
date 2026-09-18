# Aurora Steam

![许可](https://img.shields.io/github/license/NOMOAX/aurora.steam)
![版本](https://img.shields.io/badge/version-1.0.2-blue)
![最低 Unity 版本](https://img.shields.io/badge/Unity-2019.4%2B-blue)

Steamworks.NET 异步回调模型的基于任务的异步模式（TAP）封装。

[English](README.md) | 中文

## 依赖

- [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net)

## 安装

1. 打开 Unity package manager。
2. 点击左上角的 `+` 按钮，然后选择 `Add package from git URL...`。
3. 填入 `https://github.com/NOMOAX/aurora.steam.git` 并点击 `Add` 按钮。

## Steam 事件回调

Steamworks.NET 通过 `Callback<TResult>` 对象上报事件：只有当该对象已注册并且未被回收时才能收到事件，而且事件由 `SteamAPI.RunCallbacks()` 派发。`SteamTasks.WhenCallback<TResult>` 封装了这套生命周期，返回一个在该事件触发时完成的 `Task<TResult>`。

```csharp
using Steamworks;
using Aurora.Steam.Threading.Tasks;

// 等待用户从好友列表中尝试加入游戏
var joinRequest = await SteamTasks.WhenCallback<GameRichPresenceJoinRequested_t>();
Debug.Log($"收到加入请求，连接串是 {joinRequest.m_rgchConnect}");
```

`<TResult>` 是想要监听的 `SteamXxx_t` 事件结构体（见 Steamworks.NET 的 `SteamCallbacks.cs` 文件）。`Callback<TResult>` 在调用 `WhenCallback` 的当下就会注册，因此：

- 在这一刻之前已经触发过的事件不会收到，本包不缓存事件；
- 任务完成或被取消后，`Callback<TResult>` 会立即注销。

## Steam API 调用结果

`SteamUserStats.RequestUserStats` 这类方法返回的是 `SteamAPICall_t` 句柄而不是结果，Steamworks.NET 通过为这个句柄创建的 `CallResult<TResult>` 对象上报结果。`SteamTasks.WhenCallResult<TResult>` 封装了这套生命周期，返回 `Task<SteamApiCallResult<TResult>>`。

```csharp
using Steamworks;
using Aurora.Steam.Threading.Tasks;

var call = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
var callResult = await SteamTasks.WhenCallResult<UserStatsReceived_t>(call);

if (callResult.IOFailure)
{
    // 请求根本没有到达 Steam 服务器，应该重试这次调用
}
else if (callResult.Result.m_eResult != EResult.k_EResultOK)
{
    // 请求到达了 Steam，但 Steam 返回了失败
}
```

`<TResult>` 就是对应的 `SteamXxx.RequestXxx` 方法文档注释里写明的结果回调类型，例如上面的 `UserStatsReceived_t`。

### SteamApiCallResult\<TResult\>

`SteamApiCallResult<TResult>` 是一个 `readonly struct`，承载 `CallResult<TResult>.APIDispatchDelegate` 上报的两部分信息：它的 `param` 与 `bIOFailure` 参数分别对应 `Result` 与 `IOFailure` 字段。

| 字段        | 含义                                               |
|-------------|----------------------------------------------------|
| `Result`    | Steam API 调用的结果；`IOFailure` 为 `true` 时无效 |
| `IOFailure` | 请求是否因为传输层故障没有到达 Steam 服务器        |

`IOFailure` 为 `true` 表示这次调用根本没有到达 Steam 服务器，因此重试是有意义的；详细的失败原因可以通过 `SteamUtils.GetAPICallFailureReason` 查询（主要用于调试）。

## 取消

两个方法都提供了接受 `CancellationToken` 的重载。在事件触发或结果返回之前取消时，任务转为已取消状态（`await` 时抛出 `OperationCanceledException`），同时底层的 `Callback<TResult>` / `CallResult<TResult>` 会被注销，不会再有东西在等待它。调用时就已经取消的令牌会直接返回一个已经取消的任务。

```csharp
using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try
{
    var callResult = await SteamTasks.WhenCallResult<UserStatsReceived_t>(call, cancellationTokenSource.Token);
    // ...
}
catch (OperationCanceledException)
{
    // 超时了，CallResult 已经被注销
}
```

## Steam 初始化与回调派发

本包只负责把 Steam 的事件回调与调用结果变成任务，Steam 的初始化与回调派发仍然由使用方负责：

- 在调用这里的任何方法之前，`SteamAPI.Init()`（或 `SteamAPI.RestartAppIfNecessary`）必须已经成功，否则等待的任务永远不会被执行到；
- 每帧都必须调用 `SteamAPI.RunCallbacks()`，因为只有它的派发循环才会派发事件回调与调用结果，从而让等待中的任务完成。

这些等待任务的底层是默认行为的 `TaskCompletionSource<TResult>`：捕获了同步上下文的延续会回到该上下文执行，其余延续则在调用 `RunCallbacks()` 的线程上同步执行。不要在那个线程上执行耗时操作——在 Unity 游戏里，请只在主线程派发回调。

本包使用的是客户端注册形式（`Callback<T>.Create` 与 `CallResult<T>.Create`），由 `SteamAPI.RunCallbacks()` 派发；由 `GameServer.RunCallbacks()` 派发的游戏服务器回调不在本包的覆盖范围内。
