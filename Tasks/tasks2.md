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
    - "Add skai.svg placeholder"
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
