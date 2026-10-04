# Installation

## 1. Get the portable app

Download the versioned Windows app ZIP from this repository's Releases page. Extract all files to a folder your Windows account can write to. Keep `GameCoach.exe`, `assets`, `export_recap_pdf.py`, `scripts`, and `docs` together. The app stores its settings and run history next to the executable, so different folders have different data.

The prebuilt app does not require Visual Studio, Node.js, Codex, Hermes Agent, or a running Hermes bot. Do not copy another person's application data as a shortcut to setup. Each user supplies their own model configuration and API key.

## 2. Prepare Windows and the game

| Requirement | Details |
| --- | --- |
| Windows | Windows 11 with .NET Framework 4.8 or later; prebuilt app targets x64 and can run under Windows ARM64 x64 emulation |
| Speech | An installed English Windows speech recognizer; in Windows language settings, install the English speech component if missing |
| OCR | Windows English OCR language support |
| Microphone | Enable desktop microphone access in Windows privacy settings; choose the intended device as Windows' default input |
| Hades II | Installed game in English with normal local saves; Steam's default game path is recognized, and the running game's executable can locate other installs |
| Display | Select the game display in the app; borderless/windowed mode is generally easier to validate with desktop overlays |
| Internet | Needed for OpenAI coaching, transcription, speech, optional web research, and dependency downloads |

Test the microphone in Windows Sound Recorder first if voice input is uncertain. Close other copies of Game Coach; this app uses a single-instance guard and `Ctrl+Alt+L` as its voice shortcut.

## 3. Install llama.cpp and the model

