using System;

namespace ConsoleApp1
{
    // Represents a single routing table entry with destination prefix and outgoing interface
    public class Route
    {
        public uint Network { get; set; }
        public int Prefix { get; set; }
        public string? Interface { get; set; }
        public int Metric { get; set; }

        public override string ToString() => $"{IpToString(Network)}/{Prefix} -> {Interface} (metric: {Metric})";

        private static string IpToString(uint ip) =>
            $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
    }
}