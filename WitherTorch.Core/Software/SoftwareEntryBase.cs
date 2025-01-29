using System;

namespace WitherTorch.Core.Software
{
    /// <summary>
    /// <see cref="ISoftwareEntry"/> 的抽象類別版本，方便開發者繼承
    /// </summary>
    public abstract class SoftwareEntryBase<T> : ISoftwareEntry where T : Server
    {
        private readonly string _softwareId;
        private readonly Type _serverType;

        protected SoftwareEntryBase(string softwareId)
        {
            _softwareId = softwareId;
            _serverType = typeof(T);
        }

        public string GetSoftwareId() => _softwareId;

        public Type GetServerType() => _serverType;

        public abstract string[] GetSoftwareVersions();

        public abstract T? CreateServerInstance(string serverDirectory);

        public abstract bool TryInitialize();

        Server? ISoftwareEntry.CreateServerInstance(string serverDirectory)
            => CreateServerInstance(serverDirectory);
    }
}
