⚡️ Skai • Next-Gen Roadmap & Focused Backlog
tickets:
───────────────────────────────────────────────
1 ─ User Experience & UI Polish
───────────────────────────────────────────────
id: 01-01
title: "Overlay & chat UX improvements"
context: "src/UI"
tasks:
"Add subtle animations (fade, slide) to overlay/chat"
"Improve accessibility: labels, keyboard navigation"
"Add theme toggle (light/dark)"
definition_of_done:
"Overlay feels modern, accessible, and themeable"
id: 01-02
title: "Settings & profile expansion"
context: "src/UI"
tasks:
"Add more settings (e.g., voice/vision toggles, privacy options)"
"Allow profile picture/avatar upload"
definition_of_done:
"Settings window is more comprehensive"
───────────────────────────────────────────────
2 ─ Voice & Vision Next Steps
───────────────────────────────────────────────
id: 02-01
title: "Voice reliability & TTS provider fallback"
context: "src/Voice"
tasks:
"Handle TTS/Whisper errors gracefully"
"Add user feedback for voice errors"
"Allow user to set preferred TTS provider"
definition_of_done:
"Voice features are robust and user-friendly"
id: 02-02
title: "Vision Q&A polish"
context: "src/Vision"
tasks:
"Improve screenshot cropping/selection"
"Add visual feedback for vision queries"
definition_of_done:
"Vision Q&A is intuitive and reliable"
───────────────────────────────────────────────
3 ─ Memory, Recall, & Personalization
───────────────────────────────────────────────
id: 03-01
title: "Semantic memory improvements"
context: "src/Core, src/Memory"
tasks:
"Improve recall accuracy and speed"
"Add UI for browsing/searching memory"
definition_of_done:
"User can view and search past conversations"
id: 03-02
title: "Mood & persona polish"
context: "src/Core"
tasks:
"Refine mood/persona conditioning in prompts"
"Add mood indicator to UI"
definition_of_done:
"Skai feels more adaptive and personal"
───────────────────────────────────────────────
4 ─ Proactive & Agent Features
───────────────────────────────────────────────
id: 04-01
title: "Contextual nudges (proactive suggestions)"
context: "src/Nudge"
tasks:
"Implement lightweight OCR polling"
"Trigger helpful suggestions based on screen context"
definition_of_done:
"User receives relevant, non-intrusive nudges"
id: 04-02
title: "Agent/automation mode (MVP)"
context: "src/Automation"
tasks:
"Parse UI elements with OmniParser"
"Allow user to confirm/execute simple UI actions"
definition_of_done:
"User can trigger basic UI automation with confirmation"
───────────────────────────────────────────────
5 ─ Packaging, Updates, & Community
───────────────────────────────────────────────
id: 05-01
title: "Installer & auto-update"
context: "/installer, src/Core"
tasks:
"Add Inno Setup or MSIX installer"
"Integrate auto-update (Squirrel/MSIX)"
definition_of_done:
"User can install and auto-update the app"
id: 05-02
title: "Feedback & community links"
context: "src/UI, docs/"
tasks:
"Add feedback link in UI"
"Add community/Discord link to README"
definition_of_done:
"Users can easily give feedback and join the community"
───────────────────────────────────────────────
6 ─ Core Polish & Stability
───────────────────────────────────────────────
id: 06-01
title: "Clean up warnings & code hygiene"
context: "src/"
tasks:
"Address or suppress top build warnings (nullability, unused vars)"
"Add comments to key classes/methods"
definition_of_done:
"Build is warning-free or warnings are documented/suppressed"
id: 06-02
title: "README & onboarding polish"
context: "/, docs/"
tasks:
"Add screenshots/GIFs to README"
"Ensure onboarding flow is clear for new users"
definition_of_done:
"README is visually appealing, onboarding is smooth"