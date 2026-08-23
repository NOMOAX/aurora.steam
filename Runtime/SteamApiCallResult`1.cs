using Steamworks;

namespace Aurora.Steam
{
    /// <summary>
    /// Wraps the callback result received from <see cref="CallResult{T}.APIDispatchDelegate"/>.
    /// </summary>
    /// <typeparam name="TResult">The callback type that appears in the documentation comment of the <c>SteamXxx.RequestXxx</c> method when it is called.</typeparam>
    public readonly struct SteamApiCallResult<TResult>
    {
        /// <summary>
        /// The Steam callback result.
        /// </summary>
        /// <remarks>Invalid when <see cref="IOFailure"/> is <see langword="true"/>.</remarks>
        public readonly TResult Result;

        /// <summary>
        /// Whether the request failed to reach the Steam server due to a transport-layer failure.
        /// </summary>
        /// <remarks>
        /// When <see langword="true"/>, retry the Steam API call.
        /// <br/>
        /// Detailed failure reasons can be queried via <see cref="SteamUtils.GetAPICallFailureReason"/> (mainly for debugging).
        /// </remarks>
        public readonly bool IOFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteamApiCallResult{TResult}"/> structure.
        /// </summary>
        /// <param name="result">The Steam callback result.</param>
        /// <param name="ioFailure">Whether the request failed to reach the Steam server due to a transport-layer failure.</param>
        public SteamApiCallResult(TResult result, bool ioFailure)
        {
            Result    = result;
            IOFailure = ioFailure;
        }
    }
}
