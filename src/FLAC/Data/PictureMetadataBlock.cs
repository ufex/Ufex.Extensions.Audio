using System.Text;
using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class PictureMetadataBlock : MetadataBlock
{
	public UInt32 PictureType { get; }
	public UInt32 MediaTypeLength { get; }
	public Byte[] MediaType { get; }
	public UInt32 DescriptionLength { get; }
	public Byte[] Description { get; }
	public UInt32 Width { get; }
	public UInt32 Height { get; }
	public UInt32 ColorDepth { get; }
	public UInt32 ColorCount { get; }
	public UInt32 PictureDataLength { get; }
	public Int64 PictureDataPosition { get; }
	public String MediaTypeText => Encoding.ASCII.GetString(MediaType);
	public String DescriptionText => Encoding.UTF8.GetString(Description);

	internal PictureMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		EnsureAvailable(fr, 4, "picture type");
		PictureType = fr.ReadUInt32();
		EnsureAvailable(fr, 4, "picture media type length");
		MediaTypeLength = fr.ReadUInt32();
		MediaType = ReadBytes(fr, EndPosition, MediaTypeLength, "picture media type");
		EnsureAvailable(fr, 4, "picture description length");
		DescriptionLength = fr.ReadUInt32();
		Description = ReadBytes(fr, EndPosition, DescriptionLength, "picture description");
		EnsureAvailable(fr, 20, "picture dimensions and data length");
		Width = fr.ReadUInt32();
		Height = fr.ReadUInt32();
		ColorDepth = fr.ReadUInt32();
		ColorCount = fr.ReadUInt32();
		PictureDataLength = fr.ReadUInt32();
		PictureDataPosition = fr.BaseStream.Position;
		if (PictureDataLength != (UInt64)(EndPosition - PictureDataPosition))
			throw new InvalidDataException("Picture data length does not match the remaining PICTURE block length");
	}

	private void EnsureAvailable(FileReader fr, Int32 count, String fieldName)
	{
		if (EndPosition - fr.BaseStream.Position < count)
			throw new InvalidDataException($"{fieldName} extends beyond the PICTURE block");
	}
}
