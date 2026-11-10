using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class SeekTableMetadataBlockNode : MetadataBlockNode
{
	private SeekTableMetadataBlock SeekTable => (SeekTableMetadataBlock)Block;

	public override Boolean HasDeferredChildren => SeekTable.PointCount > 0;

	public SeekTableMetadataBlockNode(SeekTableMetadataBlock block)
		: base(block, TreeViewIcon.Table)
	{
		Text = $"SEEKTABLE ({block.PointCount} points)";
	}

	public override void LoadChildren(IFileContext context)
	{
		context.FileStream.Position = SeekTable.DataStartPosition;
		var fr = new FileReader(context.FileStream, Endian.Big);
		for (UInt32 i = 0; i < SeekTable.PointCount; i++)
		{
			Int64 position = context.FileStream.Position;
			var point = new SeekPoint(fr.ReadUInt64(), fr.ReadUInt64(), fr.ReadUInt16());
			Nodes.Add(new SeekPointNode(i, position, point));
		}
		base.LoadChildren(context);
	}

	protected override void AddRows(DynamicTableData table)
	{
		AddRow(table, "Seek Point Count", SeekTable.PointCount, String.Empty, SeekTable.DataStartPosition);
		AddRow(table, "Sorted", SeekTable.IsSorted, String.Empty, SeekTable.DataStartPosition);
		AddRow(table, "Duplicate Samples", SeekTable.HasDuplicateSampleNumbers, String.Empty, SeekTable.DataStartPosition);
	}
}
