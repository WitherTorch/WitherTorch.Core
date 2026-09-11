using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

using Microsoft.Win32.SafeHandles;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Property;

/// <summary>
/// <see cref="IPropertyFile"/> 物件在建立時所指定的操作模式
/// </summary>
public enum PropertyFileMode
{
    /// <summary>
    /// 僅在 <see cref="IPropertyFile.Reload()"/> 與 <see cref="IPropertyFile.Save(bool)"/> 時才建立並操作資料流，其餘時候與檔案本身完全分離
    /// </summary>
    Pure,
    /// <summary>
    /// 持續監測檔案，在內容有更新時自動同步
    /// </summary>
    KeepWatching,
    /// <summary>
    /// 持續占用檔案的存取權限，直到該物件的 <see cref="IDisposable.Dispose()"/> 被呼叫
    /// </summary>
    Blocked
}

/// <summary>
/// <seealso cref="JavaPropertyFile"/>、<seealso cref="JsonPropertyFile"/> 和 <seealso cref="YamlPropertyFile"/> 的基底類別
/// </summary>
/// <typeparam name="TValue">設定檔案內所儲存的設定值類型</typeparam>
public abstract class PropertyFileBase<TValue> : IPropertyFile
{
    private readonly FileModifyWatcher? _watcher;
    private readonly FileStream? _blockingStream;
    private readonly string _path;
    private readonly PropertyFileMode _mode;

    private IPropertyFileDescriptor? _descriptor;

    private bool _loaded, _dirty, _disposed;

    /// <inheritdoc/>
    public PropertyFileMode Mode => _mode;

    /// <inheritdoc/>
    public string FilePath => _path;

    /// <summary>
    /// 取得該設定檔案所繫結的 <see cref="SafeFileHandle"/> 物件
    /// </summary>
    public SafeFileHandle? BlockingFileHandle => _blockingStream?.SafeFileHandle;

    /// <summary>
    /// 取得該設定檔案所繫結的 <see cref="FileModifyWatcher"/> 物件
    /// </summary>
    public FileModifyWatcher? Watcher => _watcher;

    /// <inheritdoc/>
    public IPropertyFileDescriptor? Descriptor { get => _descriptor; set => _descriptor = value; }

    /// <summary>
    /// 以指定的設定檔路徑，建立新的 <see cref="PropertyFileBase{TValue}"/> 物件
    /// </summary>
    /// <param name="path">設定檔的路徑</param>
    public PropertyFileBase(string path) : this(path, WTCore.DefaultPropertyFileMode) { }

    /// <summary>
    /// 以指定的設定檔路徑與建立模式，建立新的 <see cref="PropertyFileBase{TValue}"/> 物件
    /// </summary>
    /// <param name="path">設定檔的路徑</param>
    /// <param name="mode">設定檔案物件的建立模式</param>
    public PropertyFileBase(string path, PropertyFileMode mode)
    {
        _path = path;
        _mode = mode;
        switch (mode)
        {
            case PropertyFileMode.Pure:
                break;
            case PropertyFileMode.KeepWatching:
                FileModifyWatcher? watcher = new FileModifyWatcher(path);
                watcher.Changed += FileWatcher_Changed;
                watcher.Active();
                _watcher = watcher;
                break;
            case PropertyFileMode.Blocked:
                _blockingStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                break;
        }
    }

