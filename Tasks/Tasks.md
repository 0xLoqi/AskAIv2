#  ⚡️ Skai • Complete Kanban Backlog  (copy‑paste into your AI runner / IDE)
#  Includes core roadmap + extra security, CI, auto‑update, and polish tickets.

tickets:

# ───────────────────────────────────────────────
# 0 ─ Project Bootstrap & Governance
# ───────────────────────────────────────────────
- id: 00-01
  title: "Init repo & solution" ✔️
  context: "/"
  tasks:
    - "dotnet new sln -n Skai"
    - "mkdir src tests"
  definition_of_done:
    - "git status clean"
    - "`dotnet build` succeeds"

- id: 00-02
  title: "Create Core class‑lib" ✔️
  context: "src/Core"
  tasks:
    - "dotnet new classlib -n Core"
    - "dotnet sln add src/Core/Core.csproj"
  definition_of_done:
    - "Solution builds"

- id: 00-03
  title: "Create WPF UI project" ✔️
  context: "src/UI"
  tasks:
    - "dotnet new wpf -n UI"
    - "dotnet sln add src/UI/UI.csproj"
    - "dotnet add src/UI/UI.csproj reference src/Core/Core.csproj"
  definition_of_done:
    - "`dotnet run` opens blank window"

- id: 00-04
  title: "Add privacy & consent" ✔️
  context: "/"
  tasks:
    - "Create PRIVACY.md with data‑flow diagram"
    - "First‑run dialog explaining screen capture & cloud calls, checkbox 'I agree'"
  definition_of_done:
    - "User must accept before main window appears"

- id: 00-05
  title: "GitHub Actions CI" ✔️
  context: "/.github/workflows"
  tasks:
    - "Add ci.yml: dotnet build, dotnet test, artifact publish"
  definition_of_done:
    - "PR shows green check on push"

# ───────────────────────────────────────────────
# 1 ─ Hotkey, Overlay, DPI
# ───────────────────────────────────────────────
- id: 01-01
  title: "Add hotkey listener package" ✔️
  context: "src/UI"
  tasks:
    - "dotnet add src/UI package MouseKeyHook"
  definition_of_done:
    - "Build succeeds"

- id: 01-02
  title: "Register Ctrl+Shift+Space" ✔️
  context: "src/UI"
  tasks:
    - "Hook global combo, log 'HOTKEY'"
  definition_of_done:
    - "Console logs on press"

- id: 01-03
  title: "Overlay shell window" ✔️
  context: "src/UI"
  tasks:
    - "Overlay.xaml 400x250 borderless 85% opacity"
    - "Show near cursor"
  definition_of_done:
    - "Hotkey toggles overlay"

- id: 01-04
  title: "Auto‑hide overlay" ✔️
  context: "src/UI"
  tasks:
    - "Hide on Deactivated unless pinned"
  definition_of_done:
    - "Click outside hides"

- id: 01-05
  title: "Pin toggle" ✔️
  context: "src/UI"  
  tasks:
    - "Add pin icon, bool _isPinned"
  definition_of_done:
    - "Pinned window persists"

- id: 01-06
  title: "Multi‑monitor & DPI support" ✔️
  context: "src/UI"
  tasks:
    - "Use `VisualTreeHelper.GetDpi` and `Screen.AllScreens`"
    - "Clamp overlay inside active screen bounds"
  definition_of_done:
    - "Overlay appears correctly on 4K / multiple monitors"

# ───────────────────────────────────────────────
# 2 ─ Chat Basics
# ───────────────────────────────────────────────
- id: 02-01
  title: "Add input TextBox" 
  context: "src/UI"
  tasks:
    - "Dock TextBox bottom; Enter event"
  definition_of_done:
    - "Typing logs message"

- id: 02-02
  title: "Chat list control" 
  context: "src/UI"
  tasks:
    - "ObservableCollection<Message> + ItemsControl"
  definition_of_done:
    - "Hardcoded messages show"

- id: 02-03
  title: "Markdown renderer" ✔️
  context: "src/UI"
  tasks:
    - "NuGet Markdown.Xaml; use for assistant replies"
  definition_of_done:
    - "**bold** renders"

- id: 02-04
  title: "System prompt constant" ✔️
  context: "src/Core"
  tasks:
    - "PromptTemplate.cs with default system prompt"
  definition_of_done:
    - "Referenced by ChatClient"

# ───────────────────────────────────────────────
# 3 ─ OpenAI Text Path
# ───────────────────────────────────────────────
- id: 03-01
  title: ".env key loader"
  context: "src/Core"
  tasks:
    - "Add dotenv.net; fetch OPENAI_API_KEY"
  definition_of_done:
    - "Key accessible in code"

- id: 03-02
  title: "ChatClient wrapper" ✔️
  context: "src/Core"
  tasks:
    - "SendAsync(List<Msg>) using GPT‑4‑Turbo"
  definition_of_done:
    - "Unit test gets non‑null reply"

