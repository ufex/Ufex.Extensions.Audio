using System.Text;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class ApplicationMetadataBlockNode : MetadataBlockNode
{
	public ApplicationMetadataBlockNode(ApplicationMetadataBlock block)
		: base(block, TreeViewIcon.Gear)
	{
	}

	protected override void AddRows(DynamicTableData table)
	{
		var block = (ApplicationMetadataBlock)Block;
		String id = Encoding.ASCII.GetString(block.ApplicationId.Select(value => value is >= 0x20 and <= 0x7E ? value : (Byte)'?').ToArray());
		AddRow(table, "Application ID", block.ApplicationId, id, block.DataStartPosition);
		AddRow(table, "Application Data", block.ApplicationDataLength, $"{block.ApplicationDataLength} bytes", block.ApplicationDataPosition);
	}
}
