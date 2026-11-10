using Ufex.API;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC;

internal sealed class FlacFrameReader
{
	private const Int32 ScanBufferSize = 64 * 1024;
	private static readonly UInt16[] Crc16Table = BuildCrc16Table();

	private readonly Stream _stream;
	private readonly StreamInfoMetadataBlock _streamInfo;
	private readonly Int64 _audioStart;

	public FlacFrameReader(Stream stream, StreamInfoMetadataBlock streamInfo, Int64 audioStart)
	{
		_stream = stream;
		_streamInfo = streamInfo;
		_audioStart = audioStart;
	}

	public IEnumerable<AudioFrame> ReadFrames()
	{
		if (_audioStart >= _stream.Length)
			yield break;
		if (!TryReadHeader(_stream, _audioStart, _streamInfo, out AudioFrame? currentFrame) || currentFrame == null)
			throw new InvalidDataException($"Invalid FLAC frame header at 0x{_audioStart:X}");

		currentFrame.Index = 0;
		var buffer = new Byte[ScanBufferSize];
		_stream.Position = _audioStart;
		UInt16 crc = 0;
		Boolean hasPendingByte = false;
		Byte pendingByte = 0;
		Int64 pendingPosition = 0;
		Int32 frameIndex = 0;

		while (_stream.Position < _stream.Length)
		{
			Int64 bufferStart = _stream.Position;
			Int32 bytesRead = _stream.Read(buffer, 0, buffer.Length);
			if (bytesRead == 0)
				break;

			for (Int32 i = 0; i < bytesRead; i++)
			{
				Byte currentByte = buffer[i];
				Int64 currentPosition = bufferStart + i;
				if (hasPendingByte)
				{
					Boolean possibleSync = pendingByte == 0xFF && (currentByte & 0xFE) == 0xF8;
					Boolean beyondCurrentHeader = pendingPosition - currentFrame.StartPosition >= currentFrame.HeaderLength + 2;
					if (possibleSync && beyondCurrentHeader && TryReadHeader(_stream, pendingPosition, _streamInfo, out AudioFrame? nextFrame) && nextFrame != null)
					{
						currentFrame.Complete(pendingPosition, crc == 0);
						yield return currentFrame;
						currentFrame = nextFrame;
						currentFrame.Index = ++frameIndex;
						crc = 0;
					}
					crc = UpdateCrc16(crc, pendingByte);
				}

				hasPendingByte = true;
				pendingByte = currentByte;
				pendingPosition = currentPosition;
			}
		}

		if (hasPendingByte)
			crc = UpdateCrc16(crc, pendingByte);
		currentFrame.Complete(_stream.Length, crc == 0);
		yield return currentFrame;
	}

	public static Boolean TryReadHeader(Stream stream, Int64 position, StreamInfoMetadataBlock streamInfo, out AudioFrame? frame)
	{
		Int64 originalPosition = stream.Position;
		try
		{
			stream.Position = position;
			var headerBytes = new List<Byte>(16);
			Byte first = ReadByte(stream, headerBytes);
			Byte second = ReadByte(stream, headerBytes);
			Byte third = ReadByte(stream, headerBytes);
			Byte fourth = ReadByte(stream, headerBytes);
			if (first != 0xFF || (second & 0xFE) != 0xF8)
				return Fail(out frame);

			Byte blockSizeCode = (Byte)(third >> 4);
			Byte sampleRateCode = (Byte)(third & 0x0F);
			Byte channelAssignment = (Byte)(fourth >> 4);
			Byte bitDepthCode = (Byte)((fourth >> 1) & 0x07);
			if (blockSizeCode == 0 || sampleRateCode == 15 || channelAssignment > 10 || bitDepthCode == 3 || (fourth & 1) != 0)
				return Fail(out frame);

			UInt64 codedNumber = ReadCodedNumber(stream, headerBytes, (second & 1) != 0);
			UInt32 blockSize = ReadBlockSize(stream, headerBytes, blockSizeCode);
			UInt32 sampleRate = ReadSampleRate(stream, headerBytes, sampleRateCode, streamInfo.SampleRate);
			Byte bitsPerSample = BitDepth(bitDepthCode, streamInfo.BitsPerSample);
			Byte channelCount = channelAssignment <= 7 ? (Byte)(channelAssignment + 1) : (Byte)2;
			Byte headerCrc = ReadRequiredByte(stream);
			if (CalculateCrc8(headerBytes) != headerCrc)
				return Fail(out frame);

			frame = new AudioFrame(position)
			{
				IsVariableBlockSize = (second & 1) != 0,
				BlockSizeCode = blockSizeCode,
				SampleRateCode = sampleRateCode,
				ChannelAssignment = channelAssignment,
				BitDepthCode = bitDepthCode,
				CodedNumber = codedNumber,
				FirstSampleNumber = (second & 1) != 0 ? codedNumber : checked(codedNumber * blockSize),
				BlockSize = blockSize,
				SampleRate = sampleRate,
				BitsPerSample = bitsPerSample,
				ChannelCount = channelCount,
				HeaderCrc = headerCrc,
				HeaderLength = checked((Int32)(stream.Position - position))
			};
			return true;
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException or OverflowException)
		{
			frame = null;
			return false;
		}
		finally
		{
			stream.Position = originalPosition;
		}
	}