1. Get a compatible engine from [llama.cpp releases](https://github.com/ggml-org/llama.cpp/releases). Select Windows **ARM64 or x64 for your native machine**, and the appropriate GPU backend. Install the matching GPU driver and any runtime dependencies required by that release. Extract the whole engine archive; its DLLs must stay with `llama-server.exe`.
2. Get the main `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf` and matching `mmproj-BF16.gguf` vision projector from [Unsloth's Qwen3.6 model package](https://huggingface.co/unsloth/Qwen3.6-35B-A3B-MTP-GGUF/tree/main). Use matching versions. A text-only model cannot read the game screen. Review the model publisher's license and hardware requirements.
3. The model plus projector occupy approximately 24 GB on disk. Leave additional memory for the context, image processing, runtime buffers, Windows, and Hades II. The app was tested on a Windows machine with at least 64 GB of unified memory. Choose a compatible engine for your PC and run the local performance check.
4. Open the app, go to **Settings → Local watcher → Model startup**, browse to the engine, model, and projector, and enter `Qwen3.6-35B-A3B-UD-Q4_K_M` as the model ID. Begin with the default 32,768 context size. Leave automatic startup enabled, then **Save & load model**.
5. Wait for **Ready**. Expand **Connection details**, then use **Save & test locally**. The synthetic image test checks vision, a rarity comparison, and offered-versus-owned handling without spending OpenAI credit.

The configured Qwen family uses MTP draft flags. Your engine must support these as well as multimodal image input and structured JSON responses. If it exits with an unsupported-argument or model error, use a compatible llama.cpp build; do not assume any old engine will work. CPU-only or insufficient GPU offload may be too slow for live overlays.

You may keep files anywhere and select them in Settings. For automatic file discovery beside the app, use:

```text
Hades2GameCoach/
  GameCoach.exe
  runtime/
    llama-server.exe
    (all companion engine DLLs)
  models/
    Qwen3.6-35B-A3B-UD-Q4_K_M.gguf
    mmproj-Qwen3.6-35B-A3B-BF16.gguf
```

For that automatic layout, rename the downloaded `mmproj-BF16.gguf` to the longer projector name shown above. If you select it manually, renaming is unnecessary. Compatible existing Hermes model files can also be discovered. Hermes does **not** need to be open. Advanced users may disable managed startup and configure an already-running local OpenAI-compatible server; only HTTP loopback endpoints are accepted.

## 4. Add an OpenAI Platform key

1. Sign in to [OpenAI Platform](https://platform.openai.com/), create/select a project, enable [API billing](https://platform.openai.com/settings/organization/billing/overview), and create a normal [project secret API key](https://platform.openai.com/api-keys).
2. This is separate from a ChatGPT subscription. A ChatGPT login, organization Admin API key, or Hermes runtime credential cannot replace the project API key.
3. The project needs access to Responses requests for the coach model, audio transcription, and Realtime audio. The release defaults are `gpt-6-luna`, `gpt-transcribe`, and `gpt-realtime-2.1-mini`. Restricted keys must permit the applicable write/model requests; read-only access cannot run coaching. Optional web search also uses API services. Confirm model availability and current pricing in your account.
4. In **Settings → Cloud coach**, paste the key into the masked **API key** field and select **Save API key**. It is encrypted for your Windows account and the field clears. Never put it into a report, GitHub issue, or chat.

The coach model is configurable in the UI. Transcription and cloud speech model IDs are currently compiled into the app; changing those requires a compatible source change and rebuild. Do not substitute a text-only or incompatible model and expect every feature to work.

Cloud requests need access to `api.openai.com` over HTTPS and WebSocket. A `401` generally means a rejected key; `429` can mean quota/rate limits or unavailable billing. A model access error needs an available model or project access. See [OpenAI's quickstart](https://developers.openai.com/api/docs/quickstart) and [permissions documentation](https://developers.openai.com/api/docs/guides/rbac).

## 5. Verify a first session

1. Confirm the local model reads **Ready** and passes the local vision test.
2. Pick your game display and adjust the voice volume in Settings.
3. Press **Start coaching**, return to Hades II, say **“coach”**, wait for the rising tone, then ask a short question. This live test uses API credit.
4. Say **“thanks”** during a reply to test interruption. The run should remain active.
5. Leave a boon-choice menu visible and check the automatic card. After choosing, inspect **Current build** once Hades has written its next checkpoint. Recommendations are not treated as acquisitions.
6. Review the existing local run history if available. Export HTML, then PDF if you configured it.

If you have no API key, start only the local watcher using its overlay controls. The green **Start coaching** action expects the cloud coach to be configured.

## Optional: PDF export

Install [Python 3 for Windows](https://www.python.org/downloads/windows/). In a terminal with that Python selected, run:

```powershell
python -m pip install -r scripts/requirements-pdf.txt
python -c "import sys; print(sys.executable)"
```

Create `pdf-python.txt` next to `GameCoach.exe` containing only the full path printed by the second command, for example `C:\Python313\python.exe` (no quotes). You can instead place a working Python environment with ReportLab at `runtime\python\python.exe`.

`export_recap_pdf.py` must remain beside the app. HTML, PNG, text, and CSV work without Python. The all-format share ZIP includes PDF and therefore requires this setup too.

## Optional: import item icons

No Hades artwork is shipped. To import icons from your own installed copy, close Game Coach and run:

```powershell
python -m pip install -r scripts/requirements-art.txt
python scripts/import_game_art.py --game "C:\Program Files (x86)\Steam\steamapps\common\Hades II" --app "."
```

Run those commands in the extracted app folder. Change `--game` to your actual installation folder. The importer reads the game's Lua/animation definitions and `Content\Packages\1080p\GUI.pkg`, writes only `assets\boons` and an icon index under the selected app folder, and never modifies game packages. Reopen the app afterward. Game updates can change package formats; an import failure does not prevent text-based reports.

Imported art remains Supergiant Games' property. Do not add it to the source repository or redistribute game packages. The app can embed locally available item icons in reports you explicitly export.

## Update and uninstall

Keep the app closed when updating. Back up your app folder locally, then replace only the clean program files from the new release. Preserve your settings, encrypted key files, model paths, and run directories. Do not commit the backup to GitHub. Model files can be kept outside the app folder and reused.

To uninstall, close the app and remove its portable folder when you no longer need the saved runs. External model files, Hermes installations, and shared servers are separate. No Windows service is installed.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| App opens but model is not ready | Select all three files; check architecture, compatible engine flags, projector version, GPU driver, and memory |
| No wake tone | Default microphone, Windows permission, English recognizer, Start coaching active; test the input meter and Sound Recorder |
| Tone but no answer | Wait for the tone before asking; check API billing, network, model access, and the app's status message |
| Overlay missing or stale | Local test passes, watcher active, Hades foreground, English readable menu, correct display; test borderless mode |
| Latest choice missing | Hades may not have saved yet; refresh checkpoint or explicitly tell the coach what you chose |
| No icons | Run the optional importer against your own game; text reports still work |
| PDF cannot export | Check `pdf-python.txt`, the script beside the app, and ReportLab in that exact Python environment |
| App says already running | Close the other copy; look for a minimized Game Coach window |
| Local compute persists while paused | Pause stops watching/listening; close the app to unload the engine it owns |

Review logs locally before sharing them. They can contain paths, transcription text, game context, or inference diagnostics. Never upload your whole application folder to ask for help.
