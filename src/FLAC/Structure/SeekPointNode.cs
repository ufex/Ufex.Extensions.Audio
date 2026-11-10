using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class SeekPointNode : TreeNode
{
	private readonly Int64 _position;
	private readonly SeekPoint _point;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = MetadataBlockNode.CreateTable();
		MetadataBlockNode.AddRow(table, "Sample Number", _point.SampleNumber, _point.SampleNumber == Signatures.PlaceholderSampleNumber ? "Placeholder" : String.Empty, _position);
		MetadataBlockNode.AddRow(table, "Stream Offset", _point.StreamOffset, "Relative to the first audio frame", _position + 8);
		MetadataBlockNode.AddRow(table, "Frame Samples", _point.FrameSamples, String.Empty, _position + 16);
		return [new DataGridVisual(table, "Data")];
	}

	public SeekPointNode(UInt32 index, Int64 position, SeekPoint point)
		: base(point.SampleNumber == Signatures.PlaceholderSampleNumber ? $"Point {index + 1}: Placeholder" : $"Point {index + 1}: Sample {point.SampleNumber}", TreeViewIcon.Section, TreeViewIcon.Section)
	{
		_position = position;
		_point = point;
	}
}
