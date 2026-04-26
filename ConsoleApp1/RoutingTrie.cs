using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
    // Binary Patricia Trie for longest-prefix-match lookups
    // Achieves O(1) constant time lookups by traversing 32 bits (IPv4 address size)
    public class RoutingTrie
    {
        private const int IPv4BitsLength = 32;
        private readonly TrieNode _root = new();
        private readonly List<Route> _routes = new();

        // Add a route: traverse down the trie, creating nodes as needed
        public void Insert(uint network, int prefix, string interfaceName, int metric)
        {
            var node = _root;

            // Walk down the trie for each bit in the prefix
            for (int i = 0; i < prefix; i++)
            {
                int bit = ExtractBit(network, i);
                if (node.Child[bit] == null)
                    node.Child[bit] = new TrieNode();
                node = node.Child[bit];
            }

            var route = new Route
            {
                Network = network,
                Prefix = prefix,
                Interface = interfaceName,
                Metric = metric
            };

            node.Route = route;
            
            // Keep track of all routes for display/reporting
            _routes.RemoveAll(r => r.Network == network && r.Prefix == prefix);
            _routes.Add(route);
        }

        // Remove a route from the trie
        public void Delete(uint network, int prefix)
        {
            DeleteRecursive(_root, network, prefix, 0);
            _routes.RemoveAll(r => r.Network == network && r.Prefix == prefix);
        }

        // Recursively traverse and delete a route
        private bool DeleteRecursive(TrieNode node, uint network, int prefix, int depth)
        {
            if (node == null) return false;

            if (depth == prefix)
            {
                node.Route = null;
                return IsEmptyNode(node);
            }

            int bit = ExtractBit(network, depth);
            if (DeleteRecursive(node.Child[bit], network, prefix, depth + 1))
                node.Child[bit] = null;

            return IsEmptyNode(node);
        }

        // Check if node has no route and no children
        private static bool IsEmptyNode(TrieNode node)
            => node.Route == null && node.Child[0] == null && node.Child[1] == null;

        // Find the route with the longest matching prefix for this IP
        public Route? Lookup(uint ip)
        {
            var node = _root;
            Route? bestMatch = null;

            // Traverse all 32 bits, keeping track of the best (longest) match
            for (int i = 0; i < IPv4BitsLength; i++)
            {
                if (node == null) break;

                if (node.Route != null)
                    bestMatch = node.Route;

                int bit = ExtractBit(ip, i);
                node = node.Child[bit];
            }

            return bestMatch;
        }

        // Extract the i-th bit from an IP address (left to right, 0-indexed)
        private static int ExtractBit(uint ip, int bitIndex)
            => (int)((ip >> (IPv4BitsLength - 1 - bitIndex)) & 1);

        public IEnumerable<Route> GetAllRoutes() => _routes.OrderBy(r => r.Prefix);

        public int RouteCount => _routes.Count;
    }
}