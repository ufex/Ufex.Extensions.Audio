using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal sealed class ParametersBlockNode : ExsBlockNode
{
	private readonly ParametersBlock _parameters;
	public override Boolean HasDeferredChildren => _parameters.Parameters.Count != 0;

	public ParametersBlockNode(ParametersBlock parameters) : base(parameters, TreeViewIcon.Gear) => _parameters = parameters;

	protected override IEnumerable<BlockField> GetFields()
	{
		var fields = new List<BlockField> { new("Legacy Count", _parameters.LegacyCount, String.Empty, _parameters.ContentPosition) };
		Int64 modernCountPosition = _parameters.ContentPosition + 4 + _parameters.LegacyCount * 3L;
		if (_parameters.EndPosition - modernCountPosition >= 4)
			fields.Add(new BlockField("Modern Count", _parameters.ModernCount, String.Empty, modernCountPosition));
		return fields;
	}

	public override void LoadChildren(IFileContext context)
	{
		foreach (ExsParameter parameter in _parameters.Parameters) Nodes.Add(new ParameterNode(parameter));
		base.LoadChildren(context);
	}
}

internal sealed class ParameterNode : TreeNode
{
	private readonly ExsParameter _parameter;
	public ParameterNode(ExsParameter parameter) : base($"{parameter.Name}: {parameter.Value}", TreeViewIcon.Information, TreeViewIcon.Information) => _parameter = parameter;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = new DynamicTableData(4, "EXS.Parameter");
		table.SetColumn(0, "Property"); table.SetColumn(1, "Value"); table.SetColumn(2, "Description"); table.SetColumn(3, "Offset");
		table.AddRow("ID", _parameter.Id, _parameter.Name, new FileOffset(_parameter.IdPosition));
		table.AddRow("Value", _parameter.Value, _parameter.Encoding.ToString(), new FileOffset(_parameter.ValuePosition));
		return [new DataGridVisual(table, "Data")];
	}
}
