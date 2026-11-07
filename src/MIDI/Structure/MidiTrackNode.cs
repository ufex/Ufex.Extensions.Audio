using System.Text;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiTrackNode : SectionNode
{
	public MidiTrackNode(MidiTrack track)
		: base(track, NodeText(track), TreeViewIcon.List)
	{
		foreach (MidiEvent midiEvent in track.Events)
			Nodes.Add(new MidiEventNode(midiEvent));
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