- id: 03-03
  title: "Wire send" ✔️
  context: "src/UI"
  tasks:
    - "On Enter → ChatClient → push reply"
  definition_of_done:
    - "Text Q&A works"

# ───────────────────────────────────────────────
# 4 ─ Voice I/O
# ───────────────────────────────────────────────
- id: 04-01 
  title: "AudioRecorder"
  context: "src/Voice"
  tasks:
    - "NAudio capture 16‑kHz mono temp.wav"
  definition_of_done:
    - "File saved"

- id: 04-02
  title: "WhisperService"
  context: "src/Voice"
  tasks:
    - "POST wav -> Whisper API"
  definition_of_done:
    - "Test asserts transcript contains word"

- id: 04-03
  title: "Push‑to‑talk hotkey hold"
  context: "src/UI"
  tasks:
    - "Hold >0.5 s start/stop recorder, send transcript"
  definition_of_done:
    - "Voice question appears"

- id: 04-04
  title: "SpeechSynth speak‑back"
  context: "src/Voice"
  tasks:
    - "Windows SpeechSynthesizer SpeakAsync(reply)"
  definition_of_done:
    - "Assistant voice heard"

- id: 04-04
  title: "SpeechSynth speak‑back" ✔️
  context: "src/Voice"
  tasks:
    - "AzureTTSService: SynthesizeToFileAsync(reply) using Azure TTS (upgrade from Windows SpeechSynthesizer)"
  definition_of_done:
    - "Assistant voice heard (via Azure TTS, WAV output)"
  notes:
    - "Azure TTS is now the default for speech output, replacing Windows SpeechSynthesizer."

# ───────────────────────────────────────────────
# 5 ─ Vision Q&A & Assets
# ───────────────────────────────────────────────
- id: 05-01
  title: "ScreenGrabber"
  context: "src/Vision"
  tasks:
    - "Capture active window bitmap"
  definition_of_done:
    - "PNG saved"

- id: 05-02
  title: "ImageResizer"
  context: "src/Vision"
  tasks:
    - "Resize max 1024 px"
  definition_of_done:
    - "Width ≤1024"

- id: 05-03
  title: "VisionClient"
  context: "src/Vision"
  tasks:
    - "Call GPT‑4.1 nano with image+prompt"
  definition_of_done:
    - "Gets answer"

- id: 05-04
  title: "Camera button"
  context: "src/UI"
  tasks:
    - "📷 icon triggers VisionClient"
  definition_of_done:
    - "End‑to‑end screenshot Q&A"

- id: 05-05
  title: "Model asset downloader & HW check"
  context: "src/Vision"
  tasks:
    - "On first run, fetch OmniParser weights to %APPDATA%"
    - "Detect GPU / fallback CPU"
  definition_of_done:
    - "Log 'Assets ready' or 'GPU not found using CPU'"

# ───────────────────────────────────────────────
# 6 ─ Memory & Mood
# ───────────────────────────────────────────────
- id: 06-01
  title: "LiteDB storage"
  context: "src/Memory"
  tasks:
    - "LiteDB NuGet; SaveMessage/GetRecent"
    - "Encrypt with AES passphrase stored in Windows Credential Locker"
  definition_of_done:
    - "Messages survive restart, file unreadable plaintext"

- id: 06-02
  title: "MiniLM embeddings"
  context: "src/Memory"
  tasks:
    - "Embed text via ONNX / local model"
  definition_of_done:
    - "Vector stored"

- id: 06-03
  title: "Semantic recall into prompt"
  context: "src/Core"
  tasks:
    - "Return top3 by cosine similarity"
  definition_of_done:
    - "Old fact re‑injected"

- id: 06-04
  title: "Encrypted cloud backup"
  context: "src/Memory"
  tasks:
    - "Upload LiteDB GZip to Firestore Storage weekly"
  definition_of_done:
    - "File appears in Firebase console"

- id: 06-05
  title: "Mood state machine"
  context: "src/Core"
  tasks:
    - "Track sentiment ±1 per message"
    - "Expose Mood (happy, neutral, concerned) for prompt conditioning"
  definition_of_done:
    - "Mood switches on positive/negative chat"

# ───────────────────────────────────────────────
# 7 ─ Playwright Browser Agent
# ───────────────────────────────────────────────
- id: 07-01
  title: "Install Playwright"
  context: "src/Automation"
  tasks:
    - "dotnet add package Microsoft.Playwright"
    - "playwright install"
  definition_of_done:
    - "Headless browser launches"

- id: 07-02
  title: "BrowserActionExecutor"
  context: "src/Automation"
  tasks:
    - "Open URL / click selector / type text"
  definition_of_done:
    - "Integration test fills Google search"

- id: 07-03
  title: "GPT action schema"
  context: "src/Core"
  tasks:
    - "Define JSON {action,selector,text}"
  definition_of_done:
    - "ChatClient parses JSON"

- id: 07-04
  title: "Confirm & execute actions"
  context: "src/UI"
  tasks:
    - "Modal lists actions OK/Cancel"
  definition_of_done:
    - "Demo executes after confirm"

