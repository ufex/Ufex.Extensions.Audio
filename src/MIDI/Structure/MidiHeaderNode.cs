using System.Text;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiHeaderNode : SectionNode
{
	public MidiHeaderNode(MidiHeader header)
		: base(header, "MThd", TreeViewIcon.Header)
	{
	}

	public override Object[][] GetRows()
	{
		var header = (MidiHeader)Section;
		var rows = new List<Object[]>();
		rows.Add(["Chunk ID", header.ChunkId, Encoding.ASCII.GetString(header.ChunkId)]);
		rows.Add(["Length", header.Length, $"{header.Length} bytes"]);
		rows.Add(["Format", header.Format, header.FormatText]);
		rows.Add(["Track Count", header.TrackCount]);
		rows.Add(["Division", header.Division, header.DivisionText]);
		if (header.ExtraData.Length > 0)
			rows.Add(["Extra Header Data", header.ExtraData, $"{header.ExtraData.Length} bytes"]);
		return rows.ToArray();
	}
}

