using System;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 呼叫 <see cref="ILocalProcess.Start(in LocalProcessStartInfo)"/> 時所需的資訊
    /// </summary>
    public readonly struct LocalProcessStartInfo
    {
        private readonly string _fileName;
        private readonly string _arguments;
        private readonly string _workingDirectory;

        /// <summary>
        /// 取得要啟動的檔案名稱
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// 取得要附加的命令參數
        /// </summary>
        public string Arguments => _arguments;

        /// <summary>
        /// 取得啟動處理序時的工作目錄
        /// </summary>
        public string WorkingDirectory => _workingDirectory;

        /// <inheritdoc cref="LocalProcessStartInfo(string, string, string)"/>
        public LocalProcessStartInfo(string fileName)
            : this(fileName, string.Empty) { }

        /// <inheritdoc cref="LocalProcessStartInfo(string, string, string)"/>
        public LocalProcessStartInfo(string fileName, string arguments)
            : this(fileName, arguments, string.Empty) { }

        /// <summary>
        /// 初始化 <see cref="LocalProcessStartInfo"/> 類別的新執行個體
        /// </summary>
        /// <param name="fileName">要啟動的檔案名稱</param>
        /// <param name="arguments">要附加的命令參數</param>
        /// <param name="workingDirectory">啟動處理序時的工作目錄</param>
        public LocalProcessStartInfo(string fileName, string arguments, string workingDirectory)
        {
            _fileName = fileName;
            _arguments = arguments;
            _workingDirectory = workingDirectory;
        }

        /// <summary>
        /// 初始化 <see cref="LocalProcessStartInfo"/> 類別的新執行個體
        /// </summary>
        /// <param name="startInfo">要用來當參考的 <see cref="System.Diagnostics.ProcessStartInfo"/> 物件</param>
        public LocalProcessStartInfo(System.Diagnostics.ProcessStartInfo startInfo)
            : this(startInfo.FileName, startInfo.Arguments, startInfo.WorkingDirectory) { }

        /// <summary>
        /// 取得 <see cref="LocalProcessStartInfo"/> 所對應的 <see cref="System.Diagnostics.ProcessStartInfo"/> 物件
        /// </summary>
        /// <returns></returns>
        public System.Diagnostics.ProcessStartInfo ToProcessStartInfo()
            => new System.Diagnostics.ProcessStartInfo
            {
                FileName = _fileName,
                Arguments = _arguments,
                WorkingDirectory = _workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
    }
}
