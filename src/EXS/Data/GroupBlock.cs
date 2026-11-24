using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public sealed class GroupBlock : ExsBlock
{
	public SByte Volume { get; }
	public SByte Pan { get; }
	public Byte Polyphony { get; }
	public Byte Options { get; }
	public Byte ExclusiveClass { get; }
	public Byte VelocityLow { get; }
	public Byte VelocityHigh { get; }
	public SByte RandomOffset { get; }
	public Byte UnknownByte08 { get; }
	public UInt32 ReleaseTriggerDecayTime { get; }
	public SByte VelocityCrossfade { get; }
	public Byte VelocityCrossfadeType { get; }
	public Byte NoteCrossfadeType { get; }
	public SByte NoteCrossfade { get; }
	public SByte LivePreAnchorTime { get; }
	public SByte DynamicRangeOffset { get; }
	public Byte TempoLow { get; }
	public Byte TempoHigh { get; }
	public SByte FilterCutoffOffset { get; }
	public SByte FilterResonanceOffset { get; }
	public Int32 Envelope1Attack { get; }
	public Int32 Envelope1Decay { get; }
	public Int32 Envelope1Sustain { get; }
	public Int32 Envelope1Release { get; }
	public Byte ReleaseTrigger { get; }
	public Byte Output { get; }
	public Byte EnableByNote { get; }
	public Int32 GroupCycle { get; }
	public Byte EnableByType { get; }
	public Byte ControllerNumber { get; }
	public Byte ControllerLow { get; }
	public Byte ControllerHigh { get; }
	public Byte KeyLow { get; }
	public Byte KeyHigh { get; }
	public Byte MidiChannelZeroBased { get; }
	public Byte Articulation { get; }
	public SByte? EnableByType2 { get; }
	public SByte? EnableByType3 { get; }
	public Byte? PitchLow { get; }
	public Byte? PitchHigh { get; }
	public Int32? Envelope1Hold { get; }
	public Int32? Envelope2Attack { get; }
	public Int32? Envelope2Decay { get; }
	public Int32? Envelope2Sustain { get; }
	public Int32? Envelope2Release { get; }
	public Int32? Envelope2Hold { get; }
	public Int32? Envelope1Delay { get; }
	public Int32? Envelope2Delay { get; }

	public GroupBlock(ExsBlockHeader header, FileReader fr) : base(header)
	{
		if (ContentSize < 92)
			throw new InvalidDataException("EXS group block is shorter than 92 bytes");
		fr.BaseStream.Position = ContentPosition;
		Volume = fr.ReadSByte(); Pan = fr.ReadSByte(); Polyphony = fr.ReadByte(); Options = fr.ReadByte();
		ExclusiveClass = fr.ReadByte(); VelocityLow = fr.ReadByte(); VelocityHigh = fr.ReadByte();
		RandomOffset = fr.ReadSByte(); UnknownByte08 = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 16;
		ReleaseTriggerDecayTime = fr.ReadUInt32();
		fr.BaseStream.Position = ContentPosition + 32;
		VelocityCrossfade = (SByte)(fr.ReadByte() - 128); VelocityCrossfadeType = fr.ReadByte();
		NoteCrossfadeType = fr.ReadByte(); NoteCrossfade = (SByte)(fr.ReadByte() - 128);
		LivePreAnchorTime = fr.ReadSByte(); DynamicRangeOffset = fr.ReadSByte();
		TempoLow = fr.ReadByte(); TempoHigh = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 41; FilterCutoffOffset = fr.ReadSByte();
		fr.BaseStream.Position = ContentPosition + 43; FilterResonanceOffset = fr.ReadSByte();
		fr.BaseStream.Position = ContentPosition + 56;
		Envelope1Attack = fr.ReadInt32(); Envelope1Decay = fr.ReadInt32();
		Envelope1Sustain = fr.ReadInt32(); Envelope1Release = fr.ReadInt32();
		fr.BaseStream.Position = ContentPosition + 73;
		ReleaseTrigger = fr.ReadByte(); Output = fr.ReadByte(); EnableByNote = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 80;
		GroupCycle = fr.ReadInt32(); EnableByType = fr.ReadByte(); ControllerNumber = fr.ReadByte();
		ControllerLow = fr.ReadByte(); ControllerHigh = fr.ReadByte(); KeyLow = fr.ReadByte();
		KeyHigh = fr.ReadByte(); MidiChannelZeroBased = fr.ReadByte(); Articulation = fr.ReadByte();
		EnableByType2 = ReadSByte(fr, 92); EnableByType3 = ReadSByte(fr, 93);
		PitchLow = ReadByte(fr, 94); PitchHigh = ReadByte(fr, 95);
		Envelope1Hold = ReadInt32(fr, 96); Envelope2Attack = ReadInt32(fr, 100);
		Envelope2Decay = ReadInt32(fr, 104); Envelope2Sustain = ReadInt32(fr, 108);
		Envelope2Release = ReadInt32(fr, 112); Envelope2Hold = ReadInt32(fr, 116);
		Envelope1Delay = ReadInt32(fr, 124); Envelope2Delay = ReadInt32(fr, 128);
	}

	private Byte? ReadByte(FileReader fr, Int32 offset)
	{
		if (ContentSize <= offset) return null;
		fr.BaseStream.Position = ContentPosition + offset;
		return fr.ReadByte();
	}

	private SByte? ReadSByte(FileReader fr, Int32 offset)
	{
		if (ContentSize <= offset) return null;
		fr.BaseStream.Position = ContentPosition + offset;
		return fr.ReadSByte();
	}

	private Int32? ReadInt32(FileReader fr, Int32 offset)
	{
		if (ContentSize < offset + 4) return null;
		fr.BaseStream.Position = ContentPosition + offset;
		return fr.ReadInt32();
	}
}
