using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class AudioFrameNode : TreeNode
{
	private readonly AudioFrame _frame;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = MetadataBlockNode.CreateTable();
		MetadataBlockNode.AddRow(table, "Blocking Strategy", _frame.IsVariableBlockSize, _frame.IsVariableBlockSize ? "Variable block size" : "Fixed block size", _frame.StartPosition + 1);
		MetadataBlockNode.AddRow(table, "Block Size Code", _frame.BlockSizeCode, $"{_frame.BlockSize} samples", _frame.StartPosition + 2);
		MetadataBlockNode.AddRow(table, "Sample Rate Code", _frame.SampleRateCode, $"{_frame.SampleRate:N0} Hz", _frame.StartPosition + 2);
		MetadataBlockNode.AddRow(table, "Channel Assignment", _frame.ChannelAssignment, _frame.ChannelAssignmentText, _frame.StartPosition + 3);
		MetadataBlockNode.AddRow(table, "Channel Count", _frame.ChannelCount, String.Empty, _frame.StartPosition + 3);
		MetadataBlockNode.AddRow(table, "Bit Depth Code", _frame.BitDepthCode, $"{_frame.BitsPerSample} bits per sample", _frame.StartPosition + 3);
		MetadataBlockNode.AddRow(table, _frame.IsVariableBlockSize ? "First Sample Number" : "Frame Number", _frame.CodedNumber, String.Empty, _frame.StartPosition + 4);
		MetadataBlockNode.AddRow(table, "Header CRC-8", _frame.HeaderCrc, "Valid", _frame.StartPosition + _frame.HeaderLength - 1);
		MetadataBlockNode.AddRow(table, "Frame Length", (UInt64)(_frame.EndPosition - _frame.StartPosition), "bytes", _frame.StartPosition);
		MetadataBlockNode.AddRow(table, "Frame CRC-16", _frame.FrameCrcValid, _frame.FrameCrcValid ? "Valid" : "Invalid", _frame.EndPosition - 2);
		return [new DataGridVisual(table, "Data")];
	}

	public AudioFrameNode(AudioFrame frame)
		: base($"Frame {frame.Index}: {frame.BlockSize} samples", TreeViewIcon.Binary, TreeViewIcon.Binary)
	{
		_frame = frame;
	}
}
