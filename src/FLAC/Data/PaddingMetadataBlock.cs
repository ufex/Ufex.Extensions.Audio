using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class PaddingMetadataBlock : MetadataBlock
{
	public UInt32 NonZeroByteCount { get; }

	internal PaddingMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		UInt32 nonZeroByteCount = 0;
		for (UInt32 i = 0; i < (UInt32)length; i++)
		{
			if (fr.ReadByte() != 0)
				nonZeroByteCount++;
		}
		NonZeroByteCount = nonZeroByteCount;
	}
}
