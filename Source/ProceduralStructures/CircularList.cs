using System.Collections;
using System.Collections.Generic;

namespace Game.ProceduralStructures {
    public class CircularList<T> : IEnumerable<T> {
        public static int NOT_FOUND = int.MinValue;
        public readonly int NotFound = NOT_FOUND;
        private List<T> _data;
        private bool _reversedAccess;

        public int IndexOffset { get; set; }

        public int Count => _data?.Count ?? 0;

        public CircularList(IEnumerable<T> l) {
            _data = new List<T>(l);
        }
        public void Reverse() {
            _reversedAccess = !_reversedAccess;
        }

        public IEnumerator<T> GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public T this[int index] {
            get => _data[ToDataIndex(index)];
            set => _data[ToDataIndex(index)] = value;
        }

        public bool Contains(T item) {
            return _data.Contains(item);
        }

        public int IndexOf(T item) {
            var dataIndex = _data.IndexOf(item);
            return dataIndex < 0 ? NOT_FOUND : ToVirtualIndex(dataIndex);
        }

        public bool IsConsecutiveIndex(int i1, int i2) {
            return i2 == (i1+1)%_data.Count;
        }

        public bool Remove(T item) {
            return _data.Remove(item);
        }

        public void RemoveAt(int index) {
            _data.RemoveAt(ToDataIndex(index));
        }

        private int ToDataIndex(int index) {
            var clippedIndex = index % _data.Count;
            if (clippedIndex < 0) clippedIndex += _data.Count;
            return (_data.Count + IndexOffset + clippedIndex * (_reversedAccess?-1:1)) % _data.Count;
        }

        private int ToVirtualIndex(int index) {
            return _reversedAccess ? (_data.Count - index - IndexOffset)%_data.Count : index - IndexOffset;
        }

        public class Enumerator : IEnumerator<T> {
            CircularList<T> _c;
            int _index = -1;
            public Enumerator(CircularList<T> l) {
                _c = l;
            }
            public T Current => _c[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext() {
                if (_index >= _c.Count) return false;
                _index++;
                return true;
            }

            public void Reset() {
                _index = -1;
            }

            public void Dispose() {}
        }
    }
}
