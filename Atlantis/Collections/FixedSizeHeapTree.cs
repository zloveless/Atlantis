using System;

namespace Atlantis.Collections
{
    public class FixedSizeHeapTree<T> where T : IComparable<T>, IEquatable<T>
    {
        private Node[] _nodes;

        public FixedSizeHeapTree(int maxHeapSize) {
            _nodes = new Node[maxHeapSize];
        }

        public int Count { get; private set; }

        public void Add(T item) {
            Node itemNode = new Node(item, Count);
            _nodes[Count] = itemNode;
            SortUp(itemNode);
            Count++;
        }

        public T RemoveFirst() {
            Node firstItem = _nodes[0];
            Count--;
            _nodes[0] = _nodes[Count];
            _nodes[0].Index = 0;
            SortDown(_nodes[0]);
            T result = firstItem.Value;
            firstItem = null;
            return result;
        }

        private void SortDown(Node item) {
            /*
            while (true) {
                int childLeftIndex = item.Index * 2 + 1;
                int childRightIndex = item.Index * 2 + 2;
            }
            */
        }

        private void SortUp(Node item) {
            int parentIndex = (item.Index - 1) / 2;
            while (true) {
                Node parentItem = _nodes[parentIndex];
                if (item.Value.CompareTo(parentItem.Value) > 0) {
                    Swap(item, parentItem);
                }
                else {
                    break;
                }

                parentIndex = (item.Index - 1) / 2;
            }
        }

        private void Swap(Node nodeA, Node nodeB) {
            _nodes[nodeA.Index] = nodeB;
            _nodes[nodeB.Index] = nodeA;
            int nodeAIndex = nodeA.Index;
            nodeA.Index = nodeB.Index;
            nodeB.Index = nodeAIndex;
        }

        private class Node {
            public Node(T value, int heapIndex) {
                Value = value;
                Index = heapIndex;
            }

            public int Index { get; set; }

            public T Value { get; private set; }

            public Node Parent { get; set; }
        }
    }
}
