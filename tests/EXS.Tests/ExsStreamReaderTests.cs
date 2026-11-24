using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Tests;

[TestClass]
public sealed class ExsStreamReaderTests
{
	[TestMethod]
	public void Read_ValidInstrument_ParsesCoreAndAuxiliaryBlocks()
	{
		using var stream = new MemoryStream(BuildInstrument());
		var report = new ValidationReport();
		var reader = new ExsStreamReader(stream, new Logger(), report);

		Boolean result = reader.Read();

		Assert.IsTrue(result);
		Assert.IsFalse(report.HasErrors, String.Join(Environment.NewLine, report.GetInfo()));
		Assert.HasCount(6, reader.Blocks);
		Assert.AreEqual("Test Instrument", reader.Instrument!.Name);
		Assert.AreEqual((UInt32)1, reader.Instrument.ZoneCount);
		ZoneBlock zone = reader.Blocks.OfType<ZoneBlock>().Single();
		Assert.AreEqual((Byte)60, zone.RootNote);
		Assert.AreEqual((UInt32)0, zone.SampleIndex);
		GroupBlock group = reader.Blocks.OfType<GroupBlock>().Single();
		Assert.AreEqual((Byte)32, group.Polyphony);
		SampleBlock sample = reader.Blocks.OfType<SampleBlock>().Single();
		Assert.AreEqual("Kick.wav", sample.FileName);
		Assert.AreEqual((UInt32)44100, sample.SampleRate);
		ParametersBlock parameters = reader.Blocks.OfType<ParametersBlock>().Single();
		Assert.HasCount(2, parameters.Parameters);
		Assert.AreEqual((Int16)18, parameters.Parameters[1].Id);
		Assert.AreEqual((Int16)100, parameters.Parameters[1].Value);
		Assert.IsInstanceOfType<ExsBlock>(reader.Blocks.Last());
		Assert.AreEqual(ExsBlockType.EditorState, reader.Blocks.Last().Type);
	}

	[TestMethod]
	public void Read_TruncatedBlock_ReturnsFalseAndReportsError()
	{
		Byte[] data = BuildInstrument();
		using var stream = new MemoryStream(data[..^1]);
		var report = new ValidationReport();
		var reader = new ExsStreamReader(stream, new Logger(), report);

		Boolean result = reader.Read();

		Assert.IsFalse(result);
		Assert.IsTrue(report.HasErrors);
	}

	[TestMethod]
	[DataRow(false, true)]
	[DataRow(true, false)]
	public void Read_HeaderVariants_HandlesMaskedSizeAndBigEndian(Boolean bigEndian, Boolean maskedSize)
	{
		using var stream = new MemoryStream(BuildSingleInstrument(bigEndian, maskedSize));
		var report = new ValidationReport();
		var reader = new ExsStreamReader(stream, new Logger(), report);

		Boolean result = reader.Read();

		Assert.IsTrue(result);
		Assert.IsFalse(report.HasErrors, String.Join(Environment.NewLine, report.GetInfo()));
		Assert.AreEqual(bigEndian ? Endian.Big : Endian.Little, reader.Instrument!.Header.Endian);
		Assert.AreEqual(maskedSize, reader.Instrument.Header.UsesMaskedSize);
		Assert.AreEqual((UInt32)40, reader.Instrument.ContentSize);
	}

