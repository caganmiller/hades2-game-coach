# Inside The Crossroads

[Back to the app](../README.md) · [Install](INSTALL.md) · [How to use](USAGE.md)

Local AI observes the game. Cloud AI joins the conversation when you ask. Shared run memory connects both to the same equipment, choices, and plan. These player-approved examples show that hybrid approach in the Windows app and in an exported run report.

## Hybrid AI Gaming Coach

**The last 24 hours of recorded activity: 473 local requests, 63 cloud requests, 88% local.** The full-size Activity view makes the split visible: mint for the local model, gold for cloud API requests.

![Last 24 hours of actual local and cloud request activity in Game Coach](images/hybrid-activity-24h.png)

Local vision and OCR handle automatic observation; read-only saves ground the run memory. The cloud coach uses that context for questions, explanations, optional research, and voice replies. Automatic watching has no cloud fallback. Local compute and cloud requests are shown separately so the division of work is visible.

### Reading the 24-hour snapshot

- **Window:** October 2, 2026, 7:45:42 p.m. through October 3, 2026, 7:45:42 p.m., America/New_York (EDT). This is a fixed screenshot, not a live dashboard.
- **Coverage:** all 536 retained requests fall inside that window. Recording begins October 3 at 7:38 a.m. EDT; the earlier blank period means no retained records, not proof that no work occurred. The 5,000-event retention limit was not reached.
- **Outcomes:** 363 completed, 172 cancelled, and one failed/incomplete request. The 88% figure includes all recorded outcomes and counts requests, not successful answers or money saved.
- **Timings:** bars sum request seconds within time buckets, including inference, queues, and transfer. They do not measure GPU utilization, energy, or latency of a single answer. Local requests accumulated 3,866.3 seconds in this sample.
- **Reported tokens:** 584,121 cloud input/output tokens were reported across 50 of 63 cloud requests. Missing usage remains unknown. Token totals alone are not a dollar-cost calculation.
- **Context:** this development-machine sample includes tests and 14 cloud checks from the earlier live-boss experiment. The current boss experience uses local introduction cards, with cloud explanation when requested. This sample is not a benchmark or prediction of another player's usage.

The image comes from the app's existing Activity panel, with **Last 24 hours** selected and an isolated copy of the usage records. Only the screenshot and these aggregates are published; raw usage history stays private. See [privacy and data flow](PRIVACY.md).

## Voice coach

Start coaching to listen and watch together. Ask by voice or type a question; “thanks” silences the spoken reply.

![Coach page with voice and typed-question controls](images/app-coach.png)

## Pick your build

Explore routes that match recorded unlocks. Each route has an overview, boon roadmap, and pre-run setup. Selecting a plan provides guidance; it does not equip items or play the game.

![Build planner with unlocked routes, playstyle filters, and the Flame swarm overview](images/app-build-planner.png)

## Run history

Browse completed and failed attempts, review synergies and results, then export a full HTML build or a PDF summary. This demonstration profile contains the approved Night 44 example only.

![Night 44 in the run archive with export controls and synergy groups](images/app-run-history.png)

## Full build report

A run is a set of interactions worth sharing. The report leads here with the connections between its boons, equipment, and Arcana; the standalone HTML file also keeps every retained item detail and opens without an account.

### Synergies

The report connects the pieces and states the conditions required for them to work. These groups explain interactions; they are not measured damage attribution.

![Blast engine and Magick into Hitch damage synergy groups](images/html-synergies.jpg)

### Results and full loadout

This Night 44 clear used Umbral Flames, Aspect of Melinoë, and finished in 21:04.85.

![Night 44 victory summary and recorded statistics](images/html-report.jpg)

The full build groups boons, equipment, and Arcana in that order. Search and filters narrow the list.

![Full build search, category filters, and boon icons](images/html-build.jpg)

### Boon details

Hover, focus, or click an item to see its retained rarity, level, and effect. The image shows a pinned Volcanic Strike detail card. These are screenshots of the interactive report; the gallery images themselves are static.

![Volcanic Strike detail card showing Heroic rarity, level 4, and its recorded effect](images/html-item-detail.jpg)

## Boon counsel

**Rendered card from a live observation.** On the captured Zeus menu, the local watcher read Heaven Flourish, Thunder Rush, and Ionic Gain. This is its recorded recommendation, displayed by the app's actual overlay renderer. Advice depends on the build and may be imperfect; this is an example, not a universal boon ranking.

![Actual boon card renderer displaying the recorded Heaven Flourish recommendation](images/overlay-boon.png)

## Boss briefing

**Staged renderer preview.** The real overlay renderer displays the app's built-in Chronos briefing here. Boss cards are intended for opening dialogue; ask the voice coach to expand on a pattern. This image is not evidence of a newly detected live boss encounter.

![Chronos briefing card with scythe, clock, and arena reminders](images/overlay-boss.png)

## About these captures

- App views were rendered from the existing Windows controls in an isolated profile, with no API key, conversation history, or recording. The key-setup prompt is the normal unconfigured state.
- The Activity view uses actual recorded request metadata for the stated window. Its coverage, outcomes, and measurement limits are listed above; the raw usage file is not in the repository.
- Report images are direct browser screenshots of the player-approved Night 44 HTML export, including locally imported item icons.
- Overlay images use the existing card renderer. The app deliberately excludes its overlays from screen capture so the watcher does not read its own advice; these isolated card captures show the design without changing that behavior.
- Every published image was visually checked. No computer-control banner, cursor glow, unrelated chat window, credential, or desktop path appears in the gallery. Raw saves, logs, audio, and the underlying personal report files are not published.
- Hades II item artwork and game content © Supergiant Games. Independent fan companion; see [third-party notices](../THIRD_PARTY_NOTICES.md). The clean portable release does not contain these example images or an extracted game-art pack.
