namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class AudioData : Section
{
	public AudioData(Int64 startPosition, Int64 endPosition)
	{
		StartPosition = startPosition;
		EndPosition = endPosition;
	}
}
