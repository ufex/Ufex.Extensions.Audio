using System.Text;
using Ufex.API;

namespace Ufex.Extensions.Audio.EXS.Data;

public class ExsBlockHeader
{
	public Int64 StartPosition { get; init; }
	public Byte ByteOrder { get; init; }
	public Byte VersionMajor { get; init; }
	public Byte VersionMinor { get; init; }
	public Byte RawType { get; init; }
	public UInt32 DeclaredContentSize { get; init; }
	public UInt32 ContentSize { get; init; }
	public UInt32 Index { get; init; }
	public UInt32 Flags { get; init; }
	public Byte[] Magic { get; init; } = [];
	public Byte[] NameBytes { get; init; } = [];

	public Endian Endian => ByteOrder == 0 ? Endian.Big : Endian.Little;
	public ExsBlockType Type => (ExsBlockType)(RawType & Constants.TypeMask);
	public Int64 ContentPosition => StartPosition + Constants.HeaderSize;
	public Int64 EndPosition => ContentPosition + ContentSize;
	public Boolean IsModern => (RawType & Constants.ModernTypeFlag) != 0;
	public Boolean UsesMaskedSize => (RawType & Constants.MaskedSizeFlag) != 0;
	public String MagicText => Encoding.ASCII.GetString(Magic);
	public String Name => Encoding.Latin1.GetString(NameBytes).TrimEnd('\0');
}

public class ExsBlock
{
	public ExsBlockHeader Header { get; }
	public Int64 StartPosition => Header.StartPosition;
	public Int64 ContentPosition => Header.ContentPosition;
	public Int64 EndPosition => Header.EndPosition;
	public UInt32 ContentSize => Header.ContentSize;
	public ExsBlockType Type => Header.Type;
	public String Name => Header.Name;
	public String TypeName => Type switch
	{
		ExsBlockType.Instrument => "Instrument", ExsBlockType.Zone => "Zone",
		ExsBlockType.Group => "Group", ExsBlockType.Sample => "Sample",
		ExsBlockType.Parameters => "Parameters", ExsBlockType.KeyAction => "Key actions",
		ExsBlockType.BusLabel => "Bus labels", ExsBlockType.DrumKitAuxiliary => "Drum-kit metadata",
		ExsBlockType.EditorState => "Editor state", ExsBlockType.SampleBookmark => "Sample bookmarks",
		_ => $"Auxiliary 0x{(Byte)Type:X2}"
	};

	public ExsBlock(ExsBlockHeader header) => Header = header;
}
