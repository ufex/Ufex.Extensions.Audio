using Ufex.API.Tree;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiEventNode : SectionNode
{
	public MidiEventNode(MidiEvent midiEvent)
		: base(midiEvent, $"{midiEvent.AbsoluteTime}: {midiEvent.Description}", NodeIcon(midiEvent))
	{
	}

	public override Object[][] GetRows()
	{
		var midiEvent = (MidiEvent)Section;
		var rows = new List<Object[]>();
		rows.Add(["Delta Time", midiEvent.DeltaTimeBytes, $"{midiEvent.DeltaTime} ticks; absolute time {midiEvent.AbsoluteTime}"]);

		if (midiEvent.StatusPresent)
			rows.Add(["Status", midiEvent.Status, StatusDescription(midiEvent)]);

		if (midiEvent.IsMeta)
		{
			rows.Add(["Meta Type", midiEvent.MetaType!.Value, Constants.MetaTypeDescription(midiEvent.MetaType.Value)]);
			rows.Add(["Data Length", midiEvent.LengthBytes, $"{midiEvent.Data.Length} bytes"]);
		}
		else if (midiEvent.IsSystemExclusive)
		{
			rows.Add(["Data Length", midiEvent.LengthBytes, $"{midiEvent.Data.Length} bytes"]);
		}

		if (midiEvent.Data.Length > 0)
			rows.Add(["Data", midiEvent.Data, midiEvent.Description]);
		return rows.ToArray();
	}

	private static String StatusDescription(MidiEvent midiEvent)
	{
		String runningStatus = midiEvent.UsesRunningStatus ? " (running status)" : String.Empty;
		return $"0x{midiEvent.Status:X2}{runningStatus}";
	}

	private static TreeViewIcon NodeIcon(MidiEvent midiEvent)
	{
		if (midiEvent.IsMeta)
		{
			return midiEvent.MetaType is >= 0x01 and <= 0x07
				? TreeViewIcon.Text
				: TreeViewIcon.Information;
		}
		return midiEvent.IsSystemExclusive ? TreeViewIcon.Binary : TreeViewIcon.Object;
	}
}
