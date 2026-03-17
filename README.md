# KeePass Network Checker

A [KeePass 2.x](https://keepass.info/) plugin that performs real-time network diagnostics — ping, TCP port check, and HTTP status — directly from your password entries.

![KeePass Network Checker](https://img.shields.io/badge/KeePass-Plugin-blue) ![Version](https://img.shields.io/badge/version-1.2.0-green) ![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-purple) ![License](https://img.shields.io/badge/license-GPL--3.0-orange) ![Built with Claude AI](https://img.shields.io/badge/built%20with-Claude%20AI-blueviolet?logo=anthropic)

---

## Features

- **Ping check** — ICMP round-trip time in milliseconds
- **Port check** — TCP connect to configurable ports (checkboxes for common ports + custom field)
- **HTTP status** — GET request with SSL certificate validation bypass (useful for self-signed certs on routers, NAS devices, etc.)
- **Net Status column** — inline UP/DOWN indicator directly in the KeePass entry list
- **Entry check** — right-click any entry → Network Check
- **Group check** — right-click any group → Network Check All Group Entries
- **DNS resolve** — optional hostname to IP resolution (disabled by default)
- **Sort & Filter** — click column headers to sort, filter by All / UP only / DOWN only
- **Export CSV** — export results to CSV for reporting
- **Right-click context menu** — Copy IP, Copy URL, Open URL in browser, Ping again
- **Scan time** — shows total scan duration next to last checked timestamp
- **Configurable** — port checkboxes, custom extra ports, timeout, DNS resolve toggle

---

## Screenshots

### Network Checker popup with results
![Network Checker results](https://raw.githubusercontent.com/karanikn/KeePassNetworkChecker/main/Screenshots/network_checker3.png)

The popup shows detailed results per entry with color-coded status indicators, sort/filter controls and Export CSV button.

| Column | Description |
|--------|-------------|
| Device | Entry title |
| URL    | Entry URL field |
| IP     | Resolved IP address (when DNS resolve is enabled) |
| Ping   | ICMP round-trip time (ms) or TIMEOUT |
| Open Ports | List of open TCP ports found |
| HTTP   | HTTP response code (200, 403, etc.) or ERR |
| Status | **UP** / **DOWN** composite result |

### Group context menu
![Group context menu](https://raw.githubusercontent.com/karanikn/KeePassNetworkChecker/main/Screenshots/network_checker_group-menu.png)

Right-click any group → **Network Check All Group Entries** to check all entries in that group at once.

### Options dialog
![Options dialog](https://raw.githubusercontent.com/karanikn/KeePassNetworkChecker/main/Screenshots/network_options2.png)

### Net Status column
After running a check, the `Net Status` column in the KeePass entry list is updated with UP or DOWN for each checked entry. To enable it: **View → Configure Columns → enable "Net Status"**.

---

## Requirements

| Requirement | Version |
|-------------|---------|
| KeePass | 2.x (tested on 2.61) |
| .NET Framework | 4.8 |
| Windows | 10 / 11 |

---

## Installation

### Option A — Install pre-built DLL

1. Download `KeePassNetworkChecker.dll` from the [Releases](../../releases) page
2. Copy it to your KeePass `Plugins\` folder  
   *(e.g. `C:\Program Files\KeePass Password Safe 2\Plugins\`)*
3. Restart KeePass
4. Approve the plugin in the KeePass security dialog

### Option B — Build from source

**Prerequisites:**
- Visual Studio 2019/2022 **or** [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) (select `.NET desktop build tools` during install)
- .NET Framework 4.8 (included with Windows 10/11)

```powershell
# Clone the repository
git clone https://github.com/karanikn/KeePassNetworkChecker.git
cd KeePassNetworkChecker

# Set the path to your KeePass.exe (required)
$env:KEEPASS_PATH = "C:\Path\To\KeePass\KeePass.exe"

# Build and install
.\build.ps1
```

> **Tip:** Add `$env:KEEPASS_PATH` to your PowerShell profile (`$PROFILE`) so you don't have to set it every time.

---

## build.ps1 — Build & Install Script

The `build.ps1` script automates the entire build and deployment process.

### What it does — step by step

| Step | Action |
|------|--------|
| 1 | Locates `KeePass.exe` from `$env:KEEPASS_PATH` or common install paths |
| 2 | Finds `MSBuild.exe` automatically via `vswhere.exe` |
| 3 | Cleans `bin\` and `obj\` folders from any previous build |
| 4 | Compiles the project with MSBuild in Release configuration |
| 5 | Removes any leftover `.plgx` file from the Plugins folder |
| 6 | Copies the compiled DLL to the KeePass `Plugins\` folder |
| 7 | Clears the KeePass plugin cache (`%LOCALAPPDATA%\KeePass\PluginCache\*`) |

### Usage

```powershell
# Standard build
.\build.ps1

# Override KeePass path inline
.\build.ps1 -KeePassPath "C:\Tools\KeePass\KeePass.exe"
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-KeePassPath` | string | `$env:KEEPASS_PATH` | Full path to `KeePass.exe` |
| `-Configuration` | string | `Release` | MSBuild configuration |

### After running the script

1. **Restart KeePass**
2. On first load, KeePass will show a **security approval dialog** — click **Yes**
3. The plugin appears in **Tools → Plugins** as *KeePass Network Checker*

---

## Usage

### Check a single entry
1. Select one or more entries in KeePass
2. Right-click → **Network Check (Ping / Port / HTTP)**
3. The popup opens and runs all checks automatically

### Check all entries in a group
1. Right-click on any group in the left panel
2. Select **Network Check All Group Entries**
3. Only entries directly in that group are checked (not subgroups)

### Sort & Filter results
- Click any **column header** to sort ascending/descending
- Use the **Show** dropdown to filter: All / UP only / DOWN only

### Right-click on a result row
- **Copy IP** — copies the resolved IP to clipboard
- **Copy URL** — copies the entry URL to clipboard
- **Open URL in browser** — opens the URL directly
- **Ping again** — re-pings just that host without a full rescan

### Export results
Click **Export CSV** after a scan to save results as a `.csv` file (opens Save dialog with auto-generated filename).

### Net Status column
After any check completes, the `Net Status` column updates for the checked entries.  
Enable it via **View → Configure Columns → Net Status**.

### Options
Go to **Tools → Network Checker Options** to configure:
- Show/hide the popup window
- Enable/disable DNS resolve (default: off)
- Port scan timeout in ms (default: 500ms)
- Which common ports to scan (checkboxes)
- Additional custom ports (comma-separated)

---

## How it works

For each entry, the plugin performs three independent checks:

```
Entry URL: https://192.168.1.1
         │
         ├── Ping (ICMP)         → 12 ms ✓
         ├── Port scan (TCP)     → 22, 443, 3389 open ✓
         └── HTTP GET            → 200 ✓
                                    └─ Status: UP
```

- **Ping** — ICMP with configurable timeout
- **Port scan** — sequential TCP connect per port with configurable timeout
- **HTTP** — GET request with TLS 1.1/1.2 and self-signed cert acceptance
- **Status** — UP if at least one of the three checks succeeds

Checks run in a `BackgroundWorker` so the KeePass UI remains fully responsive.

---

## Project structure

```
KeePassNetworkChecker/
├── KeePassNetworkChecker.cs        # Plugin entry point, menu registration
├── NetworkCheckerForm.cs           # Popup results window
├── NetworkStatusColumnProvider.cs  # Net Status column for KeePass entry list
├── SettingsForm.cs                 # Options dialog
├── Properties/
│   └── AssemblyInfo.cs             # Version and product metadata
├── KeePassNetworkChecker.csproj    # MSBuild project file
├── build.ps1                       # Build & install script
└── README.md                       # This file
```

---

## Changelog

### v1.2.0 — Current
- Added **Sort** — click column headers to sort results
- Added **Filter** — dropdown: All / UP only / DOWN only
- Added **Export CSV** — save results to file
- Added **Right-click context menu** — Copy IP, Copy URL, Open URL, Ping again
- Added **Scan time** display next to last checked timestamp
- Added **Net Status column** — inline UP/DOWN in KeePass entry list (updates after each check)
- Added **DNS resolve** option (disabled by default)
- Added **port checkboxes** in Options for common ports (FTP, SSH, Telnet, SMTP, HTTP, HTTPS, RTSP, RDP)
- Added **custom extra ports** text field in Options
- Added **timeout** setting in Options (default: 500ms)
- Fixed crash caused by parallel port scanning — replaced with stable sequential scan
- Fixed DataGridView crash on form load — moved scan start to `Shown` event
- Fixed group check scanning entire database instead of selected group only

### v1.1.0
- Added **Group context menu** — Network Check All Group Entries
- Added **Net Status column** provider (ColumnProvider API)
- Added **Settings dialog** — Tools → Network Checker Options
- Fixed plugin not appearing in KeePass Plugins list (AssemblyProduct must be `"KeePass Plugin"`)
- Fixed menu registration — replaced direct EntryContextMenu manipulation with `GetMenuItem()` override
- Fixed dark theme — switched to system default colors matching KeePass appearance
- Removed PLGX build in favor of DLL-only deployment

### v1.0.0 — Initial release
- Ping check (ICMP)
- TCP port check (single port auto-detected from URL scheme)
- HTTP status check with SSL bypass
- Popup results window with Device, URL, Ping, Port, HTTP, Status columns
- Entry right-click menu integration
- BackgroundWorker for non-blocking UI

---

## License

This project is licensed under the **GNU General Public License v3.0** — see the [LICENSE](LICENSE) file for details.

---

## Author

**Nikolaos Karanikolas**

---

## Acknowledgements

This plugin was developed with the assistance of **[Claude AI](https://www.anthropic.com/claude)** by [Anthropic](https://www.anthropic.com). The iterative development process — from initial concept through build system troubleshooting, KeePass plugin API research, UI refinement, and bug fixing — was carried out in collaboration with Claude, which helped navigate the KeePass plugin framework, resolve .NET Framework compatibility issues, and refine the feature set based on real-world feedback.
