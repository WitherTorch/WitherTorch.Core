namespace WitherTorch.Core.Utils
{
    partial class HashHelper
    {
        /// <summary>
        /// 雜湊演算法
        /// </summary>
        public enum HashMethod
        {
            /// <summary>
            /// 無
            /// </summary>
            None = 0,
            /// <summary>
            /// MD5 演算法
            /// </summary>
            MD5,
            /// <summary>
            /// SHA-1 演算法
            /// </summary>
            SHA1,
            /// <summary>
            /// SHA-256 演算法
            /// </summary>
            SHA256,
            /// <summary>
            /// (僅在內部使用的標記，請勿呼叫)
            /// </summary>
            _Last
        }
    }
}
