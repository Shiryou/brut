# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- GUI: Allow closing files to release them
- Logging

### Changed

- GUI: Only show rotation warning for rotated files
- GUI: Move the Most Recently Used file list to a more logical place
- GUI: Make file sizes human readable
- Update dependencies

### Fixed

- Support opening read-only files
- Fix 'Extract all' bug resulting in 0 byte files

## [0.1.0] - 2024-10-29

### Added

- CLI and C# library consistent with `RESUTIL.EXE`, but missing file compression and respfile support.
- GUI supporting all CLI/Lib features, and adding some file preview support.
- Descriptive [README](README.md), valid [LICENSE](LICENSE), and this CHANGELOG.

[unreleased]: https://github.com/Shiryou/brut/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Shiryou/brut/releases/tag/v0.1.0