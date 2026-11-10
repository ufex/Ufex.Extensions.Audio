using System.Text;
using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class CuesheetMetadataBlockNode : MetadataBlockNode
{
	public CuesheetMetadataBlockNode(CuesheetMetadataBlock block)
		: base(block, TreeViewIcon.Table)
	{
		Text = $"CUESHEET ({block.TrackCount} tracks)";
		foreach (CuesheetTrack track in block.Tracks)
			Nodes.Add(new CuesheetTrackNode(track));
	}

	protected override void AddRows(DynamicTableData table)
	{
		var block = (CuesheetMetadataBlock)Block;
		String catalogNumber = Encoding.ASCII.GetString(block.MediaCatalogNumber).TrimEnd('\0');
		AddRow(table, "Media Catalog Number", catalogNumber, String.Empty, block.DataStartPosition);
		AddRow(table, "Lead-In Samples", block.LeadInSamples, String.Empty, block.DataStartPosition + 128);
		AddRow(table, "Compact Disc", block.IsCompactDisc, String.Empty, block.DataStartPosition + 136);
		AddRow(table, "Track Count", block.TrackCount, String.Empty, block.DataStartPosition + 395);
	}
}

internal sealed class CuesheetTrackNode : TreeNode
{
	private readonly CuesheetTrack _track;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = MetadataBlockNode.CreateTable();
		MetadataBlockNode.AddRow(table, "Track Offset", _track.Offset, "Samples relative to the audio stream", _track.StartPosition);
		MetadataBlockNode.AddRow(table, "Track Number", _track.Number, String.Empty, _track.StartPosition + 8);
		MetadataBlockNode.AddRow(table, "ISRC", Encoding.ASCII.GetString(_track.Isrc).TrimEnd('\0'), String.Empty, _track.StartPosition + 9);
		MetadataBlockNode.AddRow(table, "Non-Audio", _track.IsNonAudio, String.Empty, _track.StartPosition + 21);
		MetadataBlockNode.AddRow(table, "Pre-Emphasis", _track.HasPreEmphasis, String.Empty, _track.StartPosition + 21);
		MetadataBlockNode.AddRow(table, "Index Count", (Byte)_track.Indices.Count, String.Empty, _track.StartPosition + 35);
		return [new DataGridVisual(table, "Data")];
	}

	public CuesheetTrackNode(CuesheetTrack track)
		: base($"Track {track.Number}", TreeViewIcon.Section, TreeViewIcon.Section)
	{
		_track = track;
		foreach (CuesheetTrackIndex index in track.Indices)
			Nodes.Add(new CuesheetTrackIndexNode(index));
	}
}

internal sealed class CuesheetTrackIndexNode : TreeNode
{
	private readonly CuesheetTrackIndex _index;

	public override Visual[] GetVisuals(IFileContext context)
	{
		var table = MetadataBlockNode.CreateTable();
		MetadataBlockNode.AddRow(table, "Index Offset", _index.Offset, "Samples relative to the track", _index.StartPosition);
		MetadataBlockNode.AddRow(table, "Index Number", _index.Number, String.Empty, _index.StartPosition + 8);
		return [new DataGridVisual(table, "Data")];
	}

	public CuesheetTrackIndexNode(CuesheetTrackIndex index)
		: base($"Index {index.Number}", TreeViewIcon.Section, TreeViewIcon.Section)
	{
		_index = index;
	}
}