	[TestMethod]
	public void ProcessFile_ValidInstrument_PopulatesUfexViews()
	{
		String path = Path.Combine(Path.GetTempPath(), $"ufex-exs-{Guid.NewGuid():N}.exs");
		try
		{
			File.WriteAllBytes(path, BuildInstrument());
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			var fileType = new ExsFileType { FileInStream = stream, FilePath = path, Logger = new Logger() };

			Boolean result = fileType.ProcessFile();

			Assert.IsTrue(result);
			Assert.IsFalse(fileType.ValidationReport.HasErrors, String.Join(Environment.NewLine, fileType.ValidationReport.GetInfo()));
			Assert.HasCount(1, fileType.Visuals);
			Assert.AreEqual(6, fileType.TreeNodes.Count);
			Assert.IsTrue(fileType.EnableStructure);
			Assert.IsTrue(fileType.EnableVisual);
			Assert.IsTrue(fileType.EnableValidation);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void Read_SuppliedCorpus_ParsesEveryInstrument()
	{
		String? corpusDirectory = FindCorpusDirectory();
		if (corpusDirectory == null)
		{
			Assert.Inconclusive("The optional files/audio-video/exs corpus is not available");
			return;
		}

		String[] paths = Directory.GetFiles(corpusDirectory, "*.exs", SearchOption.AllDirectories);
		Assert.IsNotEmpty(paths, $"No EXS files were found in {corpusDirectory}");
		var failures = new List<String>();
		foreach (String path in paths)
		{
			using var stream = File.OpenRead(path);
			var report = new ValidationReport();
			var reader = new ExsStreamReader(stream, new Logger(), report);
			if (!reader.Read() || report.HasErrors)
				failures.Add($"{Path.GetFileName(path)}: {String.Join("; ", report.Entries.Select(entry => entry.Message))}");
		}

		Assert.IsEmpty(failures, String.Join(Environment.NewLine, failures));
	}

	private static Byte[] BuildInstrument()
	{
		var bytes = new List<Byte>();
		Byte[] instrument = new Byte[48];
		WriteUInt32(instrument, 4, 1); WriteUInt32(instrument, 8, 1); WriteUInt32(instrument, 12, 1); WriteUInt32(instrument, 16, 1);
		AddBlock(bytes, ExsBlockType.Instrument, 0, "Test Instrument", instrument);

		Byte[] zone = new Byte[140];
		zone[1] = 60; zone[6] = 0; zone[7] = 127; zone[9] = 1; zone[10] = 127;
		WriteUInt32(zone, 16, 22050); WriteInt32(zone, 88, 0); WriteUInt32(zone, 92, 0);
		AddBlock(bytes, ExsBlockType.Zone, 0, "Kick Zone", zone);

		Byte[] group = new Byte[132];
		group[2] = 32; group[5] = 1; group[6] = 127; group[32] = 128; group[35] = 128;
		group[38] = 80; group[39] = 140; group[89] = 127;
		AddBlock(bytes, ExsBlockType.Group, 0, "Main Group", group);

		Byte[] sample = new Byte[592];
		WriteUInt32(sample, 4, 22050); WriteUInt32(sample, 8, 44100); WriteUInt32(sample, 12, 24);
		WriteUInt32(sample, 16, 1); WriteUInt32(sample, 20, 1);
		Encoding.ASCII.GetBytes("EVAW").CopyTo(sample, 28); WriteUInt32(sample, 32, 66150);
		Encoding.Latin1.GetBytes("Samples").CopyTo(sample, 80); Encoding.Latin1.GetBytes("Kick.wav").CopyTo(sample, 336);
		AddBlock(bytes, ExsBlockType.Sample, 0, "Kick", sample);

		Byte[] parameters = new Byte[15];
		WriteUInt32(parameters, 0, 1); parameters[4] = 1; WriteInt16(parameters, 5, -5);
		WriteUInt32(parameters, 7, 1); WriteInt16(parameters, 11, 18); WriteInt16(parameters, 13, 100);
		AddBlock(bytes, ExsBlockType.Parameters, 0, "Parameters", parameters);
		AddBlock(bytes, ExsBlockType.EditorState, 0, "Editor State", [1, 2, 3, 4, 5]);
		return bytes.ToArray();
	}

	private static Byte[] BuildSingleInstrument(Boolean bigEndian, Boolean maskedSize)
	{
		Byte[] content = new Byte[40];
		Byte[] header = new Byte[84];
		header[0] = bigEndian ? (Byte)0 : (Byte)1; header[1] = 1;
		header[3] = maskedSize ? (Byte)0x80 : (Byte)0;
		UInt32 declaredSize = maskedSize ? (UInt32)content.Length | 0x8000U : (UInt32)content.Length;
		if (bigEndian)
			BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), declaredSize);
		else
			WriteUInt32(header, 4, declaredSize);
		Encoding.ASCII.GetBytes(bigEndian ? "SOBT" : "TBOS").CopyTo(header, 16);
		Encoding.Latin1.GetBytes("Variant").CopyTo(header, 20);
		return [.. header, .. content];
	}

	private static void AddBlock(List<Byte> output, ExsBlockType type, UInt32 index, String name, Byte[] content)
	{
		Byte[] header = new Byte[84];
		header[0] = 1; header[1] = 1; header[2] = 0; header[3] = (Byte)((Byte)type | 0x40);
		WriteUInt32(header, 4, (UInt32)content.Length); WriteUInt32(header, 8, index);
		Encoding.ASCII.GetBytes("TBOS").CopyTo(header, 16);
		Byte[] nameBytes = Encoding.Latin1.GetBytes(name); Array.Copy(nameBytes, 0, header, 20, Math.Min(64, nameBytes.Length));
		output.AddRange(header); output.AddRange(content);
	}

	private static void WriteInt16(Byte[] data, Int32 offset, Int16 value) => BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), value);
	private static void WriteInt32(Byte[] data, Int32 offset, Int32 value) => BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), value);
	private static void WriteUInt32(Byte[] data, Int32 offset, UInt32 value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);

	private static String? FindCorpusDirectory()
	{
		DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null)
		{
			String candidate = Path.Combine(directory.FullName, "files", "audio-video", "exs");
			if (Directory.Exists(candidate)) return candidate;
			directory = directory.Parent;
		}
		return null;
	}
}
