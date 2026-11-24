# Logic Pro EXS sampler instrument plugin

This plugin parses the block-oriented `.exs` instrument format used by Logic Pro's
EXS24 and Sampler instruments. It provides quick instrument statistics, a block
file map, a structure tree, raw block views, and validation of block headers,
declared counts, bounds, and zone references.

Decoded block types include instrument headers, zones, groups, sample references,
and legacy/modern parameter tables. Modern editor-state and other auxiliary blocks
are retained as bounded file regions and remain available in the structure and hex
views even when their payload is not interpreted.

The implementation is based on the reverse-engineered format specification in
`docs/file-formats/audio-video/exs/SPEC.md` and the `exstools4` reference reader.
