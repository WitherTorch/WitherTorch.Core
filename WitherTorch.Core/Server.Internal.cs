using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;
using WitherTorch.Core.Software;
using WitherTorch.Core.Tagging;

namespace WitherTorch.Core
{
    partial class Server
    {
        private const string ServerSoftwareNode = "software";
        private const string ServerNameNode = "name";
        private const string PersistentTagsNode = "tags";
        private const string PersistentTagTypeNode = "type";

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
            server.ServerName = serverInfoJson[ServerNameNode]?.GetValue<string>() ?? GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.LoadServerCore(serverInfoJson))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            LoadPersistentTags(server, serverInfoJson);
            return server;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T? LoadServerCoreTyped<T>(ISoftwareContext software, string serverDirectory, JsonPropertyFile serverInfoJson) where T : Server
        {
            T? server = CreateServerInstanceTyped<T>(software, serverDirectory);
            if (server is null)
                return null;
            server.ServerInfoJson = serverInfoJson;
            server.ServerName = serverInfoJson[ServerNameNode]?.GetValue<string>() ?? GetDefaultServerNameCore(Path.GetFullPath(serverDirectory));
            if (!server.LoadServerCore(serverInfoJson))
            {
                (server as IDisposable)?.Dispose();
                return null;
            }
            LoadPersistentTags(server, serverInfoJson);
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

        private static void LoadPersistentTags(Server server, JsonPropertyFile serverInfoJson)
        {
            if (serverInfoJson[PersistentTagsNode] is not JsonArray array || array.Count <= 0)
                return;
            List<IPersistentTag> list = server._tagList;
            lock (list)
            {
                foreach (JsonNode? node in array)
                {
                    if (node is not JsonObject objectNode ||
                        !objectNode.TryGetPropertyValue(PersistentTagTypeNode, out JsonNode? typeNode) ||
                        typeNode is not JsonValue typeValueNode ||
                        !typeValueNode.TryGetValue(out string? type))
                        continue;
                    IPersistentTagFactory? factory = PersistentTagFactoryRegister.GetFactory(type);
                    if (factory is null)
                        continue;
                    IPersistentTag tag;
                    try
                    {
                        tag = factory.Create();
                        if (!tag.Load(objectNode))
                        {
                            (tag as IDisposable)?.Dispose();
                            continue;
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    list.Add(tag);
                }
            }
        }

        private static void SavePersistentTags(Server server, JsonPropertyFile serverInfoJson)
        {
            List<IPersistentTag> list = server._tagList;
            JsonArray array;
            lock (list)
            {
                if (list.Count <= 0)
                {
                    serverInfoJson[PersistentTagsNode] = null;
                    return;
                }
                array = new JsonArray();
                foreach (IPersistentTag tag in list)
                {
                    JsonObject obj = new JsonObject();
                    if (!obj.TryAdd(PersistentTagTypeNode, tag.GetFactory().GetTagTypeId()))
                        continue;
                    try
                    {
                        if (!tag.Store(obj))
                            continue;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    array.Add(obj);
                }
            }
            serverInfoJson[PersistentTagsNode] = array;
        }
    }
}
