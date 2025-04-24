<div align="center">
  <img src="src/UI/Assets/askai_logo.png" alt="ask.ai logo" width="400"><br>
  <br>
  <b>Ask anything, anywhere, anytime.</b>
  <br>
  <em>
    The AI assistant that's always a hotkey away—type or talk, snap a screenshot, and get instant answers. All on your desktop.
  </em>
</div>

---

A modern, privacy-conscious, and deeply personal desktop AI companion for Windows. Skai is more than a chatbot—it's your always-on, voice-enabled, vision-capable, memory-empowered digital friend. Built with .NET 9, WPF, and OpenAI APIs, Skai brings instant answers, playful conversation, and powerful productivity to your fingertips.

---

## ✨ Why Skai?
- **Instant, global overlay:** Summon your AI from anywhere with <kbd>Alt</kbd>+<kbd>Space</kbd>.
- **Personality & Memory:** Skai, your AI, learns about you, adapts, and remembers your preferences.
- **Voice & Vision:** Speak, listen, and show—Skai handles text, voice, and screenshots.
- **Privacy-first:** All memory is local and encrypted. You control your data.
- **Beautiful, modern UX:** Minimal, fast, and distraction-free.

---

## 🚀 Features
- **Global Hotkey Overlay:** Borderless, semi-transparent, always at your fingertips.
- **Pin/Unpin:** Keep the overlay visible or let it auto-hide.
- **Conversational AI:** Markdown-rendered chat with OpenAI GPT-4 Turbo.
- **Vision Q&A:** Instantly screenshot and ask about anything on your screen (GPT-4V).
- **Voice Input/Output:** Push-to-talk, Whisper speech-to-text, and natural voice replies (Azure/ElevenLabs).
- **Personal Profile:** Skai adapts to your name, interests, and preferred tone.
- **Memory:** Local, encrypted message storage and semantic recall.
- **Settings:** Edit/reset your profile, toggle TTS, and more.
- **Privacy:** No data leaves your device except for API calls you approve.

---

## 🖼️ Screenshots
<!-- Add screenshots or GIFs here for best effect -->

---

## 🔧 Setup Instructions (From Zero to Skai)

### 1. **Install Prerequisites**
- [.NET SDK 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Optional: Visual Studio 2022+ with WPF support or VS Code with C# extension

> **Heads-up:** If you’ve installed older .NET versions and get weird errors, try deleting `%AppData%\NuGet\NuGet.Config` and running:
```bash
dotnet restore
```

### 2. **Clone the Repo**
Use your preferred CLI tool to clone the repository:
```bash
git clone https://github.com/YOUR-USERNAME/AskAI.git
cd AskAI
```

### 3. **Set Up Your API Key**
Inside `src/Core`, create a file called `.env`:
```env
OPENAI_API_KEY=your-openai-key-here
```
> You’ll need an OpenAI account + key. Sign up at [platform.openai.com](https://platform.openai.com/)

### 4. **Build and Launch**
```bash
dotnet restore
dotnet build
dotnet run --project src/UI/UI.csproj
```

> ✅ If it worked, you’ll see the Skai overlay. Simply call Skai by name or press <kbd>Alt</kbd> + <kbd>Space</kbd>from anywhere to start chatting.

---

## 🛠️ Architecture
- **.NET 9, WPF:** Modern, native Windows desktop app.
- **src/Core:** API integration, business logic, and memory.
- **src/UI:** WPF overlay, chat, and user experience.
- **src/Voice, src/Vision:** Voice and image features.
- **Modular:** Easy to extend with new skills, plugins, or providers.

---

## 🌱 Roadmap
- **Deeper personalization:** More adaptive, emotionally intelligent Skai.
- **Plugin system:** Let users add new skills and integrations.
- **Cross-platform:** Mac/Linux support (via Avalonia or MAUI).
- **More voice/vision:** Smarter, more natural multimodal interaction.
- **Community features:** Share prompts, plugins, and experiences.

---

## 🤝 Contributing
- PRs and issues welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
- Looking for help with: UI polish, plugin system, accessibility, and more.

---

## 📄 License
MIT (see LICENSE) 
