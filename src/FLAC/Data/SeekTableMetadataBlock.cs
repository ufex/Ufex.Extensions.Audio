using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class SeekTableMetadataBlock : MetadataBlock
{
	public UInt32 PointCount { get; }
	public Boolean IsSorted { get; }
	public Boolean HasDuplicateSampleNumbers { get; }
	public Boolean HasPlaceholderBeforeRealPoint { get; }

	internal SeekTableMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		if ((UInt32)length % Signatures.SeekPointLength != 0)
			throw new InvalidDataException("SEEKTABLE block length must be a multiple of 18 bytes");

		PointCount = (UInt32)length / Signatures.SeekPointLength;
		Boolean isSorted = true;
		Boolean hasDuplicates = false;
		Boolean placeholderSeen = false;
		Boolean placeholderBeforeRealPoint = false;
		UInt64? previousSample = null;
		for (UInt32 i = 0; i < PointCount; i++)
		{
			UInt64 sampleNumber = fr.ReadUInt64();
			fr.BaseStream.Seek(10, SeekOrigin.Current);
			if (sampleNumber == Signatures.PlaceholderSampleNumber)
			{
				placeholderSeen = true;
				continue;
			}
			if (placeholderSeen)
				placeholderBeforeRealPoint = true;
			if (previousSample != null)
			{
				if (sampleNumber < previousSample.Value)
					isSorted = false;
				if (sampleNumber == previousSample.Value)
					hasDuplicates = true;
			}
			previousSample = sampleNumber;
		}
		IsSorted = isSorted;
		HasDuplicateSampleNumbers = hasDuplicates;
		HasPlaceholderBeforeRealPoint = placeholderBeforeRealPoint;
	}
}

public readonly record struct SeekPoint(UInt64 SampleNumber, UInt64 StreamOffset, UInt16 FrameSamples);
