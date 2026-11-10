using Ufex.Extensions.Audio.FLAC.Data;

namespace Ufex.Extensions.Audio.FLAC.Structure;

internal static class AudioFrameGrouper
{
	internal const Int32 DirectFrameLimit = 100;
	private const Int32 MaximumOverviewGroupCount = 200;

	public static IReadOnlyList<AudioFrameGroup> GroupOverview(IReadOnlyList<AudioFrame> frames, UInt32 sampleRate)
	{
		Int32 targetFrameCount = Math.Max(DirectFrameLimit, DivideRoundUp(frames.Count, MaximumOverviewGroupCount));
		return Group(frames, sampleRate, targetFrameCount);
	}

	public static IReadOnlyList<AudioFrameGroup> Group(IReadOnlyList<AudioFrame> frames, UInt32 sampleRate, Int32 targetFrameCount = DirectFrameLimit)
	{
		if (frames.Count == 0)
			return [];
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetFrameCount);
		if (sampleRate == 0)
			return GroupByCount(frames, targetFrameCount);

		UInt64 firstSample = frames.Min(frame => frame.FirstSampleNumber);
		UInt64 totalSamples = frames.Max(frame => checked(frame.FirstSampleNumber + frame.BlockSize));
		UInt64 startMilliseconds = SamplesToMilliseconds(firstSample, sampleRate);
		UInt64 endMilliseconds = SamplesToMilliseconds(totalSamples, sampleRate);
		UInt64 totalDuration = endMilliseconds - startMilliseconds;
		Int32 desiredGroupCount = DivideRoundUp(frames.Count, targetFrameCount);
		UInt64 rawDuration = Math.Max(1, DivideRoundUp(totalDuration, (UInt64)desiredGroupCount));
		UInt64 groupDuration = NiceDuration(rawDuration);
		var groups = new List<AudioFrameGroup>();
		var groupFrames = new List<AudioFrame>();
		UInt64 currentBucket = UInt64.MaxValue;

		foreach (AudioFrame frame in frames)
		{
			UInt64 frameMilliseconds = SamplesToMilliseconds(frame.FirstSampleNumber, sampleRate);
			UInt64 bucket = (frameMilliseconds - startMilliseconds) / groupDuration;
			if (groupFrames.Count > 0 && bucket != currentBucket)
			{
				groups.Add(CreateGroup(currentBucket, groupDuration, startMilliseconds, endMilliseconds, groupFrames));
				groupFrames = [];
			}
			currentBucket = bucket;
			groupFrames.Add(frame);
		}

		if (groupFrames.Count > 0)
			groups.Add(CreateGroup(currentBucket, groupDuration, startMilliseconds, endMilliseconds, groupFrames));
		if (groups.Count == 1 && frames.Count > targetFrameCount)
			return GroupByCount(frames, targetFrameCount);
		return groups;
	}

	private static AudioFrameGroup CreateGroup(UInt64 bucket, UInt64 duration, UInt64 startMilliseconds, UInt64 endMilliseconds, List<AudioFrame> frames)
	{
		UInt64 start = checked(startMilliseconds + bucket * duration);
		UInt64 end = Math.Min(endMilliseconds, checked(start + duration));
		return new AudioFrameGroup(start, end, frames.ToArray());
	}

	private static IReadOnlyList<AudioFrameGroup> GroupByCount(IReadOnlyList<AudioFrame> frames, Int32 targetFrameCount)
	{
		var groups = new List<AudioFrameGroup>();
		for (Int32 start = 0; start < frames.Count; start += targetFrameCount)
		{
			Int32 count = Math.Min(targetFrameCount, frames.Count - start);
			AudioFrame[] groupFrames = frames.Skip(start).Take(count).ToArray();
			String frameLabel = count == 1 ? "frame" : "frames";
			String text = $"Frames {start:N0} - {start + count - 1:N0} ({count:N0} {frameLabel})";
			groups.Add(new AudioFrameGroup(0, 0, groupFrames, text));
		}
		return groups;
	}

	private static UInt64 SamplesToMilliseconds(UInt64 samples, UInt32 sampleRate)
	{
		return checked(samples * 1000) / sampleRate;
	}

	private static UInt64 NiceDuration(UInt64 milliseconds)
	{
		UInt64 magnitude = 1;
		while (magnitude <= milliseconds / 10)
			magnitude *= 10;

		UInt64 scaled = DivideRoundUp(milliseconds, magnitude);
		UInt64 multiplier = scaled <= 1 ? 1UL : scaled <= 2 ? 2UL : scaled <= 5 ? 5UL : 10UL;
		return checked(multiplier * magnitude);
	}

	private static Int32 DivideRoundUp(Int32 value, Int32 divisor)
	{
		return value / divisor + (value % divisor == 0 ? 0 : 1);
	}

	private static UInt64 DivideRoundUp(UInt64 value, UInt64 divisor)
	{
		return value / divisor + (value % divisor == 0 ? 0UL : 1UL);
	}
}
