using System.Collections;

namespace Voxelmetric.Code.Common.Collections
{
    /// <summary>
    ///     Represents a circular 1D array
    /// </summary>
    public sealed class CircularArray1D<T> : IEnumerable
    {
        private readonly T[] items;

        public CircularArray1D(int width)
        {
            Offset = 0;

            items = Helpers.CreateArray1D<T>(width);
        }

        /// <summary>
        ///     Get number of elements of the array
        /// </summary>
        public int Size
        {
            get
            {
                return items.Length;
            }
        }

        /// <summary>
        ///     Offset off the beggining
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        ///     Access internal array in a circular way
        /// </summary>
        public T this[int i]
        {
            get
            {
                int pos = Helpers.Mod(i + Offset, items.Length);
                return items[pos];
            }
            set
            {
                int pos = Helpers.Mod(i + Offset, items.Length);
                items[pos] = value;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}
