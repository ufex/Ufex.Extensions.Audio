using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class StreamInfoMetadataBlockNode : MetadataBlockNode
{
	public StreamInfoMetadataBlockNode(StreamInfoMetadataBlock block)
		: base(block, TreeViewIcon.Information)
	{
	}

	protected override void AddRows(DynamicTableData table)
	{
		var block = (StreamInfoMetadataBlock)Block;
		Int64 offset = block.DataStartPosition;
		AddRow(table, "Minimum Block Size", block.MinimumBlockSize, $"{block.MinimumBlockSize} samples", offset);
		AddRow(table, "Maximum Block Size", block.MaximumBlockSize, $"{block.MaximumBlockSize} samples", offset + 2);
		AddRow(table, "Minimum Frame Size", block.MinimumFrameSize, (UInt32)block.MinimumFrameSize == 0 ? "Unknown" : $"{(UInt32)block.MinimumFrameSize} bytes", offset + 4);
		AddRow(table, "Maximum Frame Size", block.MaximumFrameSize, (UInt32)block.MaximumFrameSize == 0 ? "Unknown" : $"{(UInt32)block.MaximumFrameSize} bytes", offset + 7);
		AddRow(table, "Sample Rate", block.SampleRate, $"{block.SampleRate:N0} Hz", offset + 10);
		AddRow(table, "Channels", block.Channels, String.Empty, offset + 10);
		AddRow(table, "Bits Per Sample", block.BitsPerSample, String.Empty, offset + 10);
		AddRow(table, "Total Samples", block.TotalSamples, block.TotalSamples == 0 ? "Unknown" : String.Empty, offset + 10);
		AddRow(table, "MD5 Signature", block.Md5Signature, block.Md5Signature.All(value => value == 0) ? "Unknown" : "Unencoded audio checksum", offset + 18);
	}
}
