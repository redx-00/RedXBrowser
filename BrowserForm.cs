using System.Net;
using System.Text.RegularExpressions;
using System.Drawing;

namespace RedXBrowser;

public sealed class BrowserForm : Form
{
    private readonly TabControl tabs = new();
    private readonly TextBox address = new();
    private readonly Button back = new() { Text = "←", Width = 38 };
    private readonly Button forward = new() { Text = "→", Width = 38 };
    private readonly Button reload = new() { Text = "⟳", Width = 38 };
    private readonly Button go = new() { Text = "GO", Width = 48 };
    private readonly Button newTab = new() { Text = "+", Width = 38 };
    private readonly Dictionary<TabPage, PageState> states = new();
    private readonly HttpClient http = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
    {
        Timeout = TimeSpan.FromSeconds(20),
        DefaultRequestHeaders = { { "User-Agent", "RedXBrowser/0.1 (independent experimental browser)" } }
    };
    private readonly Color red = Color.FromArgb(255, 48, 64);
    private readonly Color background = Color.FromArgb(18, 18, 20);
    private readonly Color surface = Color.FromArgb(30, 30, 34);
    private readonly Color foreground = Color.FromArgb(235, 235, 239);

    public BrowserForm()
    {
        Text = "RedXBrowser";
        MinimumSize = new Size(780, 500);
        Size = new Size(1180, 760);
        BackColor = background;
        ForeColor = foreground;
        Font = new Font("Segoe UI", 10);
        BuildChrome();
        AddTab("RedX Home", "https://example.com");
    }

