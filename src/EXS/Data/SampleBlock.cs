using System.Text;
using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public sealed class SampleBlock : ExsBlock
{
	public UInt32 AudioDataOffset { get; }
	public UInt32 FrameCount { get; }
	public UInt32 SampleRate { get; }
	public UInt32 BitDepth { get; }
	public UInt32 Channels { get; }
	public UInt32 ChannelsDuplicate { get; }
	public Byte[] FileTypeBytes { get; }
	public UInt32 FileSize { get; }
	public UInt32 Compression { get; }
	public Int32 WindowsVolumeId { get; }
	public Byte[] FolderBytes { get; }
	public Byte[] FileNameBytes { get; }

	public String FileTypeText => Encoding.ASCII.GetString(FileTypeBytes);
	public String AudioFormat => FileTypeText switch
	{
		"EVAW" => "WAVE", "FFIA" => "AIFF", "ffac" => "CAF",
		" A4M" => "M4A", "3GPM" => "MP3", "f2dS" => "Sound Designer II",
		_ => FileTypeText
	};
	public String Folder => Encoding.Latin1.GetString(FolderBytes).TrimEnd('\0');
	public String FileName => Encoding.Latin1.GetString(FileNameBytes).TrimEnd('\0');

	public SampleBlock(ExsBlockHeader header, FileReader fr) : base(header)
	{
		if (ContentSize < 336)
			throw new InvalidDataException("EXS sample block is shorter than 336 bytes");
		fr.BaseStream.Position = ContentPosition;
		AudioDataOffset = fr.ReadUInt32(); FrameCount = fr.ReadUInt32(); SampleRate = fr.ReadUInt32();
		BitDepth = fr.ReadUInt32(); Channels = fr.ReadUInt32(); ChannelsDuplicate = fr.ReadUInt32();
		fr.BaseStream.Position = ContentPosition + 28;
		FileTypeBytes = ReadExact(fr, 4); FileSize = fr.ReadUInt32(); Compression = fr.ReadUInt32();
		fr.BaseStream.Position = ContentPosition + 72; WindowsVolumeId = fr.ReadInt32();
		fr.BaseStream.Position = ContentPosition + 80; FolderBytes = ReadExact(fr, 256);
		FileNameBytes = ContentSize >= 592 ? ReadExact(fr, 256) : [];
	}

	private static Byte[] ReadExact(FileReader fr, Int32 length)
	{
		Byte[] result = fr.ReadBytes(length);
		if (result.Length != length) throw new EndOfStreamException("EXS sample block is truncated");
		return result;
	}
}
