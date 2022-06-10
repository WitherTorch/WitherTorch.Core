#if NET5_0_OR_GREATER
using System;
using System.IO;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal static class InstallUtils
    {
        public static async Task HttpDownload(System.Net.Http.HttpClient client, string downloadURL, string filePath)
        {
            Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL));
            FileStream fileStream = new FileStream(Path.Combine(), FileMode.Create);
            byte[] buffer = new byte[1 << 20];
            while (true)
            {
                int length = dataStream.Read(buffer, 0, buffer.Length);
                if (length > 0)
                {
                    fileStream.Write(buffer, 0, length);
                }
                else
                {
                    break;
                }
            }
            dataStream.Close();
            fileStream.Close();
            await dataStream.DisposeAsync();
            await fileStream.DisposeAsync();
        }
    }
}
#endif
