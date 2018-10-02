using System;
using System.Collections.Generic;

namespace LastWriteWinsElementSet
{
    public class LastWriteWinsElement<T>
    {
        public T Element { get; }
        public DateTime Timestamp { get; }
        
        public LastWriteWinsElement(T element, DateTime timestamp)
        {
            Element = element;
            Timestamp = timestamp;
        }
    }
    
    public class LastWriteWinsElementEqualityComparer<T> : IEqualityComparer<LastWriteWinsElement<T>>
    {
        private readonly IEqualityComparer<T> _elementComparer;

        public LastWriteWinsElementEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            _elementComparer = elementComparer;
        }
            
        public bool Equals(LastWriteWinsElement<T> x, LastWriteWinsElement<T> y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return _elementComparer.Equals(x.Element, y.Element) && x.Timestamp.Equals(y.Timestamp);
        }

        public int GetHashCode(LastWriteWinsElement<T> obj)
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(obj.Element) * 397) ^ obj.Timestamp.GetHashCode();
            }
        }
    }
}