using System.Diagnostics;

namespace WineBridgePlugin.Models
{
    public class ProcessWithCorrelationId
    {
        public Process Process { get; set; }
        public string CorrelationId { get; set; }
    }
}