using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class StreamInfoMetadataBlock : MetadataBlock
{
	public UInt16 MinimumBlockSize { get; }
	public UInt16 MaximumBlockSize { get; }
	public UInt24 MinimumFrameSize { get; }
	public UInt24 MaximumFrameSize { get; }
	public UInt64 StreamProperties { get; }
	public UInt32 SampleRate { get; }
	public Byte Channels { get; }
	public Byte BitsPerSample { get; }
	public UInt64 TotalSamples { get; }
	public Byte[] Md5Signature { get; }

	internal StreamInfoMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		if ((UInt32)length != Signatures.StreamInfoLength)
			throw new InvalidDataException($"STREAMINFO block length must be {Signatures.StreamInfoLength} bytes, but is {(UInt32)length}");

		MinimumBlockSize = fr.ReadUInt16();
		MaximumBlockSize = fr.ReadUInt16();
		MinimumFrameSize = fr.ReadUInt24();
		MaximumFrameSize = fr.ReadUInt24();
		StreamProperties = fr.ReadUInt64();
		SampleRate = (UInt32)((StreamProperties >> 44) & 0xFFFFF);
		Channels = (Byte)(((StreamProperties >> 41) & 0x7) + 1);
		BitsPerSample = (Byte)(((StreamProperties >> 36) & 0x1F) + 1);
		TotalSamples = StreamProperties & 0xFFFFFFFFF;
		Md5Signature = ReadBytes(fr, EndPosition, 16, "STREAMINFO MD5 signature");
	}
}
