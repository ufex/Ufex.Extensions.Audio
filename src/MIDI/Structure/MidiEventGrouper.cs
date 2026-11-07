using Ufex.Extensions.Audio.MIDI.Data;

namespace Ufex.Extensions.Audio.MIDI.Structure;

internal static class MidiEventGrouper
{
	private const Int32 TargetEventCount = 100;
	private const UInt64 BoundaryDivisionsPerAverageGroup = 4;

	public static IReadOnlyList<MidiEventGroup> Group(IReadOnlyList<MidiEvent> events)
	{
		if (events.Count == 0)
			return [];

		MidiEvent[] orderedEvents = events
			.OrderBy(midiEvent => midiEvent.AbsoluteTime)
			.ToArray();
		var groups = new List<MidiEventGroup>();

		MidiEvent[] zeroEvents = orderedEvents
			.TakeWhile(midiEvent => midiEvent.AbsoluteTime == 0)
			.ToArray();
		if (zeroEvents.Length > 0)
			groups.Add(new MidiEventGroup(0, 0, zeroEvents));

		MidiEvent[] nonzeroEvents = orderedEvents[zeroEvents.Length..];
		if (nonzeroEvents.Length == 0)
			return groups;

		UInt64 maximumTick = nonzeroEvents[^1].AbsoluteTime;
		Int32 desiredGroupCount = DivideRoundUp(nonzeroEvents.Length, TargetEventCount);
		UInt64 averageTickRange = DivideRoundUp(maximumTick, (UInt64)desiredGroupCount);
		UInt64 rawQuantum = Math.Max(1, averageTickRange / BoundaryDivisionsPerAverageGroup);
		UInt64 boundaryQuantum = NiceQuantum(rawQuantum);

		Int32 startIndex = 0;
		UInt64 rangeStart = 1;
		while (startIndex < nonzeroEvents.Length)
		{
			Int32 remainingEventCount = nonzeroEvents.Length - startIndex;
			Int32 remainingGroupCount = DivideRoundUp(remainingEventCount, TargetEventCount);
			if (remainingGroupCount == 1)
			{
				UInt64 rangeEnd = RoundUp(maximumTick, boundaryQuantum);
				groups.Add(CreateGroup(nonzeroEvents, startIndex, nonzeroEvents.Length, rangeStart, rangeEnd));
				break;
			}

			Int32 idealEventCount = DivideRoundUp(remainingEventCount, remainingGroupCount);
			(Boundary boundary, Int32 endIndex) = FindBoundary(
				nonzeroEvents,
				startIndex,
				idealEventCount,
				rangeStart,
				maximumTick,
				boundaryQuantum);

			groups.Add(CreateGroup(nonzeroEvents, startIndex, endIndex, rangeStart, boundary.Tick));
			startIndex = endIndex;
			if (startIndex < nonzeroEvents.Length)
				rangeStart = boundary.Tick + 1;
		}

		return groups;
	}

	private static (Boundary Boundary, Int32 EndIndex) FindBoundary(
		MidiEvent[] events,
		Int32 startIndex,
		Int32 idealEventCount,
		UInt64 rangeStart,
		UInt64 maximumTick,
		UInt64 initialQuantum)
	{
		Int32 targetIndex = Math.Min(startIndex + idealEventCount - 1, events.Length - 1);
		UInt64 targetTick = events[targetIndex].AbsoluteTime;
		UInt64 quantum = initialQuantum;

		while (true)
		{
			Boundary? bestBoundary = null;
			Int32 bestEndIndex = 0;
			UInt64 beforeTarget = targetTick == 0 ? 0 : RoundDown(targetTick - 1, quantum);
			UInt64 atOrAfterTarget = RoundUp(targetTick, quantum);

			EvaluateBoundary(beforeTarget);
			if (atOrAfterTarget != beforeTarget)
				EvaluateBoundary(atOrAfterTarget);

			if (bestBoundary != null)
				return (bestBoundary.Value, bestEndIndex);
			if (quantum == 1)
			{
				Int32 endIndex = UpperBound(events, startIndex, targetTick);
				return (new Boundary(targetTick, endIndex - startIndex, idealEventCount), endIndex);
			}

			quantum = NextSmallerNiceQuantum(quantum);

			void EvaluateBoundary(UInt64 tick)
			{
				if (tick < rangeStart || tick >= maximumTick)
					return;

				Int32 endIndex = UpperBound(events, startIndex, tick);
				if (endIndex <= startIndex || endIndex >= events.Length)
					return;

				Int32 eventCount = endIndex - startIndex;
				var candidate = new Boundary(tick, eventCount, idealEventCount);
				if (bestBoundary == null || candidate.IsBetterThan(bestBoundary.Value))
				{
					bestBoundary = candidate;
					bestEndIndex = endIndex;
				}
			}
		}
	}

