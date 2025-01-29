using System;

namespace WitherTorch.Core.Software
{
    /// <summary>
    /// 包含與 <see cref="ISoftwareEntry"/> 相關的工具方法，此類別是靜態類別
    /// </summary>
    public static class SoftwareEntryHelper
    {
        /// <summary>
        /// 建立一個簡易的 <see cref="ISoftwareEntry"/> 物件
        /// </summary>
        /// <param name="softwareId">伺服器軟體的 ID</param>
        /// <param name="serverType">伺服器物件的類型，必須繼承自 <see cref="Server"/> 或其衍生類別且不可為抽象類別</param>
        /// <param name="versions">伺服器軟體的版本列表</param>
        /// <param name="createServerFactory">建立伺服器物件的工廠方法</param>
        /// <param name="initializer">註冊該 <see cref="ISoftwareEntry"/> 物件時所要執行的動作</param>
        /// <returns></returns>
        public static ISoftwareEntry CreateSoftwareEntry(string softwareId, Type serverType, string[]? versions, 
            Func<string, Server?> createServerFactory, Func<bool>? initializer = null)
        { 
            if (string.IsNullOrWhiteSpace(softwareId))
                throw new ArgumentException(nameof(softwareId) + " cannot be empty or full-whitespaced!");
            if (serverType.IsAbstract)
                throw new ArgumentException(nameof(serverType) + " cannot be an abstract class!");
            if (!typeof(Server).IsAssignableFrom(serverType))
                throw new ArgumentException(nameof(serverType) + " must inherit " + typeof(Server).FullName + " or a derivative class!");
            return new SimpleSoftwareEntry(softwareId, serverType, versions ?? Array.Empty<string>(), createServerFactory, initializer);
        }

        /// <summary>
        /// 建立一個簡易的 <see cref="ISoftwareEntry"/> 物件
        /// </summary>
        /// <param name="softwareId">伺服器軟體的 ID</param>
        /// <param name="serverType">伺服器物件的類型，必須繼承自 <see cref="Server"/> 或其衍生類別且不可為抽象類別</param>
        /// <param name="versionsFactory">伺服器軟體的版本列表的工廠方法</param>
        /// <param name="createServerFactory">建立伺服器物件的工廠方法</param>
        /// <param name="initializer">註冊該 <see cref="ISoftwareEntry"/> 物件時所要執行的動作</param>
        /// <returns></returns>
        public static ISoftwareEntry CreateSoftwareEntry(string softwareId, Type serverType, Func<string[]?> versionsFactory, 
            Func<string, Server?> createServerFactory, Func<bool>? initializer = null)
        { 
            if (string.IsNullOrWhiteSpace(softwareId))
                throw new ArgumentException(nameof(softwareId) + " cannot be empty or full-whitespaced!");
            if (serverType.IsAbstract)
                throw new ArgumentException(nameof(serverType) + " cannot be an abstract class!");
            if (!typeof(Server).IsAssignableFrom(serverType))
                throw new ArgumentException(nameof(serverType) + " must inherit " + typeof(Server).FullName + " or a derivative class!");
            return new SimpleSoftwareEntry(softwareId, serverType, versionsFactory, createServerFactory, initializer);
        }

        /// <summary>
        /// 建立一個簡易的 <see cref="ISoftwareEntry"/> 物件
        /// </summary>
        /// <typeparam name="T">伺服器物件的類型，此類型不可為抽象類別</typeparam>
        /// <param name="softwareId">伺服器軟體的 ID</param>
        /// <param name="versions">伺服器軟體的版本列表</param>
        /// <param name="createServerFactory">建立伺服器物件的工廠方法</param>
        /// <param name="initializer">註冊該 <see cref="ISoftwareEntry"/> 物件時所要執行的動作</param>
        /// <returns></returns>
        public static ISoftwareEntry CreateSoftwareEntry<T>(string softwareId, string[]? versions,
            Func<string, T?> createServerFactory, Func<bool>? initializer = null) where T : Server
        {
            if (string.IsNullOrWhiteSpace(softwareId))
                throw new ArgumentException(nameof(softwareId) + " cannot be empty or full-whitespaced!");
            if (typeof(T).IsAbstract)
                throw new ArgumentException(nameof(T) + " cannot be an abstract class!");
            return new GenericSimpleSoftwareEntry<T>(softwareId, versions ?? Array.Empty<string>(), createServerFactory, initializer);
        }

        /// <summary>
        /// 建立一個簡易的 <see cref="ISoftwareEntry"/> 物件
        /// </summary>
        /// <typeparam name="T">伺服器物件的類型，此類型不可為抽象類別</typeparam>
        /// <param name="softwareId">伺服器軟體的 ID</param>
        /// <param name="versionsFactory">伺服器軟體的版本列表的工廠方法</param>
        /// <param name="createServerFactory">建立伺服器物件的工廠方法</param>
        /// <param name="initializer">註冊該 <see cref="ISoftwareEntry"/> 物件時所要執行的動作</param>
        /// <returns></returns>
        public static ISoftwareEntry CreateSoftwareEntry<T>(string softwareId, Func<string[]?> versionsFactory,
            Func<string, T?> createServerFactory, Func<bool>? initializer = null) where T : Server
        { 
            if (string.IsNullOrWhiteSpace(softwareId))
                throw new ArgumentException(nameof(softwareId) + " cannot be empty or full-whitespaced!");
            if (typeof(T).IsAbstract)
                throw new ArgumentException(nameof(T) + " cannot be an abstract class!");
            return new GenericSimpleSoftwareEntry<T>(softwareId, versionsFactory, createServerFactory, initializer);
        }
    }
}
