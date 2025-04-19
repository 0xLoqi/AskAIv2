#  ⚡️ SkaAI • Complete Kanban Backlog  (copy‑paste into your AI runner / IDE)
#  Includes core roadmap + extra security, CI, auto‑update, and polish tickets.

tickets:

# ───────────────────────────────────────────────
# 0 ─ Project Bootstrap & Governance
# ───────────────────────────────────────────────
- id: 00-01
  title: "Init repo & solution" ✔️
  context: "/"
  tasks:
    - "dotnet new sln -n SkaAI"
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
  title: "Pin toggle" 🚧
  context: "src/UI"
  tasks:
    - "Add pin icon, bool _isPinned"
  definition_of_done:
    - "Pinned window persists"

- id: 01-06
  title: "Multi‑monitor & DPI support"
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
  title: "Markdown renderer"
  context: "src/UI"
  tasks:
    - "NuGet Markdown.Xaml; use for assistant replies"
  definition_of_done:
    - "**bold** renders"

- id: 02-04
  title: "System prompt constant"
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
  title: "ChatClient wrapper"
  context: "src/Core"
  tasks:
    - "SendAsync(List<Msg>) using GPT‑4‑Turbo"
  definition_of_done:
    - "Unit test gets non‑null reply"

- id: 03-03
  title: "Wire send"
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
    - "Call GPT‑4V with image+prompt"
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

