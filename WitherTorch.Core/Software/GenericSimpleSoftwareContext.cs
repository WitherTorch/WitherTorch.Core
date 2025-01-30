using System;

using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Software
{
    internal sealed class GenericSimpleSoftwareContext<T> : SoftwareContextBase<T> where T : Server
    {
        private readonly EitherStruct<string[], Func<string[]?>> _versionsOrFactory;
        private readonly Func<string, T?> _createServerFactory;
        private readonly Func<bool>? _initializer;

        public GenericSimpleSoftwareContext(string softwareId, string[] versions, Func<string, T?> createServerFactory, Func<bool>? initializer) : base(softwareId)
        {
            _versionsOrFactory = Either.Left<string[], Func<string[]?>>(versions);
            _createServerFactory = createServerFactory;
            _initializer = initializer;
        }

        public GenericSimpleSoftwareContext(string softwareId, Func<string[]?> versionsFactory, Func<string, T?> createServerFactory, Func<bool>? initializer) : base(softwareId)
        {
            _versionsOrFactory = Either.Right<string[], Func<string[]?>>(versionsFactory);
            _createServerFactory = createServerFactory;
            _initializer = initializer;
        }

        public override string[] GetSoftwareVersions()
        {
            EitherStruct<string[], Func<string[]?>> versionsOrFactory = _versionsOrFactory;
            if (versionsOrFactory.IsLeft)
                return versionsOrFactory.Left;
            if (versionsOrFactory.IsRight)
                return versionsOrFactory.Right.Invoke() ?? Array.Empty<string>();
            return Array.Empty<string>();
        }

        public override T? CreateServerInstance(string serverDirectory) => _createServerFactory.Invoke(serverDirectory);

        public override bool TryInitialize() => _initializer?.Invoke() ?? true;
    }
}
