#r "System.Xml.Linq"
#r "System.Linq"

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// doc-sync-gate.csx — Semantic documentation gatekeeper (commit-msg hook + CI).
//
// Runs INSIDE the commit-msg hook — the message file already exists, so we can:
//   1. Diff the staged commit against its parent
//   2. Classify changes: pure renames (safe) vs semantic (need docs)
//   3. If semantic change + no doc file touched → BLOCK
//   4. If override-no-doc: <reason> is in the message → ALLOW with audit trail
//
// For CI mode (no commit-msg file), pass --args <base-ref> to diff against a branch.
//
// Exit codes:
//   0 = docs in sync or override present
//   1 = semantic change without docs AND no override
//   2 = error

string commitMsgFile = null;
string baseRef = "origin/main";
int exitCode = 0;

try
{
    // --- Mode detection: commit-msg hook vs CI ---
    // In commit-msg hook: first arg is the path to the commit-message file (e.g. COMMIT_EDITMSG)
    // In CI mode: first arg is a git ref to diff against (no commit-msg file at all).
    if (Args.Count > 0)
    {
        var arg = Args[0];
        // Heuristic: commit-msg file paths end in .git/<name> or contain COMMIT_EDITMSG
        if (arg.Contains("COMMIT_EDITMSG") || arg.Contains(".git" + Path.DirectorySeparatorChar)
            || arg.Contains(".git/"))
            commitMsgFile = arg;
        else
            baseRef = arg;
    }

    // Read the commit message if we're in commit-msg mode
    string commitMsg = commitMsgFile != null ? ReadCommitMessage(commitMsgFile) : null;

    // Override detection: "override-no-doc: <reason>" anywhere in the message
    string overrideReason = null;
    if (commitMsg != null)
    {
        var match = Regex.Match(commitMsg, @"(?i)override-no-doc:\s*(.+?)(?:\r?\n|$)",
            RegexOptions.Multiline);
        if (match.Success)
            overrideReason = match.Groups[1].Value.Trim();
    }

    // --- Resolve what we're diffing ---
    string diffSpec;
    if (commitMsgFile != null)
    {
        // commit-msg hook: commit hasn't happened yet — staged changes are in the index.
        // Diff the index vs HEAD to see exactly what this commit will introduce.
        diffSpec = "--cached HEAD";
    }
    else
    {
        // CI or standalone: diff against a ref (e.g. origin/main)
        diffSpec = baseRef + " HEAD";
    }

    Console.WriteLine($"doc-sync-gate: diffing {diffSpec} (mode: {(commitMsgFile != null ? "commit-msg" : "CI")})");

    // --- Use -M to find renames; raw file list for diff ---
    string rawDiff = RunGit($"diff -M --name-status {diffSpec}");

    var renames = new List<(string oldPath, string newPath)>();
    var addedFiles = new List<string>();
    var deletedFiles = new List<string>();
    var modifiedFiles = new List<string>();

    foreach (var line in rawDiff.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) continue;
        var status = parts[0].Trim();

        if (status.StartsWith("R") && parts.Length >= 3)
        {
            // Renamed: old -> new (or R100, R050, etc. for rename similarity %)
            renames.Add((parts[1].Trim(), parts[2].Trim()));
        }
        else if (status == "A") addedFiles.Add(parts[1].Trim());
        else if (status == "D") deletedFiles.Add(parts[1].Trim());
        else if (status == "M") modifiedFiles.Add(parts[1].Trim());
    }

    Console.WriteLine($"  renames:  {renames.Count}");
    Console.WriteLine($"  added:    {addedFiles.Count}");
    Console.WriteLine($"  modified: {modifiedFiles.Count}");
    Console.WriteLine($"  deleted:  {deletedFiles.Count}");

    if (renames.Count == 0 && addedFiles.Count == 0 && modifiedFiles.Count == 0 && deletedFiles.Count == 0)
    {
        Console.WriteLine("doc-sync-gate: OK — no changes detected.");
        return 0;
    }

    // --- Semantic impact detection ---
    // Pure renames are SAFE: they don't change behavior, only file location.
    // To detect renames with co-changes (renamed + modified), we check if the NEW
    // path appears in modifiedFiles OR if numstat shows actual content changes.
    var renamesWithChanges = renames.Where(r =>
    {
        // If the new path appears in modified or added, something changed beyond the rename
        return modifiedFiles.Contains(r.newPath) || addedFiles.Contains(r.newPath);
    }).ToList();

    var pureRenames = renames.Except(renamesWithChanges).ToList();

    Console.WriteLine($"  pure renames (safe):    {pureRenames.Count}");
    Console.WriteLine($"  renames with changes:   {renamesWithChanges.Count}");

    // Semantic files = added + modified + renames-with-changes + deleted (deletions can also break API)
    var semantic = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var f in addedFiles) semantic.Add(f);
    foreach (var f in modifiedFiles) semantic.Add(f);
    foreach (var r in renamesWithChanges) semantic.Add(r.newPath);
    // Deletions matter if they're source code (can break public API)
    foreach (var f in deletedFiles) semantic.Add(f);

    // --- Classify into pockets ---
    bool HasImpact(string f) =>
        f.StartsWith("src/") || f.StartsWith("test/") ||
        f.EndsWith(".csproj") || f.EndsWith(".sln") || f.EndsWith(".props") || f.EndsWith(".targets");

    bool IsDocPocket(string f) =>
        f == "CONTEXT.md" || f.StartsWith("docs/") || f.StartsWith("openspec/");

    var codeFiles = semantic.Where(HasImpact).ToList();
    var docFiles = semantic.Where(IsDocPocket).ToList();
    var otherFiles = semantic.Where(f => !HasImpact(f) && !IsDocPocket(f)).ToList();

    // Core architecture patterns
    var archPatterns = new[] { "Optimizer", "Transformer", "CodeGenerator", "Parser", "Frontend", "Slab" };
    var archFiles = codeFiles.Where(f =>
        archPatterns.Any(p => Path.GetFileNameWithoutExtension(f).Contains(p, StringComparison.OrdinalIgnoreCase))
    ).ToList();

    Console.WriteLine($"  semantic code/test:  {codeFiles.Count}");
    Console.WriteLine($"  doc pocket files:   {docFiles.Count}");
    Console.WriteLine($"  other semantic:     {otherFiles.Count}");
    if (archFiles.Count > 0)
        Console.WriteLine($"  core architecture:  {archFiles.Count}");

    // --- Enforcement ---
    var errors = new List<string>();

    // Rule 1: Semantic code/config change + no doc update + no override
    if (codeFiles.Count > 0 && docFiles.Count == 0 && overrideReason == null)
    {
        var sample = codeFiles.Take(10).Select(f => "    - " + f);
        errors.Add(
            "Semantic code/config change without a documentation update.\n" +
            "  The following files need a doc pocket change (see docs/DOC-IMPACT.md):\n" +
            string.Join("\n", sample));
    }

    // Rule 2: Core architecture file changed + no CONTEXT.md or ADR + no override
    if (archFiles.Count > 0 && overrideReason == null)
    {
        var archDocs = docFiles.Any(f => f == "CONTEXT.md" || f.StartsWith("docs/adr/"));
        if (!archDocs)
        {
            var sample = archFiles.Take(10).Select(f => "    - " + f);
            errors.Add(
                "Core architecture file changed without CONTEXT.md or docs/adr/ update.\n" +
                "  Affected files:\n" +
                string.Join("\n", sample));
        }
    }

    // --- Result ---
    if (errors.Count > 0 && overrideReason == null)
    {
        Console.WriteLine();
        foreach (var e in errors)
            Console.WriteLine("doc-sync-gate: " + e.Replace("\n", "\n  "));
        Console.WriteLine();
        Console.WriteLine("  To fix: add the documentation update, then recommit.");
        Console.WriteLine("  To override: 'git commit --amend' then add 'override-no-doc: <reason>' to the message.");
        Console.WriteLine("             or 'git commit -m \"your message\\n\\noverride-no-doc: <reason>\"'.");
        exitCode = 1;
    }
    else if (overrideReason != null)
    {
        Console.WriteLine($"doc-sync-gate: PASS with override — reason: '{overrideReason}'");
        exitCode = 0;
    }
    else
    {
        Console.WriteLine("doc-sync-gate: PASS — all semantic changes have doc pocket updates.");
        exitCode = 0;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"doc-sync-gate: ERROR — {ex.Message}");
    exitCode = 2;
}

return exitCode;

// ===== Helpers =====

string ReadCommitMessage(string path)
{
    // Git passes COMMIT_EDITMSG path; we read and strip comments
    if (!File.Exists(path)) return "";
    var raw = File.ReadAllText(path);
    // Strip comment lines (lines starting with #)
    return string.Join("\n",
        raw.Split('\n').Where(l => !l.TrimStart().StartsWith("#")));
}

string RunGit(string args)
{
    var psi = new ProcessStartInfo("git", args)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    using var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException($"git {args} failed (exit {proc.ExitCode}): {stderr}");
    return stdout;
}
