namespace Ufex.Extensions.Audio.MIDI.Data;

/// <summary>
/// A decoded event from an MTrk chunk.
/// </summary>
public class MidiEvent : Section
{
	public Byte[] DeltaTimeBytes { get; init; }
	public UInt32 DeltaTime { get; init; }
	public UInt64 AbsoluteTime { get; init; }
	public Byte Status { get; init; }
	public Boolean StatusPresent { get; init; }
	public Byte? MetaType { get; init; }
	public Byte[] LengthBytes { get; init; }
	public Byte[] Data { get; init; }

	public Boolean UsesRunningStatus => !StatusPresent;
	public Boolean IsMeta => Status == 0xFF;
	public Boolean IsSystemExclusive => Status is 0xF0 or 0xF7;
	public Int32 Channel => Status is >= 0x80 and <= 0xEF ? (Status & 0x0F) + 1 : 0;
	public String Description => Constants.EventDescription(this);

	public MidiEvent(
		Int64 startPosition,
		Int64 endPosition,
		Byte[] deltaTimeBytes,
		UInt32 deltaTime,
		UInt64 absoluteTime,
		Byte status,
		Boolean statusPresent,
		Byte? metaType,
		Byte[] lengthBytes,
		Byte[] data)
	{
		DeltaTimeBytes = deltaTimeBytes;
		DeltaTime = deltaTime;
		AbsoluteTime = absoluteTime;
		Status = status;
		StatusPresent = statusPresent;
		MetaType = metaType;
		LengthBytes = lengthBytes;
		Data = data;
		StartRead(startPosition);
		EndRead(endPosition);
	}
}
