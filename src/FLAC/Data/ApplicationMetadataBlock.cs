using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class ApplicationMetadataBlock : MetadataBlock
{
	public Byte[] ApplicationId { get; }
	public Int64 ApplicationDataPosition => DataStartPosition + 4;
	public UInt32 ApplicationDataLength => (UInt32)Length - 4;

	internal ApplicationMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		if ((UInt32)length < 4)
			throw new InvalidDataException("APPLICATION block must contain a four-byte application ID");
		ApplicationId = ReadBytes(fr, EndPosition, 4, "application ID");
	}
}
