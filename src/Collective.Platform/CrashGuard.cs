// SPDX-License-Identifier: GPL-3.0-or-later
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Collective.Platform;

/// <summary>
/// A last-resort crash logger for desktop <c>Program.cs</c> entry points: best-effort <b>appends</b> a
/// "&lt;product&gt;-crash.log" next to the executable and in %TEMP%, each entry carrying an environment
/// header, then lets the exception keep propagating (so the OS/console still sees the crash). The log is
/// soft-capped so repeated crashes can't grow it without bound.
/// </summary>
public static class CrashGuard
{
    // Soft cap: when the log passes MaxLogBytes it is trimmed back to roughly the last TrimTailBytes
    // (advanced to a whole entry) before the next append.
    private const long MaxLogBytes = 512 * 1024;
    private const int TrimTailBytes = 256 * 1024;
    private const string Delimiter = "====";

    /// <summary>Hooks unhandled-exception sources that can fire outside a wrapped <c>Main</c>.</summary>
    public static void Install(string productName)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Write(productName, ex);
        };
        TaskScheduler.UnobservedTaskException += (_, e) => Write(productName, e.Exception);
    }

    /// <summary>Install + run <paramref name="main"/>, logging then rethrowing any crash.</summary>
    public static void Run(string productName, Action main)
    {
        Install(productName);
        try
        {
            main();
        }
        catch (Exception ex)
        {
            Write(productName, ex);
            throw;
        }
    }

    public static void Write(string productName, Exception ex)
        => WriteTo([AppContext.BaseDirectory, Path.GetTempPath()], productName, ex);

    internal static void WriteTo(IEnumerable<string> directories, string productName, Exception ex)
    {
        string entry = FormatEntry(productName, ex);
        foreach (string dir in directories)
        {
            try
            {
                string path = Path.Combine(dir, productName + "-crash.log");
                TrimIfOversized(path);
                File.AppendAllText(path, entry);
            }
            catch { /* best effort */ }
        }
    }

    /// <summary>Formats one crash entry: a delimited, timestamped header, an environment block, then the
    /// full exception. Pure (no disk access), so it is unit-testable.</summary>
    internal static string FormatEntry(string productName, Exception ex)
    {
        var sb = new StringBuilder();
        sb.Append(Delimiter).Append(' ').Append(productName).Append(" crash @ ")
          .Append($"{DateTimeOffset.Now:O}").Append(' ').Append(Delimiter).Append(Environment.NewLine);
        sb.Append("App:     ").Append(AppVersion()).Append(Environment.NewLine);
        sb.Append("OS:      ")
          .Append(Safe(() => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})"))
          .Append(Environment.NewLine);
        sb.Append("Runtime: ").Append(Safe(() => RuntimeInformation.FrameworkDescription)).Append(Environment.NewLine);
        sb.Append("----").Append(Environment.NewLine);
        sb.Append(ex).Append(Environment.NewLine).Append(Environment.NewLine);
        return sb.ToString();
    }

    private static string AppVersion() => Safe(() =>
    {
        var asm = Assembly.GetEntryAssembly();
        if (asm is null) return "unknown";
        string version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? asm.GetName().Version?.ToString()
                         ?? "unknown";
        string? name = asm.GetName().Name;
        return name is null ? version : $"{name} {version}";
    });

    private static string Safe(Func<string> f)
    {
        try { return f() ?? "unknown"; }
        catch { return "unknown"; }
    }

    /// <summary>If the log has grown past the soft cap, keep roughly the tail (advanced to a whole entry)
    /// so it can't grow without bound. Best-effort — a failure here must not prevent logging.</summary>
    private static void TrimIfOversized(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length <= MaxLogBytes) return;

            string all = File.ReadAllText(path);
            int keepFrom = Math.Max(0, all.Length - TrimTailBytes);
            int delim = all.IndexOf(Delimiter, keepFrom, StringComparison.Ordinal);
            string tail = delim >= 0 ? all[delim..] : all[keepFrom..];
            File.WriteAllText(path, tail);
        }
        catch { /* best effort */ }
    }
}
