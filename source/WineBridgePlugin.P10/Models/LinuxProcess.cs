using System.Diagnostics;
using System.Threading;
using WineBridgePlugin.Processes;

namespace WineBridgePlugin.Models
{
    public class LinuxProcess
    {
        public Process OriginalProcess { get; set; }
        public Process ScriptProcess { get; set; }
        public string CorrelationId { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public string ProcessTrackingFile { get; set; }
        public string OutputTrackingFile { get; set; }
        public string ErrorTrackingFile { get; set; }
        public string InputTrackingFile { get; set; }
        public string StatusTrackingFile { get; set; }
        public string PidTrackingFile { get; set; }
        public string ReadyTrackingFile { get; set; }
    }

    public class RunningLinuxProcessData
    {
        public LinuxProcessInputPipe InputPipe { get; set; }
        public LinuxProcessOutputPipe OutputPipe { get; set; }
        public LinuxProcessOutputPipe ErrorPipe { get; set; }
    }
}