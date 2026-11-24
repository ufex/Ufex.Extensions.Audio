using Ufex.API.Tree;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal sealed class GroupBlockNode : ExsBlockNode
{
	private readonly GroupBlock _group;
	public GroupBlockNode(GroupBlock group) : base(group, TreeViewIcon.List) => _group = group;

	protected override IEnumerable<BlockField> GetFields()
	{
		Int64 p = _group.ContentPosition;
		var fields = new List<BlockField>
		{
			new("Volume", _group.Volume, String.Empty, p), new("Pan", _group.Pan, String.Empty, p + 1),
			new("Polyphony", _group.Polyphony, "voices", p + 2), new("Options", _group.Options, OptionText(_group.Options), p + 3),
			new("Exclusive Class", _group.ExclusiveClass, String.Empty, p + 4),
			new("Velocity Low", _group.VelocityLow, String.Empty, p + 5), new("Velocity High", _group.VelocityHigh, String.Empty, p + 6),
			new("Random Offset", _group.RandomOffset, String.Empty, p + 7), new("Unknown 0x08", _group.UnknownByte08, String.Empty, p + 8),
			new("Release-trigger Decay", _group.ReleaseTriggerDecayTime, String.Empty, p + 16),
			new("Velocity Crossfade", _group.VelocityCrossfade, String.Empty, p + 32), new("Velocity Crossfade Type", _group.VelocityCrossfadeType, String.Empty, p + 33),
			new("Note Crossfade Type", _group.NoteCrossfadeType, String.Empty, p + 34), new("Note Crossfade", _group.NoteCrossfade, String.Empty, p + 35),
			new("Live Pre-anchor Time", _group.LivePreAnchorTime, "ms", p + 36), new("Dynamic Range Offset", _group.DynamicRangeOffset, String.Empty, p + 37),
			new("Tempo Low", _group.TempoLow, "BPM", p + 38), new("Tempo High", _group.TempoHigh, "BPM", p + 39),
			new("Filter Cutoff Offset", _group.FilterCutoffOffset, String.Empty, p + 41), new("Filter Resonance Offset", _group.FilterResonanceOffset, String.Empty, p + 43),
			new("Envelope 1 Attack", _group.Envelope1Attack, String.Empty, p + 56), new("Envelope 1 Decay", _group.Envelope1Decay, String.Empty, p + 60),
			new("Envelope 1 Sustain", _group.Envelope1Sustain, String.Empty, p + 64), new("Envelope 1 Release", _group.Envelope1Release, String.Empty, p + 68),
			new("Release Trigger", _group.ReleaseTrigger, String.Empty, p + 73), new("Output", _group.Output, String.Empty, p + 74),
			new("Enable-by Note", _group.EnableByNote, String.Empty, p + 75), new("Group Cycle", _group.GroupCycle, String.Empty, p + 80),
			new("Enable-by Type", _group.EnableByType, Constants.EnableByName(_group.EnableByType), p + 84),
			new("Controller Number", _group.ControllerNumber, String.Empty, p + 85), new("Controller Low", _group.ControllerLow, String.Empty, p + 86),
			new("Controller High", _group.ControllerHigh, String.Empty, p + 87), new("Key Low", _group.KeyLow, String.Empty, p + 88),
			new("Key High", _group.KeyHigh, String.Empty, p + 89), new("MIDI Channel", _group.MidiChannelZeroBased, $"Channel {_group.MidiChannelZeroBased + 1}", p + 90),
			new("Articulation", _group.Articulation, String.Empty, p + 91)
		};
		Add(fields, "Enable-by Type 2", _group.EnableByType2, p + 92);
		Add(fields, "Enable-by Type 3", _group.EnableByType3, p + 93);
		Add(fields, "Pitch Low", _group.PitchLow, p + 94); Add(fields, "Pitch High", _group.PitchHigh, p + 95);
		Add(fields, "Envelope 1 Hold", _group.Envelope1Hold, p + 96); Add(fields, "Envelope 2 Attack", _group.Envelope2Attack, p + 100);
		Add(fields, "Envelope 2 Decay", _group.Envelope2Decay, p + 104); Add(fields, "Envelope 2 Sustain", _group.Envelope2Sustain, p + 108);
		Add(fields, "Envelope 2 Release", _group.Envelope2Release, p + 112); Add(fields, "Envelope 2 Hold", _group.Envelope2Hold, p + 116);
		Add(fields, "Envelope 1 Delay", _group.Envelope1Delay, p + 124); Add(fields, "Envelope 2 Delay", _group.Envelope2Delay, p + 128);
		return fields;
	}

	private static void Add<T>(List<BlockField> fields, String name, T? value, Int64 position) where T : struct
	{
		if (value.HasValue) fields.Add(new BlockField(name, value.Value, String.Empty, position));
	}

	private static String OptionText(Byte value) => String.Join(", ", new[]
	{
		(value & 0x02) != 0 ? "ignore pitch wheel" : null, (value & 0x04) != 0 ? "linear dB decay" : null,
		(value & 0x10) != 0 ? "muted" : null, (value & 0x20) != 0 ? "round robin per note" : null,
		(value & 0x40) != 0 ? "release decay" : null, (value & 0x80) != 0 ? "fixed sample selection" : null
	}.Where(text => text != null));
}
