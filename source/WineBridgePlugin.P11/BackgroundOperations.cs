using Playnite;

namespace WineBridgePlugin;

public class TestBackgroundOperation : BackgroundOperation
{
    private Task? backgroundTask;
    private CancellationTokenSource cancelToken = new();

    public TestBackgroundOperation() : base("operation.id", "Test operation")
    {
        // If your operation can be paused, set this to true;
        Pausable = true;
    }

    public override async ValueTask DisposeAsync()
    {
        cancelToken.Dispose();
    }

    public override async Task StartAsync(StartArgs args)
    {
        // Start your operation here in background Task
        // Do not block here by awaiting the task!
        // This method should return immediately after background work is started.
        backgroundTask = Task.Run(async () =>
        {
            try
            {
                Status = "Doing things";
                ProgressIsIndeterminate = false;
                ProgressMaximum = 10;

                for (int i = 0; i < 10; i++)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    ProgressValue = i;
                    await Task.Delay(1_000);
                }

                Status = "Done doing things";
                // Call this when you are done
                await OperationFinishedAsync(new FinishedEventArgs());
            }
            catch (Exception e)
            {
                // Playnite does no error handling on your background task.
                // That's your responsibility, so try/catching is recommended.

                // Call this when you want to finish with an error.
                await OperationFailedAsync(new FailedEventArgs(e.Message));
            }
        });
    }

    public override async Task StopAsync(StopArgs args)
    {
        await cancelToken.CancelAsync();
    }

    // PauseAsync and ResumeAsync are only needed if Pausable is enabled.
    public override async Task PauseAsync(PauseArgs args)
    {
    }

    public override async Task ResumeAsync(ResumeArgs args)
    {
    }
}