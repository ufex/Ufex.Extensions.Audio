using Ufex.API.Tree;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal sealed class SampleBlockNode : ExsBlockNode
{
	private readonly SampleBlock _sample;
	public SampleBlockNode(SampleBlock sample) : base(sample, TreeViewIcon.Object) => _sample = sample;

	protected override IEnumerable<BlockField> GetFields()
	{
		Int64 p = _sample.ContentPosition;
		var fields = new List<BlockField>
		{
			new("Audio Data Offset", _sample.AudioDataOffset, String.Empty, p), new("Frame Count", _sample.FrameCount, String.Empty, p + 4),
			new("Sample Rate", _sample.SampleRate, "Hz", p + 8), new("Bit Depth", _sample.BitDepth, "bits", p + 12),
			new("Channels", _sample.Channels, String.Empty, p + 16), new("Channels (duplicate)", _sample.ChannelsDuplicate, String.Empty, p + 20),
			new("File Type", _sample.FileTypeBytes, $"{_sample.AudioFormat} ({_sample.FileTypeText})", p + 28), new("File Size", _sample.FileSize, "bytes", p + 32),
			new("Compression", _sample.Compression, _sample.Compression == 0 ? "Uncompressed" : "Compressed", p + 36),
			new("Windows Volume ID", _sample.WindowsVolumeId, String.Empty, p + 72), new("Folder", _sample.FolderBytes, _sample.Folder, p + 80)
		};
		if (_sample.FileNameBytes.Length != 0) fields.Add(new BlockField("File Name", _sample.FileNameBytes, _sample.FileName, p + 336));
		return fields;
	}
}
