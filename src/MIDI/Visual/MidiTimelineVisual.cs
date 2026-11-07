using System.Globalization;
using System.Text;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Visual;

/// <summary>
/// Piano-roll timeline for the notes and non-note events in a MIDI track.
/// </summary>
internal class MidiTimelineVisual : VectorImage
{
	private const Int32 LeftMargin = 150;
	private const Int32 RightMargin = 30;
	private const Int32 TopMargin = 55;
	private const Int32 EmptyNotePlotHeight = 100;
	private const Int32 SectionGap = 42;
	private const Int32 EventRowHeight = 22;
	private const Int32 BottomMargin = 65;
	private const Int32 MinimumPlotWidth = 1_200;
	private const Int32 MaximumPlotWidth = 6_000;

	public MidiTimelineVisual(MidiTrack track)
		: this(GenerateTimeline(track))
	{
	}

	private MidiTimelineVisual(TimelineSvg timeline)
		: base(timeline.Stream, "Timeline")
	{
		Width = timeline.Width;
		Height = timeline.Height;
	}

	private static TimelineSvg GenerateTimeline(MidiTrack track)
	{
		MidiEvent[] events = track.Events
			.OrderBy(midiEvent => midiEvent.AbsoluteTime)
			.ToArray();
		UInt64 maximumTick = events.Length == 0 ? 0 : events[^1].AbsoluteTime;
		List<NoteSpan> notes = BuildNoteSpans(events, maximumTick);
		List<EventMarker> eventMarkers = BuildEventMarkers(events);
		EventType[] eventTypes = eventMarkers
			.Select(marker => marker.EventType)
			.Distinct()
			.OrderBy(eventType => eventType.Order)
			.ToArray();

		(Int32 minimumNote, Int32 maximumNote) = NoteRange(notes);
		Int32 noteCount = notes.Count == 0 ? 0 : maximumNote - minimumNote + 1;
		Int32 noteRowHeight = noteCount > 80 ? 9 : 13;
		Int32 notePlotHeight = noteCount == 0 ? EmptyNotePlotHeight : noteCount * noteRowHeight;
		Int32 eventPlotTop = TopMargin + notePlotHeight + SectionGap;
		Int32 eventPlotHeight = Math.Max(EventRowHeight, eventTypes.Length * EventRowHeight);
		Int32 eventPlotBottom = eventPlotTop + eventPlotHeight;
		Int32 plotWidth = Math.Clamp(events.Length * 3, MinimumPlotWidth, MaximumPlotWidth);
		Int32 width = LeftMargin + plotWidth + RightMargin;
		Int32 height = eventPlotBottom + BottomMargin;

		UInt64 tickInterval = TickInterval(maximumTick);
		UInt64 axisMaximumTick = maximumTick == 0 ? tickInterval : RoundUp(maximumTick, tickInterval);
		var stream = new MemoryStream();
		using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);

		WriteHeader(writer, track, width, height);
		WriteStyles(writer);
		WriteBackground(writer, width, height);
		WriteTitle(writer, track, notes.Count, events.Length);
		WriteNotePlot(writer, notes, minimumNote, maximumNote, noteRowHeight, notePlotHeight, plotWidth, axisMaximumTick);
		WriteEventPlot(writer, eventMarkers, eventTypes, eventPlotTop, eventPlotHeight, plotWidth, axisMaximumTick);
		WriteTimeGrid(writer, plotWidth, eventPlotBottom, axisMaximumTick, tickInterval);
		WriteTimeAxis(writer, plotWidth, eventPlotBottom, axisMaximumTick, tickInterval);
		writer.WriteLine("</svg>");
		writer.Flush();
		stream.Position = 0;

