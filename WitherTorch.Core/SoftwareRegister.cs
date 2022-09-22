using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core
{
    public sealed class SoftwareRegister
    {
        internal static Dictionary<Type, string> registeredServerSoftwares = new Dictionary<Type, string>();
        public static Type[] RegisteredServerSoftwares => registeredServerSoftwares.Keys.ToArray();

        private static MethodInfo _genericMethodInfo;

        [Obsolete("此方法效率較慢，建議使用 RegisterServerSoftware<T>() 代替")]
        public static void RegisterServerSoftware(Type type)
        {
            if (type != null && type.IsSubclassOf(typeof(Server<>).MakeGenericType(type)))
            {
                if (_genericMethodInfo == null)
                    _genericMethodInfo = typeof(SoftwareRegister).GetMethod("RegisterServerSoftware", BindingFlags.Static | BindingFlags.Public, Type.DefaultBinder, Type.EmptyTypes, null);
                _genericMethodInfo.MakeGenericMethod(type).Invoke(null, null);
            }
        }

        public static void RegisterServerSoftware<T>() where T : Server<T>, new()
        {
            if (Server<T>.isNeedInitialize)
            {
                new T().Dispose();
            }
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
                registeredServerSoftwares.Add(typeof(T), softwareID);
            }
        }
        internal static Type GetSoftwareFromID(string id)
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
    }
}
