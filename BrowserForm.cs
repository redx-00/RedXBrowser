using CefSharp;
using CefSharp.WinForms;
using System.Drawing;

namespace RedXBrowser;

public sealed class BrowserForm : Form
{
    private readonly TextBox address = new();
    private readonly Label status = new();
    private readonly Button back = MakeButton("←", "Back");
    private readonly Button forward = MakeButton("→", "Forward");
    private readonly Button reload = MakeButton("⟳", "Reload");
    private readonly Button home = MakeButton("⌂", "Home");
    private readonly Button go = MakeButton("Go", "Go to address");
    private readonly ChromiumWebBrowser browser;

    private const string HomeUrl = "https://www.bing.com";

    public BrowserForm()
    {
        Text = "RedXBrowser";
        MinimumSize = new Size(800, 520);
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.White;

        BuildLayout();
        browser = new ChromiumWebBrowser(HomeUrl) { Dock = DockStyle.Fill };
        browser.TitleChanged += (_, e) => SafeUi(() =>
            Text = string.IsNullOrWhiteSpace(e.Title) ? "RedXBrowser" : $"{e.Title} — RedXBrowser");
        browser.AddressChanged += (_, e) => SafeUi(() => address.Text = e.Address);
        browser.LoadingStateChanged += (_, e) => SafeUi(() =>
        {
            back.Enabled = e.CanGoBack;
            forward.Enabled = e.CanGoForward;
            status.Text = e.IsLoading ? "RedXBrowser • Loading…" : "RedXBrowser • Ready";
        });

        Controls.Add(browser);
        browser.BringToFront();
        Controls.SetChildIndex(status, Controls.Count - 1);
        Controls.SetChildIndex(Controls[Controls.Count - 2], Controls.Count - 2);
    }

    private void BuildLayout()
    {
        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Top, Height = 58, Padding = new Padding(10, 9, 10, 9),
            ColumnCount = 6, RowCount = 1, BackColor = Color.FromArgb(30, 30, 36)
        };
        foreach (var width in new[] { 42, 42, 42, 42 })
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));

        foreach (var b in new[] { back, forward, reload, home, go })
        {
            b.Dock = DockStyle.Fill; b.Margin = new Padding(2);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 82);
            b.BackColor = Color.FromArgb(42, 42, 50);
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        }

        address.Dock = DockStyle.Fill; address.Margin = new Padding(6, 4, 6, 4);
        address.Font = new Font("Segoe UI", 11);
        address.BorderStyle = BorderStyle.FixedSingle;
        address.BackColor = Color.FromArgb(245, 245, 248);
        address.ForeColor = Color.FromArgb(25, 25, 30);
        address.PlaceholderText = "Search the web or enter an address";

        top.Controls.Add(back, 0, 0); top.Controls.Add(forward, 1, 0);
        top.Controls.Add(reload, 2, 0); top.Controls.Add(home, 3, 0);
        top.Controls.Add(address, 4, 0); top.Controls.Add(go, 5, 0);

        status.Dock = DockStyle.Bottom; status.Height = 24;
        status.Padding = new Padding(10, 3, 0, 0);
        status.BackColor = Color.FromArgb(30, 30, 36);
        status.ForeColor = Color.Gainsboro;
        status.Text = "RedXBrowser • Starting Chromium…";

        Controls.Add(status);
        Controls.Add(top);

        back.Click += (_, _) => { if (browser.CanGoBack) browser.Back(); };
        forward.Click += (_, _) => { if (browser.CanGoForward) browser.Forward(); };
        reload.Click += (_, _) => browser.Reload();
        home.Click += (_, _) => browser.Load(HomeUrl);
        go.Click += (_, _) => NavigateFromAddress();
        address.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { NavigateFromAddress(); e.SuppressKeyPress = true; }
        };
    }

    private static Button MakeButton(string text, string accessibleName) =>
        new() { Text = text, AccessibleName = accessibleName, TabStop = true };

    private void NavigateFromAddress()
    {
        var input = address.Text.Trim();
        if (input.Length == 0) return;

        string target;
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            target = uri.ToString();
        else if (input.Contains('.') && !input.Contains(' ') &&
                 Uri.TryCreate("https://" + input, UriKind.Absolute, out var domainUri))
            target = domainUri.ToString();
        else
            target = "https://www.bing.com/search?q=" + Uri.EscapeDataString(input);

        browser.Load(target);
    }

    private void SafeUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(action); } catch (InvalidOperationException) { }
        }
        else action();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        browser.Dispose();
        base.OnFormClosed(e);
    }
}
