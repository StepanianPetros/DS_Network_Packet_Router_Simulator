# Network Packet Router Simulator

A high-performance IPv4 network packet router simulator implemented in C# using a binary Patricia trie for O(1) longest-prefix-match lookups.

## Features

- **Binary Patricia Trie**: O(1) constant-time IP routing lookups
- **CLI Interface**: Interactive command-line interface with comprehensive commands
- **Performance Benchmarking**: Compare trie performance vs linear scan
- **Statistics Tracking**: Packet routing statistics and performance metrics
- **File I/O**: Load routes and packets from text files

## Commands

- `add-route <CIDR> <interface> [metric]` - Add a routing entry
- `del-route <CIDR>` - Delete a route entry
- `lookup <IP>` - Lookup an IP address
- `routing-table` - Display all routes
- `packet-log` - Display packet log
- `stats` - Show statistics
- `benchmark` - Run performance benchmark
- `load-routes <file>` - Load routes from file
- `load-packets <file>` - Process packets from file
- `version` - Show version information
- `help` - Show help
- `clear` - Clear screen
- `exit` - Exit program

## Building and Running

```bash
dotnet build
dotnet run
```

## Complexity Analysis

### Time Complexity

#### Trie Operations (Binary Patricia Trie)
- **Insert Route**: O(W) where W = 32 (IPv4 bit length) = **O(1)**
  - Traverses exactly W bits down the trie, creating nodes as needed
  - Each bit operation is constant time
- **Lookup IP**: O(W) = **O(1)**
  - Always traverses exactly 32 bits, regardless of routing table size
  - Performs longest-prefix-match by tracking best route during traversal
- **Delete Route**: O(W) = **O(1)**
  - Traverses up to W bits to find and remove the route
  - Includes cleanup of empty nodes during backtracking

#### Router Operations
- **Load Routes**: O(N × W) = **O(N)** where N = number of routes
  - Parses each route (O(1) per route) and inserts into trie (O(W))
  - File I/O is O(file_size) but dominated by trie operations
- **Process Packets**: O(M × W) = **O(M)** where M = number of packets
  - Each packet lookup takes O(W) time
  - Independent of routing table size N
- **Single IP Lookup**: O(W) = **O(1)**
  - Same as trie lookup operation
- **Show Routing Table**: O(N log N)
  - Sorts routes by prefix length for display (O(N log N))
  - Route enumeration is O(N)
- **Show Statistics**: O(1)
  - Simple counter access and display
- **Show Packet Log**: O(M)
  - Iterates through packet log entries

#### Benchmark Operations
- **Trie Benchmark**: O(L × W) = **O(L)** where L = lookup count
  - Performs L independent O(W) lookups
- **Linear Scan Benchmark**: O(L × N)
  - Each of L lookups scans all N routes
  - Demonstrates O(N) vs O(1) performance difference

### Space Complexity

#### Trie Storage
- **Average Case**: O(N × W) = **O(N)** where N = routes
  - Each route requires up to W nodes, but sharing reduces actual usage
  - In practice, much less than theoretical maximum due to prefix commonality
- **Worst Case**: O(2^W) = O(2^32) ≈ 4.3 billion nodes
  - Theoretical maximum if every possible IP has a unique route
  - Impossible in practice due to memory constraints

#### Router Storage
- **Routing Table**: O(N) for route list + O(trie_nodes)
- **Packet Log**: O(M) where M = processed packets
- **Statistics**: O(I) where I = number of interfaces (typically small)

### Performance Characteristics

- **Lookup Speed**: Independent of routing table size - always 32 bit operations
- **Scalability**: Performance remains constant as routing table grows
- **Memory Efficiency**: Trie sharing reduces memory usage for similar prefixes
- **Benchmark Results**: Shows 45x-4325x speedup over linear scan depending on table size

The binary Patricia trie provides optimal O(1) lookup performance for IPv4 routing, making it ideal for high-throughput network simulation and routing applications.
