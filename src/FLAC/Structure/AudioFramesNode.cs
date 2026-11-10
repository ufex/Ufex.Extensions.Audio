using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class AudioFramesNode : TreeNode
{
	private readonly AudioData _audioData;
	private readonly StreamInfoMetadataBlock _streamInfo;

	public override Boolean HasDeferredChildren => _audioData.StartPosition < _audioData.EndPosition;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = MetadataBlockNode.CreateTable();
		MetadataBlockNode.AddRow(table, "Audio Data Length", (UInt64)(_audioData.EndPosition - _audioData.StartPosition), "Encoded FLAC frames", _audioData.StartPosition);
		return [new DataGridVisual(table, "Data")];
	}

	public AudioFramesNode(AudioData audioData, StreamInfoMetadataBlock streamInfo)
		: base("Audio Frames", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen)
	{
		_audioData = audioData;
		_streamInfo = streamInfo;
	}

	public override void LoadChildren(IFileContext context)
	{
		var frameReader = new FlacFrameReader(context.FileStream, _streamInfo, _audioData.StartPosition);
		AudioFrame[] frames = frameReader.ReadFrames().ToArray();
		foreach (AudioFrameGroup group in AudioFrameGrouper.GroupOverview(frames, _streamInfo.SampleRate))
			Nodes.Add(new AudioFrameGroupNode(group, _streamInfo.SampleRate));
		Text = $"Audio Frames ({frames.Length:N0})";
		base.LoadChildren(context);
	}
}
