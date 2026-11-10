using Ufex.API;
using Ufex.API.Format;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;
using Ufex.Extensions.Audio.FLAC.Structure;

namespace Ufex.Extensions.Audio.FLAC;

/// <summary>
/// File type implementation for native FLAC streams.
/// </summary>
public sealed class FlacFileType : FileType
{
	public FlacFileType()
	{
		Description = "Free Lossless Audio Codec";
		EnableStructure = true;
		EnableVisual = true;
		EnableValidation = true;
	}

	public override Boolean ProcessFile()
	{
		var reader = new FlacStreamReader(FileInStream, Log, ValidationReport);
		Boolean result = reader.Read();
		if (reader.StreamInfo == null || reader.AudioData == null)
			return result;

		BuildQuickInfo(reader);
		BuildVisuals(reader);
		BuildStructure(reader);
		return result;
	}

	private void BuildQuickInfo(FlacStreamReader reader)
	{
		StreamInfoMetadataBlock streamInfo = reader.StreamInfo!;
		QuickInfoTable.AddRow("Format", "FLAC");
		QuickInfoTable.AddRow("Sample Rate", $"{streamInfo.SampleRate:N0} Hz");
		QuickInfoTable.AddRow("Channels", streamInfo.Channels.ToString());
		QuickInfoTable.AddRow("Bits Per Sample", streamInfo.BitsPerSample.ToString());
		if (streamInfo.TotalSamples != 0)
		{
			QuickInfoTable.AddRow("Total Samples", streamInfo.TotalSamples.ToString("N0"));
			if (streamInfo.SampleRate != 0)
			{
				TimeSpan duration = TimeSpan.FromSeconds((Double)streamInfo.TotalSamples / streamInfo.SampleRate);
				QuickInfoTable.AddRow("Duration", FormatDuration(duration));
			}
		}
		QuickInfoTable.AddRow("Metadata Blocks", reader.MetadataBlocks.Count.ToString());
		QuickInfoTable.AddRow("Encoded Audio Size", ByteCountFormatter.Format((UInt64)(reader.AudioData!.EndPosition - reader.AudioData.StartPosition)));

		VorbisCommentMetadataBlock? comments = reader.MetadataBlocks.OfType<VorbisCommentMetadataBlock>().FirstOrDefault();
		AddTag(comments, "TITLE", "Title");
		AddTag(comments, "ARTIST", "Artist");
		AddTag(comments, "ALBUM", "Album");
		Int32 pictureCount = reader.MetadataBlocks.OfType<PictureMetadataBlock>().Count();
		if (pictureCount != 0)
			QuickInfoTable.AddRow("Pictures", pictureCount.ToString());
	}

	private void BuildVisuals(FlacStreamReader reader)
	{
		var spans = new List<FileSpan>
		{
			new(0, Signatures.StreamMarker.Length, "FLAC stream marker")
		};
		foreach (MetadataBlock block in reader.MetadataBlocks)
			spans.Add(new FileSpan(block.StartPosition, block.EndPosition, block.TypeName));
		if (reader.AudioData!.StartPosition < reader.AudioData.EndPosition)
			spans.Add(new FileSpan(reader.AudioData.StartPosition, reader.AudioData.EndPosition, "Audio frames"));
		VisualsList.Add(new FileMap(spans.ToArray(), (UInt64)FileInStream.Length));
	}

	private void BuildStructure(FlacStreamReader reader)
	{
		var metadataNode = new TreeNode("Metadata", TreeViewIcon.FolderClosed, TreeViewIcon.FolderOpen);
		foreach (MetadataBlock block in reader.MetadataBlocks)
			metadataNode.Nodes.Add(MetadataBlockNode.FromBlock(block));
		TreeNodes.Add(metadataNode);
		TreeNodes.Add(new AudioFramesNode(reader.AudioData!, reader.StreamInfo!));
	}

	private void AddTag(VorbisCommentMetadataBlock? comments, String fieldName, String displayName)
	{
		String? value = comments?.GetFirstValue(fieldName);
		if (!String.IsNullOrWhiteSpace(value))
			QuickInfoTable.AddRow(displayName, value);
	}

	private static String FormatDuration(TimeSpan duration)
	{
		return duration.TotalHours >= 1
			? $"{(Int32)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds:000}"
			: $"{duration.Minutes}:{duration.Seconds:00}.{duration.Milliseconds:000}";
	}
}
