using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace WitherTorch.Core.Runtime
{
    /// <summary>
    /// 工具類別，可用於製造 <see cref="ITempFileInfo"/> 物件
    /// </summary>
    public static class TempFileInfo
    {
        /// <summary>
        /// 取得用於 <see cref="Create"/> 方法內的暫存檔案路徑生成器
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<string> CreateTempFilenameGenerator()
            => CreateTempFilenameGenerator(
                baseDirectory: Path.GetTempPath(),
                seed: unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                filenameFormat: "wt_temp_{0}.tmp");

        /// <summary>
        /// 建立一個暫存檔案路徑生成器，類似於用在 <see cref="Create"/> 內的版本
        /// </summary>
        /// <param name="baseDirectory">暫存檔案的基底路徑</param>
        /// <param name="seed">要用於建立暫存檔案的種子數字</param>
        /// <param name="filenameFormat">暫存檔案的名稱格式</param>
        public static IEnumerable<string> CreateTempFilenameGenerator(string baseDirectory, ulong seed, string filenameFormat)
        {
            do
            {
                yield return Path.Combine(baseDirectory, "./" + string.Format(filenameFormat, seed.ToString($"x{sizeof(ulong) * 2}")));
                seed++;
            } while (true);
        }

        /// <summary>
        /// 建立一個新的 <see cref="ITempFileInfo"/> 物件
        /// </summary>
        public static ITempFileInfo Create()
        {
            foreach (string filename in CreateTempFilenameGenerator())
            {
                FileStream stream;
                try
                {
                    stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 16,
                        FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.Asynchronous);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (IOException)
                {
                    continue;
                }
                return new DefaultTempFileInfoImpl(filename, stream);
            }
            throw new InvalidOperationException(); // 不可能發生的事情
        }

        private sealed class DefaultTempFileInfoImpl : ITempFileInfo
        {
            private readonly FileStream _stream;
            private readonly string _name, _fullName;

            public DefaultTempFileInfoImpl(string filename, FileStream stream)
            {
                _stream = stream;
                _name = Path.GetFileName(filename);
                _fullName = Path.GetFullPath(filename);
            }

            public string Name => _name;

            public string FullName => _fullName;

            public Stream Open(FileAccess access, int bufferSize, bool useAsync)
                => new FileStream(_fullName, FileMode.Open, access, FileShare.ReadWrite | FileShare.Delete, bufferSize, useAsync);

            public void Dispose()
            {
                _stream.Dispose();
                File.Delete(_fullName);
            }
        }
    }
}
