using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace oyasumi.IO
{
	// Modified version of https://github.com/salarcode/BinaryBuffers/blob/master/Salar.BinaryBuffers/BinaryBufferReader.cs
    public class ByteReader
    {
		private readonly byte[] _data;
		private int _position;
		private int _length;

		public ByteReader(byte[] data)
		{
			_data = data ?? throw new ArgumentNullException(nameof(data));
			
			_position = 0;
			_length = data.Length;
		}

		public int Position
		{
			get => _position;
			set
			{
				if (value > _length)
					throw new ArgumentOutOfRangeException("value", "The new position cannot be larger than the length");
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", "The new position is invalid");
				_position = value;
			}
		}

		public virtual short ReadInt16() => BinaryPrimitives.ReadInt16LittleEndian(InternalReadSpan(2));

		public virtual ushort ReadUInt16() => BinaryPrimitives.ReadUInt16LittleEndian(InternalReadSpan(2));

		public virtual int ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(InternalReadSpan(4));

		public virtual uint ReadUInt32() => BinaryPrimitives.ReadUInt32LittleEndian(InternalReadSpan(4));

		public virtual long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(InternalReadSpan(8));

		public virtual ulong ReadUInt64() => BinaryPrimitives.ReadUInt64LittleEndian(InternalReadSpan(8));
		
		public virtual float ReadSingle() =>
			BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(InternalReadSpan(4)));

		public virtual double ReadDouble() =>
			BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(InternalReadSpan(8)));

		internal protected int Read7BitEncodedInt()
		 {
			var count = 0;
			var shift = 0;
			var b = 0x00;
			do {
				if (shift == 5 * 7)
					throw new FormatException("Bad format for ULEB");
				
				b = ReadByte();
				count |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);
			return count;
		}
		
		/*public virtual string ReadString()
		{
			var b = InternalReadByte();
			var n = 0;
			if (b != 0x0b)
				return string.Empty;
			var currPos = 0;
			var length = Read7BitEncodedInt();
			
			if (length == 0)
				return string.Empty;
			
			var charBytes = new byte[128];
			var charBuffer = new byte[new UTF8Encoding().GetMaxCharCount(128)];
			
			var sb = new StringBuilder();
			do
			{
				var readLength = length - currPos > 128 ? 128 : length - currPos;
				n = 
				
			} while (currPos < length);
		} */

		public virtual decimal ReadDecimal()
		{
			var buffer = InternalReadSpan(16);
			try
			{
				return new (
					new[]
					{
						BinaryPrimitives.ReadInt32LittleEndian(buffer), // lo
						BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(4)), // mid
						BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(8)), // hi
						BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(12)) // flags
					});
			}
			catch (ArgumentException e)
			{
				// ReadDecimal cannot leak out ArgumentException
				throw new IOException("Failed to read decimal value", e);
			}
		}

		public virtual byte ReadByte() => InternalReadByte();

		public virtual byte[] ReadBytes(int count) => InternalReadSpan(count).ToArray();
		
		public virtual ReadOnlySpan<byte> ReadSpan(int count) => InternalReadSpan(count);
		
		public virtual sbyte ReadSByte() => (sbyte) InternalReadByte();

		public virtual bool ReadBoolean() => InternalReadByte() != 0;

		protected byte InternalReadByte()
		{
			var origPos = _position;
			var newPos = origPos + 1;

			if ((uint) newPos > (uint) _length)
			{
				_position = _length;
				throw new EndOfStreamException("Reached to end of byte buffer.");
			}

			var b = _data[origPos];
			_position = newPos;
			return b;
		}
		
		protected byte[] InternalReadBytes(int count)
		{
			var origPos = _position;
			var newPos = origPos + count;

			if ((uint) newPos > (uint) _length)
			{
				_position = _length;
				throw new EndOfStreamException("Reached to end of byte buffer.");
			}

			var b = _data[origPos..(origPos + count)];
			_position = newPos;
			return b;
		}


		protected ReadOnlySpan<byte> InternalReadSpan(int count)
		{
			var origPos = _position;
			var newPos = origPos + count;

			if ((uint) newPos > (uint) _length)
			{
				_position = _length;
				throw new EndOfStreamException("Reached to end of byte buffer.");
			}

			var span = new ReadOnlySpan<byte>(_data, origPos, count);
			_position = newPos;
			return span;
		}
    }
}