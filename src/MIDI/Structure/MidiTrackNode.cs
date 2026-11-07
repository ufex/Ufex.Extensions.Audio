using System.Text;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.MIDI.Data;
using Ufex.Extensions.Audio.MIDI.Visual;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiTrackNode : SectionNode
{
	private readonly MidiTimelineVisual _timelineVisual;

	public override Ufex.API.Visual.Visual[] Visuals =>
	[
		new DataGridVisual(TableData(), "Data"),
		_timelineVisual
	];

	public MidiTrackNode(MidiTrack track)
		: base(track, NodeText(track), TreeViewIcon.List)
	{
		_timelineVisual = new MidiTimelineVisual(track);
		foreach (MidiEventGroup eventGroup in MidiEventGrouper.Group(track.Events))
			Nodes.Add(new MidiEventGroupNode(eventGroup));
	}

	public override Object[][] GetRows()
	{
		var track = (MidiTrack)Section;
		return
		[
			["Chunk ID", track.ChunkId, Encoding.ASCII.GetString(track.ChunkId)],
			["Length", track.Length, $"{track.Length} bytes containing {track.Events.Count} event(s)"]
		];
	}

	private static String NodeText(MidiTrack track)
	{
		return String.IsNullOrWhiteSpace(track.Name)
			? $"Track {track.Index + 1}"
			: $"Track {track.Index + 1}: {track.Name}";
	}
}

