namespace Ufex.Extensions.Audio.FLAC.Data;

internal static class Constants
{
	public static String MetadataBlockTypeName(Byte type)
	{
		return type switch
		{
			0 => "STREAMINFO",
			1 => "PADDING",
			2 => "APPLICATION",
			3 => "SEEKTABLE",
			4 => "VORBIS_COMMENT",
			5 => "CUESHEET",
			6 => "PICTURE",
			127 => "Forbidden",
			_ => $"Reserved ({type})"
		};
	}

	public static String PictureTypeName(UInt32 type)
	{
		return type switch
		{
			0 => "Other",
			1 => "32x32 PNG file icon",
			2 => "General file icon",
			3 => "Front cover",
			4 => "Back cover",
			5 => "Liner notes page",
			6 => "Media label",
			7 => "Lead artist, lead performer, or soloist",
			8 => "Artist or performer",
			9 => "Conductor",
			10 => "Band or orchestra",
			11 => "Composer",
			12 => "Lyricist or text writer",
			13 => "Recording location",
			14 => "During recording",
			15 => "During performance",
			16 => "Movie or video screen capture",
			17 => "A bright colored fish",
			18 => "Illustration",
			19 => "Band or artist logotype",
			20 => "Publisher or studio logotype",
			_ => $"Reserved ({type})"
		};
	}

	public static String ChannelAssignmentName(Byte value)
	{
		return value switch
		{
			0 => "Mono",
			1 => "Stereo: left, right",
			2 => "3 channels: left, right, center",
			3 => "4 channels: front left, front right, back left, back right",
			4 => "5 channels: front left, front right, front center, surround left, surround right",
			5 => "5.1 surround",
			6 => "6.1 surround",
			7 => "7.1 surround",
			8 => "Stereo: left-side",
			9 => "Stereo: side-right",
			10 => "Stereo: mid-side",
			_ => "Reserved"
		};
	}
}
