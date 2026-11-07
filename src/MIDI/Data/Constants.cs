using System.Text;

namespace Ufex.Extensions.Audio.MIDI.Data;

internal static class Constants
{
	public static String EventDescription(MidiEvent midiEvent)
	{
		if (midiEvent.IsMeta)
			return MetaEventDescription(midiEvent.MetaType ?? 0, midiEvent.Data);
		if (midiEvent.IsSystemExclusive)
			return midiEvent.Status == 0xF0 ? "System Exclusive" : "System Exclusive continuation";

		Byte messageType = (Byte)(midiEvent.Status & 0xF0);
		String channel = $"channel {midiEvent.Channel}";
		return messageType switch
		{
			0x80 => $"Note Off ({channel}): {NoteDescription(midiEvent.Data)}, velocity {DataByte(midiEvent.Data, 1)}",
			0x90 when DataByte(midiEvent.Data, 1) == 0 => $"Note Off ({channel}): {NoteDescription(midiEvent.Data)}, velocity 0",
			0x90 => $"Note On ({channel}): {NoteDescription(midiEvent.Data)}, velocity {DataByte(midiEvent.Data, 1)}",
			0xA0 => $"Polyphonic Key Pressure ({channel}): {NoteDescription(midiEvent.Data)}, pressure {DataByte(midiEvent.Data, 1)}",
			0xB0 => $"Control Change ({channel}): controller {DataByte(midiEvent.Data, 0)}, value {DataByte(midiEvent.Data, 1)}",
			0xC0 => $"Program Change ({channel}): program {DataByte(midiEvent.Data, 0)}",
			0xD0 => $"Channel Pressure ({channel}): {DataByte(midiEvent.Data, 0)}",
			0xE0 => $"Pitch Bend ({channel}): {PitchBendValue(midiEvent.Data)}",
			_ => SystemEventDescription(midiEvent.Status)
		};
	}

	public static String MetaTypeDescription(Byte metaType)
	{
		return metaType switch
		{
			0x00 => "Sequence Number",
			0x01 => "Text",
			0x02 => "Copyright Notice",
			0x03 => "Track Name",
			0x04 => "Instrument Name",
			0x05 => "Lyric",
			0x06 => "Marker",
			0x07 => "Cue Point",
			0x20 => "MIDI Channel Prefix",
			0x21 => "MIDI Port",
			0x2F => "End of Track",
			0x51 => "Set Tempo",
			0x54 => "SMPTE Offset",
			0x58 => "Time Signature",
			0x59 => "Key Signature",
			0x7F => "Sequencer-Specific",
			_ => $"Unknown Meta Event (0x{metaType:X2})"
		};
	}

	private static String MetaEventDescription(Byte metaType, Byte[] data)
	{
		String name = MetaTypeDescription(metaType);
		return metaType switch
		{
			0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06 or 0x07 => $"{name}: {Encoding.Latin1.GetString(data)}",
			0x20 when data.Length == 1 => $"{name}: channel {data[0] + 1}",
			0x21 when data.Length == 1 => $"{name}: port {data[0]}",
			0x51 when data.Length == 3 => $"{name}: {TempoDescription(data)}",
			0x58 when data.Length == 4 => $"{name}: {data[0]}/{1 << data[1]}",
			0x59 when data.Length == 2 => $"{name}: {KeySignatureDescription(data)}",
			_ => name
		};
	}

	private static String SystemEventDescription(Byte status)
	{
		return status switch
		{
			0xF1 => "MIDI Time Code Quarter Frame",
			0xF2 => "Song Position Pointer",
			0xF3 => "Song Select",
			0xF6 => "Tune Request",
			0xF8 => "Timing Clock",
			0xFA => "Start",
			0xFB => "Continue",
			0xFC => "Stop",
			0xFE => "Active Sensing",
			_ => $"System Event (0x{status:X2})"
		};
	}

	private static String NoteDescription(Byte[] data)
	{
		Int32 note = DataByte(data, 0);
		String[] names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
		return $"{names[note % 12]}{note / 12 - 1} ({note})";
	}

	private static Int32 PitchBendValue(Byte[] data)
	{
		return DataByte(data, 0) | (DataByte(data, 1) << 7);
	}

	private static String TempoDescription(Byte[] data)
	{
		Int32 microseconds = (data[0] << 16) | (data[1] << 8) | data[2];
		if (microseconds == 0)
			return "invalid zero tempo";
		Double beatsPerMinute = 60_000_000.0 / microseconds;
		return $"{beatsPerMinute:0.###} BPM ({microseconds} microseconds per quarter note)";
	}

	private static String KeySignatureDescription(Byte[] data)
	{
		SByte sharpsOrFlats = unchecked((SByte)data[0]);
		String mode = data[1] == 0 ? "major" : "minor";
		return $"{sharpsOrFlats} sharps/flats, {mode}";
	}

	private static Byte DataByte(Byte[] data, Int32 index)
	{
		return index < data.Length ? data[index] : (Byte)0;
	}
}

