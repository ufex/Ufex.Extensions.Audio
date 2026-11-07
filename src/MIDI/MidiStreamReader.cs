using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI;

public class MidiStreamReader
{
	private readonly Stream _fileStream;

	public MidiHeader? Header { get; private set; }
	public List<MidiTrack> Tracks { get; }
	public Logger Log { get; set; }
	public ValidationReport ValidationReport { get; set; }

	public MidiStreamReader(Stream fileStream, Logger log, ValidationReport validationReport)
	{
		_fileStream = fileStream;
		Tracks = [];
		Log = log;
		ValidationReport = validationReport;
	}

	public Boolean Read()
	{
		try
		{
			_fileStream.Position = 0;
			var fr = new FileReader(_fileStream, Endian.Big, Encoding.Latin1);
			Header = new MidiHeader(fr);

			if (!Header.ChunkId.SequenceEqual(Signatures.HeaderChunk))
			{
				ValidationReport.Error("Invalid MIDI header chunk signature");
				return false;
			}

			ValidateHeader(Header);
			Log.LogInformation("Read MThd: format={Format}, tracks={Tracks}, division={Division}", Header.Format, Header.TrackCount, Header.DivisionText);

			while (_fileStream.Position < _fileStream.Length)
			{
				if (_fileStream.Length - _fileStream.Position < 8)
				{
					ValidationReport.Warning($"Ignoring {_fileStream.Length - _fileStream.Position} trailing byte(s) after the final chunk");
					break;
				}

				Int64 chunkStart = _fileStream.Position;
				Byte[] chunkId = fr.ReadBytes(4);
				UInt32 chunkLength = fr.ReadUInt32();
				Int64 chunkEnd = checked(_fileStream.Position + chunkLength);
				if (chunkEnd > _fileStream.Length)
					throw new InvalidDataException($"Chunk {ChunkIdText(chunkId)} at 0x{chunkStart:X} extends beyond the end of the file");

				if (chunkId.SequenceEqual(Signatures.TrackChunk))
				{
					ReadTrack(fr, chunkId, chunkLength, chunkStart, chunkEnd);
				}
				else
				{
					ValidationReport.Warning($"Skipping unknown chunk {ChunkIdText(chunkId)} at offset 0x{chunkStart:X}");
					_fileStream.Position = chunkEnd;
				}
			}

			if (Tracks.Count != Header.TrackCount)
				ValidationReport.Warning($"Header declares {Header.TrackCount} track(s), but {Tracks.Count} MTrk chunk(s) were found");

			ValidationReport.Info($"Parsed {Tracks.Count} track(s) containing {Tracks.Sum(t => t.Events.Count)} event(s)");
			return true;
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException or OverflowException)
		{
			ValidationReport.Error(exception.Message);
			Log.LogError(exception, "Failed to parse the MIDI file");
			return false;
		}
	}

	private void ValidateHeader(MidiHeader header)
	{
		if (header.Format > 2)
			ValidationReport.Error($"Unsupported MIDI file format {header.Format}");
		if (header.Format == 0 && header.TrackCount != 1)
			ValidationReport.Warning($"Format 0 files should contain exactly one track, but the header declares {header.TrackCount}");
		if (!header.UsesSmpteTimeCode && header.TicksPerQuarterNote == 0)
			ValidationReport.Error("Ticks per quarter note cannot be zero");
		if (header.UsesSmpteTimeCode && header.SmpteFramesPerSecond is not (24 or 25 or 29 or 30))
			ValidationReport.Warning($"Unrecognized SMPTE frame rate {header.SmpteFramesPerSecond}");
		if (header.ExtraData.Length > 0)
			ValidationReport.Info($"MThd contains {header.ExtraData.Length} additional header byte(s)");
	}

