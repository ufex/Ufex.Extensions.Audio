using Ufex.API.Tree;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal sealed class ZoneBlockNode : ExsBlockNode
{
	private readonly ZoneBlock _zone;
	public ZoneBlockNode(ZoneBlock zone) : base(zone, TreeViewIcon.Section) => _zone = zone;

	protected override IEnumerable<BlockField> GetFields()
	{
		Int64 p = _zone.ContentPosition;
		var fields = new List<BlockField>
		{
			new("Options", _zone.Options, OptionText(_zone.Options), p),
			new("Root Note", _zone.RootNote, NoteName(_zone.RootNote), p + 1),
			new("Fine Tune", _zone.FineTune, "cents", p + 2), new("Pan", _zone.Pan, String.Empty, p + 3),
			new("Volume", _zone.Volume, String.Empty, p + 4), new("Volume Scale", _zone.VolumeScale, String.Empty, p + 5),
			new("Key Low", _zone.KeyLow, NoteName(_zone.KeyLow), p + 6), new("Key High", _zone.KeyHigh, NoteName(_zone.KeyHigh), p + 7),
			new("Velocity Low", _zone.VelocityLow, String.Empty, p + 9), new("Velocity High", _zone.VelocityHigh, String.Empty, p + 10),
			new("Sample Start", _zone.SampleStart, "frames", p + 12), new("Sample End", _zone.SampleEnd, "frames", p + 16),
			new("Loop Start", _zone.LoopStart, "frames", p + 20), new("Loop End", _zone.LoopEnd, "frames", p + 24),
			new("Loop Crossfade", _zone.LoopCrossfade, "frames", p + 28), new("Loop Tune", _zone.LoopTune, String.Empty, p + 32),
			new("Loop Options", _zone.LoopOptions, LoopText(_zone.LoopOptions), p + 33), new("Loop Mode", _zone.LoopMode, String.Empty, p + 34),
			new("Flex Pitch Offset", _zone.FlexPitchOffset, String.Empty, p + 50), new("Flex Options", _zone.FlexOptions, String.Empty, p + 77),
			new("Flex Speed", _zone.FlexSpeed, String.Empty, p + 78), new("Tail Tune", _zone.TailTune, String.Empty, p + 79),
			new("Coarse Tune", _zone.CoarseTune, "semitones", p + 80), new("Output", _zone.Output, String.Empty, p + 82),
			new("Group Index", _zone.GroupIndex, String.Empty, p + 88), new("Sample Index", _zone.SampleIndex, String.Empty, p + 92)
		};
		Add(fields, "Fade-out Frames", _zone.FadeOutFrames, p + 104);
		Add(fields, "Sync Frame Offset", _zone.SyncFrameOffset, p + 108);
		Add(fields, "Tail Sample Index", _zone.TailSampleIndex, p + 112);
		Add(fields, "Tail Start", _zone.TailStart, p + 116); Add(fields, "Tail End", _zone.TailEnd, p + 120);
		Add(fields, "Precise Volume", _zone.PreciseVolume, p + 124); Add(fields, "Tail Volume", _zone.TailVolume, p + 132);
		Add(fields, "Fade-in Frames", _zone.FadeInFrames, p + 136);
		return fields;
	}

	private static void Add<T>(List<BlockField> fields, String name, T? value, Int64 position) where T : struct
	{
		if (value.HasValue) fields.Add(new BlockField(name, value.Value, String.Empty, position));
	}

	private static String OptionText(Byte value) => String.Join(", ", new[]
	{
		(value & 0x01) != 0 ? "one-shot" : null, (value & 0x02) == 0 ? "pitch tracking" : null,
		(value & 0x04) != 0 ? "reverse" : null, (value & 0x08) != 0 ? "velocity range" : null,
		(value & 0x10) != 0 ? "muted" : null, (value & 0x40) != 0 ? "direct output" : null
	}.Where(text => text != null));

	private static String LoopText(Byte value) => $"{((value & 1) != 0 ? "enabled" : "disabled")}, {((value & 4) != 0 ? "play to end on release" : "stop on release")}";
	private static String NoteName(Byte note) => note > 127 ? "Out of MIDI range" : $"{new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" }[note % 12]}{note / 12 - 1}";
}
