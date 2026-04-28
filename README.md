# Network Packet Router Simulator

A console-based IPv4 packet routing simulator written in C#. This repository demonstrates real-world routing concepts using a binary Patricia trie, command-line interaction, file-based route and packet ingestion, runtime statistics, and benchmark comparisons.

## Goals

- Implement longest-prefix-match IPv4 routing in a high-performance data structure.
- Expose a usable CLI for route management and packet simulation.
- Keep the design modular and easy to extend.
- Compare trie-based routing to a simple linear scan approach.

## Repository Structure

- `ConsoleApp1/CLI.cs` — User-facing command interpreter and application bootstrap.
- `ConsoleApp1/Router.cs` — Routing engine, packet processing, log storage, statistics, and benchmarking.
- `ConsoleApp1/RoutingTrie.cs` — Trie implementation that supports insertion, lookup, deletion, and traversal.
- `ConsoleApp1/TrieNode.cs` — Node definition for the binary trie.
- `ConsoleApp1/Route.cs` — Route model containing prefix, interface, and metric.
- `ConsoleApp1/PacketLogEntry.cs` — Packet log model capturing forwarding decisions and timing.
- `ConsoleApp1/Data/routes.txt` — Sample route dataset used at startup.
- `ConsoleApp1/Data/packets.txt` — Sample packet dataset used at startup.

## Design Principles

### Separation of Concerns

Each class has a clearly defined responsibility:

- `CLI` handles interactive user commands, console output, and sample file initialization.
- `Router` coordinates route ingestion, packet routing, lookup reporting, and statistics.
- `RoutingTrie` stores prefix entries and performs efficient longest-prefix-match queries.
- `TrieNode` models the binary branching structure used by the trie.
- `Route` and `PacketLogEntry` encapsulate domain-specific data.

### Encapsulation and Abstraction

- `Router` keeps routing state private and exposes only high-level operations like `AddRoute`, `DeleteRoute`, and `Lookup`.
- `RoutingTrie` hides bit-level traversal logic behind `Insert`, `Lookup`, and `Delete` methods.
- External callers do not need to know how IPv4 strings are parsed or how bit masks are computed.

### SOLID Alignment

- Single Responsibility: Each class focuses on one aspect of the application.
- Open/Closed: `RoutingTrie` can be replaced or extended without changing CLI behavior.
- Liskov Substitution: Components depend on abstractions instead of concrete behavior where practical.
- Interface Segregation: CLI methods are small and specific to each command.
- Dependency Inversion: High-level `CLI` depends on lower-level `Router` in a controlled manner.

## Core Data Structures

### Binary Patricia Trie

- A specialized binary trie for IPv4 prefix storage.
- Uses a `TrieNode` with two children: `Child[0]` for bit 0 and `Child[1]` for bit 1.
- Stores a `Route` object directly at nodes representing valid prefixes.
- Supports fast longest-prefix-match lookups by following the IP address bits from the root.

### Route List

- `RoutingTrie` maintains an internal `List<Route>` for enumeration and display.
- The list is updated on insert and delete, enabling sorted route displays.

### Packet Log

- `Router` tracks packet results in a `List<PacketLogEntry>`.
- Each log entry includes source IP, destination IP, payload size, route result, interface, and lookup latency.

### Interface Counters

- `Dictionary<string, int>` counts packets routed per interface.
- Used by statistics reporting to show load distribution.

## How the System Works

### Program Startup

1. `CLI.Start()` displays a banner.
2. It ensures the `Data` folder exists and writes sample route/packet files if needed.
3. `Router.LoadRoutes(RouteFilePath)` loads `Data/routes.txt` into the trie.
4. `Router.ProcessPackets(PacketFilePath)` processes `Data/packets.txt` and records packet routing decisions.
5. The CLI enters a loop reading commands until `exit` or `quit`.

### Route Insertion

