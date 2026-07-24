using Steamworks;

namespace Aurora.Steam
{
    /// <summary>
    /// 封装从 <see cref="CallResult{T}.APIDispatchDelegate"/> 接收到的回调结果。
    /// </summary>
    /// <typeparam name="TResult">调用 <c>SteamXxx.RequestXxx</c> 方法时，在该方法的文档注释中出现的回调类型。</typeparam>
    public readonly struct SteamApiCallResult<TResult>
    {
        /// <summary>
        /// Steam 回调结果。
        /// </summary>
        /// <remarks>当 <see cref="IOFailure"/> 为 <see langword="true"/> 时无效。</remarks>
        public readonly TResult Result;

        /// <summary>
        /// 请求是否因传输层故障而未抵达 Steam 服务器。
        /// </summary>
        /// <remarks>
        /// 为 <see langword="true"/> 时，应重试调用 Steam API。
        /// <br/>
        /// 可通过 <see cref="SteamUtils.GetAPICallFailureReason"/> 查询详细失败原因（但主要用于调试）。
        /// </remarks>
        public readonly bool IOFailure;

        /// <summary>
        /// 初始化 <see cref="SteamApiCallResult{TResult}"/> 结构的新实例。
        /// </summary>
        /// <param name="result">Steam 回调结果。</param>
        /// <param name="ioFailure">请求是否因传输层故障而未抵达 Steam 服务器。</param>
        public SteamApiCallResult(TResult result, bool ioFailure)
        {
            Result    = result;
            IOFailure = ioFailure;
        }
    }
}
