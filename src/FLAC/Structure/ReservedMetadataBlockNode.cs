using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class ReservedMetadataBlockNode : MetadataBlockNode
{
	public ReservedMetadataBlockNode(ReservedMetadataBlock block)
		: base(block, TreeViewIcon.Binary)
	{
	}

	protected override void AddRows(DynamicTableData table)
	{
		AddRow(table, "Reserved Data", (UInt32)Block.Length, $"{(UInt32)Block.Length} bytes", Block.DataStartPosition);
	}
}
