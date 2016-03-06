using System;

namespace ToriatamaText.InternalExtractors
{
    struct MiniList<T>
    {
        private T[] _array;

        public int Count { get; set; }

        public void Clear()
        {
            this.Count = 0;
        }

        public void SetCapacity(int capacity)
        {
            if (this._array == null || this._array.Length < capacity)
            {
                var newArray = new T[capacity];

                if (this.Count > 0)
                    Array.Copy(this._array, newArray, this.Count);

                this._array = newArray;
            }
        }

        public void Add(T value)
        {
            if (this._array == null)
            {
                this._array = new T[4];
            }
            else if (this._array.Length == this.Count)
            {
                var newArray = new T[this.Count * 2];
                Array.Copy(this._array, newArray, this.Count);
                this._array = newArray;
            }

            this._array[this.Count++] = value;
        }

        public T this[int index]
        {
            get
            {
                return this._array[index];
            }
            set
            {
                this._array[index] = value;
            }
        }

        public T Last => this._array[this.Count - 1];
    }
}
