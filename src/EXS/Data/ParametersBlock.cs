using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public enum ExsParameterEncoding { Legacy, Modern }

public sealed class ExsParameter
{
	public Int16 Id { get; init; }
	public Int16 Value { get; init; }
	public ExsParameterEncoding Encoding { get; init; }
	public Int64 IdPosition { get; init; }
	public Int64 ValuePosition { get; init; }
	public String Name => Constants.ParameterName(Id);
}

public sealed class ParametersBlock : ExsBlock
{
	public UInt32 LegacyCount { get; }
	public UInt32 ModernCount { get; }
	public List<ExsParameter> Parameters { get; }

	public ParametersBlock(ExsBlockHeader header, FileReader fr) : base(header)
	{
		Parameters = [];
		if (ContentSize < 4) throw new InvalidDataException("EXS parameters block is shorter than four bytes");
		fr.BaseStream.Position = ContentPosition;
		LegacyCount = fr.ReadUInt32();
		UInt64 legacyBytes = checked((UInt64)LegacyCount * 3UL);
		if (legacyBytes > ContentSize - 4) throw new InvalidDataException($"EXS legacy parameter count {LegacyCount} exceeds the block size");
		if (LegacyCount > Int32.MaxValue) throw new InvalidDataException("EXS legacy parameter count is too large");
		Byte[] ids = fr.ReadBytes((Int32)LegacyCount);
		Int64 valuesPosition = ContentPosition + 4 + LegacyCount;
		fr.BaseStream.Position = valuesPosition;
		for (Int32 index = 0; index < ids.Length; index++)
		{
			Parameters.Add(new ExsParameter
			{
				Id = ids[index], Value = fr.ReadInt16(), Encoding = ExsParameterEncoding.Legacy,
				IdPosition = ContentPosition + 4 + index, ValuePosition = valuesPosition + index * 2L
			});
		}
		Int64 modernCountPosition = ContentPosition + 4 + (Int64)legacyBytes;
		if (EndPosition - modernCountPosition < 4) return;
		fr.BaseStream.Position = modernCountPosition;
		ModernCount = fr.ReadUInt32();
		UInt64 modernBytes = checked((UInt64)ModernCount * 4UL);
		if (modernBytes > (UInt64)(EndPosition - fr.BaseStream.Position)) throw new InvalidDataException($"EXS modern parameter count {ModernCount} exceeds the block size");
		if (ModernCount > Int32.MaxValue) throw new InvalidDataException("EXS modern parameter count is too large");
		for (Int32 index = 0; index < ModernCount; index++)
		{
			Int64 idPosition = fr.BaseStream.Position;
			Int16 id = fr.ReadInt16();
			Int64 valuePosition = fr.BaseStream.Position;
			Parameters.Add(new ExsParameter
			{
				Id = id, Value = fr.ReadInt16(), Encoding = ExsParameterEncoding.Modern,
				IdPosition = idPosition, ValuePosition = valuePosition
			});
		}
	}
}
