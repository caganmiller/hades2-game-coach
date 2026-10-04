<p align="center"><img src="assets/game-coach.png" width="112" alt="Game Coach crossroads emblem"></p>

# Hades II Game Coach · The Crossroads

**Hybrid AI for Hades II: local observation, cloud conversation, one persistent build.**

Game Coach follows your Hades II run, helps you weigh your choices, and remembers the build you're putting together. Local AI watches decision screens while the cloud voice coach helps you talk through strategy. Both draw on the same run memory, so you can discuss the next step without starting from scratch. Afterward, share the story of your build through its synergies, standout boons, and results.

Created by **Caganmiller** · [Inquiries](mailto:caganmiller@gmail.com)

## Hybrid AI Gaming Coach

[![An example 24-hour activity view: 88 percent local model requests and 12 percent cloud requests](docs/images/hybrid-activity-24h.png)](docs/SCREENSHOTS.md#hybrid-ai-gaming-coach)

**88% local · 12% cloud across recorded requests in an example 24-hour period.** That's the value of the hybrid approach: ongoing observation can stay on your PC, while cloud requests bring conversation, explanations, and deeper advice when you want them. You can keep automatic guidance running without an API call for every screen read.

The Activity view makes that balance visible—mint for local AI, gold for the cloud. This development example contains 473 local and 63 cloud requests, including testing and cancellations. Your balance will vary with how you play and how often you talk to the coach. [How to read the activity view](docs/SCREENSHOTS.md#about-the-activity-example).

| Your PC watches the game | The cloud joins the conversation | Both follow your build |
| --- | --- | --- |
| Local vision, OCR, wake detection, save reading, and automatic choice/boss guidance | Question transcription, contextual coaching, streamed speech, and web research when needed | Equipped weapon and Arcana, confirmed choices, build targets, and synergy-based run reports |

Automatic watching stays local, and listening for the wake word uses no API credit. Your PC supplies the memory and compute for the local model; voice questions, replies, and optional run greetings use the cloud. Once configured, the app starts its own local engine, so you can launch Game Coach and play without opening Hermes. [See how data moves through the app](docs/PRIVACY.md).

**Preview release.** Download the portable Windows app and follow the setup below to connect your local model and API key. Source is included in this repository. The app download starts fresh, with your settings and run history kept in your own installation.

## A look inside

| Plan your next run | Share the story afterward |
| --- | --- |
| [![Build planner showing unlocked routes](docs/images/app-build-planner.png)](docs/SCREENSHOTS.md#pick-your-build) | [![Run report explaining Blast engine and Magick into Hitch damage synergies](docs/images/html-synergies.jpg)](docs/SCREENSHOTS.md#synergies) |

**[Explore the screenshot gallery →](docs/SCREENSHOTS.md)** — see the coach, build planner, synergy reports, interactive boon details, and in-game advice cards. The gallery includes notes on how each example was captured.

## What it does

| Area | Features |
| --- | --- |
| Voice | Local wake detection, short cloud transcription, streamed answers, interruption, volume and speed controls |
| Automatic guidance | Boon comparisons, keepsake suggestions, boss dialogue tips, and confident door/build target markers |
| Memory | Your equipped weapon, aspect, Arcana, confirmed choices, boon levels, and unlocks, read from game saves and pickup evidence |
| Build planning | Community-informed routes built around your unlocks, with targets that adapt as you choose |
| Run history | Completed and failed attempts, synergy groups, saved stats, loadout filters, and captured victory details |
| Sharing | Standalone interactive HTML, two-page PDF summary, PNG card, text, CSV, and a four-format ZIP |
| Hybrid activity | Local/cloud request and latency charts, outcomes, and provider-reported token usage |

You stay in control of the game. The app reads saves without changing them and refreshes your build as new checkpoints arrive, even while coaching is paused. Before a boss, a short introduction card gives you a few patterns to watch for; ask the coach to explain any of them.

## What stays local, and what uses API credit?

Wake recognition, screen reading, save parsing, and automatic guidance run on your PC. The watcher stays local even if its model has a problem. Pausing stops observation and listening while keeping the model loaded for a quick return; closing the app also closes an engine it started.

When you ask the coach, it receives your question, relevant build context, and a capture of your selected display. Voice questions use a short cloud transcription, and replies use cloud speech. You can also enable start-of-run encouragement and victory greetings. For questions that need more research, the coach can check the web. See [privacy and data flow](docs/PRIVACY.md).

## Optional extras

- **Original item icons:** run the included importer against your own Hades II installation. No extracted icon pack is bundled; gallery images show the optional artwork in use. Text, levels, descriptions, and reports work without imported icons. [Import instructions](docs/INSTALL.md#optional-import-item-icons).
- **PDF and full share ZIP:** install Python 3 and ReportLab, then set `pdf-python.txt` beside the app. HTML, PNG, text, and CSV do not need Python. [PDF setup](docs/INSTALL.md#optional-pdf-export).

## Current limits

This preview supports English Hades II on Windows and has been checked on a high-memory Windows ARM64 machine with an NVIDIA/CUDA llama.cpp engine. Performance depends on your hardware, model, display setup, and game version; use the first-run checks to confirm your setup.

Fast screen changes can delay advice, and build updates follow the evidence available from the screen and game saves. Historical reports use the details preserved for each run. Treat community routes as a starting point and adapt them to the choices you receive. The Windows executable is unsigned, so you may see a publisher or reputation prompt when opening it.

## Install in six steps

1. **Download the app** from [Releases](https://github.com/caganmiller/hades2-game-coach/releases/latest): choose `hades2-game-coach-v0.1.0-windows.zip`. GitHub's automatic “Source code” downloads are for building the app yourself. Extract the entire app ZIP to a writable folder such as `Documents\Hades2GameCoach`; do not run it from inside the ZIP or put it in Program Files.
2. **Check Windows requirements.** Use Windows 11, .NET Framework 4.8 or newer, English speech recognition and OCR language components, and a working default microphone. Run Hades II in English. The desktop executable is x64; Windows ARM64 runs it through x64 emulation. The local inference engine must match your actual CPU/GPU. See the [installation guide](docs/INSTALL.md).
3. **Install the local engine and vision model.** Download a compatible [llama.cpp Windows release](https://github.com/ggml-org/llama.cpp/releases), the `Qwen3.6-35B-A3B-UD-Q4_K_M.gguf` model, and its matching `mmproj-BF16.gguf` from the [model repository](https://huggingface.co/unsloth/Qwen3.6-35B-A3B-MTP-GGUF). Keep the engine's companion DLLs together. The model and projector occupy about 24 GB on disk; leave additional memory for inference and the game.
4. **Open `GameCoach.exe`.** In **Settings → Local watcher → Model startup**, select `llama-server.exe`, the model, and the matching projector. Set model ID to `Qwen3.6-35B-A3B-UD-Q4_K_M`. Keep automatic startup enabled and use **Save & load model**. Wait for **Ready**, then run **Save & test locally** in Connection details. Hermes is optional; once configured, the app starts its own engine.
5. **Connect voice coaching.** Create your own [OpenAI Platform project API key](https://platform.openai.com/api-keys), enable API billing, then enter it in **Settings → Cloud coach → API key → Save API key**. Your ChatGPT subscription is separate. The current defaults use `gpt-6-luna` for coaching, `gpt-transcribe` for questions, and `gpt-realtime-2.1-mini` for speech. Your project must have access to these models and endpoints. Keep the key out of GitHub and shared files.
6. **Play.** Press **Start coaching**, switch to Hades II, say **“coach”**, wait for the tone, then ask. **“Thanks”** silences a reply. **Pause coaching** stops listening and watching while preserving the run. For local guidance without a cloud key, use the separate overlay controls in Settings.

**Full instructions:** [Install and troubleshoot](docs/INSTALL.md) · [Controls and first-run checklist](docs/USAGE.md) · [Privacy and local data](docs/PRIVACY.md) · [Build from source](docs/DEVELOPMENT.md)

Independent fan companion; not affiliated with or endorsed by Supergiant Games, OpenAI, Nous Research, NVIDIA, or model/runtime publishers. See [third-party notices](THIRD_PARTY_NOTICES.md). No open-source license is granted in this preview; public access to this repository does not change third-party rights.
