using System;
using System.Collections;
using System.Collections.Generic;

namespace HoUrp.Extensions.Core
{
    public readonly struct ReadOnlyArray<T> : IReadOnlyList<T>
    {
        private static readonly T[] EmptyValues = Array.Empty<T>();
        private readonly T[] values;

        public ReadOnlyArray(params T[] values)
        {
            this.values = values == null || values.Length == 0 ? EmptyValues : (T[])values.Clone();
        }

        public int Count => values?.Length ?? 0;

        public T this[int index] => (values ?? EmptyValues)[index];

        public T[] ToArray()
        {
            return Count == 0 ? EmptyValues : (T[])(values ?? EmptyValues).Clone();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)(values ?? EmptyValues)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