    private void BuildChrome()
    {
        var chrome = new TableLayoutPanel { Dock = DockStyle.Top, Height = 92, RowCount = 2, ColumnCount = 1, BackColor = background, Padding = new Padding(8, 6, 8, 4) };
        chrome.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        chrome.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        var brand = new Label { Text = "REDX  /  BROWSER", ForeColor = red, Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        chrome.Controls.Add(brand, 0, 0);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, BackColor = background, Padding = new Padding(0, 3, 0, 0) };
        StyleButton(back); StyleButton(forward); StyleButton(reload); StyleButton(go); StyleButton(newTab);
        address.BackColor = surface; address.ForeColor = foreground; address.BorderStyle = BorderStyle.FixedSingle;
        address.Font = new Font("Segoe UI", 10); address.Width = 700; address.Height = 30;
        nav.Controls.AddRange(new Control[] { back, forward, reload, address, go, newTab });
        chrome.Controls.Add(nav, 0, 1);
        Controls.Add(tabs); Controls.Add(chrome);
        tabs.Dock = DockStyle.Fill; tabs.DrawMode = TabDrawMode.OwnerDrawFixed; tabs.ItemSize = new Size(180, 30); tabs.Padding = new Point(12, 4);
        tabs.DrawItem += (_, e) => { var page = tabs.TabPages[e.Index]; using var b = new SolidBrush(page == tabs.SelectedTab ? surface : background); e.Graphics.FillRectangle(b, e.Bounds); TextRenderer.DrawText(e.Graphics, page.Text, Font, e.Bounds, page == tabs.SelectedTab ? red : foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.Left); };
        tabs.SelectedIndexChanged += (_, _) => SyncAddress();
        go.Click += async (_, _) => await NavigateCurrentAsync(address.Text);
        address.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await NavigateCurrentAsync(address.Text); } };
        back.Click += async (_, _) => await MoveHistory(-1);
        forward.Click += async (_, _) => await MoveHistory(1);
        reload.Click += async (_, _) => { if (CurrentState() is { } s) await LoadUrl(s.Url, addHistory: false); };
        newTab.Click += (_, _) => AddTab("New Tab", "https://example.com");
        FormClosing += (_, _) => http.Dispose();
    }

    private void StyleButton(Button b) { b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Color.FromArgb(70, 35, 40); b.BackColor = surface; b.ForeColor = b == go ? red : foreground; b.Height = 29; b.Margin = new Padding(2, 0, 2, 0); }

    private void AddTab(string title, string url)
    {
        var tab = new TabPage(title) { BackColor = background, ForeColor = foreground };
        var view = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.White, Padding = new Padding(28), AutoSize = false };
        tab.Controls.Add(view); tabs.TabPages.Add(tab);
        states[tab] = new PageState(view);
        tabs.SelectedTab = tab;
        _ = LoadUrl(url, addHistory: true);
    }

    private PageState? CurrentState() => tabs.SelectedTab is { } tab && states.TryGetValue(tab, out var s) ? s : null;
    private async Task NavigateCurrentAsync(string text)
    {
        text = text.Trim();
        if (text.Length == 0) return;
        if (!Uri.TryCreate(text.Contains("://") ? text : "https://" + text, UriKind.Absolute, out var uri))
            uri = new Uri("https://www.google.com/search?q=" + Uri.EscapeDataString(text));
        if (uri.Scheme != "http" && uri.Scheme != "https") return;
        await LoadUrl(uri.ToString(), true);
    }

    private async Task LoadUrl(string url, bool addHistory)
    {
        var state = CurrentState(); if (state is null) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")) return;
        state.Url = uri.ToString();
        if (addHistory) { state.History = state.History.Take(state.HistoryIndex + 1).ToList(); state.History.Add(state.Url); state.HistoryIndex = state.History.Count - 1; }
        address.Text = state.Url;
        if (tabs.SelectedTab is { } selected) selected.Text = "Loading…";
        state.View.Controls.Clear();
        AddText(state.View, "REDX / LOADING", 22, true, Color.FromArgb(200, 20, 35));
        try
        {
            using var response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            RenderHtml(state, html, uri);
            if (tabs.SelectedTab is { } active && states.TryGetValue(active, out var activeState) && activeState == state) active.Text = string.IsNullOrWhiteSpace(state.Title) ? uri.Host : state.Title;
        }
        catch (Exception ex)
        {
            state.View.Controls.Clear();
            AddText(state.View, "RedXBrowser couldn't load this page", 22, true, Color.FromArgb(200, 20, 35));
            AddText(state.View, uri.ToString(), 11, false, Color.DimGray);
            AddText(state.View, ex.Message, 11, false, Color.DimGray);
        }
        SyncAddress();
    }

    private void RenderHtml(PageState state, string html, Uri baseUri)
    {
        state.View.SuspendLayout(); state.View.Controls.Clear();
        var title = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        state.Title = title.Success ? WebUtility.HtmlDecode(Regex.Replace(title.Groups[1].Value, "<.*?>", "")).Trim() : baseUri.Host;
        var body = Regex.Match(html, @"<body[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var source = body.Success ? body.Groups[1].Value : html;
        source = Regex.Replace(source, @"<(script|style|noscript|svg)[^>]*>.*?</\1>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var tokens = Regex.Split(source, @"(<a\b[^>]*>.*?</a>|<h[1-6][^>]*>|</h[1-6]>|<p[^>]*>|</p>|<br\s*/?>|</div>|</li>|<hr\s*/?>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;
            var link = Regex.Match(token, @"<a\b[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (link.Success)
            {
                var label = Clean(link.Groups[2].Value);
                if (label.Length == 0) continue;
                if (Uri.TryCreate(baseUri, WebUtility.HtmlDecode(link.Groups[1].Value), out var target) && (target.Scheme == "http" || target.Scheme == "https"))
                {
                    var l = new LinkLabel { Text = label, AutoSize = true, MaximumSize = new Size(900, 0), LinkColor = Color.FromArgb(190, 20, 40), ActiveLinkColor = red, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 3, 0, 3) };
                    l.Click += async (_, _) => { if (tabs.SelectedTab is not null) await LoadUrl(target.ToString(), true); };
                    state.View.Controls.Add(l);
                }
                continue;
            }
            if (token.StartsWith("<", StringComparison.Ordinal)) { if (token.StartsWith("<h", StringComparison.OrdinalIgnoreCase)) state.View.SetFlowBreak(AddText(state.View, "", 4, false, Color.Black), true); continue; }
            var cleaned = Clean(token);
            if (cleaned.Length > 0) AddText(state.View, cleaned, 10, false, Color.FromArgb(35, 35, 38));
        }
        if (state.View.Controls.Count == 0) AddText(state.View, "This page contains no text this experimental renderer can display.", 12, false, Color.DimGray);
        state.View.ResumeLayout();
    }

    private static string Clean(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " ")).Replace("\u00a0", " ").Replace("\r", " ").Replace("\n", " ").Trim();
    private static Label AddText(FlowLayoutPanel panel, string text, float size, bool bold, Color color)
    {
        var label = new Label { Text = text, AutoSize = true, MaximumSize = new Size(900, 0), Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, Margin = new Padding(0, 5, 0, 5) };
        panel.Controls.Add(label); return label;
    }
    private async Task MoveHistory(int delta)
    {
        var s = CurrentState(); if (s is null) return;
        var next = s.HistoryIndex + delta; if (next < 0 || next >= s.History.Count) return;
        s.HistoryIndex = next; await LoadUrl(s.History[next], false);
    }
    private void SyncAddress() { if (CurrentState() is { } s) address.Text = s.Url; }
    private sealed class PageState(FlowLayoutPanel view)
    {
        public FlowLayoutPanel View { get; } = view;
        public string Url { get; set; } = "about:blank";
        public string Title { get; set; } = "";
        public List<string> History { get; set; } = new();
        public int HistoryIndex { get; set; } = -1;
    }
}