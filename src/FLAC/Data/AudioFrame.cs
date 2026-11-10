namespace Ufex.Extensions.Audio.FLAC.Data;

public sealed class AudioFrame : Section
{
	public Int32 Index { get; internal set; }
	public Boolean IsVariableBlockSize { get; init; }
	public Byte BlockSizeCode { get; init; }
	public Byte SampleRateCode { get; init; }
	public Byte ChannelAssignment { get; init; }
	public Byte BitDepthCode { get; init; }
	public UInt64 CodedNumber { get; init; }
	public UInt32 BlockSize { get; init; }
	public UInt32 SampleRate { get; init; }
	public Byte BitsPerSample { get; init; }
	public Byte ChannelCount { get; init; }
	public Byte HeaderCrc { get; init; }
	public Int32 HeaderLength { get; init; }
	public Boolean FrameCrcValid { get; set; }
	public UInt64 FrameOrSampleNumber => CodedNumber;
	public UInt64 FirstSampleNumber { get; init; }
	public String ChannelAssignmentText => Constants.ChannelAssignmentName(ChannelAssignment);

	public AudioFrame(Int64 startPosition)
	{
		StartPosition = startPosition;
	}

	public void Complete(Int64 endPosition, Boolean frameCrcValid)
	{
		EndPosition = endPosition;
		FrameCrcValid = frameCrcValid;
	}
}
