using System;
using System.Text;
using System.Threading;

using CLRProcess = System.Diagnostics.Process;
using CLRProcessStartInfo = System.Diagnostics.ProcessStartInfo;

namespace WitherTorch.Core.Runtime;

/// <summary>
/// 可重覆使用的本機系統處理程序類別
/// </summary>
public class LocalProcess : ILocalProcess
{
    /// <summary>
    /// 啟動本機系統處理程序時所使用的編碼
    /// </summary>
    public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private readonly object _syncLock = new object();
    private MessageReceivedEventHandler? _receivedHandler;
    private CLRProcess? _process;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler? ProcessStarted;
    /// <inheritdoc/>
    public event EventHandler? ProcessEnded;
    /// <inheritdoc/>
    public event MessageReceivedEventHandler? MessageReceived
    {
        add
        {
            lock (_syncLock)
            {
                MessageReceivedEventHandler? handler = _receivedHandler;
                _receivedHandler = handler + value;
                if (handler is not null)
                    return;
                CLRProcess? process = _process;
                if (process is null)
                    return;
                OnMessageReceivedEventSubscribed(process);
            }
        }
        remove
        {
            lock (_syncLock)
            {
                MessageReceivedEventHandler? handler = _receivedHandler;
                handler -= value;
                _receivedHandler = handler;
                if (handler is not null)
                    return;
                CLRProcess? process = _process;
                if (process is null)
                    return;
                OnMessageReceivedEventUnsubscribed(process);
            }
        }
    }

    /// <summary>
    /// 取得用於同步存取的物件
    /// </summary>
    public object SyncRoot => _syncLock;

    /// <inheritdoc/>
    public int Id
    {
        get
        {
            CLRProcess? process = AsCLRProcess();
            if (process is null)
                return default;
            try
            {
                return process.Id;
            }
            catch (InvalidOperationException)
            {
                return default;
            }
        }
    }

    /// <inheritdoc/>
    public DateTime StartTime
    {
        get
        {
            CLRProcess? process = AsCLRProcess();
            if (process is null)
                return default;
            try
            {
                return process.StartTime;
            }
            catch (InvalidOperationException)
            {
                return default;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsAlive
    {
        get
        {
            CLRProcess? process = AsCLRProcess();
            if (process is null)
                return false;
            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public string? WorkingDirectory
    {
        get
        {
            CLRProcess? process = AsCLRProcess();
            if (process is null)
                return null;
            try
            {
                return process.StartInfo.WorkingDirectory;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        CLRProcess? process;
        if ((process = Interlocked.Exchange(ref _process, null)) is null)
            return;
        StopCore(process);
    }

    /// <summary>
    /// 終止指定的本機系統處理序
    /// </summary>
    /// <param name="process">要終止的本機系統處理序</param>
    protected virtual void StopCore(CLRProcess process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill();
        }
        catch (InvalidOperationException)
        {
        }
        process.ErrorDataReceived -= Process_ErrorDataReceived;
        process.OutputDataReceived -= Process_OutputDataReceived;
        process.Exited -= Process_Exited;
        ProcessEnded?.Invoke(this, EventArgs.Empty);
        try
        {
            process.Dispose();
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <inheritdoc/>
    public void InputCommand(string command)
        => AsCLRProcess()?.StandardInput.WriteLine(command);

    /// <inheritdoc />
    public CLRProcess? AsCLRProcess() => Volatile.Read(ref _process);

    /// <inheritdoc />
    public bool Start(in LocalProcessStartInfo startInfo)
    {
        lock (_syncLock)
        {
            CLRProcess? process = _process;
            try
            {
                if (process is not null)
                {
                    if (!process.HasExited)
                        return false;
                    process.Dispose();
                }
            }
            catch (Exception)
            {
            }

            CLRProcessStartInfo processStartInfo = startInfo.ToProcessStartInfo();
            if (WTCore.RedirectSystemProcessStream)
            {
                processStartInfo.StandardOutputEncoding = Encoding;
                processStartInfo.StandardErrorEncoding = Encoding;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardInput = true;
            }
            process = CLRProcess.Start(processStartInfo);
            try
            {
                if (process is null || process.HasExited)
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            _process = process;
            process.EnableRaisingEvents = true;
            process.Exited += Process_Exited;

            ProcessStarted?.Invoke(this, EventArgs.Empty);
            StartCore(process);

            return true;
        }
    }

    /// <summary>
    /// 在指定的本機系統處理序啟動之後要執行的程式碼
    /// </summary>
    /// <param name="process">已啟動的本機系統處理序</param>
    protected virtual void StartCore(CLRProcess process)
    {
        if (_receivedHandler is not null)
            OnMessageReceivedEventSubscribed(process);
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        if (sender is not CLRProcess process ||
            !ReferenceEquals(Interlocked.CompareExchange(ref _process, null, process), process))
            return;
        StopCore(process);
    }

    private void Process_ErrorDataReceived(object? sender, System.Diagnostics.DataReceivedEventArgs e)
        => OnMessageReceived(new MessageReceivedEventArgs(true, e.Data ?? string.Empty));

    private void Process_OutputDataReceived(object? sender, System.Diagnostics.DataReceivedEventArgs e)
        => OnMessageReceived(new MessageReceivedEventArgs(false, e.Data ?? string.Empty));

    /// <summary>
    /// 呼叫 <see cref="MessageReceived"/> 事件
    /// </summary>
    protected virtual void OnMessageReceived(in MessageReceivedEventArgs e) => Volatile.Read(ref _receivedHandler)?.Invoke(this, e);

    /// <summary>
    /// 在 <see cref="MessageReceived"/> 事件被訂閱後要執行的方法
    /// </summary>
    /// <param name="process">已啟動的本機系統處理序</param>
    protected virtual void OnMessageReceivedEventSubscribed(CLRProcess process)
    {
        CLRProcessStartInfo startInfo;
        try
        {
            startInfo = process.StartInfo;
        }
        catch (Exception)
        {
            return;
        }

        try
        {
            if (startInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
                process.OutputDataReceived += Process_OutputDataReceived;
            }
        }
        catch (Exception)
        {
        }
        try
        {
            if (startInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// 在 <see cref="MessageReceived"/> 事件被解除訂閱後要執行的方法
    /// </summary>
    /// <param name="process">已啟動的本機系統處理序</param>
    protected virtual void OnMessageReceivedEventUnsubscribed(CLRProcess process)
    {
        try
        {
            process.CancelOutputRead();
            process.CancelErrorRead();
        }
        finally
        {
            process.OutputDataReceived -= Process_OutputDataReceived;
            process.ErrorDataReceived -= Process_ErrorDataReceived;
        }
    }

    /// <inheritdoc cref="Dispose()"/>
    protected virtual void DisposeCore()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }

    /// <summary>
    /// <see cref="LocalProcess"/> 的解構子
    /// </summary>
    ~LocalProcess()
    {
        DisposeCore();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }
}
