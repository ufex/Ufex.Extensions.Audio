using System.Text;

namespace Ufex.Extensions.Audio.MIDI.Data;

internal static class Signatures
{
	public static readonly Byte[] HeaderChunk = Encoding.ASCII.GetBytes("MThd");
	public static readonly Byte[] TrackChunk = Encoding.ASCII.GetBytes("MTrk");

	public const UInt32 HeaderDataLength = 6;
}

