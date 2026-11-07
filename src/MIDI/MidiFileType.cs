using Ufex.API;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.MIDI.Structure;

namespace Ufex.Extensions.Audio.MIDI;

/// <summary>
/// File type implementation for Standard MIDI Files.
/// </summary>
public class MidiFileType : FileType
{
	public MidiFileType()
	{
		Description = "Standard MIDI File";
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override Boolean ProcessFile()
	{
		var reader = new MidiStreamReader(FileInStream, Log, ValidationReport);
		Boolean result = reader.Read();

		if (reader.Header != null)
		{
			BuildQuickInfo(reader);
			BuildVisuals(reader);
			BuildStructure(reader);
		}
		return result;
	}

	private void BuildQuickInfo(MidiStreamReader reader)
	{
		var header = reader.Header!;
		QuickInfoTable.AddRow("Format", header.FormatText);
		QuickInfoTable.AddRow("Tracks", reader.Tracks.Count.ToString());
		QuickInfoTable.AddRow("Time Division", header.DivisionText);
		QuickInfoTable.AddRow("Events", reader.Tracks.Sum(track => track.Events.Count).ToString());

		String[] trackNames = reader.Tracks
			.Select(track => track.Name)
			.Where(name => !String.IsNullOrWhiteSpace(name))
			.ToArray();
		if (trackNames.Length > 0)
			QuickInfoTable.AddRow("Track Names", String.Join(", ", trackNames));
	}

	private void BuildVisuals(MidiStreamReader reader)
	{
		var spans = new List<FileSpan>
		{
			new FileSpan
			{
				StartPosition = reader.Header!.StartPosition,
				EndPosition = reader.Header.EndPosition,
				Name = "MThd"
			}
		};

		foreach (var track in reader.Tracks)
		{
			String name = String.IsNullOrWhiteSpace(track.Name) ? $"Track {track.Index + 1}" : track.Name;
			spans.Add(new FileSpan
			{
				StartPosition = track.StartPosition,
				EndPosition = track.EndPosition,
				Name = $"MTrk: {name}"
			});
		}

		VisualsList.Add(new FileMap(spans.ToArray(), (UInt64)FileInStream.Length));
	}

	private void BuildStructure(MidiStreamReader reader)
	{
		TreeNodes.Add(new MidiHeaderNode(reader.Header!));

		var tracksNode = new TreeNode("Tracks", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		foreach (var track in reader.Tracks)
			tracksNode.Nodes.Add(new MidiTrackNode(track));
		TreeNodes.Add(tracksNode);
	}
}

