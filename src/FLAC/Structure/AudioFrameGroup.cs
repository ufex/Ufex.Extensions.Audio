using System.Globalization;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class AudioFrameGroup
{
	private readonly String? _text;

	public UInt64 StartMilliseconds { get; }
	public UInt64 EndMilliseconds { get; }
	public IReadOnlyList<AudioFrame> Frames { get; }

	public String Text
	{
		get
		{
			if (_text != null)
				return _text;
			String frameLabel = Frames.Count == 1 ? "frame" : "frames";
			return $"{FormatTime(StartMilliseconds)} - {FormatTime(EndMilliseconds)} ({Frames.Count.ToString("N0", CultureInfo.InvariantCulture)} {frameLabel})";
		}
	}

	public AudioFrameGroup(UInt64 startMilliseconds, UInt64 endMilliseconds, IReadOnlyList<AudioFrame> frames, String? text = null)
	{
		StartMilliseconds = startMilliseconds;
		EndMilliseconds = endMilliseconds;
		Frames = frames;
		_text = text;
	}

	private static String FormatTime(UInt64 milliseconds)
	{
		UInt64 totalSeconds = milliseconds / 1000;
		UInt64 hours = totalSeconds / 3600;
		UInt64 minutes = totalSeconds / 60 % 60;
		UInt64 seconds = totalSeconds % 60;
		UInt64 remainingMilliseconds = milliseconds % 1000;
		return hours == 0
			? $"{minutes:00}:{seconds:00}.{remainingMilliseconds:000}"
			: $"{hours}:{minutes:00}:{seconds:00}.{remainingMilliseconds:000}";
	}
}
