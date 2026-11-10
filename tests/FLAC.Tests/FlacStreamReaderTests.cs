using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Tests;

[TestClass]
public sealed class FlacStreamReaderTests
{
	[TestMethod]
	public void Read_ValidStream_ParsesMetadataAndFrame()
	{
		Byte[] data = BuildFlac();
		using var stream = new MemoryStream(data);
		var validationReport = new ValidationReport();
		var reader = new FlacStreamReader(stream, new Logger(), validationReport);

		Boolean result = reader.Read();

		Assert.IsTrue(result);
		Assert.IsFalse(validationReport.HasErrors, String.Join(Environment.NewLine, validationReport.GetInfo()));
		Assert.AreEqual(2, reader.MetadataBlocks.Count);
		Assert.IsNotNull(reader.StreamInfo);
		Assert.AreEqual((UInt32)8000, reader.StreamInfo.SampleRate);
		Assert.AreEqual((Byte)1, reader.StreamInfo.Channels);
		Assert.AreEqual((Byte)16, reader.StreamInfo.BitsPerSample);
		Assert.AreEqual((UInt64)16, reader.StreamInfo.TotalSamples);

		var comments = (VorbisCommentMetadataBlock)reader.MetadataBlocks[1];
		Assert.AreEqual("test", comments.VendorText);
		Assert.AreEqual("Test", comments.GetFirstValue("title"));

		var frameReader = new FlacFrameReader(stream, reader.StreamInfo, reader.AudioData!.StartPosition);
		AudioFrame[] frames = frameReader.ReadFrames().ToArray();
		Assert.HasCount(1, frames);
		Assert.AreEqual((UInt32)16, frames[0].BlockSize);
		Assert.AreEqual((UInt32)8000, frames[0].SampleRate);
		Assert.IsTrue(frames[0].FrameCrcValid);
	}

	[TestMethod]
	public void Read_InvalidMarker_ReturnsFalseAndReportsError()
	{
		using var stream = new MemoryStream(Encoding.ASCII.GetBytes("nope"));
		var validationReport = new ValidationReport();
		var reader = new FlacStreamReader(stream, new Logger(), validationReport);

		Boolean result = reader.Read();

		Assert.IsFalse(result);
		Assert.IsTrue(validationReport.HasErrors);
	}

	[TestMethod]
	public void Read_NonZeroPadding_ReportsValidationError()
	{
		Byte[] data = BuildFlac(includeInvalidPadding: true);
		using var stream = new MemoryStream(data);
		var validationReport = new ValidationReport();
		var reader = new FlacStreamReader(stream, new Logger(), validationReport);

		Boolean result = reader.Read();

		Assert.IsTrue(result);
		Assert.IsTrue(validationReport.HasErrors);
		Assert.IsTrue(validationReport.Entries.Any(entry => entry.Message.Contains("PADDING", StringComparison.Ordinal)));
	}

	[TestMethod]
	public void ReadFrames_MultipleFrames_ScansAndGroupsAllFrames()
	{
		using var stream = new MemoryStream(BuildFlac(frameCount: 120));
		var reader = new FlacStreamReader(stream, new Logger(), new ValidationReport());
		Assert.IsTrue(reader.Read());

		var frameReader = new FlacFrameReader(stream, reader.StreamInfo!, reader.AudioData!.StartPosition);
		AudioFrame[] frames = frameReader.ReadFrames().ToArray();
		IReadOnlyList<Structure.AudioFrameGroup> groups = Structure.AudioFrameGrouper.Group(frames, reader.StreamInfo!.SampleRate);

		Assert.HasCount(120, frames);
		Assert.IsTrue(frames.All(frame => frame.FrameCrcValid));
		Assert.IsTrue(groups.Count > 1);
		Assert.AreEqual(frames.Length, groups.Sum(group => group.Frames.Count));
	}

	[TestMethod]
	public void GroupOverview_LargeFrameCount_CapsTopLevelGroups()
	{
		AudioFrame[] frames = Enumerable.Range(0, 100_000)
			.Select(index => new AudioFrame(index * 12L)
			{
				Index = index,
				FirstSampleNumber = (UInt64)index * 16,
				BlockSize = 16
			})
			.ToArray();

		IReadOnlyList<Structure.AudioFrameGroup> groups = Structure.AudioFrameGrouper.GroupOverview(frames, 8000);

		Assert.IsTrue(groups.Count <= 200);
		Assert.AreEqual(frames.Length, groups.Sum(group => group.Frames.Count));
	}