	private void ReadTrack(FileReader fr, Byte[] chunkId, UInt32 chunkLength, Int64 chunkStart, Int64 chunkEnd)
	{
		var track = new MidiTrack(chunkId, chunkLength, Tracks.Count, chunkStart);
		Tracks.Add(track);
		UInt64 absoluteTime = 0;
		Byte? runningStatus = null;
		Boolean foundEndOfTrack = false;

		while (_fileStream.Position < chunkEnd)
		{
			Int64 eventStart = _fileStream.Position;
			(UInt32 deltaTime, Byte[] deltaTimeBytes) = ReadVariableLength(fr, chunkEnd);
			absoluteTime = checked(absoluteTime + deltaTime);
			EnsureAvailable(chunkEnd, 1, "event status");
			Byte firstByte = fr.ReadByte();
			Boolean statusPresent = firstByte >= 0x80;
			Byte status;
			Byte? firstDataByte = null;

			if (statusPresent)
			{
				status = firstByte;
			}
			else
			{
				if (runningStatus == null)
					throw new InvalidDataException($"Running status used before a channel status at offset 0x{eventStart:X}");
				status = runningStatus.Value;
				firstDataByte = firstByte;
			}

			Byte? metaType = null;
			Byte[] lengthBytes = [];
			Byte[] data;

			if (status is >= 0x80 and <= 0xEF)
			{
				runningStatus = status;
				Int32 dataLength = (status & 0xF0) is 0xC0 or 0xD0 ? 1 : 2;
				data = ReadChannelData(fr, chunkEnd, dataLength, firstDataByte, eventStart);
			}
			else if (status == 0xFF)
			{
				runningStatus = null;
				EnsureAvailable(chunkEnd, 1, "meta event type");
				metaType = fr.ReadByte();
				(UInt32 dataLength, Byte[] encodedLength) = ReadVariableLength(fr, chunkEnd);
				lengthBytes = encodedLength;
				data = ReadEventData(fr, chunkEnd, dataLength);
			}
			else if (status is 0xF0 or 0xF7)
			{
				runningStatus = null;
				(UInt32 dataLength, Byte[] encodedLength) = ReadVariableLength(fr, chunkEnd);
				lengthBytes = encodedLength;
				data = ReadEventData(fr, chunkEnd, dataLength);
			}
			else
			{
				runningStatus = status >= 0xF8 ? runningStatus : null;
				Int32 dataLength = SystemMessageDataLength(status);
				data = ReadEventData(fr, chunkEnd, (UInt32)dataLength);
			}

			var midiEvent = new MidiEvent(
				eventStart,
				_fileStream.Position,
				deltaTimeBytes,
				deltaTime,
				absoluteTime,
				status,
				statusPresent,
				metaType,
				lengthBytes,
				data);
			track.Events.Add(midiEvent);

			if (metaType == 0x2F)
			{
				foundEndOfTrack = true;
				if (data.Length != 0)
					ValidationReport.Warning($"End of Track event in track {track.Index + 1} has a non-zero length");
				if (_fileStream.Position < chunkEnd)
				{
					ValidationReport.Warning($"Track {track.Index + 1} has {chunkEnd - _fileStream.Position} byte(s) after its End of Track event");
					_fileStream.Position = chunkEnd;
				}
				break;
			}
		}

		if (!foundEndOfTrack)
			ValidationReport.Warning($"Track {track.Index + 1} does not contain an End of Track meta event");
		_fileStream.Position = chunkEnd;
		Log.LogInformation("Read MTrk {Track}: length={Length}, events={Events}", track.Index + 1, track.Length, track.Events.Count);
	}

	private Byte[] ReadChannelData(FileReader fr, Int64 chunkEnd, Int32 dataLength, Byte? firstDataByte, Int64 eventStart)
	{
		var data = new Byte[dataLength];
		Int32 index = 0;
		if (firstDataByte != null)
			data[index++] = firstDataByte.Value;

		EnsureAvailable(chunkEnd, dataLength - index, "channel event data");
		while (index < dataLength)
			data[index++] = fr.ReadByte();

		if (data.Any(value => value >= 0x80))
			throw new InvalidDataException($"Channel event at offset 0x{eventStart:X} contains a data byte with its high bit set");
		return data;
	}

	private Byte[] ReadEventData(FileReader fr, Int64 chunkEnd, UInt32 length)
	{
		if (length > Int32.MaxValue)
			throw new InvalidDataException($"Event data length {length} is too large");
		EnsureAvailable(chunkEnd, (Int32)length, "event data");
		Byte[] data = fr.ReadBytes((Int32)length);
		if (data.Length != length)
			throw new EndOfStreamException("Event data is truncated");
		return data;
	}

	private (UInt32 Value, Byte[] Bytes) ReadVariableLength(FileReader fr, Int64 chunkEnd)
	{
		var bytes = new List<Byte>(4);
		UInt32 value = 0;
		for (Int32 i = 0; i < 4; i++)
		{
			EnsureAvailable(chunkEnd, 1, "variable-length quantity");
			Byte current = fr.ReadByte();
			bytes.Add(current);
			value = (value << 7) | (UInt32)(current & 0x7F);
			if ((current & 0x80) == 0)
				return (value, bytes.ToArray());
		}

		throw new InvalidDataException("MIDI variable-length quantity exceeds four bytes");
	}

	private void EnsureAvailable(Int64 chunkEnd, Int32 byteCount, String fieldName)
	{
		if (byteCount < 0 || chunkEnd - _fileStream.Position < byteCount)
			throw new EndOfStreamException($"Track ends while reading {fieldName} at offset 0x{_fileStream.Position:X}");
	}

	private static Int32 SystemMessageDataLength(Byte status)
	{
		return status switch
		{
			0xF1 or 0xF3 => 1,
			0xF2 => 2,
			0xF4 or 0xF5 or 0xF6 or 0xF8 or 0xF9 or 0xFA or 0xFB or 0xFC or 0xFD or 0xFE => 0,
			_ => throw new InvalidDataException($"Unsupported MIDI status byte 0x{status:X2}")
		};
	}

	private static String ChunkIdText(Byte[] chunkId)
	{
		return Encoding.ASCII.GetString(chunkId.Select(value => value is >= 0x20 and <= 0x7E ? value : (Byte)'?').ToArray());
	}
}