		return new TimelineSvg(stream, width, height);
	}

	private static List<NoteSpan> BuildNoteSpans(MidiEvent[] events, UInt64 maximumTick)
	{
		var pendingNotes = new Dictionary<(Int32 Channel, Byte Note), Queue<MidiEvent>>();
		var notes = new List<NoteSpan>();

		foreach (MidiEvent midiEvent in events)
		{
			if (!TryGetNoteEvent(midiEvent, out Byte note, out Byte velocity, out Boolean isNoteOn))
				continue;

			var key = (midiEvent.Channel, note);
			if (isNoteOn)
			{
				if (!pendingNotes.TryGetValue(key, out Queue<MidiEvent>? queue))
				{
					queue = new Queue<MidiEvent>();
					pendingNotes[key] = queue;
				}
				queue.Enqueue(midiEvent);
			}
			else if (pendingNotes.TryGetValue(key, out Queue<MidiEvent>? queue) && queue.Count > 0)
			{
				MidiEvent noteOn = queue.Dequeue();
				notes.Add(new NoteSpan(
					note,
					noteOn.Channel,
					noteOn.Data[1],
					noteOn.AbsoluteTime,
					midiEvent.AbsoluteTime,
					false));
			}
		}

		foreach (((Int32 channel, Byte note), Queue<MidiEvent> queue) in pendingNotes)
		{
			while (queue.Count > 0)
			{
				MidiEvent noteOn = queue.Dequeue();
				notes.Add(new NoteSpan(
					note,
					channel,
					noteOn.Data[1],
					noteOn.AbsoluteTime,
					maximumTick,
					true));
			}
		}

		return notes
			.OrderBy(note => note.StartTick)
			.ThenBy(note => note.Note)
			.ToList();
	}

	private static Boolean TryGetNoteEvent(MidiEvent midiEvent, out Byte note, out Byte velocity, out Boolean isNoteOn)
	{
		note = 0;
		velocity = 0;
		isNoteOn = false;
		if (midiEvent.Data.Length < 2 || midiEvent.Status is < 0x80 or > 0x9F)
			return false;

		note = midiEvent.Data[0];
		velocity = midiEvent.Data[1];
		isNoteOn = (midiEvent.Status & 0xF0) == 0x90 && velocity > 0;
		return true;
	}

	private static List<EventMarker> BuildEventMarkers(MidiEvent[] events)
	{
		var markers = new List<EventMarker>();
		foreach (MidiEvent midiEvent in events)
		{
			EventType? eventType = ClassifyEvent(midiEvent);
			if (eventType != null)
				markers.Add(new EventMarker(midiEvent, eventType));
		}
		return markers;
	}

	private static EventType? ClassifyEvent(MidiEvent midiEvent)
	{
		if (midiEvent.Status is >= 0x80 and <= 0x9F)
			return null;
		if (midiEvent.IsSystemExclusive)
			return EventTypes.SystemExclusive;
		if (midiEvent.IsMeta)
		{
			return midiEvent.MetaType switch
			{
				0x51 => EventTypes.Tempo,
				0x58 => EventTypes.TimeSignature,
				0x59 => EventTypes.KeySignature,
				>= 0x01 and <= 0x07 => EventTypes.Text,
				0x2F => EventTypes.EndOfTrack,
				_ => EventTypes.OtherMeta
			};
		}

		return (midiEvent.Status & 0xF0) switch
		{
			0xA0 or 0xD0 => EventTypes.Pressure,
			0xB0 => EventTypes.ControlChange,
			0xC0 => EventTypes.ProgramChange,
			0xE0 => EventTypes.PitchBend,
			_ => EventTypes.System
		};
	}

	private static (Int32 Minimum, Int32 Maximum) NoteRange(List<NoteSpan> notes)
	{
		if (notes.Count == 0)
			return (0, 0);

		Int32 minimum = notes.Min(note => (Int32)note.Note);
		Int32 maximum = notes.Max(note => (Int32)note.Note);
		while (maximum - minimum + 1 < 12 && (minimum > 0 || maximum < 127))
		{
			if (minimum > 0)
				minimum--;
			if (maximum - minimum + 1 < 12 && maximum < 127)
				maximum++;
		}
		return (minimum, maximum);
	}

	private static void WriteHeader(StreamWriter writer, MidiTrack track, Int32 width, Int32 height)
	{
		String trackName = String.IsNullOrWhiteSpace(track.Name) ? $"Track {track.Index + 1}" : $"Track {track.Index + 1}: {track.Name}";
		writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
		writer.WriteLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\">");
		writer.WriteLine($"  <title>{EscapeXml(trackName)} MIDI timeline</title>");
	}

	private static void WriteTitle(StreamWriter writer, MidiTrack track, Int32 noteCount, Int32 eventCount)
	{
		String trackName = String.IsNullOrWhiteSpace(track.Name) ? $"Track {track.Index + 1}" : $"Track {track.Index + 1}: {track.Name}";
		String noteLabel = noteCount == 1 ? "note" : "notes";
		String eventLabel = eventCount == 1 ? "event" : "events";
		writer.WriteLine($"  <text class=\"title\" x=\"{LeftMargin}\" y=\"27\">{EscapeXml(trackName)} - {noteCount.ToString("N0", CultureInfo.InvariantCulture)} {noteLabel}, {eventCount.ToString("N0", CultureInfo.InvariantCulture)} {eventLabel}</text>");
	}

	private static void WriteStyles(StreamWriter writer)
	{
		writer.WriteLine("  <style>");
		writer.WriteLine("    text { font-family: Arial, sans-serif; fill: #1F2937; }");
		writer.WriteLine("    .title { font-size: 17px; font-weight: 600; }");
		writer.WriteLine("    .axis-label { font-size: 11px; }");
		writer.WriteLine("    .section-label { font-size: 12px; font-weight: 600; }");
		writer.WriteLine("    .tick-line { stroke: #CBD5E1; stroke-width: 1; stroke-opacity: 0.65; }");
		writer.WriteLine("    .axis-line { stroke: #475569; stroke-width: 1.2; }");
		writer.WriteLine("    .note-line { stroke: #E2E8F0; stroke-width: 0.7; }");
		writer.WriteLine("    .octave-line { stroke: #94A3B8; stroke-width: 1; }");
		writer.WriteLine("  </style>");
	}

	private static void WriteBackground(StreamWriter writer, Int32 width, Int32 height)
	{
		writer.WriteLine($"  <rect width=\"{width}\" height=\"{height}\" fill=\"#FFFFFF\" />");
	}

	private static void WriteTimeGrid(
		StreamWriter writer,
		Int32 plotWidth,
		Int32 plotBottom,
		UInt64 axisMaximumTick,
		UInt64 tickInterval)
	{
		for (UInt64 tick = 0; ; tick += tickInterval)
		{
			Double x = TickX(tick, plotWidth, axisMaximumTick);
			writer.WriteLine($"  <line class=\"tick-line\" x1=\"{Number(x)}\" y1=\"{TopMargin}\" x2=\"{Number(x)}\" y2=\"{plotBottom}\" />");
			if (tick >= axisMaximumTick || tick > UInt64.MaxValue - tickInterval)
				break;
		}
	}

	private static void WriteNotePlot(
		StreamWriter writer,
		List<NoteSpan> notes,
		Int32 minimumNote,
		Int32 maximumNote,
		Int32 rowHeight,
		Int32 notePlotHeight,
		Int32 plotWidth,
		UInt64 axisMaximumTick)
	{
		writer.WriteLine($"  <text class=\"section-label\" transform=\"translate(18 {TopMargin + notePlotHeight / 2}) rotate(-90)\" text-anchor=\"middle\">Notes</text>");
		writer.WriteLine($"  <rect x=\"{LeftMargin}\" y=\"{TopMargin}\" width=\"{plotWidth}\" height=\"{notePlotHeight}\" fill=\"#F8FAFC\" stroke=\"#475569\" />");

		if (notes.Count == 0)
		{
			writer.WriteLine($"  <text class=\"axis-label\" x=\"{LeftMargin + plotWidth / 2}\" y=\"{TopMargin + notePlotHeight / 2}\" text-anchor=\"middle\">No note events</text>");
			return;
		}

		for (Int32 note = minimumNote; note <= maximumNote; note++)
		{
			Int32 y = TopMargin + (maximumNote - note) * rowHeight;
			Boolean isBlackKey = note % 12 is 1 or 3 or 6 or 8 or 10;
			if (isBlackKey)
				writer.WriteLine($"  <rect x=\"{LeftMargin}\" y=\"{y}\" width=\"{plotWidth}\" height=\"{rowHeight}\" fill=\"#F1F5F9\" />");

			String lineClass = note % 12 == 0 ? "octave-line" : "note-line";
			writer.WriteLine($"  <line class=\"{lineClass}\" x1=\"{LeftMargin}\" y1=\"{y}\" x2=\"{LeftMargin + plotWidth}\" y2=\"{y}\" />");
			if (note == minimumNote || note == maximumNote || note % 12 == 0)
				writer.WriteLine($"  <text class=\"axis-label\" x=\"{LeftMargin - 8}\" y=\"{y + rowHeight * 0.75}\" text-anchor=\"end\">{NoteName((Byte)note)} ({note})</text>");
		}

		foreach (NoteSpan note in notes)
		{
			Double x = TickX(note.StartTick, plotWidth, axisMaximumTick);
			Double endX = TickX(note.EndTick, plotWidth, axisMaximumTick);
			Double width = Math.Max(1.5, endX - x);
			Int32 y = TopMargin + (maximumNote - note.Note) * rowHeight + 1;
			Int32 height = Math.Max(2, rowHeight - 2);
			Double opacity = 0.45 + (note.Velocity / 127.0 * 0.5);
			String dash = note.IsOpenEnded ? " stroke-dasharray=\"3 2\"" : String.Empty;

			writer.WriteLine($"  <rect x=\"{Number(x)}\" y=\"{y}\" width=\"{Number(width)}\" height=\"{height}\" rx=\"1\" fill=\"#2563EB\" fill-opacity=\"{Number(opacity)}\" stroke=\"#1D4ED8\" stroke-width=\"0.7\"{dash}>");
			writer.WriteLine($"    <title>{EscapeXml(NoteTooltip(note))}</title>");
			writer.WriteLine("  </rect>");
		}
	}

	private static void WriteEventPlot(
		StreamWriter writer,
		List<EventMarker> markers,
		EventType[] eventTypes,
		Int32 eventPlotTop,
		Int32 eventPlotHeight,
		Int32 plotWidth,
		UInt64 axisMaximumTick)
	{
		writer.WriteLine($"  <text class=\"section-label\" transform=\"translate(18 {eventPlotTop + eventPlotHeight / 2}) rotate(-90)\" text-anchor=\"middle\">Events</text>");
		writer.WriteLine($"  <rect x=\"{LeftMargin}\" y=\"{eventPlotTop}\" width=\"{plotWidth}\" height=\"{eventPlotHeight}\" fill=\"#F8FAFC\" stroke=\"#475569\" />");

		if (eventTypes.Length == 0)
		{
			writer.WriteLine($"  <text class=\"axis-label\" x=\"{LeftMargin + plotWidth / 2}\" y=\"{eventPlotTop + eventPlotHeight / 2 + 4}\" text-anchor=\"middle\">No non-note events</text>");
			return;
		}

		var rowByType = eventTypes
			.Select((eventType, index) => (eventType, index))
			.ToDictionary(item => item.eventType, item => item.index);

		foreach (EventType eventType in eventTypes)
		{
			Int32 row = rowByType[eventType];
			Int32 y = eventPlotTop + row * EventRowHeight;
			if (row > 0)
				writer.WriteLine($"  <line class=\"note-line\" x1=\"{LeftMargin}\" y1=\"{y}\" x2=\"{LeftMargin + plotWidth}\" y2=\"{y}\" />");
			writer.WriteLine($"  <rect x=\"{LeftMargin - 13}\" y=\"{y + 6}\" width=\"8\" height=\"8\" fill=\"{eventType.Color}\" />");
			writer.WriteLine($"  <text class=\"axis-label\" x=\"{LeftMargin - 18}\" y=\"{y + 15}\" text-anchor=\"end\">{EscapeXml(eventType.Name)}</text>");
		}

		foreach (EventMarker marker in markers)
		{
			Int32 row = rowByType[marker.EventType];
			Double x = TickX(marker.Event.AbsoluteTime, plotWidth, axisMaximumTick);
			Int32 y = eventPlotTop + row * EventRowHeight + 3;
			writer.WriteLine($"  <rect x=\"{Number(x - 2)}\" y=\"{y}\" width=\"4\" height=\"{EventRowHeight - 6}\" rx=\"1\" fill=\"{marker.EventType.Color}\" fill-opacity=\"0.9\">");
			writer.WriteLine($"    <title>{EscapeXml($"Tick {marker.Event.AbsoluteTime.ToString("N0", CultureInfo.InvariantCulture)}: {marker.Event.Description}")}</title>");
			writer.WriteLine("  </rect>");
		}
	}

	private static void WriteTimeAxis(
		StreamWriter writer,
		Int32 plotWidth,
		Int32 eventPlotBottom,
		UInt64 axisMaximumTick,
		UInt64 tickInterval)
	{
		writer.WriteLine($"  <line class=\"axis-line\" x1=\"{LeftMargin}\" y1=\"{eventPlotBottom}\" x2=\"{LeftMargin + plotWidth}\" y2=\"{eventPlotBottom}\" />");
		for (UInt64 tick = 0; ; tick += tickInterval)
		{
			Double x = TickX(tick, plotWidth, axisMaximumTick);
			String textAnchor = tick == 0 ? "start" : tick >= axisMaximumTick ? "end" : "middle";
			writer.WriteLine($"  <line class=\"axis-line\" x1=\"{Number(x)}\" y1=\"{eventPlotBottom}\" x2=\"{Number(x)}\" y2=\"{eventPlotBottom + 5}\" />");
			writer.WriteLine($"  <text class=\"axis-label\" x=\"{Number(x)}\" y=\"{eventPlotBottom + 19}\" text-anchor=\"{textAnchor}\">{tick.ToString("N0", CultureInfo.InvariantCulture)}</text>");
			if (tick >= axisMaximumTick || tick > UInt64.MaxValue - tickInterval)
				break;
		}
		writer.WriteLine($"  <text class=\"section-label\" x=\"{LeftMargin + plotWidth / 2}\" y=\"{eventPlotBottom + 46}\" text-anchor=\"middle\">Time (ticks)</text>");
	}

	private static Double TickX(UInt64 tick, Int32 plotWidth, UInt64 axisMaximumTick)
	{
		return LeftMargin + ((Double)tick / axisMaximumTick * plotWidth);
	}

	private static UInt64 TickInterval(UInt64 maximumTick)
	{
		UInt64 rawInterval = maximumTick == 0 ? 1 : DivideRoundUp(maximumTick, 10);
		UInt64 magnitude = 1;
		while (magnitude <= rawInterval / 10)
			magnitude *= 10;

		UInt64 scaled = DivideRoundUp(rawInterval, magnitude);
		UInt64 multiplier = scaled <= 1 ? 1UL : scaled <= 2 ? 2UL : scaled <= 5 ? 5UL : 10UL;
		return multiplier > UInt64.MaxValue / magnitude ? UInt64.MaxValue : multiplier * magnitude;
	}

	private static UInt64 RoundUp(UInt64 value, UInt64 quantum)
	{
		UInt64 remainder = value % quantum;
		if (remainder == 0)
			return value;
		UInt64 increase = quantum - remainder;
		return value > UInt64.MaxValue - increase ? UInt64.MaxValue : value + increase;
	}

	private static UInt64 DivideRoundUp(UInt64 value, UInt64 divisor)
	{
		return value / divisor + (value % divisor == 0 ? 0UL : 1UL);
	}

	private static String NoteName(Byte note)
	{
		String[] names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
		return $"{names[note % 12]}{note / 12 - 1}";
	}

	private static String NoteTooltip(NoteSpan note)
	{
		String ending = note.IsOpenEnded ? " (no Note Off)" : String.Empty;
		return $"{NoteName(note.Note)} ({note.Note}), channel {note.Channel}, velocity {note.Velocity}, ticks {note.StartTick.ToString("N0", CultureInfo.InvariantCulture)}-{note.EndTick.ToString("N0", CultureInfo.InvariantCulture)}{ending}";
	}

	private static String Number(Double value)
	{
		return value.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static String EscapeXml(String text)
	{
		var sanitizedText = new StringBuilder(text.Length);
		foreach (Char character in text)
			sanitizedText.Append(System.Xml.XmlConvert.IsXmlChar(character) ? character : '\uFFFD');

		return sanitizedText.ToString()
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;")
			.Replace("\"", "&quot;")
			.Replace("'", "&apos;");
	}

	private sealed record TimelineSvg(MemoryStream Stream, Int32 Width, Int32 Height);
	private sealed record NoteSpan(Byte Note, Int32 Channel, Byte Velocity, UInt64 StartTick, UInt64 EndTick, Boolean IsOpenEnded);
	private sealed record EventMarker(MidiEvent Event, EventType EventType);
	private sealed record EventType(String Name, String Color, Int32 Order);

	private static class EventTypes
	{
		public static readonly EventType Tempo = new("Tempo", "#DC2626", 0);
		public static readonly EventType TimeSignature = new("Time Signature", "#16A34A", 1);
		public static readonly EventType KeySignature = new("Key Signature", "#0891B2", 2);
		public static readonly EventType ProgramChange = new("Program Change", "#EA580C", 3);
		public static readonly EventType ControlChange = new("Control Change", "#2563EB", 4);
		public static readonly EventType PitchBend = new("Pitch Bend", "#7C3AED", 5);
		public static readonly EventType Pressure = new("Key / Channel Pressure", "#DB2777", 6);
		public static readonly EventType Text = new("Text / Marker", "#4F46E5", 7);
		public static readonly EventType SystemExclusive = new("System Exclusive", "#D97706", 8);
		public static readonly EventType OtherMeta = new("Other Meta", "#64748B", 9);
		public static readonly EventType EndOfTrack = new("End of Track", "#334155", 10);
		public static readonly EventType System = new("System", "#111827", 11);
	}
}
