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
- `help` - Show help
- `clear` - Clear screen
- `exit` - Exit program

## Building and Running

```bash
dotnet build
dotnet run
```

## Performance

The trie implementation achieves O(1) lookup complexity by traversing exactly 32 bits (IPv4 address length), providing significant performance improvements over linear scan approaches.
