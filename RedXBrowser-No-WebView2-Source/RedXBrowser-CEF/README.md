# RedXBrowser — Chromium edition (no WebView2)

This version replaces WebView2 with CefSharp, a .NET wrapper around Chromium Embedded Framework (CEF). The Chromium engine and supporting files are published alongside the application. It does not use Microsoft Edge WebView2.

## Build online
1. Replace `RedXBrowser.csproj`, `Program.cs`, and `BrowserForm.cs` in your GitHub repository with these files.
2. Replace `.github/workflows/build-windows.yml` with this project's workflow.
3. Commit the changes to `main`.
4. Open Actions and run **Build RedXBrowser without WebView2**.
5. Download the `RedXBrowser-Windows-x64` artifact and extract it.
6. Run `RedXBrowser.exe` from the extracted folder. Keep the other extracted files beside it; do not copy out just the EXE.

## Notes
- Windows x64 app. The artifact is a ZIP of the published application folder, not a single-file executable, because Chromium requires supporting native files and resources.
- CefSharp may require the Microsoft Visual C++ 2022 Redistributable: https://github.com/cefsharp/CefSharp
- No browser can guarantee that Windows security policy will allow it to run. Do not bypass organization-managed security controls.
- Chromium makes this download much larger than the original WebView2-based build.
