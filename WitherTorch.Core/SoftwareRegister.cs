using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core
{
    public class SoftwareRegister
    {
        internal static Dictionary<Type, string> registeredServerSoftwares = new Dictionary<Type, string>();
        public static Type[] RegisteredServerSoftwares => registeredServerSoftwares.Keys.ToArray();
        public static void RegisterServerSoftware(Type software)
        {
            if (software != null && software.IsSubclassOf(typeof(Server)))
            {
                string SoftwareRun()
                {
                    string result = null;
                    Server server = null;
                    try
                    {
                        RegisterToken registerToken = new RegisterToken();
                        if ((server = Activator.CreateInstance(type: software,
                            bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.CreateInstance, binder: null,
                            args: new object[] { registerToken }, culture: null) as Server) != null && registerToken)
                            result = server.GetSoftwareID();
                    }
                    catch (Exception) { }
                    finally
                    {
                        server?.Dispose();
                    }
                    return result;
                }
                if (WTCore.RegisterSoftwareTimeout == Timeout.Infinite)
                {
                    string result = SoftwareRun();
                    registeredServerSoftwares.Add(software, result);
                }
                else
                {
                    using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                    {
                        Task<string> result = Task.Run(SoftwareRun);
                        if (result.Wait(WTCore.RegisterSoftwareTimeout, tokenSource.Token))
                        {
                            registeredServerSoftwares.Add(software, result.Result);
                        }
                        else
                        {
                            tokenSource.Cancel();
                        }
                    }
                }
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

    public class RegisterToken : StrongBox<bool>
    {
        internal protected RegisterToken()
        {
            Value = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            Value = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Value = true;
        }

        public static implicit operator bool(RegisterToken a)
        {
            return a.Value;
        }
    }
}
