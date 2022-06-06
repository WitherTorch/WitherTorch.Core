using System;
using System.Collections.Generic;
using System.Linq;
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
                        if ((server = Activator.CreateInstance(software) as Server) != null)
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
}
