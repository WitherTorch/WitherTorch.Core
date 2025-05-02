namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 本機伺服器處理序的基底介面
    /// </summary>
    public interface ILocalProcess : IProcess
    {
        /// <summary>
        /// 取得當前處理序的工作目錄
        /// </summary>
        string? WorkingDirectory { get; }

        /// <summary>
        /// 使用指定的 <see cref="System.Diagnostics.ProcessStartInfo"/> 物件來啟動處理序
        /// </summary>
        /// <param name="startInfo">處理序的啟動資料</param>
        /// <returns>是否成功啟動處理序</returns>
        bool Start(System.Diagnostics.ProcessStartInfo startInfo);

        /// <summary>
        /// 終止此處理序
        /// </summary>
        void Stop();

        /// <summary>
        /// 取得此物件所對應的 <see cref="System.Diagnostics.Process"/> 物件
        /// </summary>
        /// <returns></returns>
        System.Diagnostics.Process? AsCLRProcess();
    }
}