	private static UInt64 ReadCodedNumber(Stream stream, List<Byte> headerBytes, Boolean variableBlockSize)
	{
		Byte first = ReadByte(stream, headerBytes);
		Int32 length;
		UInt64 value;
		if ((first & 0x80) == 0)
		{
			length = 1;
			value = (UInt64)(first & 0x7F);
		}
		else if ((first & 0xE0) == 0xC0)
		{
			length = 2;
			value = (UInt64)(first & 0x1F);
		}
		else if ((first & 0xF0) == 0xE0)
		{
			length = 3;
			value = (UInt64)(first & 0x0F);
		}
		else if ((first & 0xF8) == 0xF0)
		{
			length = 4;
			value = (UInt64)(first & 0x07);
		}
		else if ((first & 0xFC) == 0xF8)
		{
			length = 5;
			value = (UInt64)(first & 0x03);
		}
		else if ((first & 0xFE) == 0xFC)
		{
			length = 6;
			value = (UInt64)(first & 0x01);
		}
		else if (first == 0xFE)
		{
			length = 7;
			value = 0;
		}
		else
		{
			throw new InvalidDataException("Invalid FLAC coded number");
		}

		for (Int32 i = 1; i < length; i++)
		{
			Byte continuation = ReadByte(stream, headerBytes);
			if ((continuation & 0xC0) != 0x80)
				throw new InvalidDataException("Invalid FLAC coded number continuation byte");
			value = (value << 6) | ((UInt64)continuation & 0x3FUL);
		}

		UInt64[] minimumValues = [0, 0, 0x80, 0x800, 0x10000, 0x200000, 0x4000000, 0x80000000];
		if (value < minimumValues[length])
			throw new InvalidDataException("Overlong FLAC coded number");
		if (!variableBlockSize && value > 0x7FFFFFFF)
			throw new InvalidDataException("FLAC frame number exceeds 31 bits");
		return value;
	}

	private static UInt32 ReadBlockSize(Stream stream, List<Byte> headerBytes, Byte code)
	{
		return code switch
		{
			1 => 192,
			>= 2 and <= 5 => (UInt32)(144 << code),
			6 => (UInt32)ReadByte(stream, headerBytes) + 1,
			7 => ReadUInt16(stream, headerBytes) is UInt16 value && value != UInt16.MaxValue ? (UInt32)value + 1 : throw new InvalidDataException("Forbidden FLAC block size"),
			>= 8 and <= 15 => (UInt32)(1 << code),
			_ => throw new InvalidDataException("Reserved FLAC block size code")
		};
	}

	private static UInt32 ReadSampleRate(Stream stream, List<Byte> headerBytes, Byte code, UInt32 streamInfoRate)
	{
		return code switch
		{
			0 => streamInfoRate,
			1 => 88200,
			2 => 176400,
			3 => 192000,
			4 => 8000,
			5 => 16000,
			6 => 22050,
			7 => 24000,
			8 => 32000,
			9 => 44100,
			10 => 48000,
			11 => 96000,
			12 => checked((UInt32)ReadByte(stream, headerBytes) * 1000),
			13 => ReadUInt16(stream, headerBytes),
			14 => checked((UInt32)ReadUInt16(stream, headerBytes) * 10),
			_ => throw new InvalidDataException("Forbidden FLAC sample rate code")
		};
	}

	private static Byte BitDepth(Byte code, Byte streamInfoDepth)
	{
		return code switch
		{
			0 => streamInfoDepth,
			1 => 8,
			2 => 12,
			4 => 16,
			5 => 20,
			6 => 24,
			7 => 32,
			_ => throw new InvalidDataException("Reserved FLAC bit depth code")
		};
	}

	private static UInt16 ReadUInt16(Stream stream, List<Byte> headerBytes)
	{
		Byte high = ReadByte(stream, headerBytes);
		Byte low = ReadByte(stream, headerBytes);
		return (UInt16)((high << 8) | low);
	}

	private static Byte ReadByte(Stream stream, List<Byte> bytes)
	{
		Byte value = ReadRequiredByte(stream);
		bytes.Add(value);
		return value;
	}

	private static Byte ReadRequiredByte(Stream stream)
	{
		Int32 value = stream.ReadByte();
		if (value < 0)
			throw new EndOfStreamException();
		return (Byte)value;
	}

	private static Boolean Fail(out AudioFrame? frame)
	{
		frame = null;
		return false;
	}

	private static Byte CalculateCrc8(IEnumerable<Byte> bytes)
	{
		Byte crc = 0;
		foreach (Byte value in bytes)
		{
			crc ^= value;
			for (Int32 i = 0; i < 8; i++)
				crc = (Byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1);
		}
		return crc;
	}

	private static UInt16 UpdateCrc16(UInt16 crc, Byte value)
	{
		return (UInt16)((crc << 8) ^ Crc16Table[((crc >> 8) ^ value) & 0xFF]);
	}

	private static UInt16[] BuildCrc16Table()
	{
		var table = new UInt16[256];
		for (Int32 value = 0; value < table.Length; value++)
		{
			UInt16 crc = (UInt16)(value << 8);
			for (Int32 bit = 0; bit < 8; bit++)
				crc = (UInt16)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x8005 : crc << 1);
			table[value] = crc;
		}
		return table;
	}
}
