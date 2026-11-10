using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class VorbisCommentMetadataBlockNode : MetadataBlockNode
{
	public VorbisCommentMetadataBlockNode(VorbisCommentMetadataBlock block)
		: base(block, TreeViewIcon.Text)
	{
		Text = $"VORBIS_COMMENT ({block.Comments.Count} fields)";
	}

	protected override void AddRows(DynamicTableData table)
	{
		var block = (VorbisCommentMetadataBlock)Block;
		AddRow(table, "Vendor Length", (UInt32)block.Vendor.Length, String.Empty, block.DataStartPosition);
		AddRow(table, "Vendor", block.VendorText, String.Empty, block.DataStartPosition + 4);
		Int64 countPosition = block.DataStartPosition + 4 + block.Vendor.Length;
		AddRow(table, "Comment Count", (UInt32)block.Comments.Count, String.Empty, countPosition);
		foreach (VorbisCommentEntry comment in block.Comments)
		{
			AddRow(table, $"Comment: {comment.Name}", comment.Value, comment.Text, comment.StartPosition + 4);
		}
	}
}
