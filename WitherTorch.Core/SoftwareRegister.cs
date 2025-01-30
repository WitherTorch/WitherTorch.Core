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
                result = TryRegisterServerSoftwareCore_NoTimeout(software);
            else
                result = TryRegisterServerSoftwareCore_WithTimeout(software, timeout);

            if (!result)
                return false;

            Dictionary<Type, ISoftwareContext> serverTypeDict = _serverTypeDict;
            Dictionary<string, ISoftwareContext> softwareIdDict = _softwareIdDict;
            lock (serverTypeDict)
            {
#if NET5_0_OR_GREATER
                if (!serverTypeDict.TryAdd(serverType, software))
                    return false;
#else
                if (serverTypeDict.ContainsKey(serverType))
                    return false;
                serverTypeDict.Add(serverType, software);
#endif
            }
            lock (softwareIdDict)
            {
#if NET5_0_OR_GREATER
                if (!softwareIdDict.TryAdd(softwareId, software))
                    return false;
#else
                if (softwareIdDict.ContainsKey(softwareId))
                    return false;
                softwareIdDict.Add(softwareId, software);
#endif
            }

            return true;
        }

        private static bool TryRegisterServerSoftwareCore_NoTimeout(ISoftwareContext factory)
        {
            using Task<bool> task = Task.Run(factory.TryInitialize);
            try
            {
                task.Wait();
            }
            catch (Exception)
            {
                return false;
            }
            if (task.Exception is null)
                return task.Result;
            return false;
        }

        private static bool TryRegisterServerSoftwareCore_WithTimeout(ISoftwareContext factory, TimeSpan timeout)
        {
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            using Task<bool> task = Task.Run(factory.TryInitialize, tokenSource.Token);
            bool result;
            try
            {
                result = task.Wait(timeout);
            }
            catch (Exception)
            {
                return false;
            }
            if (!result)
            {
                try
                {
                    tokenSource.Cancel(throwOnFirstException: true);
                }
                catch (Exception)
                {
                }
                return false;
            }
            if (task.Exception is null)
                return task.Result;
            return false;
        }

        /// <summary>
        /// 取得與伺服器軟體 ID 對應的伺服器物件類型
        /// </summary>
        /// <param name="softwareId">伺服器軟體 ID</param>
        /// <returns>對應的伺服器類型，或是 <see langword="null"/></returns>
        public static Type? GetServerTypeFromSoftwareId(string? softwareId)
        {
            if (string.IsNullOrWhiteSpace(softwareId)) 
                return null;
            Dictionary<string, ISoftwareContext> softwareIdDict = _softwareIdDict;
            lock (softwareIdDict)
            {
                return softwareIdDict.TryGetValue(ObjectUtils.ThrowIfNull(softwareId), out ISoftwareContext? software) ? software.GetServerType() : null;
            }
        }

        /// <summary>
        /// 取得與該伺服器物件類型相對應的伺服器軟體 ID
        /// </summary>
        /// <param name="softwareId">伺服器物件類型</param>
        /// <returns>對應的軟體 ID，或是 <see langword="null"/></returns>
        public static string? GetSoftwareIdFromServerType(Type? serverType)
        {
            if (serverType is null || serverType.IsAbstract || !typeof(Server).IsAssignableFrom(serverType))
                return null;
            Dictionary<Type, ISoftwareContext> serverTypeDict = _serverTypeDict;
            if (serverType is null)
                return null;
            lock (serverTypeDict)
            {
                return serverTypeDict.TryGetValue(ObjectUtils.ThrowIfNull(serverType), out ISoftwareContext? software) ? software.GetSoftwareId() : null;
            }
        }

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
        /// <param name="softwareId">該伺服器物件類型</param>
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
