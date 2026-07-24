using System;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine.Assertions;

namespace Aurora.Steam.Threading.Tasks
{
    /// <summary>
    /// 提供一组返回值为 <see cref="Task{TResult}"/> 的方法。
    /// </summary>
    public static class SteamTasks
    {
        /// <summary>
        /// 获取一个在指定的 Steam 事件触发时完成的任务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <typeparam name="TResult">你想要监听的 Steam 事件回调类型（见 <c>SteamCallbacks.cs</c> 文件）。</typeparam>
        /// <returns>在 <typeparamref name="TResult"/> 事件触发时完成的任务。</returns>
        public static Task<TResult> WhenCallback<TResult>(CancellationToken cancellationToken = default)
            where TResult : struct
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TResult>(cancellationToken);
            }
            var promise = cancellationToken.CanBeCanceled switch
            {
                false => new CallbackPromise<TResult>(),
                true  => new CallbackPromiseWithCancellation<TResult>(cancellationToken)
            };
            return promise.Task;
        }

        private class CallbackPromise<TResult> : TaskCompletionSource<TResult>
        {
            private readonly Callback<TResult> _callback;

            internal CallbackPromise()
            {
                _callback = Callback<TResult>.Create(Triggered);
            }

            private void Triggered(TResult result)
            {
                if (TrySetResult(result))
                {
                    CleanUp();
                }
            }

            protected virtual void CleanUp()
            {
                _callback.Dispose();
            }
        }

        private sealed class CallbackPromiseWithCancellation<TResult> : CallbackPromise<TResult>
        {
            private static readonly Action<object> ActionCompleteCanceled = CompleteCanceled;

            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            internal CallbackPromiseWithCancellation(CancellationToken cancellationToken)
            {
                _cancellationTokenRegistration = cancellationToken.Register(
                    ActionCompleteCanceled,
                    Tuple.Create(this, cancellationToken)
                );
                if (Task.IsCompleted)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            private static void CompleteCanceled(object state)
            {
                var (callbackPromiseWithCancellation, cancellationToken) =
                    (Tuple<CallbackPromiseWithCancellation<TResult>, CancellationToken>) state;
                if (callbackPromiseWithCancellation.TrySetCanceled(cancellationToken))
                {
                    callbackPromiseWithCancellation.CleanUp();
                }
            }

            protected override void CleanUp()
            {
                _cancellationTokenRegistration.Dispose();
                base.CleanUp();
            }
        }

        /// <summary>
        /// 获取一个在指定的 Steam API 调用结果返回时完成的任务。
        /// </summary>
        /// <param name="call">各种 <c>SteamXxx.RequestXxx</c> 方法返回的 Steam API 调用句柄。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <typeparam name="TResult">对应 Steam API 方法的回调结果类型（在 <c>SteamXxx.RequestXxx</c> 方法的文档注释里）。</typeparam>
        /// <returns>在指定的 Steam API 调用结果返回时完成的任务。</returns>
        public static Task<SteamApiCallResult<TResult>> WhenCallResult<TResult>(
            SteamAPICall_t    call,
            CancellationToken cancellationToken = default)
        {
            Assert.AreNotEqual(SteamAPICall_t.Invalid, call);
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<SteamApiCallResult<TResult>>(cancellationToken);
            }
            var promise = cancellationToken.CanBeCanceled switch
            {
                false => new CallResultPromise<TResult>(call),
                true  => new CallResultPromiseWithCancellation<TResult>(call, cancellationToken)
            };
            return promise.Task;
        }

        private class CallResultPromise<TResult> : TaskCompletionSource<SteamApiCallResult<TResult>>
        {
            private readonly CallResult<TResult> _callResult;

            internal CallResultPromise(SteamAPICall_t steamApiCall)
            {
                (_callResult = CallResult<TResult>.Create(Complete)).Set(steamApiCall);
            }

            private void Complete(TResult result, bool ioFailure)
            {
                if (TrySetResult(new SteamApiCallResult<TResult>(result, ioFailure)))
                {
                    CleanUp();
                }
            }

            protected virtual void CleanUp()
            {
                _callResult.Dispose();
            }
        }

        private sealed class CallResultPromiseWithCancellation<TResult> : CallResultPromise<TResult>
        {
            private static readonly Action<object> ActionCompleteCanceled = CompleteCanceled;

            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            internal CallResultPromiseWithCancellation(
                SteamAPICall_t    steamApiCall,
                CancellationToken cancellationToken) : base(steamApiCall)
            {
                _cancellationTokenRegistration = cancellationToken.Register(
                    ActionCompleteCanceled,
                    Tuple.Create(this, cancellationToken)
                );
                if (Task.IsCompleted)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            private static void CompleteCanceled(object state)
            {
                var (callResultPromiseWithCancellation, cancellationToken) =
                    (Tuple<CallResultPromiseWithCancellation<TResult>, CancellationToken>) state;
                if (callResultPromiseWithCancellation.TrySetCanceled(cancellationToken))
                {
                    callResultPromiseWithCancellation.CleanUp();
                }
            }

            protected override void CleanUp()
            {
                _cancellationTokenRegistration.Dispose();
                base.CleanUp();
            }
        }
    }
}
