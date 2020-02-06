using System;
using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Serialization
{
    public class Save : IBinarizable
    {
        public const short SAVE_VERSION = 1;

        public Chunk Chunk { get; private set; }
        public bool IsDifferential { get; private set; }

        //! A list of modified positions
        private BlockPos[] positionsModified;
        //! A list of modified blocks
        private BlockData[] blocksModified;

        // Temporary structures
        private byte[] positionsBytes;
        private byte[] blocksBytes;

        public Save(Chunk chunk)
        {
            Chunk = chunk;
            IsDifferential = false;
        }

        public void Reset()
        {
            MarkAsProcessed();
            ResetTemporary();
        }

        public void MarkAsProcessed()
        {
            // Release the memory allocated by temporary buffers
            positionsModified = null;
            blocksModified = null;
        }

        private void ResetTemporary()
        {
            // Reset temporary buffers
            positionsBytes = null;
            blocksBytes = null;
        }

        public bool IsBinarizeNecessary()
        {
            // When doing a pure differential serialization we need data
            return !Features.useDifferentialSerialization || Features.useDifferentialSerialization_ForceSaveHeaders || blocksModified != null;
        }

        public bool Binarize(BinaryWriter bw)
        {
            bw.Write(SAVE_VERSION);
            bw.Write((byte)(Features.useDifferentialSerialization ? 1 : 0));
            bw.Write(Env.CHUNK_SIZE_POW_3);
            bw.Write(Chunk.Blocks.nonEmptyBlocks);

            int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
            int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

            // Chunk data
            if (Features.useDifferentialSerialization)
            {
                if (blocksModified == null)
                {
                    bw.Write(0);
                }
                else
                {
                    int posLenBytes = blocksModified.Length * blockPosSize;
                    int blkLenBytes = blocksModified.Length * blockDataSize;

                    bw.Write(blocksModified.Length);
                    bw.Write(positionsBytes, 0, posLenBytes);
                    bw.Write(blocksBytes, 0, blkLenBytes);
                }
            }
            else
            {
                // Write compressed data to file
                bw.Write(blocksBytes.Length);
                bw.Write(blocksBytes, 0, blocksBytes.Length);
            }

            ResetTemporary();
            return true;
        }

        public bool Debinarize(BinaryReader br)
        {
            bool success = true;

            // Read the version number
            int version = br.ReadInt16();
            if (version != SAVE_VERSION)
            {
                return false;
            }

            // 0/1 allowed for IsDifferential
            byte isDifferential = br.ReadByte();
            if (isDifferential != 0 && isDifferential != 1)
            {
                success = false;
                goto deserializeFail;
            }
            IsDifferential = isDifferential == 1;

            // Current chunk size must match the saved chunk size
            int chunkBlocks = br.ReadInt32();
            if (chunkBlocks != Env.CHUNK_SIZE_POW_3)
            {
                success = false;
                goto deserializeFail;
            }

            // NonEmptyBlocks must be a sane number in chunkBlocks range
            int nonEmptyBlocks = br.ReadInt32();
            if (nonEmptyBlocks < 0 || nonEmptyBlocks > chunkBlocks)
            {
                success = false;
                goto deserializeFail;
            }
            Chunk.Blocks.nonEmptyBlocks = nonEmptyBlocks;

            while (true)
            {
                if (IsDifferential)
                {
                    int lenBlocks = br.ReadInt32();
                    if (lenBlocks > 0)
                    {
                        int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                        int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                        int posLenBytes = lenBlocks * blockPosSize;
                        int blkLenBytes = lenBlocks * blockDataSize;

                        positionsBytes = new byte[posLenBytes];
                        int read = br.Read(positionsBytes, 0, posLenBytes);
                        if (read != posLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }

                        blocksBytes = new byte[blkLenBytes];
                        read = br.Read(blocksBytes, 0, blkLenBytes);
                        if (read != blkLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }
                    }
                    else
                    {
                        blocksBytes = null;
                        positionsBytes = null;
                    }
                }
                else
                {
                    int blkLenBytes = br.ReadInt32();
                    blocksBytes = new byte[blkLenBytes];

                    // Read raw data
                    int readLength = br.Read(blocksBytes, 0, blkLenBytes);
                    if (readLength != blkLenBytes)
                    {
                        // Length must match
                        success = false;
                        goto deserializeFail;
                    }
                }

                break;
            }
        deserializeFail:
            if (!success)
            {
                // Revert any changes we performed on our chunk
                Chunk.Blocks.nonEmptyBlocks = -1;
                ResetTemporary();
            }

            return success;
        }

        public bool DoCompression()
        {
            if (Features.useDifferentialSerialization)
            {
                Load_Resources.Blocks.BlockProvider provider = Chunk.World.blockProvider;
                int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                int posLenBytes = blocksModified.Length * blockPosSize;
                int blkLenBytes = blocksModified.Length * blockDataSize;
                positionsBytes = new byte[posLenBytes];
                blocksBytes = new byte[blkLenBytes];

                unsafe
                {
                    // Pack positions to a byte array
                    fixed (byte* pDst = positionsBytes)
                    {
                        for (int i = 0, j = 0; i < blocksModified.Length; i++, j += blockPosSize)
                        {
                            *(BlockPos*)&pDst[j] = positionsModified[i];
                        }
                    }
                    // Pack block data to a byte array
                    fixed (BlockData* pBD = blocksModified)
                    fixed (byte* pDst = blocksBytes)
                    {
                        for (int i = 0, j = 0; i < blocksModified.Length; i++, j += blockDataSize)
                        {
                            BlockData* bd = &pBD[i];
                            // Convert block types from internal optimized version into global types
                            ushort typeInConfig = provider.GetConfig(bd->Type).TypeInConfig;

                            *(BlockData*)&pDst[j] = new BlockData(typeInConfig, bd->Solid);
                        }
                    }
                }
            }
            else
            {
                Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(Chunk.ThreadID);
                Load_Resources.Blocks.BlockProvider provider = Chunk.World.blockProvider;

                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.CHUNK_SIZE_POW_3 * blockDataSize;

                // Pop large enough buffers from the pool
                byte[] tmp = pools.byteArrayPool.Pop(requestedByteSize);
                byte[] bytesCompressed = pools.byteArrayPool.Pop(requestedByteSize);
                {
                    ChunkBlocks blocks = Chunk.Blocks;
                    int i = 0;

                    int index = Helpers.ZERO_CHUNK_INDEX;
                    int yOffset = Env.CHUNK_SIZE_WITH_PADDING_POW_2 - Env.CHUNK_SIZE * Env.CHUNK_SIZE_WITH_PADDING;
                    int zOffset = Env.CHUNK_SIZE_WITH_PADDING - Env.CHUNK_SIZE;

                    for (int y = 0; y < Env.CHUNK_SIZE; ++y, index += yOffset)
                    {
                        for (int z = 0; z < Env.CHUNK_SIZE; ++z, index += zOffset)
                        {
                            for (int x = 0; x < Env.CHUNK_SIZE; ++x, i += blockDataSize, ++index)
                            {
                                BlockData bd = blocks.Get(index);

                                // Convert block types from internal optimized version into global types
                                ushort typeInConfig = provider.GetConfig(bd.Type).TypeInConfig;

                                // Write updated block data to destination buffer
                                unsafe
                                {
                                    fixed (byte* pDst = tmp)
                                    {
                                        *(BlockData*)&pDst[i] = new BlockData(typeInConfig, bd.Solid);
                                    }
                                }
                            }
                        }
                    }

                    // Compress bytes
                    int blkLenBytes = CLZF2.lzf_compress(tmp, requestedByteSize, ref bytesCompressed);
                    blocksBytes = new byte[blkLenBytes];

                    // Copy data from a temporary buffer to block buffer
                    Array.Copy(bytesCompressed, 0, blocksBytes, 0, blkLenBytes);
                }

                // Return our temporary buffer back to the pool
                pools.byteArrayPool.Push(bytesCompressed);
                pools.byteArrayPool.Push(tmp);
            }

            return true;
        }

        public bool CanDecompress()
        {
            return IsDifferential ? positionsBytes != null && blocksBytes != null : blocksBytes != null;
        }

        public bool DoDecompression()
        {
            Common.MemoryPooling.LocalPools pools = Globals.WorkPool.GetPool(Chunk.ThreadID);
            Load_Resources.Blocks.BlockProvider provider = Chunk.World.blockProvider;

            if (IsDifferential)
            {
                int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                positionsModified = new BlockPos[positionsBytes.Length / blockPosSize];
                blocksModified = new BlockData[blocksBytes.Length / blockDataSize];

                int i, j;
                unsafe
                {
                    // Extract positions
                    fixed (byte* pSrc = positionsBytes)
                    {
                        for (i = 0, j = 0; j < positionsModified.Length; i += blockPosSize, j++)
                        {
                            positionsModified[j] = *(BlockPos*)&pSrc[i];
                            Chunk.Blocks.modifiedBlocks.Add(positionsModified[j]);
                        }
                    }
                    // Extract block data
                    fixed (byte* pSrc = blocksBytes)
                    {
                        for (i = 0, j = 0; j < blocksModified.Length; i += blockDataSize, j++)
                        {
                            BlockData* bd = (BlockData*)&pSrc[i];
                            // Convert global block types into internal optimized version
                            ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                            blocksModified[j] = new BlockData(type, bd->Solid);
                        }
                    }
                }
            }
            else
            {
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.CHUNK_SIZE_POW_3 * blockDataSize;

                // Pop a large enough buffers from the pool
                byte[] bytes = pools.byteArrayPool.Pop(requestedByteSize);
                {
                    // Decompress data
                    int decompressedLength = CLZF2.lzf_decompress(blocksBytes, blocksBytes.Length, ref bytes);
                    if (decompressedLength != Env.CHUNK_SIZE_POW_3 * blockDataSize)
                    {
                        blocksBytes = null;
                        return false;
                    }

                    // Fill chunk with decompressed data
                    ChunkBlocks blocks = Chunk.Blocks;
                    int i = 0;
                    unsafe
                    {
                        fixed (byte* pSrc = bytes)
                        {
                            int index = Helpers.ZERO_CHUNK_INDEX;
                            int yOffset = Env.CHUNK_SIZE_WITH_PADDING_POW_2 - Env.CHUNK_SIZE * Env.CHUNK_SIZE_WITH_PADDING;
                            int zOffset = Env.CHUNK_SIZE_WITH_PADDING - Env.CHUNK_SIZE;

                            for (int y = 0; y < Env.CHUNK_SIZE; ++y, index += yOffset)
                            {
                                for (int z = 0; z < Env.CHUNK_SIZE; ++z, index += zOffset)
                                {
                                    for (int x = 0; x < Env.CHUNK_SIZE; ++x, i += blockDataSize, ++index)
                                    {
                                        BlockData* bd = (BlockData*)&pSrc[i];

                                        // Convert global block type into internal optimized version
                                        ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                        blocks.SetRaw(index, new BlockData(type, bd->Solid));
                                    }
                                }
                            }
                        }
                    }
                }
                // Return our temporary buffer back to the pool
                pools.byteArrayPool.Push(bytes);
            }

            ResetTemporary();
            return true;
        }

        public bool ConsumeChanges()
        {
            ChunkBlocks blocks = Chunk.Blocks;

            if (!Features.useDifferentialSerialization)
            {
                return true;
            }

            if (Features.useDifferentialSerialization_ForceSaveHeaders)
            {
                if (blocks.modifiedBlocks.Count <= 0)
                {
                    return true;
                }
            }
            else
            {
                if (blocks.modifiedBlocks.Count <= 0)
                {
                    return false;
                }
            }

            Dictionary<BlockPos, BlockData> blocksDictionary = new Dictionary<BlockPos, BlockData>();

            // Create a map of modified blocks and their positions
            // TODO: Depending on the amount of changes this could become a performance bottleneck
            for (int i = 0; i < blocks.modifiedBlocks.Count; i++)
            {
                BlockPos pos = blocks.modifiedBlocks[i];
                // Remove any existing blocks in the dictionary. They come from the existing save and are overwritten
                blocksDictionary.Remove(pos);
                blocksDictionary.Add(pos, blocks.Get(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z)));
            }

            int cnt = blocksDictionary.Keys.Count;
            if (cnt > 0)
            {
                blocksModified = new BlockData[cnt];
                positionsModified = new BlockPos[cnt];

                int index = 0;
                foreach (KeyValuePair<BlockPos, BlockData> pair in blocksDictionary)
                {
                    blocksModified[index] = pair.Value;
                    positionsModified[index] = pair.Key;
                    ++index;
                }
            }

            return true;
        }

        public void CommitChanges()
        {
            if (!IsDifferential)
            {
                return;
            }

            // Rewrite generated blocks with differential positions
            if (blocksModified != null)
            {
                for (int i = 0; i < blocksModified.Length; i++)
                {
                    BlockPos pos = positionsModified[i];
                    Chunk.Blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z), blocksModified[i]);
                }
            }

            MarkAsProcessed();
        }
    }
}
