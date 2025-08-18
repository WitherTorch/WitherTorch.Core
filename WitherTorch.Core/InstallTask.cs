using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 表示在 <see cref="InstallTask"/> 內所執行的伺服器安裝方法
    /// </summary>
    /// <param name="task">執行此委派的 <see cref="InstallTask"/> 物件</param>
    /// <param name="token">偵測安裝工作是否被要求停止的令牌</param>
    public delegate void InstallTaskStart(InstallTask task, CancellationToken token);

    /// <summary>
    /// 表示在 <see cref="InstallTask"/> 內所執行的伺服器安裝方法
    /// </summary>
    /// <param name="task">執行此委派的 <see cref="InstallTask"/> 物件</param>
    /// <param name="token">偵測安裝工作是否被要求停止的令牌</param>
    /// <param name="state">安裝方法在執行時將傳入的自定義物件</param>
    public delegate void ParameterizedInstallTaskStart(InstallTask task, CancellationToken token, object? state);

    /// <summary>
    /// <see cref="InstallTask.ValidateFailed"/> 事件專用的委派方法
    /// </summary>
    /// <param name="sender">事件的發送者 (可能為 <see langword="null"/>)</param>
    /// <param name="e">事件的回呼物件</param>
    public delegate void ValidateFailedEventHandler(object? sender, ValidateFailedCallbackEventArgs e);

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
    /// 表示一個安裝工作
    /// </summary>
    public partial class InstallTask : IDisposable
    {
        /// <summary>
        /// 安裝完成時將觸發此事件
        /// </summary>
        public event EventHandler? InstallFinished;
        /// <summary>
        /// 安裝失敗時將觸發此事件
        /// </summary>
        public event EventHandler? InstallFailed;
        /// <summary>
        /// 安裝檔案驗證失敗時將觸發此事件，該事件是個回呼事件
        /// </summary>
        public event ValidateFailedEventHandler? ValidateFailed;
        /// <summary>
        /// 安裝進度改變時將觸發此事件
        /// </summary>
        public event EventHandler? PercentageChanged;
        /// <summary>
        /// 安裝狀態改變時將觸發此事件
        /// </summary>
        public event EventHandler? StatusChanged;
        /// <summary>
        /// 安裝工作被要求停止時將觸發此事件
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
        public double InstallPercentage => _percentage;

        /// <summary>
        /// 取得目前的安裝狀態物件，此屬性有可能是 <see langword="null"/>
        /// </summary>
        public AbstractInstallStatus Status => _status;

        private readonly TaskCreationOptions _creationOptions;
        private readonly Either<InstallTaskStart, ParameterizedInstallTaskStart> _installTaskStart;
        private readonly object? _installTaskStartState;
        private readonly CancellationTokenSource _tokenSource;

        private bool _stopped;
        private bool _disposed;
        private double _percentage;
        private AbstractInstallStatus _status;

        /// <summary>
        /// <see cref="InstallTask"/> 的建構子
        /// </summary>
        /// <param name="owner">此安裝工作的擁有者</param>
        /// <param name="version">此安裝工作的所要安裝的版本</param>
        /// <param name="installTaskStart">在觸發 <see cref="Start"/> 時所要執行的安裝委派</param>
        /// <param name="creationOptions">在啟動 <paramref name="installTaskStart"/> 時所要附加的行程執行選項</param>
        public InstallTask(Server owner, string version,
            InstallTaskStart installTaskStart, TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Owner = owner;
            Version = version;
            _status = PreparingInstallStatus.Instance;
            _installTaskStart = Either.Left<InstallTaskStart, ParameterizedInstallTaskStart>(installTaskStart);
            _installTaskStartState = null;
            _tokenSource = new CancellationTokenSource();
            _creationOptions = creationOptions;
        }

        /// <summary>
        /// <see cref="InstallTask"/> 的建構子，提供一個額外的 <paramref name="state"/> 參數以便使用者儲存安裝時要使用的額外資訊
        /// </summary>
        /// <param name="owner">此安裝工作的擁有者</param>
        /// <param name="version">此安裝工作的所要安裝的版本</param>
        /// <param name="state">此安裝工作的額外資訊，會在觸發 <see cref="Start"/> 時傳入至 <paramref name="installTaskStart"/></param>
        /// <param name="installTaskStart">在觸發 <see cref="Start"/> 時所要執行的安裝委派</param>
        /// <param name="creationOptions">在啟動 <paramref name="installTaskStart"/> 時所要附加的行程執行選項</param>
        public InstallTask(Server owner, string version, object? state,
            ParameterizedInstallTaskStart installTaskStart, TaskCreationOptions creationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Owner = owner;
            Version = version;
            _status = PreparingInstallStatus.Instance;
            _installTaskStart = Either.Right<InstallTaskStart, ParameterizedInstallTaskStart>(installTaskStart);
            _installTaskStartState = state;
            _tokenSource = new CancellationTokenSource();
            _creationOptions = creationOptions;
        }

        /// <summary>
        /// 修改安裝總進度百分比，修改成功後將會觸發 <see cref="PercentageChanged"/> 事件
        /// </summary>
        /// <param name="percentage"></param>
        public void ChangePercentage(double percentage)
        {
#if NETSTANDARD2_0
            double value = percentage < 0 ? 0 : (percentage > 100 ? 100 : percentage);
#else
            double value = Math.Clamp(percentage, 0.0, 100.0);
#endif
            if (_percentage == value)
                return;
            _percentage = value;
            PercentageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 更換安裝狀態物件，更換成功後將會觸發 <see cref="StatusChanged"/> 事件
        /// </summary>
        /// <param name="status">要替換的安裝狀態物件</param>
        public void ChangeStatus(AbstractInstallStatus status)
        {
            if (_status == status)
                return;
            _status = status;
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
            var installAction = _installTaskStart;
            if (installAction.IsLeft)
            {
                StartCore(installAction.Left);
                return;
            }
            if (installAction.IsRight)
            {
                StartCore(installAction.Right, _installTaskStartState);
                return;
            }
        }

        /// <summary>
        /// 若需覆寫，請將此方法覆寫為觸發 <see cref="Start"/> 後會執行的程式碼
        /// </summary>
        /// <param name="installTaskStart"><see cref="InstallTask(Server, string, InstallTaskStart, TaskCreationOptions)"/> 所傳入的安裝工作</param>
        protected virtual void StartCore(InstallTaskStart installTaskStart)
        {
            Task.Factory.StartNew(delegate ()
            {
                try
                {
                    installTaskStart.Invoke(this, _tokenSource.Token);
                }
                catch (Exception)
                {
                    OnInstallFailed();
                }
            }, _tokenSource.Token, _creationOptions, TaskScheduler.Current);
        }

        /// <summary>
        /// 若需覆寫，請將此方法覆寫為觸發 <see cref="Start"/> 後會執行的程式碼
        /// </summary>
        /// <param name="installTaskStart"><see cref="InstallTask(Server, string, object?, ParameterizedInstallTaskStart, TaskCreationOptions)"/> 所傳入的安裝工作</param>
        /// <param name="state">呼叫 <paramref name="installTaskStart"/> 時需傳入的額外安裝資訊</param>
        protected virtual void StartCore(ParameterizedInstallTaskStart installTaskStart, object? state)
        {
            Task.Factory.StartNew(delegate (object? _state)
            {
                try
                {
                    installTaskStart.Invoke(this, _tokenSource.Token, _state);
                }
                catch (Exception)
                {
                    OnInstallFailed();
                }
            }, state, _tokenSource.Token, _creationOptions, TaskScheduler.Current);
        }

        /// <summary>
        /// 要求停止安裝流程
        /// </summary>
        public void Stop()
        {
            if (_stopped)
                return;
            _stopped = true;
            _tokenSource.Cancel();
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 引發 <see cref="StatusChanged"/> 事件
        /// </summary>
        public void OnStatusChanged()
        {
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 觸發 <see cref="ValidateFailed"/> 事件，並傳回是否取消安裝的值
        /// </summary>
        /// <param name="filename">觸發事件的檔案名稱</param>
        /// <param name="actualFileHash">實際的檔案雜湊</param>
        /// <param name="exceptedFileHash">預期的檔案雜湊</param>
        /// <returns><see cref="ValidateFailed"/> 後使用者所指示的操作狀態，預設為 <see cref="ValidateFailedState.Ignore"/></returns>
        public ValidateFailedState OnValidateFailed(string filename, byte[] actualFileHash, byte[] exceptedFileHash)
        {
            ValidateFailedEventHandler? handler = ValidateFailed;
            if (handler is null)
                return ValidateFailedState.Ignore;
            ValidateFailedCallbackEventArgs callback = new ValidateFailedCallbackEventArgs(filename, actualFileHash, exceptedFileHash);
            handler.Invoke(this, callback);
            return callback.GetState();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            DisposeCore(disposing);
        }

        /// <inheritdoc cref="Dispose()"/>
        /// <param name="disposing">
        /// Return <see langword="true"/> if the method is called by <see cref="Dispose()"/>, otherwises <see langword="false"/>.
        /// </param>
        protected virtual void DisposeCore(bool disposing)
        {
            if (!disposing)
                return;
            CancellationTokenSource tokenSource = _tokenSource;
            try
            {
                tokenSource.Cancel();
            }
            catch (Exception)
            {
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        /// <inheritdoc cref="object.Finalize()"/>
        ~InstallTask()
        {
            Dispose(disposing: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
