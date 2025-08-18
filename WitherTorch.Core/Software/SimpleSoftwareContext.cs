using System;
using System.Threading.Tasks;
using System.Threading;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Software
{
    internal sealed class SimpleSoftwareContext : ISoftwareContext
    {
        private readonly string _softwareId;
        private readonly Either<string[], Func<string[]?>> _versionsOrFactory;
        private readonly Type _serverType;
        private readonly Func<string, Server?> _createServerFactory;
        private readonly Func<Task<bool>>? _initializer;

        public SimpleSoftwareContext(string softwareId, Type serverType, string[] versions, Func<string, Server?> createServerFactory, Func<Task<bool>>? initializer)
        {
            _softwareId = softwareId;
            _serverType = serverType;
            _versionsOrFactory = Either.Left<string[], Func<string[]?>>(versions);
            _createServerFactory = createServerFactory;
            _initializer = initializer;
        }

        public SimpleSoftwareContext(string softwareId, Type serverType, Func<string[]?> versionsFactory, Func<string, Server?> createServerFactory, Func<Task<bool>>? initializer)
        {
            _softwareId = softwareId;
            _serverType = serverType;
            _versionsOrFactory = Either.Right<string[], Func<string[]?>>(versionsFactory);
            _createServerFactory = createServerFactory;
            _initializer = initializer;
        }

        public string GetSoftwareId() => _softwareId;

        public Type GetServerType() => _serverType;

        public string[] GetSoftwareVersions()
        {
            Either<string[], Func<string[]?>> versionsOrFactory = _versionsOrFactory;
            if (versionsOrFactory.IsLeft)
                return versionsOrFactory.Left;
            if (versionsOrFactory.IsRight)
                return versionsOrFactory.Right.Invoke() ?? Array.Empty<string>();
            return Array.Empty<string>();
        }

        public Server? CreateServerInstance(string serverDirectory) => _createServerFactory.Invoke(serverDirectory);

        public async Task<bool> TryInitializeAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return false;
            Func<Task<bool>>? initializer = _initializer;
            if (initializer is null)
                return true;
            return await initializer.Invoke();
        }
    }
}
