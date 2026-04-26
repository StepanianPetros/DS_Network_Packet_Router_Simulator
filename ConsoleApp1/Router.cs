using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    // Manages the routing engine: loads routes, forwards packets, tracks statistics
    public class Router
    {
        private readonly RoutingTrie _routingTable = new();
        private readonly Dictionary<string, int> _packetsByInterface = new();
        private readonly List<PacketLogEntry> _packetLog = new();
        private int _droppedCount = 0;
        private long _totalLookupTimeNs = 0;
        private int _lookupCount = 0;

        // Load routes from a text file (CIDR notation)
        public void LoadRoutes(string path)
        {
            try
            {
                int count = 0;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (TryParseRouteLine(line, out var network, out var prefix, out var interfaceName, out var metric))
                    {
                        _routingTable.Insert(network, prefix, interfaceName, metric);
                        count++;
                    }
                }
                Console.WriteLine($"✓ Loaded {count} routes");
            }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Process a file containing packets to route
        public void ProcessPackets(string path)
        {
            try
            {
                int count = 0;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (TryParsePacketLine(line, out var srcIp, out var dstIp, out var payloadSize))
                    {
                        RoutePacket(srcIp, dstIp, payloadSize);
                        count++;
                    }
                }
                Console.WriteLine($"✓ Processed {count} packets");
            }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Add a new route dynamically
        public void AddRoute(string cidr, string interfaceName, int metric = 1)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) throw new ArgumentException("Invalid CIDR format");
                
                uint network = IpStringToUint(parts[0]);
                int prefix = int.Parse(parts[1]);
                
                if (prefix < 0 || prefix > 32) throw new ArgumentException("Prefix must be 0-32");

                _routingTable.Insert(network, prefix, interfaceName, metric);
                Console.WriteLine($"✓ Route added: {cidr} -> {interfaceName}");
            }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Remove a route
        public void DeleteRoute(string cidr)
        {
            try
            {
                var parts = cidr.Split('/');
                if (parts.Length != 2) throw new ArgumentException("Invalid CIDR format");

                _routingTable.Delete(IpStringToUint(parts[0]), int.Parse(parts[1]));
                Console.WriteLine($"✓ Route deleted: {cidr}");
            }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Lookup route for a single IP address
        public void Lookup(string ip)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var route = _routingTable.Lookup(IpStringToUint(ip));
                sw.Stop();

                if (route == null)
                    Console.WriteLine($"✗ No route found for {ip}");
                else
                    Console.WriteLine($"✓ {ip} -> {route.Interface} (/{route.Prefix}, metric: {route.Metric})");
                    
                Console.WriteLine($"  Lookup time: {sw.Elapsed.TotalMicroseconds:F2} µs");
            }
            catch (Exception ex) { Console.WriteLine($"✗ Error: {ex.Message}"); }
        }

        // Route a single packet and log the result
        private void RoutePacket(string srcIp, string dstIp, uint payloadSize)
        {
            var sw = Stopwatch.StartNew();
            var route = _routingTable.Lookup(IpStringToUint(dstIp));
            sw.Stop();

            _totalLookupTimeNs += sw.Elapsed.Ticks * 100;
            _lookupCount++;

            var logEntry = new PacketLogEntry
            {
                SourceIp = srcIp,
                DestinationIp = dstIp,
                PayloadSize = payloadSize,
                LookupTimeNs = sw.Elapsed.Ticks * 100
            };

            if (route == null)
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

        // Parse a route line (format: CIDR interface [metric])
        private bool TryParseRouteLine(string line, out uint network, out int prefix, out string interfaceName, out int metric)
        {
            network = 0;
            prefix = 0;
            interfaceName = "";
            metric = 1;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) return false;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return false;

            var cidr = parts[0].Split('/');
            if (cidr.Length != 2) return false;

            try
            {
                network = IpStringToUint(cidr[0]);
                prefix = int.Parse(cidr[1]);
                interfaceName = parts[1];
                if (parts.Length > 2) metric = int.Parse(parts[2]);
                return true;
            }
            catch { return false; }
        }

        // Parse a packet line (format: source_ip dest_ip payload_size)
        private bool TryParsePacketLine(string line, out string srcIp, out string dstIp, out uint size)
        {
            srcIp = dstIp = "";
            size = 0;

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) return false;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;

            try
            {
                srcIp = parts[0];
                dstIp = parts[1];
                size = uint.Parse(parts[2]);
                return true;
            }
            catch { return false; }
        }

        // Display all routing entries
        public void ShowRoutingTable()
        {
            var routes = _routingTable.GetAllRoutes().ToList();
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              ROUTING TABLE (sorted by prefix length)              ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            if (routes.Count == 0)
            {
                Console.WriteLine("║                           [EMPTY]                               ║");
            }
            else
            {
                foreach (var route in routes)
                {
                    string entry = $"  {UintToIpString(route.Network)}/{route.Prefix,2} -> {route.Interface,8} (metric: {route.Metric})";
                    Console.WriteLine($"║ {entry,-62} ║");
                }
            }

            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Total routes: {routes.Count}\n");
        }

        // Display packet forwarding log
        public void ShowPacketLog()
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                                    PACKET LOG                                           ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ Source IP       │ Dest IP         │ Size  │ Result   │ Interface │ Lookup Time (µs)       ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════════════════════════════════════════╣");

            if (_packetLog.Count == 0)
            {
                Console.WriteLine("║                                      [EMPTY]                                              ║");
            }
            else
            {
                foreach (var entry in _packetLog)
                {
                    double timeUs = entry.LookupTimeNs / 1000.0;
                    string line = $"│ {entry.SourceIp,-15} │ {entry.DestinationIp,-15} │ {entry.PayloadSize,5} │ {entry.Result,-8} │ {entry.Interface,-9} │ {timeUs,18:F3} │";
                    Console.WriteLine(line);
                }
            }

            Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════════════════════╝\n");
        }

        // Display routing statistics
        public void ShowStatistics()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      ROUTING STATISTICS                        ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            int routed = _packetLog.Count(p => p.Result == "ROUTED");
            Console.WriteLine($"║ Total Packets Processed:      {_packetLog.Count,40} ║");
            Console.WriteLine($"║ Packets Routed:               {routed,40} ║");
            Console.WriteLine($"║ Packets Dropped:              {_droppedCount,40} ║");

            if (_packetLog.Count > 0)
            {
                double dropRate = ((double)_droppedCount / _packetLog.Count * 100);
                Console.WriteLine($"║ Drop Rate:                    {dropRate,38:F2}% ║");
            }

            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ PACKETS PER INTERFACE:                                         ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            if (_packetsByInterface.Count == 0)
            {
                Console.WriteLine("║                       [No packets routed]                      ║");
            }
            else
            {
                foreach (var kvp in _packetsByInterface.OrderByDescending(x => x.Value))
                    Console.WriteLine($"║ {kvp.Key,-30} {kvp.Value,20} ║");
            }

            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ PERFORMANCE METRICS:                                           ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");

            if (_lookupCount > 0)
            {
                double avgUs = _totalLookupTimeNs / (double)_lookupCount / 1000.0;
                Console.WriteLine($"║ Total Lookups:                {_lookupCount,40} ║");
                Console.WriteLine($"║ Average Lookup Time:          {avgUs,36:F3} µs ║");
                Console.WriteLine($"║ Lookup Complexity:            O(32) = O(1) constant    ║");
            }

            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
        }

        // Benchmark: Compare trie performance vs linear scan
        public void RunBenchmark()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  TRIE vs LINEAR SCAN BENCHMARK                 ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣\n");

            foreach (int tableSize in new[] { 50, 500, 5000 })
            {
                BenchmarkAtSize(tableSize);
            }

            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");
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
            Route? best = null;
            foreach (var route in routes)
            {
                uint mask = (0xFFFFFFFF << (32 - route.Prefix)) & 0xFFFFFFFF;
                if ((ip & mask) == (route.Network & mask))
                {
                    if (best == null || route.Prefix > best.Prefix)
                        best = route;
                }
            }
            return best;
        }

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