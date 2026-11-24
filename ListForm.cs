// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using BrightIdeasSoftware;

namespace Fungusware.WinFormsThemes.Demo;

/// <summary>
/// An ObjectListView under every theme: header padding, selection, alternate rows, status colours
/// and the empty-list message. Open with KM_FORM=5. Uses Fungusware.WinFormsThemes.ObjectListView.
/// </summary>
public class ListForm : Form
{
    record Job(string Name, string Source, string Status, int Files, DateTime LastRun);

    readonly ObjectListView _olv = new() { Dock = DockStyle.Fill, FullRowSelect = true, UseCellFormatEvents = true, ShowGroups = false };
    readonly ComboBox _picker = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly CheckBox _empty = new() { Text = "Empty list", AutoSize = true };

    public ListForm()
    {
        Text = "Fungusware.WinFormsThemes - ObjectListView";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 560);
        AutoScaleMode = AutoScaleMode.Dpi;

        _olv.AllColumns.Add(new OLVColumn("Job", nameof(Job.Name)) { Width = 200 });
        _olv.AllColumns.Add(new OLVColumn("Source", nameof(Job.Source)) { Width = 220 });
        _olv.AllColumns.Add(new OLVColumn("Status", nameof(Job.Status)) { Width = 110 });
        _olv.AllColumns.Add(new OLVColumn("Files", nameof(Job.Files)) { Width = 80, TextAlign = HorizontalAlignment.Right });
        _olv.AllColumns.Add(new OLVColumn("Last run", nameof(Job.LastRun)) { Width = 160, AspectToStringFormat = "{0:yyyy-MM-dd HH:mm}" });
        _olv.RebuildColumns();
        _olv.EmptyListMsg = "No jobs yet. Create one to see it here.";
        // Row colours come from StatusColors, so they follow the theme (the package rebuilds rows on a switch).
        _olv.FormatCell += (_, e) =>
        {
            if (e.ColumnIndex == 2 && e.Model is Job j)
                e.SubItem.ForeColor = j.Status switch { "Failed" => StatusColors.Error, "Warning" => StatusColors.Warning, "OK" => StatusColors.Success, _ => e.SubItem.ForeColor };
        };

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(10, 8, 10, 0), WrapContents = false };
        bar.Controls.Add(new Label { Text = "Theme", AutoSize = true, Margin = new Padding(0, 6, 8, 0) });
        foreach (var t in ModernTheme.All) _picker.Items.Add(t.Name);
        bar.Controls.Add(_picker);
        _empty.Margin = new Padding(20, 5, 0, 0);
        bar.Controls.Add(_empty);
        Controls.Add(_olv);
        Controls.Add(bar);

        Fill(true);
        _empty.CheckedChanged += (_, _) => Fill(!_empty.Checked);
        _picker.SelectedIndexChanged += (_, _) => ApplyTheme();
        _picker.SelectedIndex = int.TryParse(Environment.GetEnvironmentVariable("KM_THEME"), out var i) ? i : 0;
    }

    void Fill(bool rows)
    {
        var names = new[] { "Documents", "Photos", "Projects", "Mail archive", "Server config", "Databases", "Music", "Video edits" };
        var status = new[] { "OK", "OK", "Warning", "OK", "Failed", "OK", "Running", "OK" };
        _olv.SetObjects(rows ? names.Select((n, k) => new Job(n, $@"\nas\share\{n.ToLower().Replace(' ', '-')}", status[k], 120 + k * 913, DateTime.Now.AddHours(-k * 7))).ToList() : new List<Job>());
    }

    void ApplyTheme()
    {
        var t = ModernTheme.All[_picker.SelectedIndex];
        ThemeApplier.SetTheme(t);          // switches the engine; the OLV styler restyles every list
        ThemeApplier.Apply(this, t);
    }
}
