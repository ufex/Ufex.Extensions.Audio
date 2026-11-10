using System.Text;

namespace Ufex.Extensions.Audio.FLAC.Data;

internal static class Signatures
{
	public static readonly Byte[] StreamMarker = Encoding.ASCII.GetBytes("fLaC");

	public const Int32 StreamInfoLength = 34;
	public const Int32 SeekPointLength = 18;
	public const UInt64 PlaceholderSampleNumber = UInt64.MaxValue;
}
