[![tests](https://github.com/Shiryou/brut/actions/workflows/nightly.yml/badge.svg)](https://github.com/Shiryou/brut/actions/workflows/nightly.yml) [![codecov](https://codecov.io/github/Shiryou/brut/graph/badge.svg?token=XGNKCPQ1H5)](https://codecov.io/github/Shiryou/brut)

# Birthright Resource Utility (BRUT)

A library, command line, and graphical interface to manage resourse files (`.RES`) for Birthright: The Gorgon's Alliance. It is intended to provide format parity with release versions of the game and standard functionality that might be useful for the modding community.

This tool can be used in conjunction with the [modding reference document](https://www.kiranwelle.com/birthright/modding-reference/) to expand upon the game as-is or create total conversions.

![BRUT screenshot](https://i.imgur.com/owjSbTQ.png)

## Installation

BRUT is available for Windows, Ubuntu, and MacOS and can be found in the [nightly](https://github.com/Shiryou/brut/releases/tag/nightly) tag. It is currently only available as an archive or binary. No installer is available.

On Linux, you may need to install `libvlc-dev` (for WAV previews).

## Contributing

Issues and pull requests are welcome. Major changes should begin in an issue first to discuss.

Please update and run tests with `dotnet test` and lint with `dotnet format`.

### Help needed

Some high priority features that are needed to get to 1.0.

* **Real-world use testing!**

* LZSS compression to match `RESUTIL.EXE`
* Reversal of PCX rotation
* Unit tests
* Media previews (partly working for WAV and PCX)

## Changelog

Changelogs are available for the various formats here:

* [brut-lib](./brut-lib/CHANGELOG.md)

## License

[MIT](LICENSE)

If you decide to fork, extend, or integrate BRUT into your own project, please drop me a line. Knowing the project is useful helps keep the motivation going.
