﻿using System;

using WitherTorch.Core.Utils;

using YamlDotNet.Core;

namespace WitherTorch.Core.Software
{
    internal sealed class SimpleSoftwareContext : ISoftwareContext
    {
        private readonly string _softwareId;
        private readonly EitherStruct<string[], Func<string[]?>> _versionsOrFactory;
        private readonly Type _serverType;
        private readonly Func<string, Server?> _createServerFactory;
        private readonly Func<bool>? _initializer;

        public SimpleSoftwareContext(string softwareId, Type serverType, string[] versions, Func<string, Server?> createServerFactory, Func<bool>? initializer)
        {
            _softwareId = softwareId;
            _serverType = serverType;
            _versionsOrFactory = Either.Left<string[], Func<string[]?>>(versions);
            _createServerFactory = createServerFactory;
            _initializer = initializer;
        }

        public SimpleSoftwareContext(string softwareId, Type serverType, Func<string[]?> versionsFactory, Func<string, Server?> createServerFactory, Func<bool>? initializer)
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
            EitherStruct<string[], Func<string[]?>> versionsOrFactory = _versionsOrFactory;
            if (versionsOrFactory.IsLeft)
                return versionsOrFactory.Left;
            if (versionsOrFactory.IsRight)
                return versionsOrFactory.Right.Invoke() ?? Array.Empty<string>();
            return Array.Empty<string>();
        }

        public Server? CreateServerInstance(string serverDirectory) => _createServerFactory.Invoke(serverDirectory);

        public bool TryInitialize() => _initializer?.Invoke() ?? true;
    }
}
