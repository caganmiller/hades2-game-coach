# Third-party notices

Game Coach's creator credit is **Caganmiller** ([inquiries](mailto:caganmiller@gmail.com)). The app mark and navigation glyphs are original application artwork.

Hades II, its characters, names, game content, item artwork, and screenshots belong to **Supergiant Games**. This is an independent fan companion, with no claim of affiliation or endorsement. No extracted game artwork, game packages, localization files, save files, or player screenshots are included in this source tree or clean release. The optional icon importer reads the user's installed game locally. Locally generated reports can contain game imagery.

These external components are installed separately and governed by their own publishers' terms and licenses:

- [llama.cpp](https://github.com/ggml-org/llama.cpp) — local inference engine; not bundled.
- [Unsloth Qwen3.6-35B-A3B-MTP-GGUF](https://huggingface.co/unsloth/Qwen3.6-35B-A3B-MTP-GGUF) and its upstream model — vision weights and projector; not bundled. Read the selected model card and license.
- [OpenAI API](https://developers.openai.com/api/docs/) — optional cloud reasoning, transcription, speech, and web research; requires each user's API project and billing.
- [Nous Research Hermes Agent](https://github.com/NousResearch/hermes-agent) — optional existing local runtime/model discovery; Hermes is not required and is not bundled.
- Microsoft Windows, .NET Framework, Windows OCR, speech APIs, and system fonts — supplied by Windows and the developer's SDK installation, not redistributed here.
- [ReportLab](https://pypi.org/project/reportlab/) — optional PDF rendering dependency, not bundled.
- [Pillow](https://python-pillow.org/) and [deppth2 / SGG-Modding](https://github.com/SGG-Modding/deppth) — optional local artwork-import dependencies, not bundled.

The independent read-only save decoder cites format references from [hades2-tools](https://github.com/jakobhellermann/hades2-tools) and [Hades-SavesExtractor](https://github.com/TheNormalnij/Hades-SavesExtractor). It does not execute game Lua or vendor those tools.

Build route entries retain their community source links in `BuildPlanner.cs`. Routes are adapted suggestions, not endorsements or a promise of current leaderboard performance.

This private preview does not grant an open-source license to the app. Third-party material and dependencies retain their respective rights; the creator credit is not a claim of ownership over them.
