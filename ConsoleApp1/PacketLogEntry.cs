namespace ConsoleApp1
{
    // Records each packet's routing decision, source/destination, and lookup time
    public class PacketLogEntry
    {
        public string? SourceIp { get; set; }
        public string? DestinationIp { get; set; }
        public uint PayloadSize { get; set; }
        public string? Result { get; set; }
        public string? Interface { get; set; }
        public long LookupTimeNs { get; set; }
    }
}