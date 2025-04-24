System Architecture & Components
Skai is designed as a client-centric application (a Windows desktop app) augmented by cloud AI services. Below is a breakdown of the major components and how they interact:
Windows Client Application (Frontend UI)
AI Integration Layer (LLM, Vision, Voice APIs)
Automation Agent (OmniParser + Action Execution)
Context Monitor (for nudges)
Backend Services (for user accounts, subscription, and data sync)
The diagram below conceptualizes how these pieces fit together (user inputs flow in from UI/voice, go to the AI engine, and results or actions flow back to the UI and possibly the automation agent): (Diagram description: A user presses hotkey -> Windows client captures text/voice + optional screenshot -> sends to AI engine (GPT-4 + Whisper via API) -> receives response -> displays answer in UI. If response includes an action, the Automation Agent (with OmniParser and Selenium) is triggered to perform it on the system. Meanwhile, a Context Monitor thread may periodically send screen info to the AI to generate proactive suggestions, shown as notifications. All API calls and user data can be authenticated/stored via the backend.)