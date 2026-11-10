using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class ReservedMetadataBlock : MetadataBlock
{
	internal ReservedMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length)
		: base(startPosition, headerByte, length)
	{
	}
}
