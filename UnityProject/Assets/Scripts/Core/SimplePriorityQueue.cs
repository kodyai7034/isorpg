using System.Collections.Generic;

namespace IsoRPG.Core
{
    /// <summary>
    /// Simple min-heap priority queue for pathfinding.
    /// Provides PriorityQueue&lt;TElement, TPriority&gt; API compatible with .NET 6+.
    /// Used as fallback when System.Collections.Generic.PriorityQueue is unavailable.
    /// </summary>
    public class SimplePriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
    {
        private readonly List<(TElement Element, TPriority Priority)> _heap = new();

        /// <summary>Number of items in the queue.</summary>
        public int Count => _heap.Count;

        /// <summary>Add an element with the given priority.</summary>
        public void Enqueue(TElement element, TPriority priority)
        {
            _heap.Add((element, priority));
            BubbleUp(_heap.Count - 1);
        }

        /// <summary>Remove and return the element with the lowest priority.</summary>
        public TElement Dequeue()
        {
            if (_heap.Count == 0)
                throw new System.InvalidOperationException("Queue is empty.");

            var result = _heap[0].Element;
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            if (_heap.Count > 0)
                BubbleDown(0);

            return result;
        }

        /// <summary>Look at the lowest-priority element without removing it.</summary>
        public TElement Peek()
        {
            if (_heap.Count == 0)
                throw new System.InvalidOperationException("Queue is empty.");
            return _heap[0].Element;
        }

        /// <summary>Remove all elements.</summary>
        public void Clear() => _heap.Clear();

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_heap[index].Priority.CompareTo(_heap[parent].Priority) >= 0)
                    break;

                (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < count && _heap[left].Priority.CompareTo(_heap[smallest].Priority) < 0)
                    smallest = left;
                if (right < count && _heap[right].Priority.CompareTo(_heap[smallest].Priority) < 0)
                    smallest = right;

                if (smallest == index)
                    break;

                (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
                index = smallest;
            }
        }
    }
}
