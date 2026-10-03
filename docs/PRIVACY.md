# Privacy and data flow

## Local observation

Windows wake recognition listens locally while coaching is enabled. OCR, automatic screenshot classification, save parsing, choice verification, and local build guidance run on your PC. Automatic watcher requests are restricted to HTTP loopback (`127.0.0.1` or `::1`), with redirects disabled. If local inference fails, it does not silently send the screen to a cloud model.

The app reads supported Hades II saves without writing to them. It may import your existing save history and refresh checkpoints while the app is open even when voice coaching is paused. Visible choices, saved choices, and confirmed acquisitions are separate evidence sources.

## Cloud requests

Explicit cloud coaching requests can include:

- The typed or transcribed question.
- A screenshot of the selected display and relevant build/run context.
- Setup-review screenshots you ask the coach to evaluate.
- Short microphone recordings after a question starts, for transcription.
- Text for spoken replies and optional run greetings/congratulations.
- Optional web-search requests during complex or uncertain planning.

The cloud provider is OpenAI. API charges and provider data policies apply to the user's Platform project. Responses requests use `store: false`; this is not a promise of zero provider-side retention. Check the applicable OpenAI policies for your project. Idle local wake listening makes no API calls. Automatic start/victory speech can use credit while coaching is active, and can be disabled.

Screen capture uses your selected monitor, so keep unrelated private windows off that display when asking the cloud coach. The app does not inspect only the game's internal graphics buffer.

## Files next to the app

| Files/folders | Contents |
| --- | --- |
| `api-key.dpapi` | OpenAI key encrypted with Windows DPAPI for the current account |
| `local-runtime-key.dpapi` | Local server credential, likewise encrypted |
| `settings.json`, `local-model.json`, `pdf-python.txt` | Preferences, model paths/endpoint, optional Python path |
| `run-memory.json`, `run-moments.json`, `coach-journal.json`, `run-history`, `run-archive`, `run-snapshots` | Build evidence, greeting history, sandbox goals, archives, reports, and retained game imagery |
| `usage-history.json` | Local/cloud timing, request outcomes, and reported usage |
| `voice-cache`, diagnostic/startup logs | Cached speech and troubleshooting data; logs may contain question text or paths |
| `assets/boons`, `assets/boon-icons.json` | Optional game icons imported locally |

If no saved OpenAI key exists, the app can also use an `OPENAI_API_KEY` environment variable inherited from the launching process. The release contains neither.

DPAPI binds a saved key to a Windows user; it is not a portable credential-transfer method. The app starts its managed engine on loopback with a randomly generated key passed through the child process environment rather than command-line arguments. Other processes running as the same Windows user remain within that user's trust boundary.

## Sharing and publishing

Reports contain gameplay information you explicitly export and may embed captured victory imagery or imported art. They do not contain the OpenAI key. Share creates local files/clipboard content; it does not automatically publish them.

Never upload a live app folder, logs, saved settings, encrypted key files, save files, screenshots, or cached audio as a release. The repository ignore rules are a secondary safeguard; `scripts/package.py` uses a fixed allowlist and is the supported packaging path. No personal game fixtures are used in the shipped tests.

The public documentation includes a [screenshot gallery](SCREENSHOTS.md) approved by the player. It shows selected app views, a 24-hour Activity snapshot with request counts/timings/reported tokens, a Night 44 run report, and overlay card examples. These reviewed images are separate from the clean portable release. They do not include credentials, raw saves, audio, or conversation history. Raw logs, usage history, and the underlying personal report/profile files are not published.
