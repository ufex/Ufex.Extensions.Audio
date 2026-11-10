namespace Ufex.Extensions.Audio.FLAC.Data;

/// <summary>
/// Base class for regions of a native FLAC stream.
/// </summary>
public abstract class Section
{
	public Int64 StartPosition { get; protected init; }
	public Int64 EndPosition { get; protected set; }
}
