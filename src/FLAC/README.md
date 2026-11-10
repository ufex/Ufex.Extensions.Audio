# FLAC Plugin

Parses native Free Lossless Audio Codec streams as specified by RFC 9639.

The plugin provides:

- stream information, duration, tags, and embedded-picture summary in Quick Info;
- a file map covering the stream marker, metadata blocks, and encoded audio;
- structure nodes for all seven defined metadata block types and reserved blocks;
- lazy embedded-picture previews;
- buffered, lazy audio-frame discovery with frame-header and CRC validation;
- time-based frame groups with frame nodes created only when their group is expanded; and
- validation of metadata ordering, cardinality, lengths, reserved fields, seek tables, cuesheets, comments, and picture fields.

Audio subframes are intentionally not decoded. Frame scanning runs only when the **Audio Frames** node is expanded, so opening large files does not require loading or scanning all encoded audio.
