using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public sealed class ZoneBlock : ExsBlock
{
	public Byte Options { get; }
	public Byte RootNote { get; }
	public SByte FineTune { get; }
	public SByte Pan { get; }
	public SByte Volume { get; }
	public SByte VolumeScale { get; }
	public Byte KeyLow { get; }
	public Byte KeyHigh { get; }
	public Byte VelocityLow { get; }
	public Byte VelocityHigh { get; }
	public UInt32 SampleStart { get; }
	public UInt32 SampleEnd { get; }
	public UInt32 LoopStart { get; }
	public UInt32 LoopEnd { get; }
	public UInt32 LoopCrossfade { get; }
	public SByte LoopTune { get; }
	public Byte LoopOptions { get; }
	public Byte LoopMode { get; }
	public SByte FlexPitchOffset { get; }
	public Byte FlexOptions { get; }
	public SByte FlexSpeed { get; }
	public SByte TailTune { get; }
	public SByte CoarseTune { get; }
	public Byte Output { get; }
	public Int32 GroupIndex { get; }
	public UInt32 SampleIndex { get; }
	public UInt32? FadeOutFrames { get; }
	public UInt32? SyncFrameOffset { get; }
	public Int32? TailSampleIndex { get; }
	public UInt32? TailStart { get; }
	public UInt32? TailEnd { get; }
	public Single? PreciseVolume { get; }
	public Single? TailVolume { get; }
	public UInt32? FadeInFrames { get; }

	public ZoneBlock(ExsBlockHeader header, FileReader fr) : base(header)
	{
		if (ContentSize < 96)
			throw new InvalidDataException("EXS zone block is shorter than 96 bytes");
		fr.BaseStream.Position = ContentPosition;
		Options = fr.ReadByte();
		RootNote = fr.ReadByte();
		FineTune = fr.ReadSByte();
		Pan = fr.ReadSByte();
		Volume = fr.ReadSByte();
		VolumeScale = fr.ReadSByte();
		KeyLow = fr.ReadByte();
		KeyHigh = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 9;
		VelocityLow = fr.ReadByte();
		VelocityHigh = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 12;
		SampleStart = fr.ReadUInt32();
		SampleEnd = fr.ReadUInt32();
		LoopStart = fr.ReadUInt32();
		LoopEnd = fr.ReadUInt32();
		LoopCrossfade = fr.ReadUInt32();
		LoopTune = fr.ReadSByte();
		LoopOptions = fr.ReadByte();
		LoopMode = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 50;
		FlexPitchOffset = fr.ReadSByte();
		fr.BaseStream.Position = ContentPosition + 77;
		FlexOptions = fr.ReadByte();
		FlexSpeed = fr.ReadSByte();
		TailTune = fr.ReadSByte();
		CoarseTune = fr.ReadSByte();
		fr.BaseStream.Position = ContentPosition + 82;
		Output = fr.ReadByte();
		fr.BaseStream.Position = ContentPosition + 88;
		GroupIndex = fr.ReadInt32();
		SampleIndex = fr.ReadUInt32();
		FadeOutFrames = ReadUInt32(fr, 104);
		SyncFrameOffset = ReadUInt32(fr, 108);
		TailSampleIndex = ReadInt32(fr, 112);
		TailStart = ReadUInt32(fr, 116);
		TailEnd = ReadUInt32(fr, 120);
		PreciseVolume = ReadSingle(fr, 124);
		TailVolume = ReadSingle(fr, 132);
		FadeInFrames = ReadUInt32(fr, 136);
	}

	private UInt32? ReadUInt32(FileReader fr, Int32 offset)
	{
		if (ContentSize < offset + 4) return null;
		fr.BaseStream.Position = ContentPosition + offset;
		return fr.ReadUInt32();
	}

	private Int32? ReadInt32(FileReader fr, Int32 offset)
	{
		if (ContentSize < offset + 4) return null;
		fr.BaseStream.Position = ContentPosition + offset;
		return fr.ReadInt32();
	}

	private Single? ReadSingle(FileReader fr, Int32 offset)
	{
		UInt32? bits = ReadUInt32(fr, offset);
		return bits == null ? null : BitConverter.Int32BitsToSingle(unchecked((Int32)bits.Value));
	}
}
