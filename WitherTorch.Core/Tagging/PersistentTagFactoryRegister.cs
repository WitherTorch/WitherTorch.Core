using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using WitherTorch.Core.Software;
using WitherTorch.Core.Utils;

namespace WitherTorch.Core.Tagging
{
    /// <summary>
    /// 持久標籤類型之工廠的註冊工具，此類別是靜態類別
    /// </summary>
    public static class PersistentTagFactoryRegister
    {
        private static readonly Dictionary<Type, IPersistentTagFactory> _tagTypeDict = new();
        private static readonly Dictionary<string, IPersistentTagFactory> _tagTypeIdDict = new();

        /// <summary>
        /// 已註冊的伺服器物件類別列表
        /// </summary>
        public static Type[] RegisteredTagTypes => _tagTypeDict.Keys.ToArray();
        /// <summary>
        /// 已註冊的持久標籤類型 ID 列表
        /// </summary>
        public static string[] RegisteredTagTypeIds => _tagTypeIdDict.Keys.ToArray();

        /// <summary>
        /// 註冊持久標籤類型的工廠
        /// </summary>
        /// <param name="factory">與特定持久標籤類型相關聯的工廠物件</param>
        public static bool TryRegisterFactory(IPersistentTagFactory factory)
            => TryRegisterFactoryAsync(factory).Result;

        /// <summary>
        /// 註冊持久標籤類型的工廠
        /// </summary>
        /// <param name="factory">與特定持久標籤類型相關聯的工廠物件</param>
        /// <remarks>備註: 此方法可能會因為工廠物件的初始化出現異常而拋出相對應之異常</remarks>
        public static async Task<bool> TryRegisterFactoryAsync(IPersistentTagFactory factory)
        {
            Type serverType = factory.GetTagType();
            if (serverType.IsAbstract || !typeof(IPersistentTag).IsAssignableFrom(serverType))
                return false;
            string softwareId = factory.GetTagTypeId();
            if (string.IsNullOrWhiteSpace(softwareId))
                return false;

            TimeSpan timeout = WTCore.RegisterSoftwareTimeout;

            bool result;

            if (timeout == Timeout.InfiniteTimeSpan)
                result = await TryRegisterFactoryCoreAsync_NoTimeout(factory);
            else
                result = await TryRegisterFactoryCoreAsync_WithTimeout(factory, timeout);

            if (!result)
                return false;

            Dictionary<Type, IPersistentTagFactory> tagTypeDict = _tagTypeDict;
            Dictionary<string, IPersistentTagFactory> tagTypeIdDict = _tagTypeIdDict;
            lock (tagTypeDict)
            {
                if (!tagTypeDict.TryAdd(serverType, factory))
                    return false;
            }
            lock (tagTypeIdDict)
            {
                if (!tagTypeIdDict.TryAdd(softwareId, factory))
                    return false;
            }

            return true;
        }

        private static Task<bool> TryRegisterFactoryCoreAsync_NoTimeout(IPersistentTagFactory factory)
            => factory.TryInitializeAsync(CancellationToken.None);

        private static Task<bool> TryRegisterFactoryCoreAsync_WithTimeout(IPersistentTagFactory factory, TimeSpan timeout)
            => TaskHelper.WaitForResultAsync(factory.TryInitializeAsync, timeout);

        /// <summary>
        /// 取得與持久標籤類型 ID 對應的工廠類型
        /// </summary>
        /// <param name="typeId">持久標籤類型的唯一辨識字串</param>
        /// <returns>對應的伺服器類型，或是 <see langword="null"/></returns>
        public static Type? GetTagTypeFromTypeId(string? typeId)
            => GetFactory(typeId, throwExceptionIfNotRegistered: false)?.GetTagType();

        /// <summary>
        /// 取得與該工廠類型相對應的持久標籤類型 ID
        /// </summary>
        /// <param name="tagType">持久標籤的類型</param>
        /// <returns>對應的軟體 ID，或是 <see langword="null"/></returns>
        public static string? GetTypeIdFromTagType(Type? tagType)
            => GetFactory(tagType, throwExceptionIfNotRegistered: false)?.GetTagTypeId();

        /// <summary>
        /// 取得與軟體 ID 相對應的 <see cref="ISoftwareContext"/> 物件
        /// </summary>
        /// <param name="typeId">持久標籤類型 ID</param>
        /// <param name="throwExceptionIfNotRegistered">是否在持久標籤類型 ID 符合格式但未註冊時擲回 <see cref="ServerSoftwareIsNotRegisteredException"/></param>
        /// <returns></returns>
        /// <exception cref="PersistentTagFactoryIsNotRegisteredException"></exception>
        public static IPersistentTagFactory? GetFactory(string? typeId, bool throwExceptionIfNotRegistered = false)
        {
            if (typeId is null || string.IsNullOrWhiteSpace(typeId))
                return null;
            Dictionary<string, IPersistentTagFactory> tagIdDict = _tagTypeIdDict;
            IPersistentTagFactory? software;
            lock (tagIdDict)
            {
                software = tagIdDict.TryGetValue(typeId, out IPersistentTagFactory? _factory) ? _factory : null;
            }
            if (software is null && throwExceptionIfNotRegistered)
                throw new PersistentTagFactoryIsNotRegisteredException(typeId);
            return software;
        }

        /// <summary>
        /// 取得與該伺服器物件類型相對應的 <see cref="ISoftwareContext"/> 物件
        /// </summary>
        /// <param name="tagType">伺服器物件的類型</param>
        /// <param name="throwExceptionIfNotRegistered">是否在伺服器物件類型符合要求但未註冊時擲回 <see cref="ServerSoftwareIsNotRegisteredException"/></param>
        /// <returns></returns>
        /// <exception cref="PersistentTagFactoryIsNotRegisteredException"></exception>
        public static IPersistentTagFactory? GetFactory(Type? tagType, bool throwExceptionIfNotRegistered = false)
        {
            if (tagType is null || tagType.IsAbstract || !typeof(IPersistentTagFactory).IsAssignableFrom(tagType))
                return null;
            Dictionary<Type, IPersistentTagFactory> serverTypeDict = _tagTypeDict;
            IPersistentTagFactory? result;
            lock (serverTypeDict)
            {
                result = serverTypeDict.TryGetValue(tagType, out IPersistentTagFactory? _factory) ? _factory : null;
            }
            if (result is null && throwExceptionIfNotRegistered)
                throw new PersistentTagFactoryIsNotRegisteredException(tagType);
            return result;
        }
    }
}
