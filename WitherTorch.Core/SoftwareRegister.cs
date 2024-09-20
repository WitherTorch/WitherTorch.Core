using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core
{
    /// <summary>
    /// 伺服器軟體的註冊器
    /// </summary>
    public sealed class SoftwareRegister
    {
        internal static Dictionary<Type, string> registeredServerSoftwares = new Dictionary<Type, string>();
        public static Type[] RegisteredServerSoftwares => registeredServerSoftwares.Keys.ToArray();

        private static readonly Lazy<MethodInfo> _genericMethodInfo = new Lazy<MethodInfo>(() 
            => typeof(SoftwareRegister).GetMethod(nameof(RegisterServerSoftware), BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, Type.EmptyTypes, null), 
            true);

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
            Type t = typeof(T);
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
            string softwareID = Server<T>.SoftwareID;
            Action regDelegate = Server<T>.SoftwareRegistrationDelegate;
            if (!string.IsNullOrEmpty(softwareID))
            {
                if (regDelegate != null)
                {
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
                            if (!result.Wait((int)WTCore.RegisterSoftwareTimeout.TotalMilliseconds, tokenSource.Token))
                            {
                                tokenSource.Cancel();
                                return;
                            }
                        }
                    }
                }
                registeredServerSoftwares.Add(t, softwareID);
            }
        }

        public static Type GetSoftwareTypeFromID(string id)
        {
            foreach (KeyValuePair<Type, string> software in registeredServerSoftwares)
            {
                if (id == software.Value)
                {
                    return software.Key;
                }
            }
            return null;
        }

        public static string GetSoftwareIDFromType(Type type)
        {
            if (registeredServerSoftwares.TryGetValue(type, out string result))
                return result;
            return null;
        }
    }
}
