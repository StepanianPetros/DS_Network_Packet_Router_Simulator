using System;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    // CLI Command Handler: Manages user interaction and routes commands to router
    public class CLI
    {
        private readonly Router _router = new();

        public void Start()
        {
            PrintBanner();
            InitializeTestData();

            _router.LoadRoutes("Data/routes.txt");
            _router.ProcessPackets("Data/packets.txt");
            PrintHelp();

            while (true)
            {
                Console.Write("\n> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                ExecuteCommand(input);
            }
        }

        private void ExecuteCommand(string input)
        {
            try
            {
                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return;

                string cmd = parts[0].ToLower();

                switch (cmd)
                {
                    case "add-route":
                        if (parts.Length < 3) ShowError("Usage: add-route <CIDR> <interface> [metric]");
                        else _router.AddRoute(parts[1], parts[2], parts.Length > 3 ? int.Parse(parts[3]) : 1);
                        break;

                    case "del-route":
                        if (parts.Length < 2) ShowError("Usage: del-route <CIDR>");
                        else _router.DeleteRoute(parts[1]);
                        break;

                    case "lookup":
                        if (parts.Length < 2) ShowError("Usage: lookup <IP>");
                        else _router.Lookup(parts[1]);
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
                        if (parts.Length < 2) ShowError("Usage: load-routes <file>");
                        else _router.LoadRoutes(parts[1]);
                        break;

                    case "load-packets":
                        if (parts.Length < 2) ShowError("Usage: load-packets <file>");
                        else _router.ProcessPackets(parts[1]);
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
                        Console.WriteLine("\n✓ Goodbye!");
                        Environment.Exit(0);
                        break;

                    default:
                        ShowError($"Unknown command: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private static void PrintBanner()
        {
            Console.Clear();
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         NETWORK PACKET ROUTER SIMULATOR v1.0                   ║");
            Console.WriteLine("║                   (IP Routing with Trie)                       ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        private static void PrintHelp()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                       AVAILABLE COMMANDS                       ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ add-route <CIDR> <iface> [metric]      Add a routing entry     ║");
            Console.WriteLine("║ del-route <CIDR>                         Delete a route entry   ║");
            Console.WriteLine("║ lookup <IP>                              Lookup an IP address   ║");
            Console.WriteLine("║ routing-table, routes                    Display all routes     ║");
            Console.WriteLine("║ packet-log, log                          Display packet log     ║");
            Console.WriteLine("║ stats                                    Show statistics        ║");
            Console.WriteLine("║ load-routes <file>                       Load routes from file  ║");
            Console.WriteLine("║ load-packets <file>                      Process packets        ║");
            Console.WriteLine("║ benchmark                                Benchmark Trie vs O(n) ║");
            Console.WriteLine("║ help                                     Show this help         ║");
            Console.WriteLine("║ clear                                    Clear screen           ║");
            Console.WriteLine("║ exit, quit                               Exit program           ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }

        private static void ShowError(string message) => Console.WriteLine($"✗ {message}");

        private static void InitializeTestData()
        {
            Directory.CreateDirectory("Data");

            // Create a realistic routing table with 54 routes
            var routeData = new StringBuilder();
            routeData.AppendLine("# Corporate Network Routes");
            routeData.AppendLine("192.168.1.0/24 eth0 1");
            routeData.AppendLine("192.168.2.0/24 eth0 2");
            routeData.AppendLine("192.168.3.0/24 eth1 1");
            routeData.AppendLine("192.168.0.0/16 eth1 10");
            routeData.AppendLine("10.0.0.0/8 eth2 1");
            routeData.AppendLine("10.1.0.0/16 eth2 5");
            routeData.AppendLine("10.1.1.0/24 eth3 1");
            routeData.AppendLine("10.2.0.0/16 eth2 2");
            routeData.AppendLine("172.16.0.0/12 eth4 1");
            routeData.AppendLine("172.16.0.0/16 eth4 10");
            routeData.AppendLine("172.17.0.0/16 eth5 1");
            routeData.AppendLine("172.31.0.0/16 eth5 15");
            routeData.AppendLine("203.0.113.0/24 eth6 5");
            routeData.AppendLine("203.0.114.0/24 eth6 10");
            routeData.AppendLine("198.51.100.0/24 eth7 1");
            routeData.AppendLine("198.51.101.0/24 eth7 2");
            routeData.AppendLine("192.0.2.0/24 eth8 1");
            routeData.AppendLine("192.0.2.128/25 eth9 1");

            // Add dynamic routes to reach 50+ entries
            for (int i = 0; i < 35; i++)
                routeData.AppendLine($"1.{i}.0.0/16 eth{i % 10} {i % 5 + 1}");

            routeData.AppendLine("0.0.0.0/0 default 1000");

            File.WriteAllText("Data/routes.txt", routeData.ToString());

            // Create packet stream for testing
            var packetData = new StringBuilder();
            packetData.AppendLine("# SourceIP DestIP PayloadSize");
            packetData.AppendLine("192.168.1.1 192.168.1.10 100");
            packetData.AppendLine("192.168.1.1 192.168.2.5 150");
            packetData.AppendLine("10.0.0.1 10.1.1.100 200");
            packetData.AppendLine("10.2.0.1 10.2.50.50 512");
            packetData.AppendLine("203.0.113.1 203.0.113.100 256");
            packetData.AppendLine("172.16.1.1 172.16.2.10 1024");
            packetData.AppendLine("192.168.3.1 192.168.3.50 300");
            packetData.AppendLine("1.5.1.1 1.5.2.10 400");
            packetData.AppendLine("172.31.255.1 172.31.0.1 500");
            packetData.AppendLine("203.0.113.50 203.0.114.50 256");

            // Add more test packets
            for (int i = 0; i < 20; i++)
            {
                packetData.AppendLine($"192.168.1.{i} 10.0.0.{i} {100 + i * 50}");
                packetData.AppendLine($"1.{i}.1.1 1.{i}.2.2 {200 + i * 30}");
            }

            File.WriteAllText("Data/packets.txt", packetData.ToString());
        }
    }
}