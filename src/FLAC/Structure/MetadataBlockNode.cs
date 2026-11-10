using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal class MetadataBlockNode : TreeNode
{
	protected MetadataBlock Block { get; }

	public override Visual[] GetVisuals(IFileContext context) => [new DataGridVisual(TableData(), "Data")];

	protected MetadataBlockNode(MetadataBlock block, TreeViewIcon icon)
		: base(block.TypeName, icon, icon)
	{
		Block = block;
	}

	public static MetadataBlockNode FromBlock(MetadataBlock block)
	{
		return block switch
		{
			StreamInfoMetadataBlock streamInfo => new StreamInfoMetadataBlockNode(streamInfo),
			PaddingMetadataBlock padding => new PaddingMetadataBlockNode(padding),
			ApplicationMetadataBlock application => new ApplicationMetadataBlockNode(application),
			SeekTableMetadataBlock seekTable => new SeekTableMetadataBlockNode(seekTable),
			VorbisCommentMetadataBlock vorbisComment => new VorbisCommentMetadataBlockNode(vorbisComment),
			CuesheetMetadataBlock cuesheet => new CuesheetMetadataBlockNode(cuesheet),
			PictureMetadataBlock picture => new PictureMetadataBlockNode(picture),
			ReservedMetadataBlock reserved => new ReservedMetadataBlockNode(reserved),
			_ => new MetadataBlockNode(block, TreeViewIcon.Section)
		};
	}

	protected virtual DynamicTableData TableData()
	{
		var table = CreateTable();
		AddRow(table, "Last Metadata Block", Block.IsLast, Block.IsLast ? "No metadata blocks follow" : "Another metadata block follows", Block.StartPosition);
		AddRow(table, "Block Type", Block.Type, Block.TypeName, Block.StartPosition);
		AddRow(table, "Block Length", Block.Length, $"{(UInt32)Block.Length} bytes, excluding header", Block.StartPosition + 1);
		AddRows(table);
		return table;
	}

	protected virtual void AddRows(DynamicTableData table)
	{
	}

	internal static DynamicTableData CreateTable()
	{
		var table = new DynamicTableData(4, "FLAC.PropertyValueDescription");
		table.SetColumn(0, "Property");
		table.SetColumn(1, "Value");
		table.SetColumn(2, "Description");
		table.SetColumn(3, "Offset");
		return table;
	}

	internal static void AddRow(DynamicTableData table, Object property, Object value, Object description, Int64 offset)
	{
		table.AddRow(property, value, description, new FileOffset(offset));
	}
}
