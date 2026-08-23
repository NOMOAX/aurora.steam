using System;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine.Assertions;

namespace Aurora.Steam.Threading.Tasks
{
    /// <summary>
    /// Provides a set of methods that return <see cref="Task{TResult}"/>.
    /// </summary>
    public static class SteamTasks
    {
        /// <summary>
        /// Gets a task that completes when the specified Steam event fires.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="TResult">The Steam event callback type you want to listen for (see the <c>SteamCallbacks.cs</c> file).</typeparam>
        /// <returns>The task that completes when the <typeparamref name="TResult"/> event fires.</returns>
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
                    (Tuple<CallbackPromiseWithCancellation<TResult>, CancellationToken>)state;
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
        /// Gets a task that completes when the specified Steam API call result returns.
        /// </summary>
        /// <param name="call">The Steam API call handle returned by various <c>SteamXxx.RequestXxx</c> methods.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="TResult">The callback result type of the corresponding Steam API method (in the documentation comment of the <c>SteamXxx.RequestXxx</c> method).</typeparam>
        /// <returns>The task that completes when the specified Steam API call result returns.</returns>
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
                    (Tuple<CallResultPromiseWithCancellation<TResult>, CancellationToken>)state;
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
