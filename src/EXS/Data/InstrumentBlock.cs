using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public sealed class InstrumentBlock : ExsBlock
{
	public UInt32 Reserved { get; }
	public UInt32 ZoneCount { get; }
	public UInt32 GroupCount { get; }
	public UInt32 SampleCount { get; }
	public UInt32 ParameterBlockCount { get; }
	public UInt32 Auxiliary5Count { get; }
	public UInt32 Auxiliary6Count { get; }
	public UInt32 KeyActionCount { get; }
	public UInt32 BusLabelCount { get; }
	public UInt32 DrumKitAuxiliaryCount { get; }
	public UInt32 SampleBookmarkCount { get; }

	public InstrumentBlock(ExsBlockHeader header, FileReader fr) : base(header)
	{
		if (ContentSize < 40)
			throw new InvalidDataException("EXS instrument block is shorter than 40 bytes");
		fr.BaseStream.Position = ContentPosition;
		Reserved = fr.ReadUInt32();
		ZoneCount = fr.ReadUInt32();
		GroupCount = fr.ReadUInt32();
		SampleCount = fr.ReadUInt32();
		ParameterBlockCount = fr.ReadUInt32();
		Auxiliary5Count = fr.ReadUInt32();
		Auxiliary6Count = fr.ReadUInt32();
		KeyActionCount = fr.ReadUInt32();
		BusLabelCount = fr.ReadUInt32();
		DrumKitAuxiliaryCount = fr.ReadUInt32();
		if (ContentSize >= 48)
		{
			fr.BaseStream.Position = ContentPosition + 44;
			SampleBookmarkCount = fr.ReadUInt32();
		}
	}
}