	private static MidiEventGroup CreateGroup(
		MidiEvent[] events,
		Int32 startIndex,
		Int32 endIndex,
		UInt64 rangeStart,
		UInt64 rangeEnd)
	{
		var groupEvents = new MidiEvent[endIndex - startIndex];
		Array.Copy(events, startIndex, groupEvents, 0, groupEvents.Length);
		return new MidiEventGroup(rangeStart, rangeEnd, groupEvents);
	}

	private static Int32 UpperBound(MidiEvent[] events, Int32 startIndex, UInt64 tick)
	{
		Int32 low = startIndex;
		Int32 high = events.Length;
		while (low < high)
		{
			Int32 middle = low + (high - low) / 2;
			if (events[middle].AbsoluteTime <= tick)
				low = middle + 1;
			else
				high = middle;
		}
		return low;
	}

	private static UInt64 NiceQuantum(UInt64 value)
	{
		UInt64 magnitude = 1;
		while (magnitude <= value / 10)
			magnitude *= 10;

		Decimal scaledValue = (Decimal)value / magnitude;
		UInt64 multiplier = scaledValue < 1.5m
			? 1UL
			: scaledValue < 3.5m
				? 2UL
				: scaledValue < 7.5m
					? 5UL
					: 10UL;

		return multiplier > UInt64.MaxValue / magnitude
			? UInt64.MaxValue
			: multiplier * magnitude;
	}

	private static UInt64 NextSmallerNiceQuantum(UInt64 quantum)
	{
		UInt64 magnitude = 1;
		while (magnitude <= quantum / 10)
			magnitude *= 10;
		UInt64 multiplier = quantum / magnitude;

		if (multiplier >= 5)
			return 2 * magnitude;
		if (multiplier >= 2)
			return magnitude;
		return magnitude == 1 ? 1 : 5 * (magnitude / 10);
	}

	private static UInt64 RoundDown(UInt64 value, UInt64 quantum)
	{
		return value - value % quantum;
	}

	private static UInt64 RoundUp(UInt64 value, UInt64 quantum)
	{
		UInt64 remainder = value % quantum;
		if (remainder == 0)
			return value;

		UInt64 increase = quantum - remainder;
		return value > UInt64.MaxValue - increase ? UInt64.MaxValue : value + increase;
	}

	private static Int32 DivideRoundUp(Int32 value, Int32 divisor)
	{
		return value / divisor + (value % divisor == 0 ? 0 : 1);
	}

	private static UInt64 DivideRoundUp(UInt64 value, UInt64 divisor)
	{
		return value / divisor + (value % divisor == 0 ? 0UL : 1UL);
	}

	private readonly record struct Boundary(UInt64 Tick, Int32 EventCount, Int32 IdealEventCount)
	{
		private Int32 Distance => Math.Abs(EventCount - IdealEventCount);

		public Boolean IsBetterThan(Boundary other)
		{
			if (Distance != other.Distance)
				return Distance < other.Distance;
			if (EventCount != other.EventCount)
				return EventCount < other.EventCount;
			return Tick < other.Tick;
		}
	}
}
