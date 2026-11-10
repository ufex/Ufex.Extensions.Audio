using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class CuesheetMetadataBlock : MetadataBlock
{
	public Byte[] MediaCatalogNumber { get; }
	public UInt64 LeadInSamples { get; }
	public Byte Flags { get; }
	public Boolean IsCompactDisc { get; }
	public Byte[] Reserved { get; }
	public Byte TrackCount { get; }
	public IReadOnlyList<CuesheetTrack> Tracks { get; }

	internal CuesheetMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		MediaCatalogNumber = ReadBytes(fr, EndPosition, 128, "cuesheet media catalog number");
		EnsureAvailable(fr, 9, "cuesheet lead-in and flags");
		LeadInSamples = fr.ReadUInt64();
		Flags = fr.ReadByte();
		IsCompactDisc = (Flags & 0x80) != 0;
		Reserved = ReadBytes(fr, EndPosition, 258, "cuesheet reserved data");
		EnsureAvailable(fr, 1, "cuesheet track count");
		TrackCount = fr.ReadByte();

		var tracks = new List<CuesheetTrack>(TrackCount);
		for (Int32 i = 0; i < TrackCount; i++)
			tracks.Add(ReadTrack(fr));
		if (fr.BaseStream.Position != EndPosition)
			throw new InvalidDataException("CUESHEET block contains trailing data");
		Tracks = tracks;
	}

	public Boolean ReservedBitsAreZero => (Flags & 0x7F) == 0 && Reserved.All(value => value == 0) && Tracks.All(track => track.ReservedBitsAreZero);

	private CuesheetTrack ReadTrack(FileReader fr)
	{
		Int64 startPosition = fr.BaseStream.Position;
		EnsureAvailable(fr, 36, "cuesheet track");
		UInt64 offset = fr.ReadUInt64();
		Byte number = fr.ReadByte();
		Byte[] isrc = fr.ReadBytes(12);
		Byte flags = fr.ReadByte();
		Byte[] reserved = fr.ReadBytes(13);
		Byte indexCount = fr.ReadByte();
		var indices = new List<CuesheetTrackIndex>(indexCount);
		for (Int32 i = 0; i < indexCount; i++)
		{
			Int64 indexStart = fr.BaseStream.Position;
			EnsureAvailable(fr, 12, "cuesheet track index");
			indices.Add(new CuesheetTrackIndex(indexStart, fr.ReadUInt64(), fr.ReadByte(), fr.ReadBytes(3)));
		}
		return new CuesheetTrack(startPosition, offset, number, isrc, flags, reserved, indices);
	}

	private void EnsureAvailable(FileReader fr, Int32 count, String fieldName)
	{
		if (EndPosition - fr.BaseStream.Position < count)
			throw new InvalidDataException($"{fieldName} extends beyond the CUESHEET block");
	}
}

public sealed class CuesheetTrack
{
	public Int64 StartPosition { get; }
	public UInt64 Offset { get; }
	public Byte Number { get; }
	public Byte[] Isrc { get; }
	public Byte Flags { get; }
	public Boolean IsNonAudio => (Flags & 0x80) != 0;
	public Boolean HasPreEmphasis => (Flags & 0x40) != 0;
	public Byte[] Reserved { get; }
	public IReadOnlyList<CuesheetTrackIndex> Indices { get; }
	public Boolean ReservedBitsAreZero => (Flags & 0x3F) == 0 && Reserved.All(value => value == 0) && Indices.All(index => index.Reserved.All(value => value == 0));

	public CuesheetTrack(Int64 startPosition, UInt64 offset, Byte number, Byte[] isrc, Byte flags, Byte[] reserved, IReadOnlyList<CuesheetTrackIndex> indices)
	{
		StartPosition = startPosition;
		Offset = offset;
		Number = number;
		Isrc = isrc;
		Flags = flags;
		Reserved = reserved;
		Indices = indices;
	}
}

public readonly record struct CuesheetTrackIndex(Int64 StartPosition, UInt64 Offset, Byte Number, Byte[] Reserved);
