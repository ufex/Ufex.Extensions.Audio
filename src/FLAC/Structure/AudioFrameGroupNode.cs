using Ufex.API;
using Ufex.API.Tree;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class AudioFrameGroupNode : TreeNode
{
	private readonly AudioFrameGroup _group;
	private readonly UInt32 _sampleRate;

	public override Boolean HasDeferredChildren => _group.Frames.Count > 0;

	public AudioFrameGroupNode(AudioFrameGroup group, UInt32 sampleRate)
		: base(group.Text, TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen)
	{
		_group = group;
		_sampleRate = sampleRate;
	}

	public override void LoadChildren(IFileContext context)
	{
		if (_group.Frames.Count <= AudioFrameGrouper.DirectFrameLimit)
		{
			foreach (var frame in _group.Frames)
				Nodes.Add(new AudioFrameNode(frame));
		}
		else
		{
			foreach (AudioFrameGroup subgroup in AudioFrameGrouper.Group(_group.Frames, _sampleRate))
				Nodes.Add(new AudioFrameGroupNode(subgroup, _sampleRate));
		}
		base.LoadChildren(context);
	}
}
