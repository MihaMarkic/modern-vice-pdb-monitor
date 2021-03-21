using System;
using System.Buffers;

namespace ModernVicePdbMonitor.Engine
{
    public static class BufferManager
    {
        public static ManagedBuffer GetBuffer(int minLength)
        {
            return GetBuffer(ArrayPool<byte>.Shared, minLength);
        }
        public static ManagedBuffer GetBuffer(this ArrayPool<byte> pool, int minLength)
        {
            return new ManagedBuffer(pool, pool.Rent(minLength), minLength);
        }
    }

    public readonly struct ManagedBuffer: IDisposable
    {
        public byte[] Data { get; }
        readonly ArrayPool<byte> pool;
        public int Size { get; }
        public ManagedBuffer(ArrayPool<byte> pool, byte[] data, int size)
        {
            this.pool = pool;
            Data = data;
            Size = size;
        }

        public void Dispose()
        {
            pool.Return(Data);
        }
    }
}
