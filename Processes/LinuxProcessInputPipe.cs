using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using WineBridgePlugin.Settings;
using WineBridgePlugin.Utils;

namespace WineBridgePlugin.Processes
{
    public class LinuxProcessInputPipe : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        public AnonymousPipeServerStream WriteStream { get; }
        public CancellationToken Token => TokenSource.Token;

        private CancellationTokenSource TokenSource { get; } = new CancellationTokenSource();

        private AnonymousPipeClientStream ReadStream { get; }
        private string FifoPath { get; }

        private Task _runTask;
        private bool _disposed;

        public LinuxProcessInputPipe(string fifoPath)
        {
            WriteStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            ReadStream = new AnonymousPipeClientStream(PipeDirection.In, WriteStream.GetClientHandleAsString());

            FifoPath = fifoPath;
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
                    for (var i = 0; i < 100; i++)
                    {
                        processToken.ThrowIfCancellationRequested();

                        try
                        {
                            await RedirectReadStreamToFifo(processToken);

                            if (WineBridgeSettings.DebugLoggingEnabled)
                            {
                                Logger.Debug($"Finished input pipe redirection for FIFO {FifoPath}!");
                            }

                            return;
                        }
                        catch (FileNotFoundException)
                        {
                            if (WineBridgeSettings.DebugLoggingEnabled)
                            {
                                Logger.Warn($"FIFO file not found: {FifoPath}");
                            }

                            await Task.Delay(100, processToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Failed to redirect Linux FIFO to an input stream!");
                }
                finally
                {
                    TokenSource.Cancel();
                }
            });
        }

        private async Task RedirectReadStreamToFifo(CancellationToken cancellationToken)
        {
            using (var fs = WineFifo.OpenWriteStream(FifoPath))
            {
                try
                {
                    await ReadStream.CopyToAsync(fs, 16 * 1024, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    if (WineBridgeSettings.DebugLoggingEnabled)
                    {
                        Logger.Debug($"Operation cancelled for FIFO {FifoPath}.");
                    }
                }
                catch (ObjectDisposedException)
                {
                    if (WineBridgeSettings.DebugLoggingEnabled)
                    {
                        Logger.Debug($"Object disposed for FIFO {FifoPath}.");
                    }
                }
                catch (IOException e)
                {
                    if (WineBridgeSettings.DebugLoggingEnabled)
                    {
                        Logger.Debug(e, $"IO exception for FIFO {FifoPath}.");
                    }
                }
            }
        }

        public Task WaitForDone()
        {
            return _runTask ?? Task.CompletedTask;
        }

        public void Stop()
        {
            if (_disposed)
            {
                return;
            }

            if (WineBridgeSettings.DebugLoggingEnabled)
            {
                Logger.Debug($"Stop called for FIFO {FifoPath}.");
            }

            CloseStream(ReadStream);
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
                Logger.Debug($"Dispose called for FIFO {FifoPath}.");
            }

            Stop();
            try
            {
                TokenSource.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to dispose token source!");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}