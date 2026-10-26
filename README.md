# RedXBrowser

RedXBrowser is an experimental Windows browser prototype written in C# with the RedX Theme: near-black surfaces, red accents, and a compact native interface.

## Current build

- Native Windows Forms UI
- Multiple tabs
- Address/search bar, back, forward, reload, and new tab controls
- Basic HTTP/HTTPS fetching and a very limited text/link renderer
- No Chromium, CefSharp, WebView2, or other embedded browser engine
- GitHub Actions publishes a self-contained Windows x64 single-file `RedXBrowser.exe`

## Download

1. Open the repository's **Actions** tab.
2. Select **Build RedXBrowser** and open a successful run.
3. Under Artifacts, download **RedXBrowser-Windows-x64**.
4. The artifact contains the EXE directly; no application-folder ZIP is required.

The executable is built for Windows x64 and includes the .NET runtime. It may be larger than a framework-dependent application.

## Important limitations

This is **not yet a fully fledged modern web browser**. The independent renderer currently extracts simple text and links from HTML. It does not implement JavaScript, CSS layout, images, media, cookies, authentication, downloads, extensions, or modern web standards. Many websites will not work correctly. Building a standards-compatible HTML/CSS/JavaScript engine from scratch is a substantial long-term project.

This prototype deliberately avoids existing browser engines as requested. Do not use it for sensitive browsing until security and web-platform support have been developed and audited.