    /// <summary>
    /// 取得或修改設定檔案內的設定值
    /// </summary>
    /// <param name="key">該設定值所在的路徑</param>
    /// <returns><paramref name="key"/> 所對應的設定值，如果設定值不存在則為 <see langword="null"/></returns>
    /// <remarks>此屬性不會自動呼叫 <see cref="Save(bool)"/> 方法，如果修改了設定檔案的設定值，請手動呼叫一次上述方法來儲存設定檔案</remarks>
    public TValue? this[string? key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (key is null)
                return default;
            Load(force: false);
            return GetValueCore(key);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (key is null)
                return;
            Load(force: false);
            bool dirty;
            if (value is null)
                dirty = RemoveValueCore(key);
            else
                dirty = SetValueCore(key, value);
            if (dirty)
                _dirty = true;
        }
    }

    /// <summary>
    /// 將設定檔案的內容載入至記憶體內
    /// </summary>
    /// <param name="force">是否在設定檔案已經處於載入狀態時強制重新載入</param>
    public void Load(bool force)
    {
        if (_loaded)
        {
            if (!force)
                return;
            Unload();
        }
        if (TryGetStreamForRead(out Stream? stream, out bool needDispose))
        {
            try
            {
                LoadCore(stream);
            }
            finally
            {
                if (needDispose)
                    stream.Dispose();
            }
        }
        else
        {
            LoadCore(null);
        }
        _loaded = true;
    }

    /// <summary>
    /// 將記憶體內的設定檔案內容清除
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unload()
    {
        _loaded = false;
        _dirty = false;
        UnloadCore();
    }

    /// <summary>
    /// 將該設定檔案標記為已修改
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkDirty()
    {
        _dirty = true;
    }

    /// <summary>
    /// 將記憶體內的設定檔案內容儲存至原始檔案內
    /// </summary>
    /// <param name="force">是否在檔案未修改的情況下仍強制執行儲存操作</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Save(bool force)
    {
        if (!_loaded || (!force && !_dirty))
            return;
        FileModifyWatcher? watcher = _watcher;
        if (watcher is null)
        {
            if (TryGetStreamForWrite(out Stream? stream, out bool needDispose))
            {
                try
                {
                    SaveCore(stream);
                }
                finally
                {
                    if (needDispose)
                        stream.Dispose();
                    else
                        stream.Flush();
                }
            }
        }
        else
        {
            watcher.Changed -= FileWatcher_Changed;
            watcher.Deactive();
            if (TryGetStreamForWrite(out Stream? stream, out bool needDispose))
            {
                try
                {
                    SaveCore(stream);
                }
                finally
                {
                    if (needDispose)
                        stream.Dispose();
                    else
                        stream.Flush();
                }
            }
            watcher.Changed += FileWatcher_Changed;
            watcher.Active();
        }
        Unload();
    }

    /// <summary>
    /// 嘗試取得可用於讀取的 <see cref="Stream"/> 物件
    /// </summary>
    /// <param name="result">如果傳回值為 <see langword="true"/>，返回的結果為可讀取的資料流，並需要根據 <paramref name="needDispose"/> 的結果來決定是否要釋放；反之則為 <see langword="null"/></param>
    /// <param name="needDispose">決定 <paramref name="result"/> 傳回的結果需不需要釋放</param>
    /// <returns>是否成功取得資料流</returns>
    protected bool TryGetStreamForRead([NotNullWhen(true)] out Stream? result, out bool needDispose)
    {
        Stream? stream = _blockingStream;
        if (stream is not null)
        {
            stream.Position = 0;
            result = stream;
            needDispose = false;
            return true;
        }

        try
        {
            stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception)
        {
            result = default;
            needDispose = false;
            return false;
        }

        result = stream;
        needDispose = true;
        return true;
    }

    /// <summary>
    /// 嘗試取得可用於寫入的 <see cref="Stream"/> 物件
    /// </summary>
    /// <param name="result">如果傳回值為 <see langword="true"/>，返回的結果為可寫入的資料流，並需要根據 <paramref name="needDispose"/> 的結果來決定是否要釋放；反之則為 <see langword="null"/></param>
    /// <param name="needDispose">決定 <paramref name="result"/> 傳回的結果需不需要釋放</param>
    /// <returns>是否成功取得資料流</returns>
    protected bool TryGetStreamForWrite([NotNullWhen(true)] out Stream? result, out bool needDispose)
    {
        Stream? stream = _blockingStream;
        if (stream is not null)
        {
            stream.Position = 0;
            result = stream;
            needDispose = false;
            return true;
        }

        try
        {
            stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
        catch (Exception)
        {
            result = default;
            needDispose = false;
            return false;
        }

        result = stream;
        needDispose = true;
        return true;
    }

    /// <summary>
    /// 子類別應實作此方法為從檔案載入設定的程式碼
    /// </summary>
    /// <param name="stream">檔案的資料串流，如果檔案不存在則為 <see langword="null"/></param>
    protected abstract void LoadCore(Stream? stream);

    /// <summary>
    /// 子類別應實作此方法為從記憶體內清除暫存設定資料的程式碼
    /// </summary>
    protected abstract void UnloadCore();

    /// <summary>
    /// 子類別應實作此方法為將記憶體內設定資料存入檔案的程式碼
    /// </summary>
    /// <param name="stream">檔案的資料串流</param>
    protected abstract void SaveCore(Stream stream);

    /// <summary>
    /// 子類別應實作此方法為取得記憶體中特定設定值的程式碼
    /// </summary>
    /// <param name="key">要取得的設定路徑</param>
    /// <returns></returns>
    protected abstract TValue? GetValueCore(string key);

    /// <summary>
    /// 子類別應實作此方法為修改記憶體中特定設定值的程式碼
    /// </summary>
    /// <param name="key">要修改的設定路徑</param>
    /// <param name="value">要應用的設定值</param>
    /// <returns>是否成功修改設定</returns>
    protected abstract bool SetValueCore(string key, TValue value);

    /// <summary>
    /// 子類別應實作此方法為移除記憶體中特定設定值的程式碼
    /// </summary>
    /// <param name="key">要移除的設定路徑</param>
    /// <returns>是否成功移除設定</returns>
    protected abstract bool RemoveValueCore(string key);

    private void FileWatcher_Changed(object? sender, EventArgs e)
    {
        if (!_loaded || _dirty)
            return;
        Unload();
    }

    /// <inheritdoc/>
    public void Reload()
    {
        Load(force: true);
    }

    private void DisposeCore()
    {
        if (_disposed)
            return;
        _disposed = true;
        _watcher?.Deactive();
        _blockingStream?.Dispose();
    }

    /// <inheritdoc cref="object.Finalize()"/>
    ~PropertyFileBase()
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
