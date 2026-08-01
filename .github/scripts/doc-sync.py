#!/usr/bin/env python3
"""Doc-sync check for GameVM.

Given a list of changed files (from a git diff or PR), verifies that the
documentation which should accompany each change was also modified.

Reads the authoritative mapping from .github/doc-mapping.yaml.

Exit codes:
  0  - all changes are synced with docs (or no code changes)
  1  - code changes exist without corresponding doc updates
  2  - fatal: missing mapping file, no PyYAML, etc.
"""

import fnmatch
import os
import sys


def load_mapping(mapping_path):
    """Load the doc-mapping.yaml. Uses PyYAML if available, else a minimal fallback."""
    try:
        import yaml
        with open(mapping_path, "r", encoding="utf-8") as fh:
            return yaml.safe_load(fh)
    except ImportError:
        return _fallback_load(mapping_path)


def _fallback_load(mapping_path):
    """Minimal YAML reader for the specific structure of doc-mapping.yaml.

    Avoids a hard PyYAML dependency for environments where it's unavailable.
    Only supports the flat 'key:' / '- item' subset used by this file.
    """
    mapping = {}
    current_key = None
    current_list = None
    in_list = False
    with open(mapping_path, "r", encoding="utf-8") as fh:
        for raw in fh:
            line = raw.rstrip("\n")
            stripped = line.strip()
            if not stripped or stripped.startswith("#"):
                continue
            if stripped.startswith("- ") and in_list:
                current_list.append(stripped[2:].strip())
                continue
            if ":" in line and not line.startswith(" "):
                key = line.split(":", 1)[0].strip()
                current_key = key
                current_list = []
                mapping[key] = current_list
                in_list = True
                continue
            # nested key like 'code:' / 'docs:' under a rule
            if line.startswith("  ") and ":" in line:
                nested = line.split(":", 1)[0].strip()
                current_list = []
                mapping[f"{current_key}.{nested}"] = current_list
                in_list = True
                continue
    # Convert nested entries into a clean structure: {rule: {code: [...], docs: [...]}}
    rules = {}
    for key in list(mapping.keys()):
        if "." in key:
            rule, part = key.split(".", 1)
            rules.setdefault(rule, {})[part] = mapping[key]
    for key in list(mapping.keys()):
        if "." not in key and isinstance(mapping[key], list):
            rules.setdefault(key, {})["docs"] = mapping[key]
    rules["always_check"] = {"docs": mapping.get("always_check", [])}
    return rules


def collect_rules(mapping):
    """Normalize the mapping into a list of (code_patterns, doc_globs) and always_check globs."""
    rules = []
    always_check = []
    for rule, body in mapping.items():
        if rule == "always_check":
            if isinstance(body, list):
                always_check.extend(body)
            elif isinstance(body, dict):
                always_check.extend(body.get("docs", []))
            continue
        if not isinstance(body, dict):
            continue
        code = body.get("code", []) or []
        docs = body.get("docs", []) or []
        if docs:
            rules.append((code, docs))
    return rules, always_check


def match_any(path, patterns):
    for pat in patterns:
        pat = pat.rstrip("/")
        if pat.endswith("/") or os.path.isdir(os.path.join(".", pat)):
            if path.startswith(pat.rstrip("/") + "/") or fnmatch.fnmatch(path, pat + "*"):
                return True
        elif fnmatch.fnmatch(path, pat) or fnmatch.fnmatch(path, pat + "*"):
            return True
    return False


def main():
    mapping_path = os.path.join(os.path.dirname(__file__), "..", "doc-mapping.yaml")
    mapping_path = os.path.normpath(mapping_path)

    changed_files = [f for f in sys.argv[1:] if f.strip()]
    if not changed_files:
        print("doc-sync: no changed files provided - nothing to check.")
        return 0

    if not os.path.exists(mapping_path):
        print(f"doc-sync: ERROR mapping file not found: {mapping_path}", file=sys.stderr)
        return 2

    mapping = load_mapping(mapping_path)
    if not mapping:
        print("doc-sync: ERROR failed to parse mapping file.", file=sys.stderr)
        return 2

    rules, always_check = collect_rules(mapping)
    changed_set = set(changed_files)

    # Docs modified in this change set.
    modified_docs = {f for f in changed_set if f.startswith("docs/")}

    # Determine which rules fire based on changed code files.
    fired = []
    for code, docs in rules:
        if not code:
            continue
        triggered = [f for f in changed_set if match_any(f, code) and not f.startswith("docs/")]
        if triggered:
            fired.append((triggered, docs))

    if not fired:
        print("doc-sync: OK - no code changes require documentation updates.")
        return 0

    missing = []
    for triggered, docs in fired:
        for doc in docs:
            doc = doc.rstrip("/")
            if doc.endswith("/"):
                # Directory glob: any file under it counts.
                if not any(md.startswith(doc) and md in modified_docs for md in modified_docs):
                    missing.append((triggered, doc))
            else:
                if doc not in modified_docs:
                    missing.append((triggered, doc))

    # always_check docs must be reviewed if any code changed.
    for doc in always_check:
        doc = doc.rstrip("/")
        if doc.endswith("/"):
            if not any(md.startswith(doc) and md in modified_docs for md in modified_docs):
                missing.append((changed_files, doc))
        else:
            if doc not in modified_docs:
                missing.append((changed_files, doc))

    if not missing:
        print("doc-sync: OK - all code changes are accompanied by documentation updates.")
        return 0

    print("doc-sync: WARNING - the following code changes should include documentation updates:")
    for triggered, doc in missing:
        print(f"  - {doc}  (triggered by: {', '.join(triggered)})")
    print()
    print("Refer to .github/doc-mapping.yaml and AGENTS.md 'Documentation Update Rules'.")
    print("If this change genuinely has no documented behavior, add an explicit note to the PR.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