	[TestMethod]
	public void ProcessFile_ValidStream_PopulatesUfexViews()
	{
		String filePath = Path.Combine(Path.GetTempPath(), $"ufex-flac-{Guid.NewGuid():N}.flac");
		try
		{
			File.WriteAllBytes(filePath, BuildFlac());
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var fileType = new FlacFileType
			{
				FileInStream = stream,
				FilePath = filePath,
				Logger = new Logger()
			};

			Boolean result = fileType.ProcessFile();

			Assert.IsTrue(result);
			Assert.IsFalse(fileType.ValidationReport.HasErrors, String.Join(Environment.NewLine, fileType.ValidationReport.GetInfo()));
			Assert.AreEqual(2, fileType.TreeNodes.Count);
			Assert.HasCount(1, fileType.Visuals);
			Assert.IsTrue(fileType.EnableStructure);
			Assert.IsTrue(fileType.EnableVisual);
			Assert.IsTrue(fileType.EnableValidation);
		}
		finally
		{
			File.Delete(filePath);
		}
	}

	private static Byte[] BuildFlac(Boolean includeInvalidPadding = false, Int32 frameCount = 1)
	{
		if (frameCount is < 1 or > 127)
			throw new ArgumentOutOfRangeException(nameof(frameCount));
		var bytes = new List<Byte>(128);
		bytes.AddRange(Encoding.ASCII.GetBytes("fLaC"));

		bytes.Add(0x00);
		WriteUInt24(bytes, 34);
		WriteUInt16(bytes, 16);
		WriteUInt16(bytes, 16);
		WriteUInt24(bytes, 0);
		WriteUInt24(bytes, 0);
		UInt64 streamProperties = ((UInt64)8000 << 44) | ((UInt64)15 << 36) | checked((UInt64)frameCount * 16UL);
		WriteUInt64(bytes, streamProperties);
		bytes.AddRange(new Byte[16]);

		if (includeInvalidPadding)
		{
			bytes.Add(0x01);
			WriteUInt24(bytes, 1);
			bytes.Add(1);
		}

		Byte[] vendor = Encoding.UTF8.GetBytes("test");
		Byte[] comment = Encoding.UTF8.GetBytes("TITLE=Test");
		UInt32 commentBlockLength = checked((UInt32)(4 + vendor.Length + 4 + 4 + comment.Length));
		bytes.Add(0x84);
		WriteUInt24(bytes, commentBlockLength);
		WriteUInt32LittleEndian(bytes, (UInt32)vendor.Length);
		bytes.AddRange(vendor);
		WriteUInt32LittleEndian(bytes, 1);
		WriteUInt32LittleEndian(bytes, (UInt32)comment.Length);
		bytes.AddRange(comment);

		for (Int32 frameNumber = 0; frameNumber < frameCount; frameNumber++)
		{
			var frame = new List<Byte> { 0xFF, 0xF8, 0x64, 0x08, (Byte)frameNumber, 0x0F };
			frame.Add(CalculateCrc8(frame));
			frame.AddRange([0x00, 0x00, 0x00]);
			UInt16 frameCrc = CalculateCrc16(frame);
			WriteUInt16(frame, frameCrc);
			bytes.AddRange(frame);
		}
		return bytes.ToArray();
	}

	private static void WriteUInt16(List<Byte> bytes, UInt16 value)
	{
		bytes.Add((Byte)(value >> 8));
		bytes.Add((Byte)value);
	}

	private static void WriteUInt24(List<Byte> bytes, UInt32 value)
	{
		bytes.Add((Byte)(value >> 16));
		bytes.Add((Byte)(value >> 8));
		bytes.Add((Byte)value);
	}

	private static void WriteUInt64(List<Byte> bytes, UInt64 value)
	{
		for (Int32 shift = 56; shift >= 0; shift -= 8)
			bytes.Add((Byte)(value >> shift));
	}

	private static void WriteUInt32LittleEndian(List<Byte> bytes, UInt32 value)
	{
		bytes.Add((Byte)value);
		bytes.Add((Byte)(value >> 8));
		bytes.Add((Byte)(value >> 16));
		bytes.Add((Byte)(value >> 24));
	}

	private static Byte CalculateCrc8(IEnumerable<Byte> bytes)
	{
		Byte crc = 0;
		foreach (Byte value in bytes)
		{
			crc ^= value;
			for (Int32 i = 0; i < 8; i++)
				crc = (Byte)((crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1);
		}
		return crc;
	}

	private static UInt16 CalculateCrc16(IEnumerable<Byte> bytes)
	{
		UInt16 crc = 0;
		foreach (Byte value in bytes)
		{
			crc ^= (UInt16)(value << 8);
			for (Int32 i = 0; i < 8; i++)
				crc = (UInt16)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x8005 : crc << 1);
		}
		return crc;
	}
}
