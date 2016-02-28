using System;

namespace ToriatamaText.InternalExtractors
{
    struct MiniList<T>
    {
        private T[] _array;

        public int Count { get; private set; }

        public void Initialize()
        {
            if (this._array == null)
                this._array = new T[4];
            this.Count = 0;
        }

        public void Add(T value)
        {
            if (this._array.Length == this.Count)
            {
                var newArray = new T[this.Count * 2];
                Array.Copy(this._array, newArray, this.Count);
                this._array = newArray;
            }

            this._array[this.Count++] = value;
        }

        public T this[int index] => this._array[index];

        public T Last => this._array[this.Count - 1];
    }
}
