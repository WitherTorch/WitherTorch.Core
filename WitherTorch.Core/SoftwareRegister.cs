using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using WitherTorch.Core.Software;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core
{
    /// <summary>
    /// 伺服器軟體的註冊工具，此類別是靜態類別
    /// </summary>
    public static class SoftwareRegister
    {
        private static readonly Dictionary<Type, ISoftwareContext> _serverTypeDict = new();
        private static readonly Dictionary<string, ISoftwareContext> _softwareIdDict = new();

        /// <summary>
        /// 已註冊的伺服器物件類別列表
        /// </summary>
        public static Type[] RegisteredServerTypes => _serverTypeDict.Keys.ToArray();
        /// <summary>
        /// 已註冊的伺服器軟體 ID 列表
        /// </summary>
        public static string[] RegisteredSoftwareIds => _softwareIdDict.Keys.ToArray();

        /// <summary>
        /// 註冊伺服器軟體
        /// </summary>
        /// <param name="software">與特定伺服器軟體相關聯的物件</param>
        public static bool TryRegisterServerSoftware(ISoftwareContext software)
            => TryRegisterServerSoftwareAsync(software).Result;

        /// <summary>
        /// 註冊伺服器軟體
        /// </summary>
        /// <param name="software">與特定伺服器軟體相關聯的物件</param>
        /// <remarks>備註: 此方法可能會因為伺服器軟體的初始化出現異常而拋出相對應之異常</remarks>
        public static async Task<bool> TryRegisterServerSoftwareAsync(ISoftwareContext software)
        {
            Type serverType = software.GetServerType();
            if (serverType.IsAbstract || !typeof(Server).IsAssignableFrom(serverType))
                return false;
            string softwareId = software.GetSoftwareId();
            if (string.IsNullOrWhiteSpace(softwareId))
                return false;

            TimeSpan timeout = WTCore.RegisterSoftwareTimeout;

            bool result;

            if (timeout == Timeout.InfiniteTimeSpan)
                result = await TryRegisterServerSoftwareCoreAsync_NoTimeout(software);
            else
                result = await TryRegisterServerSoftwareCoreAsync_WithTimeout(software, timeout);

            if (!result)
                return false;

            Dictionary<Type, ISoftwareContext> serverTypeDict = _serverTypeDict;
            Dictionary<string, ISoftwareContext> softwareIdDict = _softwareIdDict;
            lock (serverTypeDict)
            {
                if (!serverTypeDict.TryAdd(serverType, software))
                    return false;
            }
            lock (softwareIdDict)
            {
                if (!softwareIdDict.TryAdd(softwareId, software))
                    return false;
            }

            return true;
        }

        private static Task<bool> TryRegisterServerSoftwareCoreAsync_NoTimeout(ISoftwareContext factory)
            => factory.TryInitializeAsync(CancellationToken.None);

        private static Task<bool> TryRegisterServerSoftwareCoreAsync_WithTimeout(ISoftwareContext factory, TimeSpan timeout)
            => TaskHelper.WaitForResultAsync(factory.TryInitializeAsync, timeout);

        /// <summary>
        /// 取得與伺服器軟體 ID 對應的伺服器物件類型
        /// </summary>
        /// <param name="softwareId">伺服器軟體 ID</param>
        /// <returns>對應的伺服器類型，或是 <see langword="null"/></returns>
        public static Type? GetServerTypeFromSoftwareId(string? softwareId)
            => GetSoftwareContext(softwareId, throwExceptionIfNotRegistered: false)?.GetType();

        /// <summary>
        /// 取得與該伺服器物件類型相對應的伺服器軟體 ID
        /// </summary>
        /// <param name="serverType">伺服器物件的類型</param>
        /// <returns>對應的軟體 ID，或是 <see langword="null"/></returns>
        public static string? GetSoftwareIdFromServerType(Type? serverType)
            => GetSoftwareContext(serverType, throwExceptionIfNotRegistered: false)?.GetSoftwareId();

        /// <summary>
        /// 取得與軟體 ID 相對應的 <see cref="ISoftwareContext"/> 物件
        /// </summary>
        /// <param name="softwareId">伺服器軟體 ID</param>
        /// <param name="throwExceptionIfNotRegistered">是否在伺服器軟體 ID 符合格式但未註冊時擲回 <see cref="ServerSoftwareIsNotRegisteredException"/></param>
        /// <returns></returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"></exception>
        public static ISoftwareContext? GetSoftwareContext(string? softwareId, bool throwExceptionIfNotRegistered = false)
        {
            if (softwareId is null || string.IsNullOrWhiteSpace(softwareId))
                return null;
            Dictionary<string, ISoftwareContext> softwareIdDict = _softwareIdDict;
            ISoftwareContext? software;
            lock (softwareIdDict)
            {
                software = softwareIdDict.TryGetValue(softwareId, out ISoftwareContext? _factory) ? _factory : null;
            }
            if (software is null && throwExceptionIfNotRegistered)
                throw new ServerSoftwareIsNotRegisteredException(softwareId);
            return software;
        }

        /// <summary>
        /// 取得與該伺服器物件類型相對應的 <see cref="ISoftwareContext"/> 物件
        /// </summary>
        /// <param name="serverType">伺服器物件的類型</param>
        /// <param name="throwExceptionIfNotRegistered">是否在伺服器物件類型符合要求但未註冊時擲回 <see cref="ServerSoftwareIsNotRegisteredException"/></param>
        /// <returns></returns>
        /// <exception cref="ServerSoftwareIsNotRegisteredException"></exception>
        public static ISoftwareContext? GetSoftwareContext(Type? serverType, bool throwExceptionIfNotRegistered = false)
        {
            if (serverType is null || serverType.IsAbstract || !typeof(Server).IsAssignableFrom(serverType))
                return null;
            Dictionary<Type, ISoftwareContext> serverTypeDict = _serverTypeDict;
            ISoftwareContext? software;
            lock (serverTypeDict)
            {
                software = serverTypeDict.TryGetValue(serverType, out ISoftwareContext? _factory) ? _factory : null;
            }
            if (software is null && throwExceptionIfNotRegistered)
                throw new ServerSoftwareIsNotRegisteredException(serverType);
            return software;
        }
    }
}
