using Ufex.API.Tree;
using Ufex.Extensions.Audio.EXS.Data;

namespace Ufex.Extensions.Audio.EXS.Structure;

internal sealed class InstrumentBlockNode : ExsBlockNode
{
	private readonly InstrumentBlock _instrument;
	public InstrumentBlockNode(InstrumentBlock instrument) : base(instrument, TreeViewIcon.Header) => _instrument = instrument;

	protected override IEnumerable<BlockField> GetFields()
	{
		Int64 p = _instrument.ContentPosition;
		return
		[
			new("Reserved", _instrument.Reserved, String.Empty, p),
			new("Zone Count", _instrument.ZoneCount, String.Empty, p + 4),
			new("Group Count", _instrument.GroupCount, String.Empty, p + 8),
			new("Sample Count", _instrument.SampleCount, String.Empty, p + 12),
			new("Parameter Block Count", _instrument.ParameterBlockCount, String.Empty, p + 16),
			new("Auxiliary 5 Count", _instrument.Auxiliary5Count, String.Empty, p + 20),
			new("Auxiliary 6 Count", _instrument.Auxiliary6Count, String.Empty, p + 24),
			new("Key Action Count", _instrument.KeyActionCount, String.Empty, p + 28),
			new("Bus Label Count", _instrument.BusLabelCount, String.Empty, p + 32),
			new("Drum-kit Auxiliary Count", _instrument.DrumKitAuxiliaryCount, String.Empty, p + 36),
			..(_instrument.ContentSize >= 48 ? [new BlockField("Sample Bookmark Count", _instrument.SampleBookmarkCount, String.Empty, p + 44)] : Array.Empty<BlockField>())
		];
	}
}
