using System;
using System.IO;
using System.Runtime.CompilerServices;

using WitherTorch.Core.Property;
using WitherTorch.Core.Software;

namespace WitherTorch.Core
{
    partial class Server
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Server? CreateServerInstance(ISoftwareContext software, string serverDirectory)
        {
            Server? server = software.CreateServerInstance(serverDirectory);
            if (server is null)
                return null;
            if (!software.GetServerType().IsAssignableFrom(server.GetType()))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T? CreateServerInstanceTyped<T>(ISoftwareContext software, string serverDirectory) where T : Server
        {
            Server? rawServer = software.CreateServerInstance(serverDirectory);
            if (rawServer is null)
                return null;
            if (rawServer is not T server)
            {
                (rawServer as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Server? CreateServerCore(ISoftwareContext software, string serverDirectory)
        {
            Server? server = CreateServerInstance(software, serverDirectory);
            if (server is null)
                return null;
            server.ServerName = GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.CreateServerCore())
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T? CreateServerCoreTyped<T>(ISoftwareContext software, string serverDirectory) where T : Server
        {
            T? server = CreateServerInstanceTyped<T>(software, serverDirectory);
            if (server is null)
                return null;
            server.ServerName = GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.CreateServerCore())
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Server? LoadServerCore(ISoftwareContext software, string serverDirectory, JsonPropertyFile serverInfoJson)
        {
            Server? server = CreateServerInstance(software, serverDirectory);
            if (server is null)
                return null;
            server.ServerInfoJson = serverInfoJson;
            server.ServerName = serverInfoJson["name"]?.GetValue<string>() ?? GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.LoadServerCore(serverInfoJson))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T? LoadServerCoreTyped<T>(ISoftwareContext software, string serverDirectory, JsonPropertyFile serverInfoJson) where T : Server
        {
            T? server = CreateServerInstanceTyped<T>(software, serverDirectory);
            if (server is null)
                return null;
            server.ServerInfoJson = serverInfoJson;
            server.ServerName = serverInfoJson["name"]?.GetValue<string>() ?? GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.LoadServerCore(serverInfoJson))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            return server;
        }

        private static string GetDefaultServerNameCore(string serverDirectory)
        {
#if NETSTANDARD2_1_OR_GREATER || NET8_0_OR_GREATER
            ReadOnlySpan<char> span = serverDirectory.AsSpan().TrimEnd(Path.DirectorySeparatorChar);
            if (span.Length > 3)
                return Path.GetFileName(span).ToString();
#else
            serverDirectory = serverDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (serverDirectory.Length > 3)
                return Path.GetFileName(serverDirectory);
#endif
            return serverDirectory;
        }
    }
}
