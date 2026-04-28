using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    public class CLI
    {
        private const string RouteFilePath = "Data/routes.txt";
        private const string PacketFilePath = "Data/packets.txt";

        private readonly Router _router = new();

        public void Start()
        {
            PrintBanner();
            EnsureDataFolderExists();
            InitializeSampleFiles();

            _router.LoadRoutes(RouteFilePath);
            _router.ProcessPackets(PacketFilePath);
            PrintHelp();

            while (true)
            {
                Console.Write("\n> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    continue;

                ExecuteCommand(input);
            }
        }

        private void ExecuteCommand(string input)
        {
            try
            {
                var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return;

                var command = parts[0].ToLowerInvariant();

                switch (command)
                {
                    case "add-route":
                        HandleAddRoute(parts);
                        break;
                    case "del-route":
                        HandleDeleteRoute(parts);
                        break;
                    case "lookup":
                        HandleLookup(parts);
                        break;
                    case "routing-table":
                    case "routes":
                        _router.ShowRoutingTable();
                        break;
                    case "packet-log":
                    case "log":
                        _router.ShowPacketLog();
                        break;
                    case "stats":
                        _router.ShowStatistics();
                        break;
                    case "benchmark":
                        _router.RunBenchmark();
                        break;
                    case "load-routes":
                        HandleLoadRoutes(parts);
                        break;
                    case "load-packets":
                        HandleLoadPackets(parts);
                        break;
                    case "version":
                        PrintVersion();
                        break;
                    case "help":
                        PrintHelp();
                        break;
                    case "clear":
                        Console.Clear();
                        PrintBanner();
                        break;
                    case "exit":
                    case "quit":
                        Console.WriteLine("\nGoodbye!");
                        Environment.Exit(0);
                        break;
                    default:
                        PrintError($"Unknown command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }

        private void HandleAddRoute(string[] parts)
        {
            if (parts.Length < 3)
            {
                PrintError("Usage: add-route <CIDR> <interface> [metric]");
                return;
            }

            var metric = parts.Length > 3 ? int.Parse(parts[3]) : 1;
            _router.AddRoute(parts[1], parts[2], metric);
        }

        private void HandleDeleteRoute(string[] parts)
        {
            if (parts.Length < 2)
            {
                PrintError("Usage: del-route <CIDR>");
                return;
            }

            _router.DeleteRoute(parts[1]);
        }

        private void HandleLookup(string[] parts)
        {
            if (parts.Length < 2)
            {
                PrintError("Usage: lookup <IP>");
                return;
            }

            _router.Lookup(parts[1]);
        }

        private void HandleLoadRoutes(string[] parts)
        {
            if (parts.Length < 2)
            {
                PrintError("Usage: load-routes <file>");
                return;
            }

            _router.LoadRoutes(parts[1]);
        }

        private void HandleLoadPackets(string[] parts)
        {
            if (parts.Length < 2)
            {
                PrintError("Usage: load-packets <file>");
                return;
            }

            _router.ProcessPackets(parts[1]);
        }

        private static void PrintBanner()
        {
            Console.Clear();
            Console.WriteLine("NETWORK PACKET ROUTER SIMULATOR v1.0");
            Console.WriteLine("IP Routing with Trie");
            Console.WriteLine();
        }

        private static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Available commands:");
            Console.WriteLine("  add-route <CIDR> <iface> [metric]   Add a routing entry");
            Console.WriteLine("  del-route <CIDR>                    Delete a route entry");
            Console.WriteLine("  lookup <IP>                         Lookup an IP address");
            Console.WriteLine("  routing-table, routes               Display all routes");
            Console.WriteLine("  packet-log, log                     Display packet log");
            Console.WriteLine("  stats                               Show statistics");
            Console.WriteLine("  load-routes <file>                  Load routes from file");
            Console.WriteLine("  load-packets <file>                 Process packets");
            Console.WriteLine("  benchmark                           Benchmark Trie vs linear scan");
            Console.WriteLine("  version                             Show version info");
            Console.WriteLine("  help                                Show this help");
            Console.WriteLine("  clear                               Clear screen");
            Console.WriteLine("  exit, quit                          Exit program");
            Console.WriteLine();
        }

        private static void PrintVersion()
        {
            Console.WriteLine();
            Console.WriteLine("Version information:");
            Console.WriteLine("  Network Packet Router Simulator v1.0.0");
            Console.WriteLine("  Built with .NET 10.0");
            Console.WriteLine("  Algorithm: Binary Patricia Trie");
            Console.WriteLine("  Author: StepanianPetros");
            Console.WriteLine("  Date: April 2026");
            Console.WriteLine();
        }

        private static void PrintError(string message) => Console.WriteLine($"Error: {message}");

        private static void EnsureDataFolderExists() => Directory.CreateDirectory("Data");

        private static void InitializeSampleFiles()
        {
            var routeLines = new List<string>
            {
                "# Corporate Network Routes",
                "192.168.1.0/24 eth0 1",
                "192.168.2.0/24 eth0 2",
                "192.168.3.0/24 eth1 1",
                "192.168.0.0/16 eth1 10",
                "10.0.0.0/8 eth2 1",
                "10.1.0.0/16 eth2 5",
                "10.1.1.0/24 eth3 1",
                "10.2.0.0/16 eth2 2",
                "172.16.0.0/12 eth4 1",
                "172.16.0.0/16 eth4 10",
                "172.17.0.0/16 eth5 1",
                "172.31.0.0/16 eth5 15",
                "203.0.113.0/24 eth6 5",
                "203.0.114.0/24 eth6 10",
                "198.51.100.0/24 eth7 1",
                "198.51.101.0/24 eth7 2",
                "192.0.2.0/24 eth8 1",
                "192.0.2.128/25 eth9 1",
            };

            for (var i = 0; i < 35; i++)
                routeLines.Add($"1.{i}.0.0/16 eth{i % 10} {i % 5 + 1}");

            routeLines.Add("0.0.0.0/0 default 1000");
            File.WriteAllLines(RouteFilePath, routeLines);

            var packetLines = new List<string>
            {
                "# SourceIP DestIP PayloadSize",
                "192.168.1.1 192.168.1.10 100",
                "192.168.1.1 192.168.2.5 150",
                "10.0.0.1 10.1.1.100 200",
                "10.2.0.1 10.2.50.50 512",
                "203.0.113.1 203.0.113.100 256",
                "172.16.1.1 172.16.2.10 1024",
                "192.168.3.1 192.168.3.50 300",
                "1.5.1.1 1.5.2.10 400",
                "172.31.255.1 172.31.0.1 500",
                "203.0.113.50 203.0.114.50 256",
            };

            for (var i = 0; i < 20; i++)
            {
                packetLines.Add($"192.168.1.{i} 10.0.0.{i} {100 + i * 50}");
                packetLines.Add($"1.{i}.1.1 1.{i}.2.2 {200 + i * 30}");
            }

            File.WriteAllLines(PacketFilePath, packetLines);
        }
    }
}