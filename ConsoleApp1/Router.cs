using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    public class Router
    {
        private const int Ipv4BitCount = 32;

        private readonly RoutingTrie _routingTable = new();
        private readonly Dictionary<string, int> _packetsByInterface = new();
        private readonly List<PacketLogEntry> _packetLog = new();

        private int _droppedCount;
        private long _totalLookupTimeNs;
        private int _lookupCount;

        public void LoadRoutes(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                var loaded = 0;

                foreach (var line in lines)
                {
                    if (!TryParseRouteLine(line, out var network, out var prefix, out var interfaceName, out var metric))
                        continue;

                    _routingTable.Insert(network, prefix, interfaceName, metric);
                    loaded++;
                }

                Console.WriteLine($"✓ Loaded {loaded} routes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        public void ProcessPackets(string path)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                var processed = 0;

                foreach (var line in lines)
                {
                    if (!TryParsePacketLine(line, out var sourceIp, out var destinationIp, out var payloadSize))
                        continue;

                    RoutePacket(sourceIp, destinationIp, payloadSize);
                    processed++;
                }

                Console.WriteLine($"Processed {processed} packets");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        public void AddRoute(string cidr, string interfaceName, int metric = 1)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid CIDR format");

                var network = IpStringToUint(parts[0]);
                var prefix = int.Parse(parts[1]);

                if (prefix < 0 || prefix > Ipv4BitCount)
                    throw new ArgumentException("Prefix must be 0-32");

                _routingTable.Insert(network, prefix, interfaceName, metric);
                Console.WriteLine($"Route added: {cidr} -> {interfaceName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        public void DeleteRoute(string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid CIDR format");

                var network = IpStringToUint(parts[0]);
                var prefix = int.Parse(parts[1]);

                _routingTable.Delete(network, prefix);
                Console.WriteLine($"Route deleted: {cidr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        public void Lookup(string ip)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                var route = _routingTable.Lookup(IpStringToUint(ip));
                timer.Stop();

                if (route is null)
                {
                    Console.WriteLine($"No route found for {ip}");
                }
                else
                {
                    Console.WriteLine($"{ip} -> {route.Interface} (/{route.Prefix}, metric: {route.Metric})");
                }

                Console.WriteLine($"  Lookup time: {timer.Elapsed.TotalMicroseconds:F2} µs");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }

        private void RoutePacket(string sourceIp, string destinationIp, uint payloadSize)
        {
            var timer = Stopwatch.StartNew();
            var route = _routingTable.Lookup(IpStringToUint(destinationIp));
            timer.Stop();

            var lookupTimeNs = timer.Elapsed.Ticks * 100;
            _totalLookupTimeNs += lookupTimeNs;
            _lookupCount++;

            var logEntry = new PacketLogEntry
            {
                SourceIp = sourceIp,
                DestinationIp = destinationIp,
                PayloadSize = payloadSize,
                LookupTimeNs = lookupTimeNs,
            };

            if (route is null)
            {
                _droppedCount++;
                logEntry.Result = "DROPPED";
                logEntry.Interface = "N/A";
            }
            else
            {
                if (!_packetsByInterface.ContainsKey(route.Interface!))
                    _packetsByInterface[route.Interface!] = 0;

                _packetsByInterface[route.Interface!]++;
                logEntry.Result = "ROUTED";
                logEntry.Interface = route.Interface;
            }

            _packetLog.Add(logEntry);
        }

        private bool TryParseRouteLine(string line, out uint network, out int prefix, out string interfaceName, out int metric)
        {
            network = 0;
            prefix = 0;
            interfaceName = string.Empty;
            metric = 1;

            if (IsEmptyOrComment(line))
                return false;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return false;

            var cidrParts = parts[0].Split('/');
            if (cidrParts.Length != 2)
                return false;

            try
            {
                network = IpStringToUint(cidrParts[0]);
                prefix = int.Parse(cidrParts[1]);
                interfaceName = parts[1];

                if (parts.Length > 2)
                    metric = int.Parse(parts[2]);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryParsePacketLine(string line, out string sourceIp, out string destinationIp, out uint size)
        {
            sourceIp = string.Empty;
            destinationIp = string.Empty;
            size = 0;

            if (IsEmptyOrComment(line))
                return false;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return false;

            try
            {
                sourceIp = parts[0];
                destinationIp = parts[1];
                size = uint.Parse(parts[2]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsEmptyOrComment(string text)
            => string.IsNullOrWhiteSpace(text) || text.TrimStart().StartsWith("#", StringComparison.Ordinal);

        // Display all routing entries
        public void ShowRoutingTable()
        {
            var routes = _routingTable.GetAllRoutes().ToList();
            Console.WriteLine();
            Console.WriteLine("Routing table (sorted by prefix length):");

            if (routes.Count == 0)
            {
                Console.WriteLine("  [EMPTY]");
            }
            else
            {
                foreach (var route in routes)
                {
                    Console.WriteLine($"  {UintToIpString(route.Network)}/{route.Prefix,2} -> {route.Interface,8} (metric: {route.Metric})");
                }
            }

            Console.WriteLine($"Total routes: {routes.Count}");
            Console.WriteLine();
        }

        // Display packet forwarding log
        public void ShowPacketLog()
        {
            Console.WriteLine();
            Console.WriteLine("Packet log:");
            Console.WriteLine("  Source IP       | Dest IP         | Size  | Result   | Interface | Lookup Time (µs)");
            Console.WriteLine("  ------------------------------------------------------------------------------");

            if (_packetLog.Count == 0)
            {
                Console.WriteLine("  [EMPTY]");
            }
            else
            {
                foreach (var entry in _packetLog)
                {
                    var timeUs = entry.LookupTimeNs / 1000.0;
                    Console.WriteLine($"  {entry.SourceIp,-15} | {entry.DestinationIp,-15} | {entry.PayloadSize,5} | {entry.Result,-8} | {entry.Interface,-9} | {timeUs,12:F3}");
                }
            }

            Console.WriteLine();
        }

        // Display routing statistics
        public void ShowStatistics()
        {
            Console.WriteLine();
            Console.WriteLine("Routing statistics:");

            var routed = _packetLog.Count(p => p.Result == "ROUTED");
            Console.WriteLine($"  Total packets processed: {_packetLog.Count}");
            Console.WriteLine($"  Packets routed:          {routed}");
            Console.WriteLine($"  Packets dropped:         {_droppedCount}");

            if (_packetLog.Count > 0)
            {
                var dropRate = (_droppedCount / (double)_packetLog.Count) * 100;
                Console.WriteLine($"  Drop rate:               {dropRate:F2}%");
            }

            Console.WriteLine();
            Console.WriteLine("Packets per interface:");

            if (_packetsByInterface.Count == 0)
            {
                Console.WriteLine("  [No packets routed]");
            }
            else
            {
                foreach (var kvp in _packetsByInterface.OrderByDescending(x => x.Value))
                    Console.WriteLine($"  {kvp.Key,-30} {kvp.Value}");
            }

            if (_lookupCount > 0)
            {
                var avgUs = _totalLookupTimeNs / (double)_lookupCount / 1000.0;
                Console.WriteLine();
                Console.WriteLine("Performance metrics:");
                Console.WriteLine($"  Total lookups:           {_lookupCount}");
                Console.WriteLine($"  Average lookup time:     {avgUs:F3} µs");
                Console.WriteLine("  Lookup complexity:       O(1) constant");
            }

            Console.WriteLine();
        }

        // Benchmark: Compare trie performance vs linear scan
        public void RunBenchmark()
        {
            Console.WriteLine();
            Console.WriteLine("Benchmark: trie vs linear scan");
            Console.WriteLine();

            foreach (var tableSize in new[] { 50, 500, 5000 })
            {
                BenchmarkAtSize(tableSize);
            }

            Console.WriteLine();
        }

        private void BenchmarkAtSize(int tableSize)
        {
            Console.WriteLine($"Routing table size: {tableSize} routes");
            Console.WriteLine("─────────────────────────────────────────────────────────");

            var testRoutes = GenerateRandomRoutes(tableSize);
            uint testIp = IpStringToUint("192.168.1.100");
            const int lookupCount = 100000;

            // Benchmark trie
            var trie = new RoutingTrie();
            foreach (var route in testRoutes)
                trie.Insert(route.Network, route.Prefix, route.Interface!, route.Metric);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < lookupCount; i++)
                _ = trie.Lookup(testIp);
            sw.Stop();
            double trieMs = sw.Elapsed.TotalMilliseconds;

            // Benchmark linear scan
            sw.Restart();
            for (int i = 0; i < lookupCount; i++)
                _ = LinearScanLookup(testIp, testRoutes);
            sw.Stop();
            double linearMs = sw.Elapsed.TotalMilliseconds;

            double speedup = linearMs / trieMs;
            Console.WriteLine($"  Trie:          {trieMs,10:F2} ms");
            Console.WriteLine($"  Linear Scan:   {linearMs,10:F2} ms");
            Console.WriteLine($"  Speedup:       {speedup,10:F2}x faster");
            Console.WriteLine();
        }

        private List<Route> GenerateRandomRoutes(int count)
        {
            var routes = new List<Route>();
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                uint network = (uint)(random.Next() & 0xFFFFFFFF);
                int prefix = random.Next(8, 25);
                string interfaceName = $"eth{i % 10}";
                routes.Add(new Route { Network = network, Prefix = prefix, Interface = interfaceName, Metric = 1 });
            }

            return routes;
        }

        private Route? LinearScanLookup(uint ip, List<Route> routes)
        {
            Route? bestMatch = null;

            foreach (var route in routes)
            {
                var mask = GetNetworkMask(route.Prefix);
                if ((ip & mask) != (route.Network & mask))
                    continue;

                if (bestMatch == null || route.Prefix > bestMatch.Prefix)
                    bestMatch = route;
            }

            return bestMatch;
        }

        private uint GetNetworkMask(int prefix)
            => prefix == 0 ? 0u : 0xFFFFFFFFu << (Ipv4BitCount - prefix);

        private uint IpStringToUint(string ip)
        {
            var octets = ip.Split('.');
            return (uint)(
                (int.Parse(octets[0]) << 24) |
                (int.Parse(octets[1]) << 16) |
                (int.Parse(octets[2]) << 8) |
                int.Parse(octets[3])
            );
        }

        private string UintToIpString(uint ip) =>
            $"{(ip >> 24) & 0xFF}.{(ip >> 16) & 0xFF}.{(ip >> 8) & 0xFF}.{ip & 0xFF}";
    }
}