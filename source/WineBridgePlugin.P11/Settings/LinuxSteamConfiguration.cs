namespace WineBridgePlugin.Settings
{
    public class LinuxSteamConfiguration
    {
        public required string Type { get; set; }
        public string? ExecutablePath { get; set; }
        public string? DataPath { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Type)}: {Type}, {nameof(ExecutablePath)}: {ExecutablePath}, {nameof(DataPath)}: {DataPath}";
        }
    }
}