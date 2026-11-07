namespace Ufex.Extensions.Audio.MIDI.Data;

/// <summary>
/// Base class for sections of a Standard MIDI File.
/// </summary>
public abstract class Section
{
	public Int64 StartPosition { get; protected set; }
	public Int64 EndPosition { get; protected set; }

	protected void StartRead(Int64 position)
	{
		StartPosition = position;
	}

	protected void EndRead(Int64 position)
	{
		EndPosition = position;
	}
}

