using Ufex.API.Tree;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal class MidiEventGroupNode : TreeNode
{
	public MidiEventGroupNode(MidiEventGroup eventGroup)
		: base(eventGroup.Text, TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen)
	{
		foreach (var midiEvent in eventGroup.Events)
			Nodes.Add(new MidiEventNode(midiEvent));
	}
}

