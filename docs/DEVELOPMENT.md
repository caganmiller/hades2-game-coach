# Build from source

## Toolchain

Use Windows with the .NET Framework 4.x C# compiler, System.Speech, and a Windows 10/11 SDK supplying `Windows.winmd`. The release was built with the installed framework compiler and SDK 10.0.26100.0. The build script discovers the newest installed versioned SDK metadata directory; it does not require a developer-specific path. Python 3 is used for packaging, optional artwork import, and PDF export.

From the repository root:

```powershell
powershell -NoProfile -File scripts/build.ps1
```

This creates `build\app\GameCoach.exe` and its runtime resources. No key, model, or game data is needed to compile. The app is built optimized for x64 without debug symbols. For a full portable folder with guides and optional scripts, make and extract the release ZIP below.

## Offline checks

```powershell
powershell -NoProfile -File scripts/build.ps1 -Tests -OutputDirectory build/checks
.\build\checks\DistributionChecks.exe
```

These checks use synthetic run data in an isolated directory. They check clean defaults, absence of credentials/history, loopback-only inference, report escaping, missing-icon behavior, and run archival. Speech checks cover URL/citation removal, streamed link boundaries, preserved boon names and numbers, early playback, and interruption. They do not call cloud models, open the microphone, or capture the desktop. For a separate clean UI preview:

```powershell
.\build\checks\DistributionChecks.exe --ui
```

The preview suppresses periodic save import and managed engine boot so it can render a fresh installation without importing the developer's real runs. Close it when finished. It is a development harness, not a full live microphone/game/model test.

The production app also retains an explicit `--self-test` mode with microphone/screenshot and local speech checks. That mode captures the local display in memory and speaks test audio; do not confuse it with the synthetic checks above.

## Make a clean release

```powershell
python scripts/package.py
```

The packager reads the built executable plus an explicit list of repository resources. It does not enumerate a live app folder. Output goes to ignored `dist`, containing the Windows ZIP and `SHA256SUMS.txt`; the ZIP includes per-file hashes. Validate the ZIP and inspect `git diff --cached` before every upload. Never commit generated binaries or archives to Git; attach only the allowlisted ZIP/checksum to a release.

Gallery links and image references in the packaged guides point to the published online gallery. The app ZIP does not include those example screenshots.

## Code map

| Source | Responsibility |
| --- | --- |
| `GameCoach.cs`, `VoicePipeline.cs` | Voice lifecycle, transcription, cloud request/response and streaming playback |
| `ManagedRuntime.cs`, `RuntimeSettings.cs` | Engine discovery, process lifecycle, readiness, configuration |
| `LocalWatcher.cs`, `FastSignals.cs`, `BossBriefing.cs` | Local inference, OCR gates, overlays, boss introductions |
| `WatcherMemory.cs`, `SaveMemory.cs` | Evidence and read-only save parsing |
| `BuildPlanner.cs`, `BuildGuidance.cs`, `BuildPickerUI.cs` | Unlock-aware routes, goals, and guidance |
| `RunArchive.cs`, `RunRecapDetails.cs`, `RunHtml.cs`, `RecapPresentation.cs`, `RecapExports.cs` | Run projections, synergy recaps and exports |
| `AppExperience.cs`, `WorkspaceUI.cs`, `GuidePages.cs`, `AppGlyph.cs` | Desktop navigation, controls, documentation and original artwork |
| `Usage.cs`, `Sandbox.cs`, `RunMoments.cs`, `BoonArt.cs` | Metrics, experiments, greetings and optional imported icons |

Keep the automatic watcher local-only and saves read-only. Record acquisitions from confirmed pickup or save evidence, and include historical values only when the retained data supports them. Use synthetic fixtures for committed tests.

No model weights, engine binaries, downloaded packages, game-art packs, or Windows SDK assemblies are vendored. The documentation gallery contains approved example images with game artwork. Dependency publishers retain their own licenses. This preview has no open-source redistribution license.
