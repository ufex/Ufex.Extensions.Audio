using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

/// <summary>
/// Base class for a FLAC metadata block, including its four-byte header.
/// </summary>
public abstract class MetadataBlock : Section
{
	public Byte HeaderByte { get; }
	public Boolean IsLast { get; }
	public Byte Type { get; }
	public UInt24 Length { get; }
	public Int64 DataStartPosition => StartPosition + 4;
	public String TypeName => Constants.MetadataBlockTypeName(Type);

	protected MetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length)
	{
		StartPosition = startPosition;
		HeaderByte = headerByte;
		IsLast = (headerByte & 0x80) != 0;
		Type = (Byte)(headerByte & 0x7F);
		Length = length;
		EndPosition = checked(DataStartPosition + (UInt32)length);
	}

	public static MetadataBlock Read(FileReader fr)
	{
		Int64 startPosition = fr.BaseStream.Position;
		Byte headerByte = fr.ReadByte();
		var length = fr.ReadUInt24();
		Byte type = (Byte)(headerByte & 0x7F);
		Int64 endPosition = checked(startPosition + 4 + (UInt32)length);
		if (endPosition > fr.BaseStream.Length)
			throw new InvalidDataException($"{Constants.MetadataBlockTypeName(type)} block at 0x{startPosition:X} extends beyond the end of the file");

		MetadataBlock block = type switch
		{
			0 => new StreamInfoMetadataBlock(startPosition, headerByte, length, fr),
			1 => new PaddingMetadataBlock(startPosition, headerByte, length, fr),
			2 => new ApplicationMetadataBlock(startPosition, headerByte, length, fr),
			3 => new SeekTableMetadataBlock(startPosition, headerByte, length, fr),
			4 => new VorbisCommentMetadataBlock(startPosition, headerByte, length, fr),
			5 => new CuesheetMetadataBlock(startPosition, headerByte, length, fr),
			6 => new PictureMetadataBlock(startPosition, headerByte, length, fr),
			_ => new ReservedMetadataBlock(startPosition, headerByte, length)
		};

		if (fr.BaseStream.Position > block.EndPosition)
			throw new InvalidDataException($"{block.TypeName} block at 0x{startPosition:X} contains fields beyond its declared length");
		fr.BaseStream.Position = block.EndPosition;
		return block;
	}

	protected static Byte[] ReadBytes(FileReader fr, Int64 blockEnd, UInt32 count, String fieldName)
	{
		if (count > Int32.MaxValue || count > (UInt64)(blockEnd - fr.BaseStream.Position))
			throw new InvalidDataException($"{fieldName} extends beyond its metadata block");
		Byte[] value = fr.ReadBytes((Int32)count);
		if (value.Length != count)
			throw new EndOfStreamException($"Unexpected end of file while reading {fieldName}");
		return value;
	}
}