1. The route string is parsed as CIDR: `network/prefix`.
2. The network portion is converted from dotted-decimal to a `uint`.
3. The prefix length determines how many bits to traverse in the trie.
4. For each prefix bit, the trie descends left or right and creates missing nodes.
5. The resulting node stores a `Route` object and removes any existing route with the same prefix.

### Longest-Prefix-Match Lookup

1. The destination IP is converted to a 32-bit `uint`.
2. The trie is traversed bit-by-bit from the root.
3. Whenever a node contains a route, that route becomes the current best match.
4. Traversal continues until either 32 bits are processed or a missing child is reached.
5. The last matched route is returned, which implements the longest prefix rule.

### Route Deletion

1. Parse the CIDR route to remove.
2. Traverse the trie exactly `prefix` bits deep.
3. Clear the stored route at that node.
4. Recursively delete nodes that are no longer needed once they have no route and no children.
5. Remove the route from the internal route list.

### Packet Processing

1. Packet lines are parsed as `sourceIP destinationIP payloadSize`.
2. Each packet runs through the trie lookup for destination IP.
3. The packet is marked `ROUTED` if a matching route exists, otherwise `DROPPED`.
4. Routing time is measured in nanoseconds and stored in the packet log.
5. Interface counters are incremented when packets are routed.

## Command Reference

### Route Commands

- `add-route <CIDR> <interface> [metric]`
  - Example: `add-route 10.1.1.0/24 eth1 5`
  - Inserts or updates a route in the trie.

- `del-route <CIDR>`
  - Example: `del-route 10.1.1.0/24`
  - Deletes a route from the trie and route list.

### Lookup Commands

- `lookup <IP>`
  - Example: `lookup 10.1.1.123`
  - Performs a single longest-prefix-match lookup.

### Display Commands

- `routing-table` or `routes`
  - Prints the active routing table sorted by prefix.

- `packet-log` or `log`
  - Shows the packet forwarding history and per-packet lookup latency.

- `stats`
  - Displays packet counts, drop rate, and per-interface traffic.

### File Commands

- `load-routes <file>`
  - Example: `load-routes Data/routes.txt`
  - Loads additional route definitions from a file.

- `load-packets <file>`
  - Example: `load-packets Data/packets.txt`
  - Processes packets from a file and appends them to the log.

### Utility Commands

- `benchmark`
  - Runs the built-in performance comparison between trie lookup and linear scan.

- `version`
  - Prints application and build information.

- `help`
  - Displays command usage.

- `clear`
  - Clears the console and reprints the banner.

- `exit` / `quit`
  - Terminates the program.

## CLI Processing Flow

`CLI.ExecuteCommand()`:

1. Reads a line from the user.
2. Splits the input into command tokens.
3. Dispatches to handler methods such as `HandleAddRoute`, `HandleDeleteRoute`, `HandleLookup`, etc.
4. Each handler validates arguments and prints an error message when usage is invalid.
5. Exceptions are caught and presented in a readable format.

## Data Model Details

### `Route`

- `uint Network` — Network address stored as 32-bit integer.
- `int Prefix` — CIDR prefix length (0..32).
- `string? Interface` — Outgoing interface name.
- `int Metric` — Route metric cost.

### `PacketLogEntry`

- `string? SourceIp` — Source IP address as text.
- `string? DestinationIp` — Destination IP address as text.
- `uint PayloadSize` — Packet size in bytes.
- `string? Result` — `ROUTED` or `DROPPED`.
- `string? Interface` — Chosen output interface or `N/A`.
- `long LookupTimeNs` — Lookup latency in nanoseconds.

### `TrieNode`

- `TrieNode?[] Child` — Two-branch binary trie.
- `Route? Route` — Optional route stored at this node.

## File Formats and Sample Initialization

### Route File Format

- Each route line: `<CIDR> <interface> [metric]`
- Example:

```text
192.168.1.0/24 eth0 1
10.0.0.0/8 eth2 1
0.0.0.0/0 default 1000
```

- `#` begins a comment.
- Blank lines are ignored.

### Packet File Format

