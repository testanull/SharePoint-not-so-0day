using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestCacheSP
{
    internal class ChunkStream : Stream
    {
        public ChunkStream()
            : this(65536)
        {
        }

        public ChunkStream(int capacity)
        {
            if (capacity == 0)
            {
                capacity = 65536;
            }
            int num = capacity / 65536;
            if (capacity % 65536 > 0)
            {
                this._buffers = new byte[num + 1][];
                this._buffers[num] = new byte[capacity - num * 65536];
            }
            else
            {
                this._buffers = new byte[num][];
            }
            for (int i = 0; i < num; i++)
            {
                this._buffers[i] = new byte[65536];
            }
            this._currentBuffer = this._buffers[0];
            this._isOpen = true;
            this._isWritable = true;
            this._capacity = capacity;
        }

        public ChunkStream(byte[][] buffers)
            : this(buffers, false)
        {
        }

       

        public ChunkStream(byte[][] buffers, bool writable)
        {
            int num = 0;
            for (int i = 0; i < buffers.Length; i++)
            {
                num += buffers[i].Length;
            }
            this._buffers = buffers;
            this._currentBuffer = this._buffers[0];
            this._isOpen = true;
            this._isWritable = writable;
            this._length = num;
            this._capacity = num;
            if (writable)
            {
                this._position = num;
                this._buffersPos = this._buffers.Length - 1;
                this._currentBuffer = this._buffers[this._buffersPos];
                this._posInCurrentBuffer = this._currentBuffer.Length;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this._isOpen = false;
                    this._isWritable = false;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private bool EnsureCapacity(int value)
        {
            if (value <= 65536)
            {
                this.Capacity = 65536;
            }
            else
            {
                this.Capacity = (value + 65536) / 65536 * 65536;
            }
            return true;
        }

        public override void Flush()
        {
        }

        public virtual byte[] GetBuffer()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            int num = this._length - this._position;
            if (num > count)
            {
                num = count;
            }
            if (num <= 0)
            {
                return 0;
            }
            int i = num;
            while (i > 0)
            {
                int num2 = Math.Min(i, this._currentBuffer.Length - this._posInCurrentBuffer);
                if (num2 <= 8)
                {
                    int num3 = num2;
                    while (--num3 >= 0)
                    {
                        buffer[offset + num3] = this._currentBuffer[this._posInCurrentBuffer + num3];
                    }
                }
                else
                {
                    Buffer.BlockCopy(this._currentBuffer, this._posInCurrentBuffer, buffer, offset, num2);
                }
                i -= num2;
                offset += num2;
                this._position += num2;
                this._posInCurrentBuffer += num2;
                if (this._posInCurrentBuffer == this._currentBuffer.Length && this._buffersPos < this._buffers.Length && i != 0)
                {
                    this._buffersPos++;
                    this._currentBuffer = this._buffers[this._buffersPos];
                    this._posInCurrentBuffer = 0;
                }
            }
            return num;
        }

        public override int ReadByte()
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (this._position >= this._length)
            {
                return -1;
            }
            while (this._posInCurrentBuffer == this._currentBuffer.Length && this._buffersPos < this._buffers.Length - 1)
            {
                this._buffersPos++;
                this._currentBuffer = this._buffers[this._buffersPos];
                this._posInCurrentBuffer = 0;
            }
            this._position++;
            this._posInCurrentBuffer++;
            return (int)this._currentBuffer[this._posInCurrentBuffer - 1];
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            long num = 0L;
            switch (loc)
            {
                case SeekOrigin.Begin:
                    num = offset;
                    break;
                case SeekOrigin.Current:
                    num = (long)this._position + offset;
                    break;
                case SeekOrigin.End:
                    num = (long)this._length + offset;
                    break;
            }
            if (num < 0L)
            {
                throw new IOException("Seek: OutOfRange");
            }
            if (num > 2147483647L)
            {
                throw new IOException("Seek: OutOfRange");
            }
            this._position = (int)num;
            this.SetBufferVariables(this._position);
            return (long)this._position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public virtual byte[] ToArray()
        {
            throw new NotImplementedException();
        }

        public byte[][] ToChunkedArray()
        {
            if (this._capacity == this._length)
            {
                return this._buffers;
            }
            byte[][] array = new byte[this._buffersPos + 1][];
            for (int i = 0; i < this._buffersPos; i++)
            {
                array[i] = this._buffers[i];
            }
            if (this._posInCurrentBuffer == this._currentBuffer.Length)
            {
                array[this._buffersPos] = this._currentBuffer;
            }
            else
            {
                array[this._buffersPos] = new byte[this._posInCurrentBuffer];
                Buffer.BlockCopy(this._currentBuffer, 0, array[this._buffersPos], 0, this._posInCurrentBuffer);
            }
            return array;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (!this._isWritable)
            {
                throw new NotSupportedException();
            }
            int num = this._position + count;
            if (num < 0)
            {
                throw new IOException("IOStreamTooLong");
            }
            if (num > this._length)
            {
                if (num > this._capacity)
                {
                    this.EnsureCapacity(num);
                }
                if (this._position > this._length)
                {
                    this.SetBufferVariables(this._position);
                }
                this._length = num;
            }
            while (count > 0)
            {
                int num2 = Math.Min(count, this._currentBuffer.Length - this._posInCurrentBuffer);
                if (num2 <= 8)
                {
                    int num3 = num2;
                    while (--num3 >= 0)
                    {
                        this._currentBuffer[this._posInCurrentBuffer + num3] = buffer[offset + num3];
                    }
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, this._currentBuffer, this._posInCurrentBuffer, num2);
                }
                count -= num2;
                offset += num2;
                this._position += num2;
                this._posInCurrentBuffer += num2;
                if (this._posInCurrentBuffer == this._currentBuffer.Length && this._buffersPos < this._buffers.Length - 1)
                {
                    this._buffersPos++;
                    this._currentBuffer = this._buffers[this._buffersPos];
                    this._posInCurrentBuffer = 0;
                }
            }
        }

        public override void WriteByte(byte value)
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (!this._isWritable)
            {
                throw new NotSupportedException();
            }
            int num = this._position + 1;
            if (num < 0)
            {
                throw new IOException("IOStreamTooLong");
            }
            if (num > this._length)
            {
                if (num > this._capacity)
                {
                    this.EnsureCapacity(num);
                }
                if (this._position > this._length)
                {
                    this.SetBufferVariables(this._position);
                }
                this._length = num;
            }
            while (this._posInCurrentBuffer == this._currentBuffer.Length && this._buffersPos < this._buffers.Length - 1)
            {
                this._buffersPos++;
                this._currentBuffer = this._buffers[this._buffersPos];
                this._posInCurrentBuffer = 0;
            }
            this._position++;
            this._posInCurrentBuffer++;
            this._currentBuffer[this._posInCurrentBuffer - 1] = value;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (!this._isOpen)
            {
                throw new ObjectDisposedException(null);
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get
            {
                return this._isOpen;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return this._isWritable;
            }
        }

        public virtual int Capacity
        {
            get
            {
                if (!this._isOpen)
                {
                    throw new ObjectDisposedException(null);
                }
                return this._capacity;
            }
            set
            {
                if (!this._isOpen)
                {
                    throw new ObjectDisposedException(null);
                }
                if (value < this._capacity)
                {
                    throw new NotImplementedException();
                }
                if (value > this._capacity)
                {
                    int num = value - this._capacity;
                    int num2 = num / 65536;
                    byte[][] array;
                    if (num % 65536 > 0)
                    {
                        array = new byte[this._buffers.Length + num2 + 1][];
                        array[this._buffers.Length + num2] = new byte[num % 65536];
                    }
                    else
                    {
                        array = new byte[this._buffers.Length + num2][];
                    }
                    for (int i = 0; i < this._buffers.Length; i++)
                    {
                        array[i] = this._buffers[i];
                    }
                    for (int j = 0; j < num2; j++)
                    {
                        array[this._buffers.Length + j] = new byte[65536];
                    }
                    this._buffers = array;
                    this._capacity = value;
                }
            }
        }

        public override long Length
        {
            get
            {
                if (!this._isOpen)
                {
                    throw new ObjectDisposedException(null);
                }
                return (long)this._length;
            }
        }

        public override long Position
        {
            get
            {
                if (!this._isOpen)
                {
                    throw new ObjectDisposedException(null);
                }
                return (long)this._position;
            }
            set
            {
                if (!this._isOpen)
                {
                    throw new ObjectDisposedException(null);
                }
                if (value < 0L)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                if (value > 2147483647L)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                this._position = (int)value;
                this.SetBufferVariables(this._position);
            }
        }

        private void SetBufferVariables(int position)
        {
            int num = 0;
            for (int i = 0; i < this._buffers.Length; i++)
            {
                num += this._buffers[i].Length;
                if (num >= position)
                {
                    this._currentBuffer = this._buffers[i];
                    this._buffersPos = i;
                    break;
                }
            }
            if (position > num)
            {
                this._buffersPos = this._buffers.Length - 1;
                this._currentBuffer = this._buffers[this._buffersPos];
                this._posInCurrentBuffer = this._currentBuffer.Length;
                return;
            }
            this._posInCurrentBuffer = this._currentBuffer.Length - (num - position);
        }

        public const int MaxAllocSize = 65536;

        private byte[][] _buffers;

        private int _buffersPos;

        private byte[] _currentBuffer;

        private int _posInCurrentBuffer;

        private bool _isOpen;

        private bool _isWritable;

        private int _length;

        private int _capacity;

        private int _position;
    }
}
