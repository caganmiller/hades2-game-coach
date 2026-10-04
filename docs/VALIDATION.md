# Release validation · v0.1.0

Tested on a Windows machine with at least 64 GB of unified memory during preparation of the clean preview release.

- Optimized x64 application compiled from the published source, using the .NET Framework compiler and Windows SDK metadata 10.0.26100.0.
- Synthetic checks passed for fresh settings, empty credentials/history, no developer runtime paths, loopback-only watcher endpoints, and missing optional artwork.
- Synthetic run HTML retained interactive details/filters, escaped hostile item text, and contained no external image dependency. A synthetic completed run archived correctly.
- PNG and PDF export succeeded without imported game artwork; the share ZIP contained exactly HTML, PDF, PNG, and text. No actual player run was used as a test fixture.
- Clean UI preview was inspected with no API key or imported history. Voice defaults and volume/speed controls were visible. The preview disabled real save import, engine boot, and microphone use.
- The optional local icon importer was exercised against an installed game in a separate ignored test folder. It mapped 601 trait IDs to 591 icon files. Those extracted images are not included in the repository or release.
- The final Git staging area and release ZIP were scanned for credential patterns, private/generated files, and literal Windows user-profile paths. The ZIP's file hashes were checked against its internal manifest.

These are clean-distribution checks, not certification on a second PC. No cloud credentials or paid model calls were used during this packaging test. Live wake accuracy, API access, GPU/model compatibility, in-game latency, and overlays on another display require the first-run checklist in [INSTALL.md](INSTALL.md). Leave memory headroom for the local model alongside the game.

The local output folders used for tests are ignored and never copied wholesale into a release. Source checks and packaging can be repeated using [DEVELOPMENT.md](DEVELOPMENT.md).
