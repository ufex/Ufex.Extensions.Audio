using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

/// <summary>
/// Base tree node for Standard MIDI File sections.
/// </summary>
internal class SectionNode : TreeNode
{
	public Section Section { get; }

	public override Ufex.API.Visual.Visual[] Visuals => [new DataGridVisual(TableData(), "Data")];

	protected SectionNode(Section section, String text, TreeViewIcon icon)
		: base(text, icon, icon)
	{
		Section = section;
	}

	public virtual DynamicTableData TableData()
	{
		var tableData = new DynamicTableData(4, "MIDI.PropertyValueDescription");
		tableData.SetColumn(0, "Property");
		tableData.SetColumn(1, "Value");
		tableData.SetColumn(2, "Description");
		tableData.SetColumn(3, "Offset");

		Object[][] rows = GetRows();
		Int64 offset = Section.StartPosition;
		foreach (Object[] row in rows)
		{
			tableData.AddRow(row[0], row[1], row.Length > 2 ? row[2] : String.Empty, new FileOffset(offset));
			offset += ByteUtil.GetObjectSize(row[1]);
		}
		return tableData;
	}

	public virtual Object[][] GetRows() => [];
}

