namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 表示一個執行環境
    /// </summary>
    public interface IRuntimeEnvironment { }

    /// <summary>
    /// 執行環境的靜態類別
    /// </summary>
    public static class RuntimeEnvironment
    {
        /// <summary>
        /// 空的執行環境物件，應當只用於那些不需要特殊執行環境的伺服器
        /// </summary>
        public static readonly IRuntimeEnvironment Empty = new EmptyImpl();

        private sealed class EmptyImpl : IRuntimeEnvironment { }
    }
}
