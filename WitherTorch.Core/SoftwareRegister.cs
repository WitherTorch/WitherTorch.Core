using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core
{
    /// <summary>
    /// 伺服器軟體的註冊器
    /// </summary>
    public sealed class SoftwareRegister
    {
        private static readonly Dictionary<string, Type> _softwareDict = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> _softwareDictReversed = new Dictionary<Type, string>();

        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private static readonly Lazy<MethodInfo> _genericMethodInfo = new Lazy<MethodInfo>(()
            => typeof(SoftwareRegister).GetMethod(nameof(RegisterServerSoftware), BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, Type.EmptyTypes, null),
            true);

        public static Type[] RegisteredServerSoftwares => _softwareDict.Values.ToArray();

        /// <summary>
        /// 註冊伺服器軟體
        /// </summary>
        /// <param name="type">伺服器軟體的類別</param>
        public static void RegisterServerSoftware(Type type)
        {
            if (type is null || !type.IsSubclassOf(typeof(Server)) || !type.IsSubclassOf(typeof(Server<>).MakeGenericType(type)))
                return;
            _genericMethodInfo.Value.MakeGenericMethod(type).Invoke(null, null);
        }

        /// <summary>
        /// 註冊伺服器軟體
        /// </summary>
        /// <typeparam name="T">伺服器軟體的類別</typeparam>
        public static void RegisterServerSoftware<T>() where T : Server<T>, new()
        {
            Type type = typeof(T);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            string softwareID = Server<T>.SoftwareID;
            if (string.IsNullOrEmpty(softwareID))
                return;
            Action regDelegate = Server<T>.SoftwareRegistrationDelegate;
            if (regDelegate == null)
            {
                _lock.EnterWriteLock();
                _softwareDict[softwareID] = type;
                _softwareDictReversed[type] = softwareID;
                _lock.ExitWriteLock();
                return;
            }
            if (WTCore.RegisterSoftwareTimeout == Timeout.InfiniteTimeSpan)
            {
                try
                {
                    regDelegate();
                }
                catch (Exception)
                {
                    return;
                }
            }
            else
            {
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    Task result = Task.Run(regDelegate);
                    if (!result.Wait(unchecked((int)(WTCore.RegisterSoftwareTimeout.Ticks / TimeSpan.TicksPerMillisecond)), tokenSource.Token))
                    {
                        tokenSource.Cancel();
                        return;
                    }
                }
            }
            _lock.EnterWriteLock();
            _softwareDict[softwareID] = type;
            _softwareDictReversed[type] = softwareID;
            _lock.ExitWriteLock();
        }

        public static Type GetSoftwareTypeFromID(string id)
        {
            _lock.EnterReadLock();
            Type result = _softwareDict.TryGetValue(id, out Type type) ? type : null;
            _lock.ExitReadLock();
            return result;
        }

        public static string GetSoftwareIDFromType(Type type)
        {
            _lock.EnterReadLock();
            string result = _softwareDictReversed.TryGetValue(type, out string id) ? id : null;
            _lock.ExitReadLock();
            return result;
        }
    }
}