# ───────────────────────────────────────────────
# 8 ─ Native WinAppDriver Agent
# ───────────────────────────────────────────────
- id: 08-00
  title: "Admin‑rights installer"
  context: "automation/setup"
  tasks:
    - "Check UAC; prompt elevation to install WinAppDriver"
  definition_of_done:
    - "Service installs under admin"

- id: 08-01
  title: "Install WinAppDriver service"
  context: "automation/setup"
  tasks:
    - "Download & register service"
  definition_of_done:
    - "`localhost:4723` reachable"

- id: 08-02
  title: "NativeActionExecutor"
  context: "src/Automation"
  tasks:
    - "Connect via Appium.WebDriver; click by AutomationId"
  definition_of_done:
    - "Clicks Notepad 'File'"

- id: 08-03
  title: "UIA element JSON"
  context: "src/Vision"
  tasks:
    - "Enumerate active window controls -> JSON"
  definition_of_done:
    - "JSON lists Edit control"

# ───────────────────────────────────────────────
# 9 ─ Contextual Nudges
# ───────────────────────────────────────────────
- id: 09-01
  title: "Foreground OCR poller"
  context: "src/Nudge"
  tasks:
    - "Timer capture > OCR text"
  definition_of_done:
    - "Text prints"

- id: 09-02
  title: "Trigger heuristics"
  context: "src/Nudge"
  tasks:
    - "If 'attached' but no file, set flag"
  definition_of_done:
    - "Rule fires in test doc"

- id: 09-03
  title: "Toast UI"
  context: "src/UI"
  tasks:
    - "Pop‑up near tray with dismiss"
  definition_of_done:
    - "Toast shows and opens overlay on click"

# ───────────────────────────────────────────────
# 10 ─ Auth & Billing
# ───────────────────────────────────────────────
- id: 10-01
  title: "Firebase email auth"
  context: "src/Auth"
  tasks:
    - "Firebase REST sign‑up/login"
    - "Login window"
  definition_of_done:
    - "User account created"

- id: 10-02
  title: "Usage meter"
  context: "src/Core"
  tasks:
    - "Increment Firestore counter per query"
    - "Block at quota"
  definition_of_done:
    - "Upgrade prompt appears after limit"

- id: 10-03
  title: "Stripe webhook"
  context: "functions/"
  tasks:
    - "On payment set plan=pro"
  definition_of_done:
    - "Stripe test event upgrades user"

# ───────────────────────────────────────────────
# 11 ─ Polish, Security, Accessibility
# ───────────────────────────────────────────────
- id: 11-01
  title: "Mascot SVG"
  context: "assets/"
  tasks:
    - "Add Skai.svg placeholder"
  definition_of_done:
    - "Overlay shows icon"

- id: 11-02
  title: "Theme switch"
  context: "src/UI"
  tasks:
    - "ResourceDictionary Light/Dark"
  definition_of_done:
    - "Toggle reflects"

- id: 11-03
  title: "First‑run tutorial"
  context: "src/UI"
  tasks:
    - "Wizard overlay steps"
  definition_of_done:
    - "Shows on clean profile"

- id: 11-04
  title: "Code‑sign executable"
  context: "build/"
  tasks:
    - "Add signtool command using EV cert"
  definition_of_done:
    - "Binary has valid signature"

- id: 11-05
  title: "Telemetry opt‑out"
  context: "src/Core"
  tasks:
    - "Settings flag skips Sentry init"
  definition_of_done:
    - "Flag true => no HTTP to Sentry"

- id: 11-06
  title: "Accessibility labels"
  context: "src/UI"
  tasks:
    - "Set AutomationProperties.Name on buttons/inputs"
  definition_of_done:
    - "Narrator reads element names"

# ───────────────────────────────────────────────
# 12 ─ Packaging, Updates, Notices
# ───────────────────────────────────────────────
- id: 12-01
  title: "Single‑instance guard"
  context: "src/Core"
  tasks:
    - "Global mutex focus first instance"
  definition_of_done:
    - "Second launch focuses first"

- id: 12-02
  title: "Inno Setup installer"
  context: "/installer"
  tasks:
    - ".iss script + shortcuts"
  definition_of_done:
    - "Installer runs"

- id: 12-03
  title: "Crash telemetry"
  context: "src/Core"
  tasks:
    - "Integrate Sentry SDK"
  definition_of_done:
    - "Unhandled exception reported"

- id: 12-04
  title: "Feedback link"
  context: "src/UI"
  tasks:
    - "Menu 'Send Feedback' mailto"
  definition_of_done:
    - "Opens email"

- id: 12-05
  title: "Auto‑update mechanism"
  context: "src/Core"
  tasks:
    - "Integrate Squirrel.Windows or MSIX auto‑update"
  definition_of_done:
    - "App downloads & applies delta update in test channel"

- id: 12-06
  title: "Third‑party notices"
  context: "/"
  tasks:
    - "Generate THIRD_PARTY_NOTICES.md listing OSS licenses"
  definition_of_done:
    - "File committed and referenced in installer"

# End of backlog
