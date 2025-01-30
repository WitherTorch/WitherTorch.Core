using System;
using System.Threading.Tasks;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示一個安裝工作
    /// </summary>
    public class InstallTask
    {
        public delegate void ValidateFailedEventHandler(object sender, ValidateFailedCallbackEventArgs e);

        /// <summary>
        /// 驗證失敗後的操作狀態
        /// </summary>
        public enum ValidateFailedState
        {
            /// <summary>
            /// 取消下載
            /// </summary>
            Cancel = 0,
            /// <summary>
            /// 忽略並繼續
            /// </summary>
            Ignore = 1,
            /// <summary>
            /// 重新下載
            /// </summary>
            Retry = 2
        }

        /// <summary>
        /// 當安裝完成時觸發
        /// </summary>
        public event EventHandler? InstallFinished;
        /// <summary>
        /// 當安裝失敗時觸發
        /// </summary>
        public event EventHandler? InstallFailed;
        /// <summary>
        /// 當安裝檔案驗證失敗時觸發，此事件是個回呼事件
        /// </summary>
        public event ValidateFailedEventHandler? ValidateFailed;
        /// <summary>
        /// 當安裝進度改變時觸發
        /// </summary>
        public event EventHandler? PercentageChanged;
        /// <summary>
        /// 當安裝狀態改變時觸發
        /// </summary>
        public event EventHandler? StatusChanged;
        /// <summary>
        /// 當安裝工作被要求停止時觸發
        /// </summary>
        public event EventHandler? StopRequested;

        /// <summary>
        /// 取得此安裝工作的所有者
        /// </summary>
        public Server Owner { get; }

        /// <summary>
        /// 取得此安裝工作的所要安裝的版本
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 取得目前的安裝總進度百分比
        /// </summary>
        public double InstallPercentage { get; private set; }

        /// <summary>
        /// 取得目前的安裝狀態物件，此屬性有可能是 <see langword="null"/>
        /// </summary>
        public AbstractInstallStatus? Status { get; private set; }

        private readonly EitherStruct<Action<InstallTask>, Action<InstallTask, object?>> _installAction;
        private readonly object? _installActionState;

        private bool _isStopped;

        public InstallTask(Server owner, string version, Action<InstallTask> installAction)
        {
            Owner = owner;
            Version = version;
            Status = PreparingInstallStatus.Instance;
            _installAction = Either.Left<Action<InstallTask>, Action<InstallTask, object?>>(installAction);
            _installActionState = null;
        }

        public InstallTask(Server owner, string version, object? state, Action<InstallTask, object?> installAction)
        {
            Owner = owner;
            Version = version;
            Status = PreparingInstallStatus.Instance;
            _installAction = Either.Right<Action<InstallTask>, Action<InstallTask, object?>>(installAction);
            _installActionState = null;
        }

        /// <summary>
        /// 修改安裝總進度百分比，修改成功後將會觸發 <see cref="PercentageChanged"/> 事件
        /// </summary>
        /// <param name="percentage"></param>
        public void ChangePercentage(double percentage)
        {
            double value = percentage < 0 ? 0 : (percentage > 100 ? 100 : percentage);
            if (InstallPercentage != value)
            {
                InstallPercentage = value;
                PercentageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 更換安裝狀態物件，更換成功後將會觸發 <see cref="StatusChanged"/> 事件
        /// </summary>
        /// <param name="status">要替換的安裝狀態物件</param>
        public void ChangeStatus(AbstractInstallStatus status)
        {
            if (Status == status)
                return;
            Status = status;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 引發 <see cref="InstallFinished"/> 事件
        /// </summary>
        public virtual void OnInstallFinished()
        {
            InstallFinished?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 引發 <see cref="InstallFailed"/> 事件
        /// </summary>
        public virtual void OnInstallFailed()
        {
            InstallFailed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 要求開始安裝流程
        /// </summary>
        /// <remarks>(安裝過程為非同步執行，伺服器軟體會呼叫 <see cref="InstallTask"/> 內的各項事件以更新目前的安裝狀態)</remarks>
        public void Start()
        {
            var installAction = _installAction;
            if (installAction.IsLeft)
            {
                StartCore(installAction.Left);
                return;
            }
            if (installAction.IsRight)
            {
                StartCore(installAction.Right, _installActionState);
                return;
            }
        }

        protected virtual void StartCore(Action<InstallTask> installAction)
            => Task.Factory.StartNew(delegate ()
            {
                try
                {
                    installAction.Invoke(this);
                }
                catch (Exception)
                {
                    OnInstallFailed();
                }
            }, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

        protected virtual void StartCore(Action<InstallTask, object?> installAction, object? state)
            => Task.Factory.StartNew(delegate (object? state)
            {
                try
                {
                    installAction.Invoke(this, state);
                }
                catch (Exception)
                {
                    OnInstallFailed();
                }
            }, state, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);

        /// <summary>
        /// 要求停止安裝流程
        /// </summary>
        public void Stop()
        {
            if (!_isStopped)
            {
                _isStopped = true;
                StopRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 引發 <see cref="StatusChanged"/> 事件
        /// </summary>
        public void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 引發 <see cref="ValidateFailed"/> 事件，並傳回是否取消安裝的值
        /// </summary>
        public ValidateFailedState OnValidateFailed(string filename, byte[] actualFileHash, byte[] exceptedFileHash)
        {
            ValidateFailedCallbackEventArgs callback = new ValidateFailedCallbackEventArgs(filename, actualFileHash, exceptedFileHash);
            StatusChanged?.Invoke(this, callback);
            return callback.GetState();
        }

        /// <summary>
        /// 提供 <see cref="ValidateFailed"/> 事件的資料
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
