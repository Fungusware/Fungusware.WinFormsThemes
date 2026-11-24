// Copyright (c) 2026 Fungusware. MIT License - see LICENSE.
using System.Diagnostics;
using System.Text.RegularExpressions;
using Krypton.Toolkit;

namespace Fungusware.WinFormsThemes.Demo.VsClone;

public partial class VsCloneForm
{
    Process _runProc, _buildProc;
    bool _building;
    KryptonButton _startBtn;
    ToolStripButton _tbStop, _tbSave, _tbSaveAll, _tbUndo, _tbRedo, _tbBuild;
    KryptonComboBox _cfgCombo;

    static readonly Regex DiagRx = new(@"^(?<file>.+?)\((?<l>\d+),(?<c>\d+)\): (?<sev>error|warning) (?<code>\w+): (?<msg>.*?)(?: \[(?<proj>[^\]]+)\])?\s*$", RegexOptions.Compiled);

    string OutDirFor(string proj) => Path.Combine(Path.GetTempPath(), "KryptonStudio", Path.GetFileNameWithoutExtension(proj));

    void UpdateStartButton()
    {
        if (_startBtn == null) return;
        _startBtn.Values.Text = "  " + (_startup != null ? Path.GetFileNameWithoutExtension(_startup) : "Start");
        _startBtn.Enabled = _startup != null;
    }

    void UpdateToolbarState()
    {
        var ed = Ed;
        if (_tbSave != null) { _tbSave.Enabled = ed != null && ed.Modified; _tbSaveAll.Enabled = _docs.Values.Any(d => d.View.Editor.Modified); }
        if (_tbUndo != null) { _tbUndo.Enabled = ed != null && ed.CanUndo; _tbRedo.Enabled = ed != null && ed.CanRedo; }
        bool running = _runProc != null && !_runProc.HasExited;
        if (_tbStop != null) _tbStop.Enabled = running || _building;
        if (_startBtn != null) _startBtn.Enabled = _startup != null && !_building && !running;
    }

    /// <summary>dotnet build into a temp folder (so a running copy of this very demo is never locked).</summary>
    async Task<bool> BuildAsync(bool rebuild, bool clean)
    {
        if (_building) return false;
        if (_startup == null) { KMessage.Show(this, "Open a project or solution first.", "Krypton Studio", KryptonMessageBoxButtons.OK, KryptonMessageBoxIcon.Information); return false; }
        SaveAll();
        _building = true; RecolorStatus(); UpdateToolbarState();
        ShowTool(_pgOutput);
        _outputBox.Clear(); _diags.Clear(); FillErrors();
        string proj = _startup, name = Path.GetFileNameWithoutExtension(proj), outDir = OutDirFor(proj);
        var sw = Stopwatch.StartNew();
        Say(clean ? "Clean started..." : "Build started...");
        Out(clean ? $"Clean started at {DateTime.Now:T}..." : $"Build started at {DateTime.Now:T}...", _t.TextMuted);
        string cfg = _cfgCombo?.Text ?? "Debug";
        var args = clean ? new[] { "clean", proj, "-nologo", "-c", cfg } : rebuild
            ? new[] { "build", proj, "-nologo", "-c", cfg, "-o", outDir, "--no-incremental", "-nodeReuse:false", "-v:m" }
            : new[] { "build", proj, "-nologo", "-c", cfg, "-o", outDir, "-nodeReuse:false", "-v:m" };
        int exit = await RunDotnet(args, Path.GetDirectoryName(proj), line =>
        {
            var m = DiagRx.Match(line);
            if (m.Success)
            {
                var d = new Diag(m.Groups["sev"].Value == "error", m.Groups["code"].Value, m.Groups["msg"].Value, Path.GetFileNameWithoutExtension(m.Groups["proj"].Success ? m.Groups["proj"].Value : proj),
                    m.Groups["file"].Value.Trim(), int.Parse(m.Groups["l"].Value), int.Parse(m.Groups["c"].Value));
                BeginInvoke(() => { if (!_diags.Any(x => x.File == d.File && x.Line == d.Line && x.Code == d.Code && x.Message == d.Message)) { _diags.Add(d); FillErrors(); } });
                Out(line, d.IsError ? (_t.Canvas.GetBrightness() < 0.5 ? Color.FromArgb(0xFF, 0x8A, 0x8A) : Color.FromArgb(0xB0, 0x1A, 0x1A)) : Color.FromArgb(0xD6, 0xA2, 0x1E));
            }
            else Out(line);
        });
        sw.Stop();
        bool ok = exit == 0;
        Out($"========== {(clean ? "Clean" : "Build")}: {(ok ? 1 : 0)} succeeded, {(ok ? 0 : 1)} failed - {sw.Elapsed.TotalSeconds:0.0}s ==========", ok ? Color.FromArgb(0x4C, 0xB0, 0x5C) : Color.IndianRed);
        _building = false; RecolorStatus();
        foreach (var d in _docs.Values) ApplyDiagnostics(d);
        Say(ok ? (clean ? "Clean succeeded" : "Build succeeded") : $"Build failed - {_diags.Count(x => x.IsError)} error(s)");
        if (!ok && _diags.Count > 0) ShowTool(_pgErrors);
        UpdateToolbarState();
        return ok;
    }

    Task<int> RunDotnet(string[] args, string dir, Action<string> onLine)
    {
        return Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo("dotnet") { WorkingDirectory = dir, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var a in args) psi.ArgumentList.Add(a);
                psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en"; psi.Environment["DOTNET_NOLOGO"] = "1"; psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
                using var p = Process.Start(psi); _buildProc = p;
                p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                p.WaitForExit();
                return p.ExitCode;
            }
            catch (Exception ex) { onLine("Could not start dotnet: " + ex.Message); return -1; }
            finally { _buildProc = null; }
        });
    }

    async Task StartAsync(bool debug)
    {
        if (_startup == null) return;
        if (_runProc != null && !_runProc.HasExited) return;
        if (!await BuildAsync(false, false)) return;
        string proj = _startup, name = Path.GetFileNameWithoutExtension(proj), outDir = OutDirFor(proj);
        string exe = Directory.GetFiles(outDir, name + ".exe").FirstOrDefault();
        string dll = Path.Combine(outDir, name + ".dll");
        var psi = new ProcessStartInfo { WorkingDirectory = Path.GetDirectoryName(proj), UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (exe != null) psi.FileName = exe; else if (File.Exists(dll)) { psi.FileName = "dotnet"; psi.ArgumentList.Add(dll); }
        else { Out("Nothing to run: no executable was produced.", Color.IndianRed); return; }
        try
        {
            _debugBox.Clear();
            Out($"'{name}' started (process {Path.GetFileName(psi.FileName)}).", _t.TextMuted, true);
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, e) => { if (e.Data != null) Out(e.Data, null, true); };
            p.ErrorDataReceived += (_, e) => { if (e.Data != null) Out(e.Data, Color.IndianRed, true); };
            p.Exited += (_, _) => BeginInvoke(() =>
            {
                Out($"The program '{name}' has exited with code {p.ExitCode}.", _t.TextMuted, true);
                RecolorStatus(); UpdateToolbarState(); Say("Ready");
            });
            p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
            _runProc = p;
            Say("Running " + name); RecolorStatus(); UpdateToolbarState();
        }
        catch (Exception ex) { Out("Could not start: " + ex.Message, Color.IndianRed); }
    }

    void StopRun()
    {
        try { if (_runProc != null && !_runProc.HasExited) _runProc.Kill(true); } catch { }
        try { if (_buildProc != null && !_buildProc.HasExited) _buildProc.Kill(true); } catch { }
    }
}