- Each packet line: `<sourceIp> <destinationIp> <payloadSize>`
- Example:

```text
192.168.1.1 10.0.0.5 150
172.16.0.1 192.168.3.2 512
```

### Sample File Generation

At startup, `CLI.InitializeSampleFiles()` writes default route and packet data into `Data/routes.txt` and `Data/packets.txt`.
This ensures the application can run immediately in a fresh checkout.

## Routing Algorithm Details

### IP Conversion

- Dotted-decimal IPv4 strings are split on `.`.
- Each octet is parsed and shifted into a `uint`.
- Example: `192.168.1.1` becomes `0xC0A80101`.

### Bit Extraction

- A specific bit is extracted using:
  - `(ip >> (31 - bitIndex)) & 1`
- Bits are examined from most significant to least significant.

### Longest Prefix Match

- The trie traverses from root to leaf.
- Each visited node with a route updates the candidate best match.
- The final route returned is the most specific prefix that matches the destination.
- This mirrors router behaviour in IP forwarding tables.

### Route Pruning

- After deletion, nodes with no children and no route are removed.
- This keeps trie size compact and avoids dead branches.

## Benchmark Behavior

- `Router.RunBenchmark()` generates deterministic random routes for sizes `50`, `500`, and `5000`.
- It measures:
  - Trie lookup for `100000` repeated queries.
  - Linear scan lookup for the same queries.
- The benchmark demonstrates how trie lookup remains constant while linear scan cost grows with route count.
- Output includes milliseconds and speedup ratio.

## Complexity Analysis

### IPv4 Trie Time Complexity

- `RoutingTrie.Insert(network, prefix, interface, metric)`:
  - Worst-case path length = `prefix` bits.
  - With IPv4, `prefix <= 32`, so this is bounded by a constant: `O(32) = O(1)`.

- `RoutingTrie.Lookup(ip)`:
  - Traverses up to 32 nodes.
  - Each step reads one bit and follows one branch, so `O(32) = O(1)`.

- `RoutingTrie.Delete(network, prefix)`:
  - Traverses `prefix` bits and then optionally prunes nodes.
  - `O(32) = O(1)`.

### Router-Level Complexity

- `Router.LoadRoutes(path)`:
  - Reads `N` lines, parses each route, and inserts into the trie.
  - Total cost is `O(N × W) = O(N)`.

- `Router.ProcessPackets(path)`:
  - Parses `M` packet lines and performs `M` lookups.
  - Total cost is `O(M × W) = O(M)`.

- `Router.ShowRoutingTable()`:
  - Retrieves all routes and sorts by prefix.
  - Cost is `O(N log N)`.

- `Router.ShowPacketLog()`:
  - Iterates through packet entries: `O(M)`.

- `Router.RunBenchmark()`:
  - Trie benchmark: `O(L × W)` where `L` is lookup count.
  - Linear scan benchmark: `O(L × N)`.

### Space Complexity

- Active routes are stored in `List<Route>`: `O(N)`.
- Packet logs are stored in `List<PacketLogEntry>`: `O(M)`.
- Trie nodes are at most `O(N × W)` in the worst case.
- Interface counters use `O(I)` space where `I` is the number of distinct interfaces.

### Practical Notes

- Real router tables are far smaller than the theoretical worst-case trie size because prefixes share common prefixes.
- This implementation is optimized for IPv4; extending to IPv6 would require 128-bit keys and larger bit depth.

## Build and Run

From the repository root:

```bash
dotnet build
dotnet run --project ConsoleApp1/ConsoleApp1.csproj
```

If the `Data` directory does not exist, the app creates it and writes sample `routes.txt` and `packets.txt`.

## Extension Opportunities

- Add a persistence layer to save routes between sessions.
- Add support for IPv6 using 128-bit prefix keys.
- Add route metrics and tie-breaking rules beyond prefix length.
- Introduce a pluggable backend so `RoutingTrie` can be swapped with an alternative route storage strategy.
- Add unit tests for parsing, lookup, and route deletion.
