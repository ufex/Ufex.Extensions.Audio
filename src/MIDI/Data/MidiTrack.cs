using System.Text;

namespace Ufex.Extensions.Audio.MIDI.Data;

/// <summary>
/// An MTrk chunk and its decoded events.
/// </summary>
public class MidiTrack : Section
{
	public Byte[] ChunkId { get; init; }
	public UInt32 Length { get; init; }
	public Int32 Index { get; init; }
	public Int64 DataStartPosition { get; init; }
	public List<MidiEvent> Events { get; init; }

	public String Name
	{
		get
		{
			MidiEvent? nameEvent = Events.FirstOrDefault(e => e.MetaType == 0x03);
			return nameEvent == null ? String.Empty : Encoding.Latin1.GetString(nameEvent.Data);
		}
	}

	public MidiTrack(Byte[] chunkId, UInt32 length, Int32 index, Int64 startPosition)
	{
		ChunkId = chunkId;
		Length = length;
		Index = index;
		DataStartPosition = startPosition + 8;
		Events = [];
		StartRead(startPosition);
		EndRead(checked(DataStartPosition + length));
	}
}

