using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal readonly record struct BlockField(String Name, Object Value, String Description, Int64 Position);

internal class ExsBlockNode : TreeNode
{
	protected ExsBlock Block { get; }

	protected ExsBlockNode(ExsBlock block, TreeViewIcon icon)
		: base(NodeText(block), icon, icon)
	{
		Block = block;
	}

	public static ExsBlockNode FromBlock(ExsBlock block) => block switch
	{
		InstrumentBlock instrument => new InstrumentBlockNode(instrument),
		ZoneBlock zone => new ZoneBlockNode(zone),
		GroupBlock group => new GroupBlockNode(group),
		SampleBlock sample => new SampleBlockNode(sample),
		ParametersBlock parameters => new ParametersBlockNode(parameters),
		_ => new ExsBlockNode(block, TreeViewIcon.Binary)
	};

	public override Visual[] GetVisuals(IFileContext context) =>
	[
		new DataGridVisual(TableData(), "Data"),
		new HexViewerVisual(context.FileStream, Block.StartPosition, "Raw block")
	];

	protected virtual IEnumerable<BlockField> GetFields() => [];

	private DynamicTableData TableData()
	{
		var table = new DynamicTableData(4, "EXS.PropertyValueDescriptionOffset");
		table.SetColumn(0, "Property"); table.SetColumn(1, "Value");
		table.SetColumn(2, "Description"); table.SetColumn(3, "Offset");
		ExsBlockHeader header = Block.Header;
		Add(table, "Byte Order", header.ByteOrder, header.Endian == Endian.Little ? "Little-endian" : "Big-endian", header.StartPosition);
		Add(table, "Version Major", header.VersionMajor, String.Empty, header.StartPosition + 1);
		Add(table, "Version Minor", header.VersionMinor, String.Empty, header.StartPosition + 2);
		Add(table, "Raw Type", header.RawType, Block.TypeName, header.StartPosition + 3);
		Add(table, "Content Size", header.DeclaredContentSize, header.UsesMaskedSize ? $"Masked size: {header.ContentSize} bytes" : $"{header.ContentSize} bytes", header.StartPosition + 4);
		Add(table, "Index", header.Index, String.Empty, header.StartPosition + 8);
		Add(table, "Flags", header.Flags, String.Empty, header.StartPosition + 12);
		Add(table, "Magic", header.Magic, header.MagicText, header.StartPosition + 16);
		Add(table, "Name", header.NameBytes, header.Name, header.StartPosition + 20);
		foreach (BlockField field in GetFields()) Add(table, field.Name, field.Value, field.Description, field.Position);
		return table;
	}

	private static void Add(DynamicTableData table, String name, Object value, String description, Int64 position) =>
		table.AddRow(name, value, description, new FileOffset(position));

	private static String NodeText(ExsBlock block)
	{
		String index = block.Type is ExsBlockType.Instrument or ExsBlockType.Parameters ? String.Empty : $" {block.Header.Index}";
		String name = String.IsNullOrWhiteSpace(block.Name) ? String.Empty : $": {block.Name}";
		return $"{block.TypeName}{index}{name}";
	}
}
