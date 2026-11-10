using Ufex.API;
using Ufex.API.Tables;
using Ufex.API.Tree;
using Ufex.API.Visual;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal sealed class PictureMetadataBlockNode : MetadataBlockNode
{
	private PictureMetadataBlock Picture => (PictureMetadataBlock)Block;

	public PictureMetadataBlockNode(PictureMetadataBlock block)
		: base(block, TreeViewIcon.Image)
	{
		Text = $"PICTURE: {Constants.PictureTypeName(block.PictureType)}";
	}

	public override Visual[] GetVisuals(IFileContext context)
	{
		var visuals = new List<Visual> { new DataGridVisual(TableData(), "Data") };
		if (Picture.MediaTypeText == "-->")
			return visuals.ToArray();

		try
		{
			context.FileStream.Position = Picture.PictureDataPosition;
			Byte[] data = new Byte[checked((Int32)Picture.PictureDataLength)];
			context.FileStream.ReadExactly(data);
			visuals.Add(new RasterImageVisual(new MemoryStream(data, writable: false), "Picture Preview"));
		}
		catch (Exception exception) when (exception is IOException or ArgumentException or OverflowException)
		{
			visuals.Add(new TextVisual($"Unable to load embedded picture: {exception.Message}", "Picture Error"));
		}
		return visuals.ToArray();
	}

	protected override void AddRows(DynamicTableData table)
	{
		Int64 offset = Picture.DataStartPosition;
		AddRow(table, "Picture Type", Picture.PictureType, Constants.PictureTypeName(Picture.PictureType), offset);
		offset += 4;
		AddRow(table, "Media Type Length", Picture.MediaTypeLength, String.Empty, offset);
		offset += 4;
		AddRow(table, "Media Type", Picture.MediaTypeText, String.Empty, offset);
		offset += Picture.MediaTypeLength;
		AddRow(table, "Description Length", Picture.DescriptionLength, String.Empty, offset);
		offset += 4;
		AddRow(table, "Description", Picture.DescriptionText, String.Empty, offset);
		offset += Picture.DescriptionLength;
		AddRow(table, "Width", Picture.Width, "pixels", offset);
		AddRow(table, "Height", Picture.Height, "pixels", offset + 4);
		AddRow(table, "Color Depth", Picture.ColorDepth, "bits per pixel", offset + 8);
		AddRow(table, "Color Count", Picture.ColorCount, Picture.ColorCount == 0 ? "Not indexed" : "Indexed colors", offset + 12);
		AddRow(table, "Picture Data Length", Picture.PictureDataLength, $"{Picture.PictureDataLength} bytes", offset + 16);
	}
}
