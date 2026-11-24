using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS;

public sealed class ExsStreamReader
{
	private readonly Stream _fileStream;
	public List<ExsBlock> Blocks { get; } = [];
	public InstrumentBlock? Instrument { get; private set; }
	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	public ExsStreamReader(Stream fileStream, Logger log, ValidationReport validationReport)
	{
		_fileStream = fileStream;
		Log = log;
		ValidationReport = validationReport;
	}

	public Boolean Read()
	{
		try
		{
			Blocks.Clear(); Instrument = null; _fileStream.Position = 0;
			while (_fileStream.Position < _fileStream.Length)
			{
				Int64 remaining = _fileStream.Length - _fileStream.Position;
				if (remaining < Constants.HeaderSize) throw new InvalidDataException($"Truncated EXS block header at offset 0x{_fileStream.Position:X}");
				ExsBlockHeader header = ReadHeader();
				ValidateHeader(header);
				if (header.EndPosition > _fileStream.Length) throw new InvalidDataException($"EXS {header.Type} block at 0x{header.StartPosition:X} extends beyond the end of the file");
				var fr = new FileReader(_fileStream, header.Endian, Encoding.Latin1);
				ExsBlock block = header.Type switch
				{
					ExsBlockType.Instrument => new InstrumentBlock(header, fr),
					ExsBlockType.Zone => new ZoneBlock(header, fr),
					ExsBlockType.Group => new GroupBlock(header, fr),
					ExsBlockType.Sample => new SampleBlock(header, fr),
					ExsBlockType.Parameters => new ParametersBlock(header, fr),
					_ => new ExsBlock(header)
				};
				Blocks.Add(block);
				if (block is InstrumentBlock instrument)
				{
					if (Instrument != null) ValidationReport.Warning($"Additional instrument block at offset 0x{block.StartPosition:X}");
					else Instrument = instrument;
				}
				_fileStream.Position = header.EndPosition;
				Log.LogInformation("Read EXS {Type} block at {Offset} ({Size} content bytes)", block.TypeName, block.StartPosition, block.ContentSize);
			}
			ValidateFile();
			ValidationReport.Info($"Parsed {Blocks.Count} EXS block(s)");
			return Instrument != null;
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException or OverflowException)
		{
			ValidationReport.Error(exception.Message);
			Log.LogError(exception, "Failed to parse the EXS instrument file");
			return false;
		}
	}

	private ExsBlockHeader ReadHeader()
	{
		Int64 start = _fileStream.Position;
		Int32 byteOrderValue = _fileStream.ReadByte();
		if (byteOrderValue < 0) throw new EndOfStreamException();
		Byte byteOrder = (Byte)byteOrderValue;
		var fr = new FileReader(_fileStream, byteOrder == 0 ? Endian.Big : Endian.Little, Encoding.Latin1);
		Byte major = fr.ReadByte(); Byte minor = fr.ReadByte(); Byte rawType = fr.ReadByte();
		UInt32 declaredSize = fr.ReadUInt32();
		UInt32 contentSize = (rawType & Constants.MaskedSizeFlag) != 0 ? declaredSize & 0x7FFFU : declaredSize;
		return new ExsBlockHeader
		{
			StartPosition = start, ByteOrder = byteOrder, VersionMajor = major, VersionMinor = minor,
			RawType = rawType, DeclaredContentSize = declaredSize, ContentSize = contentSize,
			Index = fr.ReadUInt32(), Flags = fr.ReadUInt32(), Magic = ReadExact(fr, 4), NameBytes = ReadExact(fr, 64)
		};
	}

	private void ValidateHeader(ExsBlockHeader header)
	{
		if (header.ByteOrder is not (0 or 1)) throw new InvalidDataException($"Invalid EXS byte-order marker {header.ByteOrder} at offset 0x{header.StartPosition:X}");
		if (header.VersionMajor != 1 || header.VersionMinor != 0) throw new InvalidDataException($"Unsupported EXS block version {header.VersionMajor}.{header.VersionMinor} at offset 0x{header.StartPosition:X}");
		if (!Constants.ValidMagic.Any(magic => magic.SequenceEqual(header.Magic))) throw new InvalidDataException($"Invalid EXS block magic '{header.MagicText}' at offset 0x{header.StartPosition:X}");
		Boolean magicIsBigEndian = header.MagicText is "SOBT" or "SOBJ";
		if (magicIsBigEndian != (header.ByteOrder == 0)) ValidationReport.Warning($"EXS byte-order marker and magic disagree at offset 0x{header.StartPosition:X}");
	}

	private void ValidateFile()
	{
		if (Blocks.Count == 0 || Blocks[0] is not InstrumentBlock) ValidationReport.Error("The first EXS block is not an instrument block");
		if (Instrument == null) { ValidationReport.Error("No EXS instrument block was found"); return; }
		ValidateCount(ExsBlockType.Zone, Instrument.ZoneCount);
		ValidateCount(ExsBlockType.Group, Instrument.GroupCount);
		ValidateCount(ExsBlockType.Sample, Instrument.SampleCount);
		ValidateCount(ExsBlockType.Parameters, Instrument.ParameterBlockCount);
		Int32 groupCount = Blocks.Count(block => block.Type == ExsBlockType.Group);
		Int32 sampleCount = Blocks.Count(block => block.Type == ExsBlockType.Sample);
		foreach (ZoneBlock zone in Blocks.OfType<ZoneBlock>())
		{
			if (zone.GroupIndex < 0 || zone.GroupIndex >= groupCount) ValidationReport.Warning($"Zone {zone.Header.Index} references group {zone.GroupIndex}, but only {groupCount} group(s) exist");
			if (zone.SampleIndex >= sampleCount) ValidationReport.Warning($"Zone {zone.Header.Index} references sample {zone.SampleIndex}, but only {sampleCount} sample(s) exist");
		}
	}

	private void ValidateCount(ExsBlockType type, UInt32 declared)
	{
		Int32 actual = Blocks.Count(block => block.Type == type);
		if (declared != actual) ValidationReport.Warning($"Instrument declares {declared} {type} block(s), but {actual} were found");
	}

	private static Byte[] ReadExact(FileReader fr, Int32 length)
	{
		Byte[] bytes = fr.ReadBytes(length);
		if (bytes.Length != length) throw new EndOfStreamException("EXS block header is truncated");
		return bytes;
	}
}
