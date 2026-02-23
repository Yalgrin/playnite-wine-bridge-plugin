using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using WineBridgePlugin.Settings;

namespace WineBridgePlugin.Processes
{
    public class LinuxProcessOutputPipe : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public MemoryStream Output { get; } = new MemoryStream();

        public AnonymousPipeClientStream ReadStream { get; }
        public CancellationToken Token => TokenSource.Token;

        private CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();
        private AnonymousPipeServerStream WriteStream { get; }
        private string FilePath { get; }
        private int PollMs { get; }

        private Task _runTask;
        private bool _disposed;

        public LinuxProcessOutputPipe(string filePath, int pollMs)
        {
            WriteStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            ReadStream = new AnonymousPipeClientStream(PipeDirection.In, WriteStream.GetClientHandleAsString());
            Output = new MemoryStream();
            FilePath = filePath;
            PollMs = pollMs;
        }

        public void Start(CancellationToken processToken)
        {
            ThrowIfDisposed();

            if (_runTask != null)
            {
                throw new InvalidOperationException("LinuxProcessInputPipe has already been started.");
            }

            _runTask = Task.Run(async () =>
            {
                try
                {
                    var lastPosition = 0L;
                    var errorCount = 5;
                    var buffer = new byte[16 * 1024];
                    while (!processToken.IsCancellationRequested)
                    {
                        try
                        {
                            lastPosition = await LoadFromFileToStream(buffer, lastPosition);

                            try
                            {
                                await Task.Delay(PollMs, processToken);
                            }
                            catch (TaskCanceledException)
                            {
                                if (WineBridgeSettings.DebugLoggingEnabled)
                                {
                                    Logger.Debug($"Output pipe redirection cancelled for file {FilePath}");
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                if (WineBridgeSettings.DebugLoggingEnabled)
                                {
                                    Logger.Debug($"Output pipe redirection token disposed for file {FilePath}");
                                }
                            }
                        }
                        catch (IOException e)
                        {
                            Logger.Error(e, $"Failed to read from file {FilePath}.");

                            errorCount--;
                            if (errorCount <= 0)
                            {
                                throw;
                            }
                        }
                    }

                    if (WineBridgeSettings.DebugLoggingEnabled)
                    {
                        Logger.Debug(
                            $"Output pipe redirection cancelled, doing one final write operation to {FilePath}");
                    }

                    await LoadFromFileToStream(buffer, lastPosition);

                    if (WineBridgeSettings.DebugLoggingEnabled)
                    {
                        Logger.Debug($"Finished output pipe redirection to file {FilePath}!");
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to redirect the output stream to a Linux file!");
                }
                finally
                {
                    CloseStream(WriteStream);
                }
            });
        }

        private async Task<long> LoadFromFileToStream(byte[] buffer, long lastPosition)
        {
            using (
                var fs = new FileStream(
                    FilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 16 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                fs.Seek(lastPosition, SeekOrigin.Begin);

                await ReadAndWriteProcessOutput(fs, buffer);
                var position = fs.Position;
                return position > lastPosition ? position : lastPosition;
            }
        }

        private async Task<int> ReadAndWriteProcessOutput(FileStream fs, byte[] buffer)
        {
            var total = 0;
            int n;
            do
            {
                n = await fs.ReadAsync(buffer, 0, buffer.Length);
                if (n > 0)
                {
                    total += n;
                    await WriteStream.WriteAsync(buffer, 0, n);
                }
            } while (n > 0);

            return total;
        }

        public Task WaitForDone()
        {
            return _runTask ?? Task.CompletedTask;
        }

        private static void CloseStream(Stream stream)
        {
            try
            {
                stream?.Close();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to close stream!");
            }
        }

        public void Stop()
        {
            if (_disposed)
            {
                return;
            }

            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug($"Stop called for file {FilePath}.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LinuxProcessInputPipe));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }


            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug($"Dispose called for file {FilePath}.");
            }

            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}