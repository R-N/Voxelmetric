using Voxelmetric.Code.Common;

namespace Voxelmetric.Code.Utilities.Noise
{
    public class NoiseInterpolator
    {
        //! Downsampled size of chunk
        protected int size;
        protected int sizePow2;
        protected int sizePow2plusSize;
        //! +1 interpolated into downsampled state
        protected int step;
        //! Interpolation scale
        protected float scale;

        /// <summary>
        /// Initiates NoiseInterpolator
        /// </summary>
        /// <param name="size">Value which we want to downsample. When calling Interpolate, the coordinates must be within 0..size-1 range</param>
        /// <param name="downsamplingFactor">Factor says how much is ChunkSize supposed to be downsampled</param>
        public void SetInterpBitStep(int size, int downsamplingFactor)
        {
            step = downsamplingFactor;
            this.size = (size >> step) + 1;
            sizePow2 = this.size * this.size;
            sizePow2plusSize = sizePow2 + this.size;
            scale = 1f / (1 << step);
        }

        public int Step { get { return step; } }
        public int Size { get { return size; } }

        /// <summary>
        /// Interpolates the given coordinate into a downsampled coordinate and returns a value from the lookup table on that position
        /// </summary>
        /// <param name="x">Position on the x axis</param>
        /// <param name="lookupTable">Lookup table to be used to interpolate</param>
        public float Interpolate(int x, float[] lookupTable)
        {
            float xs = (x + 0.5f) * scale;

            int x0 = Helpers.FastFloor(xs);

            xs = (xs - x0);

            return Helpers.Interpolate(lookupTable[x0], lookupTable[x0 + 1], xs);
        }

        /// <summary>
        /// Interpolates given coordinates into downsampled coordinates and returns a value from the lookup table on that position
        /// </summary>
        /// <param name="x">Position on the x axis</param>
        /// <param name="z">Position on the z axis</param>
        /// <param name="lookupTable">Lookup table to be used to interpolate</param>
        public float Interpolate(int x, int z, float[] lookupTable)
        {
            float xs = (x + 0.5f) * scale;
            float zs = (z + 0.5f) * scale;

            int x0 = Helpers.FastFloor(xs);
            int z0 = Helpers.FastFloor(zs);

            xs = (xs - x0);
            zs = (zs - z0);

            int lookupIndex = Helpers.GetIndex1DFrom2D(x0, z0, size);
            int lookupIndex2 = lookupIndex + size; // x0,z0+1

            return Helpers.Interpolate(
                Helpers.Interpolate(lookupTable[lookupIndex], lookupTable[lookupIndex + 1], xs),
                Helpers.Interpolate(lookupTable[lookupIndex2], lookupTable[lookupIndex2 + 1], xs),
                zs);
        }

        /// <summary>
        /// Interpolates given coordinates into downsampled coordinates and returns a value from the lookup table on that position
        /// </summary>
        /// <param name="x">Position on the x axis</param>
        /// <param name="y">Position on the y axis</param>
        /// <param name="z">Position on the z axis</param>
        /// <param name="lookupTable">Lookup table to be used to interpolate</param>
        public float Interpolate(int x, int y, int z, float[] lookupTable)
        {
            float xs = (x + 0.5f) * scale;
            float ys = (y + 0.5f) * scale;
            float zs = (z + 0.5f) * scale;

            int x0 = Helpers.FastFloor(xs);
            int y0 = Helpers.FastFloor(ys);
            int z0 = Helpers.FastFloor(zs);

            xs = (xs - x0);
            ys = (ys - y0);
            zs = (zs - z0);

            int lookupIndex = Helpers.GetIndex1DFrom3D(x0, y0, z0, size, size);
            int lookupIndexY = lookupIndex + sizePow2; // x0, y0+1, z0
            int lookupIndexZ = lookupIndex + size;  // x0, y0, z0+1
            int lookupIndexYZ = lookupIndex + sizePow2plusSize; // x0, y0+1, z0+1

            return Helpers.Interpolate(
                Helpers.Interpolate(
                    Helpers.Interpolate(lookupTable[lookupIndex], lookupTable[lookupIndex + 1], xs),
                    Helpers.Interpolate(lookupTable[lookupIndexY], lookupTable[lookupIndexY + 1], xs),
                    ys),
                Helpers.Interpolate(
                    Helpers.Interpolate(lookupTable[lookupIndexZ], lookupTable[lookupIndexZ + 1], xs),
                    Helpers.Interpolate(lookupTable[lookupIndexYZ], lookupTable[lookupIndexYZ + 1], xs),
                    ys),
                zs);
        }
    }
}
