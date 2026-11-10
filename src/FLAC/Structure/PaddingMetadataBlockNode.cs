using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class PaddingMetadataBlockNode : MetadataBlockNode
{
	public PaddingMetadataBlockNode(PaddingMetadataBlock block)
		: base(block, TreeViewIcon.NullIcon)
	{
	}

	protected override void AddRows(DynamicTableData table)
	{
		var block = (PaddingMetadataBlock)Block;
		AddRow(table, "Non-Zero Bytes", block.NonZeroByteCount, block.NonZeroByteCount == 0 ? "Valid zero-filled padding" : "Invalid padding data", block.DataStartPosition);
	}
}
