using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

using WitherTorch.Core.Property;
using WitherTorch.Core.Software;

namespace WitherTorch.Core
{
    partial class Server
    {
        /// <summary>
        /// 以指定的伺服器物件類型與伺服器資料夾路徑來建立新的伺服器
        /// </summary>
        /// <typeparam name="T">伺服器類型，需使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</typeparam>
        /// <param name="serverDirectory">伺服器資料夾的路徑</param>
        /// <returns>建立好的伺服器，或是 <see langword="null"/> (如果建立伺服器時發生問題的話)</returns>
        public static T? CreateServer<T>(string serverDirectory) where T : Server
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(typeof(T), throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return CreateServerCoreTyped<T>(software, serverDirectory);
        }

        /// <summary>
        /// 以指定的伺服器物件類型與伺服器資料夾路徑來建立新的伺服器
        /// </summary>
        /// <param name="serverType">伺服器類型，需繼承 <see cref="Server"/> 並使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</param>
        /// <param name="serverDirectory">伺服器資料夾的路徑</param>
        /// <returns>建立好的伺服器，或是 <see langword="null"/> (如果建立伺服器時發生問題的話)</returns>
        public static Server? CreateServer(Type serverType, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(serverType, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return CreateServerCore(software, serverDirectory);
        }

        /// <summary>
        /// 建立伺服器
        /// </summary>
        /// <param name="softwareId">軟體的ID</param>
        /// <param name="serverDirectory">伺服器路徑</param>
        /// <returns>建立好的伺服器</returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        public static Server? CreateServer(string softwareId, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(softwareId, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return CreateServerCore(software, serverDirectory);
        }


        /// <summary>        
        /// 載入位於指定路徑內的伺服器
        /// </summary>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"/>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(string serverDirectory)
        {
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            JsonPropertyFile serverInformation = new JsonPropertyFile(path, useFileWatcher: false);
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(serverInformation["software"]?.GetValue<string>(), throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            return LoadServerCore(software, serverDirectory, serverInformation);
        }

        /// <summary>
        /// 載入位於指定路徑內的伺服器，並將其指定為 <paramref name="softwareId"/> 所對應的伺服器軟體
        /// </summary>
        /// <param name="softwareId">伺服器軟體 ID</param>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(string softwareId, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(softwareId, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            return LoadServerCore(software, serverDirectory, new JsonPropertyFile(path, useFileWatcher: false));
        }

        /// <summary>
        /// 載入位於指定路徑內的伺服器，並將其指定為 <paramref name="serverType"/> 所對應的伺服器軟體
        /// </summary>
        /// <param name="serverType">伺服器類型，需繼承 <see cref="Server"/> 並使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</param>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static Server? LoadServer(Type serverType, string serverDirectory)
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(serverType, throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            return LoadServerCore(software, serverDirectory, new JsonPropertyFile(path, useFileWatcher: false));
        }

        /// <summary>
        /// 載入位於指定路徑內的伺服器，並將其指定為 <typeparamref name="T"/> 所對應的伺服器軟體
        /// </summary>
        /// <typeparam name="T">伺服器類型，需使用 <see cref="SoftwareRegister.TryRegisterServerSoftware(ISoftwareContext)"/> 註冊</typeparam>
        /// <param name="serverDirectory">伺服器資料夾路徑</param>
        /// <returns>指定的伺服器，若伺服器不存在則為 <see langword="null"/></returns>
        public static T? LoadServer<T>(string serverDirectory) where T : Server
        {
            ISoftwareContext? software = SoftwareRegister.GetSoftwareContext(typeof(T), throwExceptionIfNotRegistered: true);
            if (software is null)
                return null;
            string path = Path.Combine(serverDirectory, "./server_info.json");
            if (!File.Exists(path))
                return null;
            return LoadServerCoreTyped<T>(software, serverDirectory, new JsonPropertyFile(path, useFileWatcher: false));
        }

        /// <summary>
        /// 取得預設的伺服器名稱 (通常是伺服器資料夾的名字)
        /// </summary>
        /// <returns>預設的伺服器名稱</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDefaultServerName(Server server) => GetDefaultServerNameCore(server.ServerDirectory);
    }
}
