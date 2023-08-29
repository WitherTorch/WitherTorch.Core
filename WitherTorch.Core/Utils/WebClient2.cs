using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WitherTorch.Core.Utils
{
    /// <summary>
    /// 基於 <see cref="HttpClient"/> 的類 WebClient 實現
    /// </summary>
    public class WebClient2 : HttpClient
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        #region Event Handlers
        public delegate void DownloadProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);
        public delegate void DownloadStringCompletedEventHandler(object sender, DownloadStringCompletedEventArgs e);
        public delegate void DownloadDataCompletedEventHandler(object sender, DownloadDataCompletedEventArgs e);
        public delegate void OpenReadCompletedEventHandler(object sender, OpenReadCompletedEventArgs e);
        #endregion

        #region Events
        /// <inheritdoc cref="WebClient.DownloadStringCompleted"/>
        public event DownloadStringCompletedEventHandler DownloadStringCompleted;

        /// <inheritdoc cref="WebClient.DownloadDataCompleted"/>
        public event DownloadDataCompletedEventHandler DownloadDataCompleted;

        /// <inheritdoc cref="WebClient.DownloadFileCompleted"/>
        public event AsyncCompletedEventHandler DownloadFileCompleted;

        /// <inheritdoc cref="WebClient.OpenReadCompleted"/>
        public event OpenReadCompletedEventHandler OpenReadCompleted;

        /// <inheritdoc cref="WebClient.DownloadProgressChanged"/>
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        #endregion

        #region Constructors
        /// <inheritdoc cref="HttpClient()"/>
        public WebClient2() : base() { }

        /// <inheritdoc cref="HttpClient(HttpMessageHandler)"/>
        public WebClient2(HttpMessageHandler handler) : base(handler) { }

        /// <inheritdoc cref="HttpClient(HttpMessageHandler, bool)"/>
        public WebClient2(HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler) { }
        #endregion

        /// <inheritdoc cref="WebClient.CancelAsync()"/>
        public void CancelAsync()
        {
            CancelPendingRequests();
            CancellationTokenSource tokenSource = this.tokenSource;
            if (tokenSource is object)
            {
                bool disposed = false;
                try
                {
                    tokenSource.Cancel(true);
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                }
                catch (AggregateException)
                {
                    //Do nothing
                }
                if (!disposed)
                    tokenSource.Dispose();
            }
            this.tokenSource = new CancellationTokenSource();
        }

        #region Download String Functions
        /// <inheritdoc cref="WebClient.DownloadString(string)"/>
        /// <exception cref="HttpRequestException"/>
        public string DownloadString(string address) => GetStringAsync(address).Result;

        /// <inheritdoc cref="WebClient.DownloadString(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public string DownloadString(Uri address) => GetStringAsync(address).Result;

        /// <inheritdoc cref="WebClient.DownloadStringTaskAsync(string)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<string> DownloadStringTaskAsync(string address) => GetStringAsync(address);

        /// <inheritdoc cref="WebClient.DownloadStringTaskAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<string> DownloadStringTaskAsync(Uri address) => GetStringAsync(address);

        /// <inheritdoc cref="WebClient.DownloadStringAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadStringAsync(Uri address) => DownloadStringAsync(address, null);

        /// <inheritdoc cref="WebClient.DownloadStringAsync(Uri, object)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadStringAsync(Uri address, object userToken)
        {
            CancellationToken cancellationToken = tokenSource.Token;
            Task.Factory.StartNew((token) =>
            {
                DownloadStringCompletedEventArgs eventArgs;
                try
                {
                    using (HttpResponseMessage response = GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                using (Stream contentStream = content.ReadAsStreamAsync().Result)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    MemoryStream memoryStream;
                                    long length = content.Headers.ContentLength ?? -1;
                                    if (length < 0)
                                        memoryStream = new MemoryStream();
                                    else
                                        memoryStream = new MemoryStream(length <= int.MaxValue ? (int)length : int.MaxValue);
                                    using (memoryStream)
                                    {
                                        DownloadBits(contentStream, memoryStream, length, token, cancellationToken);
                                        contentStream.Close();
                                        memoryStream.Position = 0;
                                        using (StreamReader reader = new StreamReader(memoryStream))
                                            eventArgs = new DownloadStringCompletedEventArgs(reader.ReadToEnd(), null, false, token);
                                    }
                                }
                            }
                        }
                        else
                            eventArgs = new DownloadStringCompletedEventArgs(string.Empty, null, false, token);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    eventArgs = new DownloadStringCompletedEventArgs(null, ex, true, token);
                }
                catch (Exception ex)
                {
                    eventArgs = new DownloadStringCompletedEventArgs(null, ex, false, token);
                }
                OnDownloadStringCompleted(eventArgs);
            }, userToken, cancellationToken);
        }
        #endregion

        #region Download Data Functions
        /// <inheritdoc cref="WebClient.DownloadData(string)"/>
        /// <exception cref="HttpRequestException"/>
        public byte[] DownloadData(string address) => GetByteArrayAsync(address).Result;

        /// <inheritdoc cref="WebClient.DownloadData(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public byte[] DownloadData(Uri address) => GetByteArrayAsync(address).Result;

        /// <inheritdoc cref="WebClient.DownloadDataTaskAsync(string)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<byte[]> DownloadDataTaskAsync(string address) => GetByteArrayAsync(address);

        /// <inheritdoc cref="WebClient.DownloadDataTaskAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<byte[]> DownloadDataTaskAsync(Uri address) => GetByteArrayAsync(address);

        /// <inheritdoc cref="WebClient.DownloadDataAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadDataAsync(Uri address) => DownloadDataAsync(address, null);

        /// <inheritdoc cref="WebClient.DownloadDataAsync(Uri, object)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadDataAsync(Uri address, object userToken)
        {
            CancellationToken cancellationToken = tokenSource.Token;
            Task.Factory.StartNew((token) =>
            {
                DownloadDataCompletedEventArgs eventArgs;
                try
                {
                    using (HttpResponseMessage response = GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                using (Stream contentStream = content.ReadAsStreamAsync().Result)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    MemoryStream memoryStream;
                                    long length = content.Headers.ContentLength ?? -1;
                                    if (length < 0)
                                        memoryStream = new MemoryStream();
                                    else
                                        memoryStream = new MemoryStream(length <= int.MaxValue ? (int)length : int.MaxValue);
                                    using (memoryStream)
                                    {
                                        DownloadBits(contentStream, memoryStream, length, token, cancellationToken);
                                        contentStream.Close();
                                        memoryStream.Position = 0;
                                        eventArgs = new DownloadDataCompletedEventArgs(memoryStream.ToArray(), null, false, token);
                                    }
                                }
                            }
                        }
                        else
                            eventArgs = new DownloadDataCompletedEventArgs(Array.Empty<byte>(), null, false, token);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    eventArgs = new DownloadDataCompletedEventArgs(null, ex, true, token);
                }
                catch (Exception ex)
                {
                    eventArgs = new DownloadDataCompletedEventArgs(null, ex, false, token);
                }
                OnDownloadDataCompleted(eventArgs);
            }, userToken, cancellationToken);
        }
        #endregion

        #region Download File Functions
        /// <inheritdoc cref="WebClient.DownloadFile(string, string)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadFile(string address, string fileName) => DownloadFileTaskAsync(new Uri(address), fileName, tokenSource.Token).Wait();

        /// <inheritdoc cref="WebClient.DownloadFile(Uri, string)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadFile(Uri address, string fileName) => DownloadFileTaskAsync(address, fileName, tokenSource.Token).Wait();

        /// <inheritdoc cref="WebClient.DownloadFileTaskAsync(string, string)"/>
        /// <exception cref="HttpRequestException"/>
        public Task DownloadFileTaskAsync(string address, string fileName) => DownloadFileTaskAsync(new Uri(address), fileName, tokenSource.Token);

        /// <inheritdoc cref="WebClient.DownloadFileTaskAsync(Uri, string)"/>
        /// <exception cref="HttpRequestException"/>
        public Task DownloadFileTaskAsync(Uri address, string fileName) => DownloadFileTaskAsync(address, fileName, tokenSource.Token);

        private async Task DownloadFileTaskAsync(Uri address, string fileName, CancellationToken token)
        {
            using (HttpResponseMessage response = (await GetAsync(address, HttpCompletionOption.ResponseContentRead, token)).EnsureSuccessStatusCode())
            using (HttpContent content = response.Content)
            using (Stream contentStream = await content.ReadAsStreamAsync())
            using (FileStream fileStream = new FileStream(fileName, FileMode.Truncate, FileAccess.Write, FileShare.Read, 4096, true))
            {
                fileStream.Position = 0;
                await contentStream.CopyToAsync(fileStream, Environment.SystemPageSize, token);
                contentStream.Close();
                fileStream.Close();
            }
        }

        /// <inheritdoc cref="WebClient.DownloadFileAsync(Uri, string)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadFileAsync(Uri address, string fileName) => DownloadFileAsync(address, fileName, null);

        /// <inheritdoc cref="WebClient.DownloadFileAsync(Uri, string, object)"/>
        /// <exception cref="HttpRequestException"/>
        public void DownloadFileAsync(Uri address, string fileName, object userToken)
        {
            CancellationToken cancellationToken = tokenSource.Token;
            Task.Factory.StartNew((token) =>
            {
                AsyncCompletedEventArgs eventArgs;
                try
                {
                    using (HttpResponseMessage response = GetAsync(address, HttpCompletionOption.ResponseHeadersRead, cancellationToken).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                using (Stream contentStream = content.ReadAsStreamAsync().Result)
                                using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    fileStream.Position = 0;
                                    long length = content.Headers.ContentLength ?? -1;
                                    DownloadBits(contentStream, fileStream, length, token, cancellationToken);
                                    contentStream.Close();
                                    fileStream.Close();
                                }
                            }
                        }
                        eventArgs = new AsyncCompletedEventArgs(null, false, token);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    eventArgs = new AsyncCompletedEventArgs(ex, true, token);
                }
                catch (Exception ex)
                {
                    eventArgs = new AsyncCompletedEventArgs(ex, false, token);
                }
                OnDownloadFileCompleted(eventArgs);
            }, userToken, cancellationToken);
        }
        #endregion

        #region Open Read Functions
        /// <inheritdoc cref="WebClient.OpenRead(string)"/>
        /// <exception cref="HttpRequestException"/>
        public Stream OpenRead(string address) => GetStreamAsync(address).Result;

        /// <inheritdoc cref="WebClient.OpenRead(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public Stream OpenRead(Uri address) => GetStreamAsync(address).Result;

        /// <inheritdoc cref="WebClient.OpenReadTaskAsync(string)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<Stream> OpenReadTaskAsync(string address) => GetStreamAsync(address);

        /// <inheritdoc cref="WebClient.OpenReadTaskAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public Task<Stream> OpenReadTaskAsync(Uri address) => GetStreamAsync(address);

        /// <inheritdoc cref="WebClient.OpenReadAsync(Uri)"/>
        /// <exception cref="HttpRequestException"/>
        public void OpenReadAsync(Uri address) => OpenReadAsync(address, null);

        /// <inheritdoc cref="WebClient.OpenReadAsync(Uri, object)"/>
        /// <exception cref="HttpRequestException"/>
        public void OpenReadAsync(Uri address, object userToken)
        {
            Task.Factory.StartNew((token) =>
            {
                OpenReadCompletedEventArgs eventArgs;
                try
                {
                    using (HttpResponseMessage response = GetAsync(address, HttpCompletionOption.ResponseHeadersRead, tokenSource.Token).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (HttpContent content = response.Content)
                            {
                                try
                                {
                                    using (Stream contentStream = content.ReadAsStreamAsync().Result)
                                    {
                                        OnOpenReadCompleted(new OpenReadCompletedEventArgs(contentStream, null, false, token));
                                        eventArgs = null;
                                    }
                                }
                                catch (Exception)
                                {
                                    eventArgs = null;
                                }
                            }
                        }
                        else
                            eventArgs = new OpenReadCompletedEventArgs(null, null, false, token);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    eventArgs = new OpenReadCompletedEventArgs(null, ex, true, token);
                }
                catch (Exception ex)
                {
                    eventArgs = new OpenReadCompletedEventArgs(null, ex, false, token);
                }
                if (eventArgs is object)
                    OnOpenReadCompleted(eventArgs);
            }, userToken, tokenSource.Token);
        }
        #endregion

        #region Event Triggers
        /// <inheritdoc cref="WebClient.OnDownloadStringCompleted(System.Net.DownloadStringCompletedEventArgs)"/>
        protected virtual void OnDownloadStringCompleted(DownloadStringCompletedEventArgs args)
        {
            DownloadStringCompleted?.Invoke(this, args);
        }

        /// <inheritdoc cref="WebClient.OnDownloadDataCompleted(System.Net.DownloadDataCompletedEventArgs)"/>
        protected virtual void OnDownloadDataCompleted(DownloadDataCompletedEventArgs args)
        {
            DownloadDataCompleted?.Invoke(this, args);
        }

        /// <inheritdoc cref="WebClient.OnDownloadFileCompleted(AsyncCompletedEventArgs)"/>
        protected virtual void OnDownloadFileCompleted(AsyncCompletedEventArgs args)
        {
            DownloadFileCompleted?.Invoke(this, args);
        }

        /// <inheritdoc cref="WebClient.OnOpenReadCompleted(System.Net.OpenReadCompletedEventArgs)"/>
        protected virtual void OnOpenReadCompleted(OpenReadCompletedEventArgs args)
        {
            OpenReadCompleted?.Invoke(this, args);
        }

        /// <inheritdoc cref="WebClient.OnDownloadProgressChanged(System.Net.DownloadProgressChangedEventArgs)"/>
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs args)
        {
            DownloadProgressChanged?.Invoke(this, args);
        }
        #endregion

        #region Download Helpers
        private void DownloadBits(Stream contentStream, Stream downloadStream, long contentStreamLength, object token, CancellationToken cancellationToken)
        {
            int bufferSize = Environment.SystemPageSize;
            byte[] buffer = new byte[bufferSize];
            long position = 0;
            while (position < contentStreamLength || contentStreamLength < 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead = contentStream.Read(buffer, 0, bufferSize);
                if (bytesRead <= 0)
                    break;
                else
                {
                    downloadStream.Write(buffer, 0, bytesRead);
                    position += bytesRead;
                    int percentage;
                    if (contentStreamLength < 0) percentage = -1;
                    else if (position > long.MaxValue << 7)
                    {
                        long miniLength = contentStreamLength / 100;
                        if (miniLength > 0)
                            percentage = (int)(position / miniLength);
                        else
                            percentage = (int)Math.Floor(position * 1.0 / miniLength * 100.0);
                    }
                    else
                    {
                        percentage = (int)(position * 100 / contentStreamLength);
                    }
                    DownloadProgressChangedEventArgs progressChangedEventArgs = new DownloadProgressChangedEventArgs(percentage, token, position, contentStreamLength);
                    OnDownloadProgressChanged(progressChangedEventArgs);
                }
            }
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            CancellationTokenSource tokenSource = this.tokenSource;
            if (tokenSource is object)
            {
                bool disposed = false;
                try
                {
                    tokenSource.Cancel(true);
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                }
                catch (AggregateException)
                {
                    //Do nothing
                }
                if (!disposed)
                    tokenSource.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Alternate EventArgs
        public class DownloadProgressChangedEventArgs : ProgressChangedEventArgs
        {
            private long m_BytesReceived;

            private long m_TotalBytesToReceive;

            public long BytesReceived => m_BytesReceived;

            public long TotalBytesToReceive => m_TotalBytesToReceive;

            public DownloadProgressChangedEventArgs(int progressPercentage, object userToken, long bytesReceived, long totalBytesToReceive)
                : base(progressPercentage, userToken)
            {
                m_BytesReceived = bytesReceived;
                m_TotalBytesToReceive = totalBytesToReceive;
            }
        }

        public class DownloadStringCompletedEventArgs : AsyncCompletedEventArgs
        {
            private string m_Result;

            public string Result
            {
                get
                {
                    RaiseExceptionIfNecessary();
                    return m_Result;
                }
            }

            public DownloadStringCompletedEventArgs(string result, Exception exception, bool cancelled, object userToken)
                : base(exception, cancelled, userToken)
            {
                m_Result = result;
            }
        }

        public class DownloadDataCompletedEventArgs : AsyncCompletedEventArgs
        {
            private byte[] m_Result;

            public byte[] Result
            {
                get
                {
                    RaiseExceptionIfNecessary();
                    return m_Result;
                }
            }

            internal DownloadDataCompletedEventArgs(byte[] result, Exception exception, bool cancelled, object userToken)
                : base(exception, cancelled, userToken)
            {
                m_Result = result;
            }
        }

        public class OpenReadCompletedEventArgs : AsyncCompletedEventArgs
        {
            private Stream m_Result;

            public Stream Result
            {
                get
                {
                    RaiseExceptionIfNecessary();
                    return m_Result;
                }
            }

            public OpenReadCompletedEventArgs(Stream result, Exception exception, bool cancelled, object userToken)
                : base(exception, cancelled, userToken)
            {
                m_Result = result;
            }
        }
        #endregion
    }
}
