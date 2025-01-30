namespace WitherTorch.Core
{
    /// <summary>
    /// 定義一個 Java 執行環境
    /// </summary>
    public class JavaRuntimeEnvironment : RuntimeEnvironment
    {
        /// <summary>
        /// <see cref="JavaRuntimeEnvironment"/> 的預設建構子
        /// </summary>
        /// <param name="path">執行時所使用的 Java 虛擬機 (java) 位置</param>
        /// <param name="preArgs">執行時所使用的 Java 前置參數 (-jar server.jar 前的參數)</param>
        /// <param name="postArgs">執行時所使用的 Java 後置參數 (-jar server.jar 後的參數)</param>
        public JavaRuntimeEnvironment(string? path = null, string? preArgs = null, string? postArgs = null)
        {
            JavaPath = path;
            JavaPreArguments = preArgs;
            JavaPostArguments = postArgs;
        }
        /// <summary>
        /// 取得或設定執行時所使用的 Java 虛擬機 (java) 位置
        /// </summary>
        public string? JavaPath { get; set; }
        /// <summary>
        /// 取得或設定執行時所使用的 Java 前置參數 (-jar server.jar 前的參數)
        /// </summary>
        public string? JavaPreArguments { get; set; }
        /// <summary>
        /// 取得或設定執行時所使用的 Java 後置參數 (-jar server.jar 後的參數)
        /// </summary>
        public string? JavaPostArguments { get; set; }

        /// <summary>
        /// 傳回一個與目前物件相同的另一個 <see cref="JavaRuntimeEnvironment"/> 副本
        /// </summary>
        public JavaRuntimeEnvironment Clone()
        {
            return new JavaRuntimeEnvironment(JavaPath, JavaPreArguments, JavaPostArguments);
        }
    }

    /// <summary>
    /// 定義一個執行環境，此類別為抽象類別
    /// </summary>
    public abstract class RuntimeEnvironment
    {
        /// <summary>
        /// 取得預設的 Java 執行環境
        /// </summary>
        public static JavaRuntimeEnvironment JavaDefault { get; } = new JavaRuntimeEnvironment("java", string.Empty, string.Empty);
    }
}
