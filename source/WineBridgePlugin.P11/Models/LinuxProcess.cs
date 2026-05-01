using System.Diagnostics;
using WineBridgePlugin.Processes;

namespace WineBridgePlugin.Models
{
    public class LinuxProcess
    {
        public Process? OriginalProcess { get; set; }
        public required Process ScriptProcess { get; set; }
        public required string CorrelationId { get; set; }
        public required CancellationTokenSource CancellationTokenSource { get; set; }
        public required string ProcessTrackingFile { get; set; }
        public required string OutputTrackingFile { get; set; }
        public required string ErrorTrackingFile { get; set; }
        public required string InputTrackingFile { get; set; }
        public required string StatusTrackingFile { get; set; }
        public required string PidTrackingFile { get; set; }
        public required string ReadyTrackingFile { get; set; }
    }

    public class RunningLinuxProcessData
    {
        public LinuxProcessInputPipe? InputPipe { get; set; }
        public LinuxProcessOutputPipe? OutputPipe { get; set; }
        public LinuxProcessOutputPipe? ErrorPipe { get; set; }
    }
}