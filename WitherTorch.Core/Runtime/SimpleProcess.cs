using System;
using System.Threading;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// <see cref="IProcess"/> 類別的簡易實作
    /// </summary>
    public sealed class SimpleProcess : IProcess
    {
        private static int _idCounter = 0;
        private readonly Either<Action, Action<object?>> _startFunc, _stopFunc;
        private readonly Either<Action<string>, Action<string, object?>> _commandSendFunc;
        private readonly object? _state;

        private DateTime _startTime;
        private long _isAlive;
        private int _id;

        /// <inheritdoc />
        public event EventHandler? ProcessStarted;

        /// <inheritdoc />
        public event EventHandler? ProcessEnded;

        /// <inheritdoc />
        public event MessageReceivedEventHandler? MessageReceived;

        /// <inheritdoc/>
        public DateTime StartTime => _startTime;

        /// <inheritdoc />
        public int Id => _id;

        /// <inheritdoc />
        public bool IsAlive => Interlocked.Read(ref _isAlive) != 0L;

        /// <summary>
        /// 建構一個新的 <see cref="SimpleProcess"/> 實例
        /// </summary>
        /// <param name="startFunc">啟動執行緒時所要執行的委派</param>
        /// <param name="stopFunc">終止執行緒時所要執行的委派</param>
        /// <param name="commandSendFunc">呼叫 <see cref="InputCommand(string)"/> 所要執行的委派</param>
        public SimpleProcess(Action startFunc, Action stopFunc, Action<string> commandSendFunc)
        {
            _startFunc = Either.Left<Action, Action<object?>>(startFunc);
            _stopFunc = Either.Left<Action, Action<object?>>(stopFunc);
            _commandSendFunc = Either.Left<Action<string>, Action<string, object?>>(commandSendFunc);
            _state = null;
        }

        /// <summary>
        /// 建構一個新的 <see cref="SimpleProcess"/> 實例
        /// </summary>
        /// <param name="startFunc">啟動執行緒時所要執行的委派</param>
        /// <param name="stopFunc">終止執行緒時所要執行的委派</param>
        /// <param name="commandSendFunc">呼叫 <see cref="InputCommand(string)"/> 所要執行的委派</param>
        /// <param name="state">使用者自定義的狀態物件</param>
        public SimpleProcess(Action<object?> startFunc, Action<object?> stopFunc, Action<string, object?> commandSendFunc, object? state)
        {
            _startFunc = Either.Right<Action, Action<object?>>(startFunc);
            _stopFunc = Either.Right<Action, Action<object?>>(stopFunc);
            _commandSendFunc = Either.Right<Action<string>, Action<string, object?>>(commandSendFunc);
            _state = state;
        }

        /// <summary>
        /// 啟動此處理序
        /// </summary>
        public void Start()
        {
            if (Interlocked.CompareExchange(ref _isAlive, 1L, 0L) != 0L)
                return;
            _id = Interlocked.Increment(ref _idCounter);
            _startTime = DateTime.Now;
            _startFunc.Invoke(
                static (action, _) => action.Invoke(),
                static (action, state) => action.Invoke(state),
                _state);
            ProcessStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 終止此處理序
        /// </summary>
        public void Stop()
        {
            if (Interlocked.Exchange(ref _isAlive, 0L) == 0L)
                return;
            _startTime = default;
            _stopFunc.Invoke(
                static (action, _) => action.Invoke(),
                static (action, state) => action.Invoke(state),
                _state);
            ProcessEnded?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void InputCommand(string command)
        {
            if (Interlocked.Read(ref _isAlive) == 0L)
                return;
            _commandSendFunc.Invoke(
                static (action, pair) => action.Invoke(pair.command),
                static (action, pair) => action.Invoke(pair.command, pair._state),
                (command, _state));
        }
    }
}
