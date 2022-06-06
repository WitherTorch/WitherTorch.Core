using System;

namespace WitherTorch.Core
{
    public class JavaRuntimeEnvironment : RuntimeEnvironment
    {
        public JavaRuntimeEnvironment(string path = null, string preArgs = null, string postArgs = null)
        {
            JavaPath = path;
            JavaPreArguments = preArgs;
            JavaPostArguments = postArgs;
        }
        /// <summary>
        /// 取得或設定執行時所使用的Java 虛擬機(java)位置
        /// </summary>
        public string JavaPath { get; set; }
        /// <summary>
        /// 取得或設定執行時所使用的Java 前置參數 (-jar server.jar 前的參數)
        /// </summary>
        public string JavaPreArguments { get; set; }
        /// <summary>
        /// 取得或設定執行時所使用的Java 後置參數 (-jar server.jar 後的參數)
        /// </summary>
        public string JavaPostArguments { get; set; }

        public JavaRuntimeEnvironment Clone()
        {
            return new JavaRuntimeEnvironment(JavaPath, JavaPreArguments, JavaPostArguments);
        }
    }

    /// <summary>
    /// 定義一個執行環境
    /// </summary>
    public abstract class RuntimeEnvironment
    {
        public static JavaRuntimeEnvironment JavaDefault { get; } = new JavaRuntimeEnvironment("java", string.Empty, string.Empty);
    }
}
