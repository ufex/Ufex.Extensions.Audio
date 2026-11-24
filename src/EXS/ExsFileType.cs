using Ufex.API;
using Ufex.API.Format;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.EXS.Data;
using Ufex.Extensions.Audio.EXS.Structure;

namespace Ufex.Extensions.Audio.EXS;

/// <summary>
/// File type implementation for Logic Pro EXS sampler instruments.
/// </summary>
public sealed class ExsFileType : FileType
{
	public ExsFileType()
	{
		Description = "Logic Pro EXS Sampler Instrument";
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override Boolean ProcessFile()
	{
		var reader = new ExsStreamReader(FileInStream, Log, ValidationReport);
		Boolean result = reader.Read();
		if (reader.Instrument == null) return result;
		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);
		return result;
	}

	private void BuildQuickInfo(ExsStreamReader reader)
	{
		InstrumentBlock instrument = reader.Instrument!;
		QuickInfoTable.AddRow("Format", "Logic Pro EXS sampler instrument");
		if (!String.IsNullOrWhiteSpace(instrument.Name)) QuickInfoTable.AddRow("Instrument", instrument.Name);
		QuickInfoTable.AddRow("Byte Order", instrument.Header.Endian == Endian.Little ? "Little-endian" : "Big-endian");
		QuickInfoTable.AddRow("Layout", instrument.Header.IsModern ? "Modern Sampler" : "Legacy EXS24");
		QuickInfoTable.AddRow("Zones", reader.Blocks.Count(block => block.Type == ExsBlockType.Zone).ToString());
		QuickInfoTable.AddRow("Groups", reader.Blocks.Count(block => block.Type == ExsBlockType.Group).ToString());
		QuickInfoTable.AddRow("Samples", reader.Blocks.Count(block => block.Type == ExsBlockType.Sample).ToString());
		QuickInfoTable.AddRow("Blocks", reader.Blocks.Count.ToString());
		QuickInfoTable.AddRow("File Size", ByteCountFormatter.Format((UInt64)FileInStream.Length));
		String[] sampleTypes = reader.Blocks.OfType<SampleBlock>().Select(sample => sample.AudioFormat).Distinct().ToArray();
		if (sampleTypes.Length != 0) QuickInfoTable.AddRow("Referenced Audio Types", String.Join(", ", sampleTypes));
	}

	private void BuildVisuals(ExsStreamReader reader)
	{
		FileSpan[] spans = reader.Blocks.Select(block => new FileSpan(block.StartPosition, block.EndPosition, block.TypeName)).ToArray();
		VisualsList.Add(new FileMap(spans, (UInt64)FileInStream.Length));
	}

	private void BuildStructure(ExsStreamReader reader)
	{
		foreach (ExsBlock block in reader.Blocks.Where(block => block.Type == ExsBlockType.Instrument))
			TreeNodes.Add(ExsBlockNode.FromBlock(block));
		AddFolder(reader, ExsBlockType.Zone, "Zones");
		AddFolder(reader, ExsBlockType.Group, "Groups");
		AddFolder(reader, ExsBlockType.Sample, "Samples");
		AddFolder(reader, ExsBlockType.Parameters, "Parameters");

		ExsBlock[] auxiliary = reader.Blocks.Where(block => block.Type is not (ExsBlockType.Instrument or ExsBlockType.Zone or ExsBlockType.Group or ExsBlockType.Sample or ExsBlockType.Parameters)).ToArray();
		if (auxiliary.Length == 0) return;
		var folder = new TreeNode("Auxiliary Blocks", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		foreach (ExsBlock block in auxiliary) folder.Nodes.Add(ExsBlockNode.FromBlock(block));
		TreeNodes.Add(folder);
	}

	private void AddFolder(ExsStreamReader reader, ExsBlockType type, String text)
	{
		ExsBlock[] blocks = reader.Blocks.Where(block => block.Type == type).ToArray();
		if (blocks.Length == 0) return;
		var folder = new TreeNode(text, TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		foreach (ExsBlock block in blocks) folder.Nodes.Add(ExsBlockNode.FromBlock(block));
		TreeNodes.Add(folder);
	}
}
