using System;

namespace WitherTorch.Core
{
public partial class InstallTask
    {
        /// <summary>
        /// 提供 <see cref="ValidateFailed"/> 事件的資料和回呼方法
        /// </summary>
        public class ValidateFailedCallbackEventArgs : EventArgs
        {
            private ValidateFailedState state = ValidateFailedState.Cancel;

            /// <summary>
            /// 取得驗證失敗的檔案路徑
            /// </summary>
            public string Filename { get; }

            /// <summary>
            /// 取得實際的檔案雜湊
            /// </summary>
            public byte[] ActualFileHash { get; }

            /// <summary>
            /// 取得預期的檔案雜湊
            /// </summary>
            public byte[] ExceptedFileHash { get; }

            /// <summary>
            /// <see cref="ValidateFailedCallbackEventArgs"/> 的建構子
            /// </summary>
            /// <param name="filename">觸發事件的檔案路徑</param>
            /// <param name="actualFileHash">實際的檔案雜湊</param>
            /// <param name="exceptedFileHash">預期的檔案雜湊</param>
            public ValidateFailedCallbackEventArgs(string filename, byte[] actualFileHash, byte[] exceptedFileHash)
            {
                Filename = filename;
                ActualFileHash = actualFileHash;
                ExceptedFileHash = exceptedFileHash;
            }

            /// <summary>
            /// 取消下載
            /// </summary>
            public void Cancel()
            {
                state = ValidateFailedState.Cancel;
            }

            /// <summary>
            /// 忽略驗證錯誤並繼續
            /// </summary>
            public void Ignore()
            {
                state = ValidateFailedState.Ignore;
            }

            /// <summary>
            /// 重新下載
            /// </summary>
            public void Retry()
            {
                state = ValidateFailedState.Retry;
            }

            /// <summary>
            /// 取得驗證失敗後的操作狀態
            /// </summary>
            /// <returns></returns>
            public ValidateFailedState GetState() => state;
        }
    }
}
