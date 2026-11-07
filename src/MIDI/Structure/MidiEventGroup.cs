using System.Globalization;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiEventGroup
{
	public UInt64 StartTick { get; }
	public UInt64 EndTick { get; }
	public IReadOnlyList<MidiEvent> Events { get; }

	public String Text
	{
		get
		{
			String range = StartTick == EndTick
				? StartTick.ToString("N0", CultureInfo.InvariantCulture)
				: $"{StartTick.ToString("N0", CultureInfo.InvariantCulture)} - {EndTick.ToString("N0", CultureInfo.InvariantCulture)}";
			String eventLabel = Events.Count == 1 ? "event" : "events";
			return $"{range} ({Events.Count.ToString("N0", CultureInfo.InvariantCulture)} {eventLabel})";
		}
	}

	public MidiEventGroup(UInt64 startTick, UInt64 endTick, IReadOnlyList<MidiEvent> events)
	{
		StartTick = startTick;
		EndTick = endTick;
		Events = events;
	}
}

