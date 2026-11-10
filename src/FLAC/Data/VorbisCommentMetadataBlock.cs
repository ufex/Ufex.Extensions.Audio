using System.Text;
using Ufex.API;
using Ufex.API.Types;

namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class VorbisCommentMetadataBlock : MetadataBlock
{
	public Byte[] Vendor { get; }
	public IReadOnlyList<VorbisCommentEntry> Comments { get; }
	public String VendorText => Encoding.UTF8.GetString(Vendor);

	internal VorbisCommentMetadataBlock(Int64 startPosition, Byte headerByte, UInt24 length, FileReader fr)
		: base(startPosition, headerByte, length)
	{
		Endian originalEndian = fr.Endian;
		fr.Endian = Endian.Little;
		try
		{
			EnsureAvailable(fr, 4, "Vorbis vendor length");
			UInt32 vendorLength = fr.ReadUInt32();
			Vendor = ReadBytes(fr, EndPosition, vendorLength, "Vorbis vendor string");
			EnsureAvailable(fr, 4, "Vorbis comment count");
			UInt32 commentCount = fr.ReadUInt32();
			var comments = new List<VorbisCommentEntry>();
			for (UInt32 i = 0; i < commentCount; i++)
			{
				Int64 entryStart = fr.BaseStream.Position;
				EnsureAvailable(fr, 4, "Vorbis comment length");
				UInt32 commentLength = fr.ReadUInt32();
				Byte[] value = ReadBytes(fr, EndPosition, commentLength, "Vorbis comment");
				comments.Add(new VorbisCommentEntry(entryStart, commentLength, value));
			}
			if (fr.BaseStream.Position != EndPosition)
				throw new InvalidDataException("VORBIS_COMMENT block contains trailing data");
			Comments = comments;
		}
		finally
		{
			fr.Endian = originalEndian;
		}
	}

	public String? GetFirstValue(String fieldName)
	{
		return Comments.FirstOrDefault(comment => String.Equals(comment.Name, fieldName, StringComparison.OrdinalIgnoreCase))?.Value;
	}

	private void EnsureAvailable(FileReader fr, Int32 count, String fieldName)
	{
		if (EndPosition - fr.BaseStream.Position < count)
			throw new InvalidDataException($"{fieldName} extends beyond the VORBIS_COMMENT block");
	}
}

public sealed class VorbisCommentEntry
{
	public Int64 StartPosition { get; }
	public UInt32 Length { get; }
	public Byte[] Data { get; }
	public String Text => Encoding.UTF8.GetString(Data);
	public String Name => Text.Contains('=') ? Text[..Text.IndexOf('=')] : Text;
	public String Value => Text.Contains('=') ? Text[(Text.IndexOf('=') + 1)..] : String.Empty;

	public VorbisCommentEntry(Int64 startPosition, UInt32 length, Byte[] data)
	{
		StartPosition = startPosition;
		Length = length;
		Data = data;
	}
}
