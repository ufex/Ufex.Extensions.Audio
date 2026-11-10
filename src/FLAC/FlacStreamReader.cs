using System.Text;
using Microsoft.Extensions.Logging;
using Ufex.API;
using Ufex.API.Validation;
using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC;

public sealed class FlacStreamReader
{
	private readonly Stream _fileStream;

	public List<MetadataBlock> MetadataBlocks { get; } = [];
	public StreamInfoMetadataBlock? StreamInfo { get; private set; }
	public AudioData? AudioData { get; private set; }
	public Logger Log { get; }
	public ValidationReport ValidationReport { get; }

	public FlacStreamReader(Stream fileStream, Logger log, ValidationReport validationReport)
	{
		_fileStream = fileStream;
		Log = log;
		ValidationReport = validationReport;
	}

	public Boolean Read()
	{
		try
		{
			_fileStream.Position = 0;
			var fr = new FileReader(_fileStream, Endian.Big, Encoding.UTF8);
			Byte[] marker = fr.ReadBytes(Signatures.StreamMarker.Length);
			if (!marker.SequenceEqual(Signatures.StreamMarker))
			{
				ValidationReport.Error("Invalid FLAC stream marker; expected fLaC");
				return false;
			}

			Boolean isLast;
			do
			{
				if (_fileStream.Position >= _fileStream.Length)
					throw new InvalidDataException("FLAC stream ends before the final metadata block");
				MetadataBlock block = MetadataBlock.Read(fr);
				MetadataBlocks.Add(block);
				ValidateBlock(block);
				Log.LogInformation("Read {BlockType} metadata block at 0x{Offset:X} ({Length} bytes)", block.TypeName, block.StartPosition, (UInt32)block.Length);
				isLast = block.IsLast;
			} while (!isLast);

			if (MetadataBlocks[0] is not StreamInfoMetadataBlock streamInfo)
				ValidationReport.Error("The first FLAC metadata block must be STREAMINFO");
			else
				StreamInfo = streamInfo;

			ValidateBlockCounts();
			AudioData = new AudioData(_fileStream.Position, _fileStream.Length);
			if (AudioData.StartPosition == AudioData.EndPosition)
				ValidationReport.Error("The FLAC stream contains no audio frames");
			else if (StreamInfo != null && !FlacFrameReader.TryReadHeader(_fileStream, AudioData.StartPosition, StreamInfo, out _))
				ValidationReport.Error($"No valid FLAC frame header was found at audio offset 0x{AudioData.StartPosition:X}");

			ValidationReport.Info($"Parsed {MetadataBlocks.Count} metadata block(s)");
			return true;
		}
		catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException or OverflowException or DecoderFallbackException)
		{
			ValidationReport.Error(exception.Message);
			Log.LogError(exception, "Failed to parse the FLAC file");
			return false;
		}
	}

	private void ValidateBlock(MetadataBlock block)
	{
		switch (block)
		{
			case StreamInfoMetadataBlock streamInfo:
				ValidateStreamInfo(streamInfo);
				break;
			case PaddingMetadataBlock padding when padding.NonZeroByteCount != 0:
				ValidationReport.Error($"PADDING block at 0x{padding.StartPosition:X} contains {padding.NonZeroByteCount} non-zero byte(s)");
				break;
			case SeekTableMetadataBlock seekTable:
				if (!seekTable.IsSorted)
					ValidationReport.Error("SEEKTABLE sample numbers are not sorted");
				if (seekTable.HasDuplicateSampleNumbers)
					ValidationReport.Error("SEEKTABLE contains duplicate sample numbers");
				if (seekTable.HasPlaceholderBeforeRealPoint)
					ValidationReport.Error("SEEKTABLE placeholder points must occur after all real seek points");
				break;
			case VorbisCommentMetadataBlock vorbisComment:
				ValidateVorbisComment(vorbisComment);
				break;
			case CuesheetMetadataBlock cuesheet:
				ValidateCuesheet(cuesheet);
				break;
			case PictureMetadataBlock picture:
				ValidatePicture(picture);
				break;
			case ReservedMetadataBlock reserved when reserved.Type == 127:
				ValidationReport.Error("Metadata block type 127 is forbidden");
				break;
			case ReservedMetadataBlock reserved:
				ValidationReport.Info($"Preserving reserved metadata block type {reserved.Type} at 0x{reserved.StartPosition:X}");
				break;
		}
	}

	private void ValidateStreamInfo(StreamInfoMetadataBlock streamInfo)
	{
		if (streamInfo.MinimumBlockSize < 16 || streamInfo.MaximumBlockSize < 16)
			ValidationReport.Error("STREAMINFO block sizes must be at least 16 samples");
		if (streamInfo.MinimumBlockSize > streamInfo.MaximumBlockSize)
			ValidationReport.Error("STREAMINFO minimum block size is greater than its maximum block size");
		if ((UInt32)streamInfo.MinimumFrameSize != 0 && (UInt32)streamInfo.MaximumFrameSize != 0 && streamInfo.MinimumFrameSize > streamInfo.MaximumFrameSize)
			ValidationReport.Error("STREAMINFO minimum frame size is greater than its maximum frame size");
		if (streamInfo.BitsPerSample is < 4 or > 32)
			ValidationReport.Error($"STREAMINFO bit depth {streamInfo.BitsPerSample} is outside the supported 4-32 range");
	}

	private void ValidateVorbisComment(VorbisCommentMetadataBlock vorbisComment)
	{
		var strictUtf8 = new UTF8Encoding(false, true);
		strictUtf8.GetString(vorbisComment.Vendor);
		foreach (VorbisCommentEntry comment in vorbisComment.Comments)
		{
			String text = strictUtf8.GetString(comment.Data);
			Int32 separator = text.IndexOf('=');
			if (separator <= 0 || text[..separator].Any(value => value is < '\x20' or > '\x7E' || value == '='))
				ValidationReport.Warning($"Invalid Vorbis comment field at 0x{comment.StartPosition:X}");
		}
	}

	private void ValidateCuesheet(CuesheetMetadataBlock cuesheet)
	{
		if (!cuesheet.ReservedBitsAreZero)
			ValidationReport.Error("CUESHEET contains non-zero reserved bits");
		if (cuesheet.TrackCount == 0)
			ValidationReport.Error("CUESHEET must contain a lead-out track");
		if (cuesheet.IsCompactDisc && cuesheet.TrackCount > 100)
			ValidationReport.Error("A CD-DA CUESHEET cannot contain more than 100 tracks");
		if (cuesheet.IsCompactDisc && cuesheet.LeadInSamples < 88200)
			ValidationReport.Error("A CD-DA CUESHEET lead-in must be at least two seconds (88,200 samples)");
		if (cuesheet.IsCompactDisc && cuesheet.LeadInSamples % 588 != 0)
			ValidationReport.Error("A CD-DA CUESHEET lead-in must be divisible by 588 samples");

		var trackNumbers = new HashSet<Byte>();
		for (Int32 i = 0; i < cuesheet.Tracks.Count; i++)
		{
			CuesheetTrack track = cuesheet.Tracks[i];
			Boolean isLeadOut = i == cuesheet.Tracks.Count - 1;
			if (track.Number == 0 || !trackNumbers.Add(track.Number))
				ValidationReport.Error($"CUESHEET track number {track.Number} is invalid or duplicated");
			if (isLeadOut && track.Number != (cuesheet.IsCompactDisc ? 170 : 255))
				ValidationReport.Error($"CUESHEET lead-out track number must be {(cuesheet.IsCompactDisc ? 170 : 255)}");
			if (!isLeadOut && cuesheet.IsCompactDisc && track.Number is < 1 or > 99)
				ValidationReport.Error($"CD-DA CUESHEET track number {track.Number} is outside the 1-99 range");
			if (isLeadOut && track.Indices.Count != 0)
				ValidationReport.Error("CUESHEET lead-out track must not contain index points");
			if (!isLeadOut && track.Indices.Count == 0)
				ValidationReport.Error($"CUESHEET track {track.Number} must contain at least one index point");
			if (cuesheet.IsCompactDisc && track.Offset % 588 != 0)
				ValidationReport.Error($"CD-DA CUESHEET track {track.Number} offset is not divisible by 588 samples");
			if (cuesheet.IsCompactDisc && track.Indices.Count > 100)
				ValidationReport.Error($"CD-DA CUESHEET track {track.Number} contains more than 100 index points");
			for (Int32 indexPosition = 0; indexPosition < track.Indices.Count; indexPosition++)
			{
				CuesheetTrackIndex index = track.Indices[indexPosition];
				Int32 expectedNumber = indexPosition == 0 ? index.Number : track.Indices[indexPosition - 1].Number + 1;
				if ((indexPosition == 0 && index.Number is not (0 or 1)) || (indexPosition > 0 && index.Number != expectedNumber))
					ValidationReport.Error($"CUESHEET track {track.Number} index numbers are not sequential");
				if (cuesheet.IsCompactDisc && index.Offset % 588 != 0)
					ValidationReport.Error($"CD-DA CUESHEET track {track.Number} index {index.Number} offset is not divisible by 588 samples");
			}
		}
	}

	private void ValidatePicture(PictureMetadataBlock picture)
	{
		if (picture.PictureType > 20)
			ValidationReport.Warning($"PICTURE block uses reserved picture type {picture.PictureType}");
		if (picture.MediaType.Any(value => value is < 0x20 or > 0x7E))
			ValidationReport.Error("PICTURE media type contains non-printable ASCII characters");
		_ = new UTF8Encoding(false, true).GetString(picture.Description);
	}

	private void ValidateBlockCounts()
	{
		ValidateMaximumCount(0, 1, "STREAMINFO");
		ValidateMaximumCount(3, 1, "SEEKTABLE");
		ValidateMaximumCount(4, 1, "VORBIS_COMMENT");
		ValidateMaximumCount(5, 1, "CUESHEET");
		UInt32[] uniquePictureTypes = [1, 2];
		foreach (UInt32 pictureType in uniquePictureTypes)
		{
			Int32 count = MetadataBlocks.OfType<PictureMetadataBlock>().Count(block => block.PictureType == pictureType);
			if (count > 1)
				ValidationReport.Error($"Only one PICTURE block of type {pictureType} is permitted");
		}
	}

	private void ValidateMaximumCount(Byte type, Int32 maximum, String name)
	{
		Int32 count = MetadataBlocks.Count(block => block.Type == type);
		if (count > maximum)
			ValidationReport.Error($"FLAC permits at most {maximum} {name} metadata block(s), but {count} were found");
	}
}
