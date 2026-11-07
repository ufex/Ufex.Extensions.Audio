using Ufex.API;

namespace Ufex.Extensions.Audio.MIDI.Data;

/// <summary>
/// The MThd chunk at the beginning of a Standard MIDI File.
/// </summary>
public class MidiHeader : Section
{
	public Byte[] ChunkId { get; init; }
	public UInt32 Length { get; init; }
	public UInt16 Format { get; init; }
	public UInt16 TrackCount { get; init; }
	public UInt16 Division { get; init; }
	public Byte[] ExtraData { get; init; }

	public Boolean UsesSmpteTimeCode => (Division & 0x8000) != 0;
	public UInt16 TicksPerQuarterNote => UsesSmpteTimeCode ? (UInt16)0 : Division;
	public Int32 SmpteFramesPerSecond => UsesSmpteTimeCode ? -unchecked((SByte)(Division >> 8)) : 0;
	public Byte TicksPerFrame => UsesSmpteTimeCode ? (Byte)(Division & 0xFF) : (Byte)0;

	public String FormatText => Format switch
	{
		0 => "Format 0 (single track)",
		1 => "Format 1 (synchronous tracks)",
		2 => "Format 2 (independent tracks)",
		_ => $"Unknown format ({Format})"
	};

	public String DivisionText
	{
		get
		{
			if (!UsesSmpteTimeCode)
				return $"{TicksPerQuarterNote} ticks per quarter note";

			String frameRate = SmpteFramesPerSecond == 29 ? "29.97 drop-frame" : SmpteFramesPerSecond.ToString();
			return $"{frameRate} frames per second, {TicksPerFrame} ticks per frame";
		}
	}

	public MidiHeader(FileReader fr)
	{
		StartRead(fr.BaseStream.Position);
		ChunkId = fr.ReadBytes(4);
		Length = fr.ReadUInt32();
		if (Length < Signatures.HeaderDataLength)
			throw new InvalidDataException($"MIDI header length must be at least {Signatures.HeaderDataLength} bytes");
		if (Length - Signatures.HeaderDataLength > Int32.MaxValue)
			throw new InvalidDataException("MIDI header extension is too large");

		Format = fr.ReadUInt16();
		TrackCount = fr.ReadUInt16();
		Division = fr.ReadUInt16();
		ExtraData = fr.ReadBytes((Int32)(Length - Signatures.HeaderDataLength));
		if (ExtraData.Length != Length - Signatures.HeaderDataLength)
			throw new EndOfStreamException("MIDI header is truncated");
		EndRead(fr.BaseStream.Position);
	}
}

