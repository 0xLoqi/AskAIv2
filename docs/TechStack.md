## Final Tech Stack Choices

| Layer                    | Tool / API                                                | Rationale                                                                                  |
|--------------------------|-----------------------------------------------------------|--------------------------------------------------------------------------------------------|
| Desktop UI control       | Microsoft UI Automation API (`System.Windows.Automation`) | First‑party Windows UI automation; direct access to native controls without extra deps.    |
| Windows‑app agent        | WinAppDriver                                              | WebDriver‑style automation for any Win32/WPF/UWP app; built on UIA for reliability.        |
| Browser agent            | Playwright‑Sharp (C#)                                     | Modern, deterministic browser automation with built‑in retry/auto‑waits; multi‑browser.     |
| Vision context           | GPT-4 Vision (cloud) + OmniParser (local)                 | Cloud multimodal reasoning + local structured UI parsing for precise context extraction.   |
| Speech‑to‑text (ASR)     | Whisper API (cloud) + whisper.cpp (offline)               | High‑accuracy cloud STT with $0.006/min pricing; optional offline mode for privacy.        |
| Text‑to‑speech (TTS)     | Windows SpeechSynthesizer                                 | Native Windows TTS; dozens of voices; async streaming at zero additional cost.             |
| Language Model (LLM)     | GPT-4 Turbo + GPT-3.5 Turbo                               | Best-in-class performance; fallback to GPT-3.5 for cost‑sensitive or simple queries.       |
| Memory store             | LiteDB/SQLite + MiniLM embeddings                         | Local semantic vector search; minimizes API calls; encrypted on disk for user privacy.     |
| Authentication & Billing | Firebase Authentication + Stripe webhooks                 | Managed user sign‑in; easy subscription & payment handling; scales with minimal ops.       |
