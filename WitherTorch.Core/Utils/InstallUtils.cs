#if NET5_0_OR_GREATER
using System;
using System.IO;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal static class InstallUtils
    {
        const int BUFFER_SIZE = 81920;

        public static async Task HttpDownload(System.Net.Http.HttpClient client, string downloadURL, string filePath)
        {
            Stream dataStream = await client.GetStreamAsync(new Uri(downloadURL));
            FileStream fileStream = new FileStream(Path.Combine(), FileMode.Create);
            byte[] buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = dataStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
            {
                fileStream.Write(buffer, 0, length);
                fileStream.Flush();
            }
            dataStream.Close();
            fileStream.Close();
            await dataStream.DisposeAsync();
            await fileStream.DisposeAsync();
        }
    }
}
#endif
