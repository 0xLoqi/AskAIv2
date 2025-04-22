# ⚡️ Ask.AI V2

A modern, privacy-conscious desktop AI assistant for Windows, featuring a hotkey-activated overlay, chat, vision, voice, and memory. Built with .NET 9, WPF, and OpenAI APIs.

## Features
- **Global Hotkey Overlay**: Press <kbd>Alt</kbd>+<kbd>Space</kbd> to toggle a borderless, semi-transparent overlay near your cursor.
- **Pin/Unpin Overlay**: Keep the overlay visible or auto-hide when clicking outside.
- **Chat with AI**: Markdown-rendered Q&A with OpenAI GPT-4 Turbo.
- **Vision Q&A**: Screenshot and image-based questions (GPT-4V).
- **Voice Input/Output**: Push-to-talk, speech-to-text (Whisper), and voice replies.
- **Memory**: Local encrypted message storage and semantic recall.
- **Privacy First**: Consent dialog and clear data flow.

## Quick Start
1. **Clone the repo:**
   ```sh
   git clone https://github.com/YOUR-USERNAME/AskAI.git
   cd AskAI
   ```
2. **Set up OpenAI API key:**
   - Create a `.env` file in `src/Core` with:
     ```
     OPENAI_API_KEY=your-key-here
     ```
3. **Build and run:**
   ```sh
   dotnet run --project src/UI/UI.csproj
   ```

## Usage
- Press <kbd>Alt</kbd>+<kbd>Space</kbd> to open the overlay.
- Pin/unpin with the 📌 button.
- Type or use voice to ask questions.
- Use the camera button for vision Q&A.

## Contributing
PRs welcome! Please open issues for bugs or feature requests.

## License
MIT (see LICENSE) 