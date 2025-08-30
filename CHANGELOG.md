# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1]

### Added

- GUI: Allow closing files to release them
- Logging
- GUI: Add File menu entry to create new resource files
- GUI: Add an audio preview button for WAV files

### Changed

- GUI: Only show rotation warning for rotated files
- GUI: Move the Most Recently Used file list to a more logical place
- GUI: Make file sizes human readable
- Update dependencies
- Test: Add tests for LZSS decompression and PCX to bitmap conversion

### Fixed

- Support opening read-only files
- Fix 'Extract all' bug resulting in 0 byte files
- Lib: Properly run-length encode during PCX restoration
- GUI: Fix MRU overflow issue

## [0.1.0] - 2024-10-29

### Added

- CLI and C# library consistent with `RESUTIL.EXE`, but missing file compression and respfile support.
- GUI supporting all CLI/Lib features, and adding some file preview support.
- Descriptive [README](README.md), valid [LICENSE](LICENSE), and this CHANGELOG.

[unreleased]: https://github.com/Shiryou/brut/compare/v0.1.0...HEAD
[0.1.1]: https://github.com/Shiryou/brut/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Shiryou/brut/releases/tag/v0.1.0