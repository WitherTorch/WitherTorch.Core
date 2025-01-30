using System;

namespace WitherTorch.Core.Software
{
    /// <summary>
    /// <see cref="ISoftwareContext"/> 的抽象類別版本，方便開發者繼承
    /// </summary>
    /// <typeparam name="T">與此類別相關聯的伺服器物件類型</typeparam>
    public abstract class SoftwareContextBase<T> : ISoftwareContext where T : Server
    {
        private readonly string _softwareId;
        private readonly Type _serverType;

        /// <summary>
        /// <see cref="SoftwareContextBase{T}"/> 的建構子
        /// </summary>
        /// <param name="softwareId">軟體的唯一辨識符 (ID)</param>
        protected SoftwareContextBase(string softwareId)
        {
            _softwareId = softwareId;
            _serverType = typeof(T);
        }

        /// <inheritdoc/>
        public string GetSoftwareId() => _softwareId;

        /// <inheritdoc/>
        public Type GetServerType() => _serverType;

        /// <inheritdoc/>
        public abstract string[] GetSoftwareVersions();

        /// <inheritdoc cref="ISoftwareContext.CreateServerInstance(string)"/>
        public abstract T? CreateServerInstance(string serverDirectory);

        /// <inheritdoc/>
        public abstract bool TryInitialize();

        Server? ISoftwareContext.CreateServerInstance(string serverDirectory)
            => CreateServerInstance(serverDirectory);
    }
}
