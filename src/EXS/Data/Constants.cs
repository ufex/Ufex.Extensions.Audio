namespace Ufex.Extensions.Audio.EXS.Data;

public enum ExsBlockType : Byte
{
	Instrument = 0x00,
	Zone = 0x01,
	Group = 0x02,
	Sample = 0x03,
	Parameters = 0x04,
	Auxiliary5 = 0x05,
	Auxiliary6 = 0x06,
	KeyAction = 0x07,
	BusLabel = 0x08,
	DrumKitAuxiliary = 0x09,
	EditorState = 0x0A,
	SampleBookmark = 0x0B
}

internal static class Constants
{
	public const Int32 HeaderSize = 84;
	public const Byte TypeMask = 0x3F;
	public const Byte ModernTypeFlag = 0x40;
	public const Byte MaskedSizeFlag = 0x80;

	public static readonly Byte[][] ValidMagic =
	[
		[(Byte)'T', (Byte)'B', (Byte)'O', (Byte)'S'],
		[(Byte)'J', (Byte)'B', (Byte)'O', (Byte)'S'],
		[(Byte)'S', (Byte)'O', (Byte)'B', (Byte)'T'],
		[(Byte)'S', (Byte)'O', (Byte)'B', (Byte)'J']
	];

	public static String ParameterName(Int16 id) => id switch
	{
		3 => "Pitch Bend Up", 4 => "Pitch Bend Down", 5 => "Polyphony Voices",
		7 => "Master Volume", 8 => "Volume Keyscale", 10 => "Mono Legato",
		14 => "Coarse Tune", 15 => "Fine Tune", 20 => "Glide",
		29 => "Filter 1 Resonance", 30 => "Filter 1 Cutoff", 44 => "Filter 1 On",
		45 => "Transpose", 46 => "Filter 1 Key Tracking", 56 => "Amp Envelope Hold",
		60 => "LFO 1 Fade", 61 => "LFO 1 Rate", 62 => "LFO 1 Wave Shape",
		63 => "LFO 2 Rate", 64 => "LFO 2 Wave Shape", 75 => "Filter 1 Drive",
		81 => "Amp Envelope Sustain", 84 => "Amp Envelope Decay",
		85 => "Amp Envelope Release", 88 => "Filter Envelope Hold",
		89 => "Amp Dynamic Range High", 90 => "Amp Velocity Sensitivity",
		95 => "Velocity Offset", 97 => "Crossfade Amount", 98 => "Random Pitch",
		163 => "Random Sample Selection", 164 => "Random Velocity",
		165 => "Crossfade Type", 166 => "Coarse Tune Remote",
		167 => "LFO 3 Rate", 170 => "Filter 1 Fat", 171 => "Unison On",
		172 => "Hold via Controller", 243 => "Filter 1 Type", 352 => "Master Pan",
		_ => id == 0 ? "Padding" : $"Parameter {id}"
	};

	public static String EnableByName(Byte value) => value switch
	{
		0 => "None", 1 => "Note", 2 => "Group round robin", 3 => "Controller",
		4 => "Pitch bend", 5 => "MIDI channel", 6 => "Articulation",
		7 => "Tempo", _ => "Unknown"
	};
}
