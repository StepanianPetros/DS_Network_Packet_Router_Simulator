using System.Collections.Generic;

namespace ConsoleApp1
{
    // Binary tree node for trie structure (0 = left child, 1 = right child)
    public class TrieNode
    {
        public TrieNode?[] Child { get; } = new TrieNode?[2];
        public Route? Route { get; set; }
    }
}