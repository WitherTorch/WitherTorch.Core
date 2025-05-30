﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    internal readonly struct CacheStorageData
    {
        public DateTimeOffset ExpiredTime { get; }

        public string FileName { get; }

        public string FileFullName { get; }

        public CacheStorageData(string fileName, string fileFullName)
        {
            ExpiredTime = DateTimeOffset.UtcNow + WTCore.CacheFileTTL;
            FileName = fileName;
            FileFullName = fileFullName;
        }

        public CacheStorageData(long unixEpoch, string fileName, string fileFullName)
        {
            try
            {
                ExpiredTime = DateTimeOffset.FromUnixTimeMilliseconds(unixEpoch);
            }
            catch (Exception)
            {
                ExpiredTime = default;
            }
            FileName = fileName;
            FileFullName = fileFullName;
        }

        public CacheStorageData(DateTimeOffset expiredTime, string fileName, string fileFullName)
        {
            ExpiredTime = expiredTime;
            FileName = fileName;
            FileFullName = fileFullName;
        }

        public CacheStorageData ResetExpiredTime()
            => new CacheStorageData(FileName, FileFullName);
    }

    internal sealed class CacheStorage : IDisposable
    {
        private readonly Dictionary<string, CacheStorageData> _dict;
        private readonly string _dirName;
        private readonly string _fileName;

        private long _predictedSaveTime;
        private bool _disposed;

        public CacheStorage(string filename)
        {
            filename = Path.GetFullPath(filename);
            _fileName = filename;
            string dirName = ObjectUtils.ThrowIfNull(Path.GetDirectoryName(filename));
            if (!File.Exists(filename))
            {
                _dict = new Dictionary<string, CacheStorageData>();
                _dirName = dirName;
                return;
            }
            JsonNode? node;
            {
                FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    node = JsonNode.Parse(stream);
                }
                catch (Exception)
                {
                    node = null;
                }
                finally
                {
                    stream.Dispose();
                }
            }
            if (node is not JsonObject rootNode)
            {
                _dict = new Dictionary<string, CacheStorageData>();
                _dirName = dirName;
                return;
            }
            Dictionary<string, CacheStorageData> dict = new Dictionary<string, CacheStorageData>(rootNode.Count);
            foreach (KeyValuePair<string, JsonNode?> prop in rootNode)
            {
                if (prop.Value is not JsonObject itemNode ||
                    itemNode["expiredTime"] is not JsonValue expiredTimeNode ||
                    expiredTimeNode.GetValueKind() != JsonValueKind.Number ||
                    itemNode["value"] is not JsonValue valueNode ||
                    valueNode.GetValueKind() != JsonValueKind.String)
                    continue;
                string value = valueNode.GetValue<string>();
                dict.Add(prop.Key, new CacheStorageData(
                   unixEpoch: expiredTimeNode.GetValue<long>(),
                   fileName: value,
                   fileFullName: Path.Combine(dirName, "./", value)));
            }
            _dirName = dirName;
            _dict = dict;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<string?> GetStorageDataOrTryRenewAsync(string key, Func<string, CancellationToken, Task<string?>> renewFactory)
            => GetStorageDataOrTryRenewAsync(key, renewFactory, CancellationToken.None);

        public async Task<string?> GetStorageDataOrTryRenewAsync(string key, Func<string, CancellationToken, Task<string?>> renewFactory, CancellationToken cancellationToken)
        {
            Dictionary<string, CacheStorageData> dict = _dict;

            key = Uri.EscapeDataString(key);

            CacheStorageData data;

            lock (dict)
            {
                if (!dict.TryGetValue(key, out data))
                    data = default;
            }

            string? result;
            if (data.ExpiredTime != default)
            {
                if ((result = GetStorageDataOrNull(data, checkExpired: true)) is not null)
                    return result;
            }

            try
            {
                result = await renewFactory.Invoke(key, cancellationToken);
            }
            catch (Exception)
            {
                result = null;
            }

            if (result is not null)
            {
                if (data.ExpiredTime == default)
                    data = GenerateStorageData();
                else
                    data = data.ResetExpiredTime();
#if NETSTANDARD2_0
                File.WriteAllText(data.FileFullName, result, Encoding.UTF8);
#else
                await File.WriteAllTextAsync(data.FileFullName, result, Encoding.UTF8, cancellationToken);
#endif
                lock (dict)
                {
                    dict[key] = data;
                }
                Save(debounce: true);
                return result;
            }

            return GetStorageDataOrNull(data, checkExpired: false);
        }

        public void Save(bool debounce)
        {
            if (!debounce)
            {
                Interlocked.Exchange(ref _predictedSaveTime, 0L);
                SaveCore();
                return;
            }
            long predictedSaveTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1000;
            Interlocked.Exchange(ref _predictedSaveTime, predictedSaveTime);
            Task.Delay(1000).ContinueWith(_ =>
            {
                if (Interlocked.CompareExchange(ref _predictedSaveTime, 0L, predictedSaveTime) != predictedSaveTime)
                    return;

                Thread.MemoryBarrier();
                SaveCore();
            });
        }

        private void SaveCore()
        {
            Dictionary<string, CacheStorageData> dict = _dict;

            using FileStream stream = new FileStream(_fileName, FileMode.Create, FileAccess.Write, FileShare.Read);
            using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });

            writer.WriteStartObject();

            if (dict.Count > 0)
            {
                lock (dict)
                {
                    foreach (KeyValuePair<string, CacheStorageData> entry in dict)
                    {
                        writer.WriteStartObject(entry.Key);

                        CacheStorageData data = entry.Value;

                        writer.WriteNumber("expiredTime", data.ExpiredTime.ToUnixTimeMilliseconds());
                        writer.WriteString("value", data.FileName);

                        writer.WriteEndObject();
                    }
                }
                writer.Flush();
            }

            writer.WriteEndObject();
            writer.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetStorageDataOrNull(in CacheStorageData data, bool checkExpired)
        {
            if (checkExpired)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (data.ExpiredTime < now)
                    return null;
            }
            string path = data.FileFullName;
            if (!File.Exists(path))
                return null;
            return File.ReadAllText(path, Encoding.UTF8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CacheStorageData GenerateStorageData()
        {
            string dirName = _dirName;
            string name, fullName;
            do
            {
                name = Guid.NewGuid().ToString("N").ToLower();
                fullName = Path.Combine(dirName, "./", name);
            } while (File.Exists(fullName));

            return new CacheStorageData(
                fileName: name,
                fileFullName: fullName);
        }

        private void DisposeCore()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (Interlocked.Read(ref _predictedSaveTime) != 0L)
                SaveCore();
        }

        ~CacheStorage()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            DisposeCore();
        }

        public void Dispose()
        {
            // 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
            DisposeCore();
            GC.SuppressFinalize(this);
        }
    }
}
