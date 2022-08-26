#if NET5_0_OR_GREATER
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal static class InstallUtils
    {
        internal const int BUFFER_SIZE = 81920;

        public static async Task HttpDownload(System.Net.Http.HttpClient client, string downloadURL, string filePath, StrongBox<bool> stopFlag)
        {
            using Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL));
            using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            byte[] buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = dataStream.Read(buffer, 0, BUFFER_SIZE)) > 0 && !stopFlag.Value)
            {
                fileStream.Write(buffer, 0, length);
                fileStream.Flush();
            }
            dataStream.Close();
            fileStream.Close();
        }
    }
}
#endif
