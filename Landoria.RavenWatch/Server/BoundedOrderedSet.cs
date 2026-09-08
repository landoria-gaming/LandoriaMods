using System.Collections.Generic;

namespace Landoria.RavenWatch.Server
{
    internal sealed class BoundedOrderedSet<T>
    {
        private readonly HashSet<T> values;
        private readonly Queue<T> insertionOrder = new Queue<T>();
        private readonly int capacity;

        internal BoundedOrderedSet(int capacity, IEqualityComparer<T> comparer)
        {
            this.capacity = capacity;
            values = new HashSet<T>(comparer);
        }

        internal bool ContainsOrAdd(T value)
        {
            if (!values.Add(value)) return true;
            insertionOrder.Enqueue(value);
            if (insertionOrder.Count > capacity)
                values.Remove(insertionOrder.Dequeue());
            return false;
        }
    }
}
