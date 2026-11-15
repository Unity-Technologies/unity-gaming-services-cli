#!/usr/bin/env python3
"""Build a UDP documentation bundle from pre-formatted MDX sources.

Sources in docs/ugs-cli/latest/ are already in UDP-compatible format.
This script copies them into the bundle layout, generates versioned sidebars
and metadata files, and applies build-time link fixes.

Usage:
  python3 scripts/build-docs.py \
    --input docs/ugs-cli/latest \
    --output build/udp-bundle \
    --version 2.0.0-exp.6 \
    --exclude-staging
"""
import argparse
import glob
import os
import re
import shutil
import sys
import yaml


DIRECTORY_DISPLAY_NAMES = {
    "dlq": "DLQ",
    "ci-cd-pipeline-usage": "CI/CD Pipeline Usage",
}


def load_services_config(config_path):
    with open(config_path, "r", encoding="utf-8") as f:
        data = yaml.safe_load(f)
    production = []
    staging_only = set()
    display_names = {}
    for entry in data["services"]:
        name = entry["name"]
        display_names[name] = entry.get("display_name", name.replace("-", " ").title())
        if entry.get("staging_only", False):
            staging_only.add(name)
        else:
            production.append(name)
    return production, staging_only, display_names


def get_title_from_mdx(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    match = re.match(r'^---\n(.*?)\n---\n', content, re.DOTALL)
    if match:
        fm = yaml.safe_load(match.group(1)) or {}
        return fm.get("title", os.path.splitext(os.path.basename(filepath))[0].replace("-", " ").title())
    return os.path.splitext(os.path.basename(filepath))[0].replace("-", " ").title()


def is_unlisted(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    match = re.match(r'^---\n(.*?)\n---\n', content, re.DOTALL)
    if match:
        fm = yaml.safe_load(match.group(1)) or {}
        return fm.get("unlisted", False)
    return False


def pages_missing_required_front_matter(pages_dir):
    missing = []
    for filepath in sorted(glob.glob(os.path.join(pages_dir, "**", "*.mdx"), recursive=True)):
        with open(filepath, "r", encoding="utf-8") as f:
            match = re.match(r'^---\n(.*?)\n---\n', f.read(), re.DOTALL)
        fm = (yaml.safe_load(match.group(1)) if match else None) or {}
        if not fm.get("title") or not fm.get("description"):
            missing.append(os.path.relpath(filepath, pages_dir))
    return missing


DOCS_DOMAIN_URL = re.compile(r'https?://docs\.unity\.com\S*')


def pages_linking_to_docs_domain(pages_dir):
    found = []
    for filepath in sorted(glob.glob(os.path.join(pages_dir, "**", "*.mdx"), recursive=True)):
        with open(filepath, "r", encoding="utf-8") as f:
            for number, line in enumerate(f, start=1):
                for url in DOCS_DOMAIN_URL.findall(line):
                    found.append(f"{os.path.relpath(filepath, pages_dir)}:{number}: {url}")
    return found


def build_sidebar_items(dir_path, url_prefix):
    items = []
    entries = sorted(os.listdir(dir_path))
    mdx_files = [e for e in entries if e.endswith(".mdx") and e != "_index.mdx"]
    subdirs = [e for e in entries if os.path.isdir(os.path.join(dir_path, e))]

    for mdx in mdx_files:
        mdx_path = os.path.join(dir_path, mdx)
        if is_unlisted(mdx_path):
            continue
        title = get_title_from_mdx(mdx_path)
        slug = os.path.splitext(mdx)[0]
        items.append({"title": title, "href": f"{url_prefix}/{slug}"})

    for subdir in subdirs:
        sub_items = build_sidebar_items(os.path.join(dir_path, subdir), f"{url_prefix}/{subdir}")
        if sub_items:
            label = DIRECTORY_DISPLAY_NAMES.get(subdir, subdir.replace("-", " ").title())
            items.append({"title": label, "items": sub_items})

    return items


def generate_service_sidebar(service_dir, service_name, version, display_names):
    display_name = display_names.get(service_name, service_name.replace("-", " ").title())
    url_base = f"/services-cli/{version}/manual/{service_name}"
    items = [{"title": "Overview", "href": url_base}]
    items.extend(build_sidebar_items(service_dir, url_base))

    return {
        "title": display_name,
        "parentLink": {"title": "Unity Gaming Services CLI", "href": f"/services-cli/{version}"},
        "sections": [{"title": display_name, "items": items}],
    }


def generate_root_sidebar(pages_dir, version, display_names):
    url_base = f"/services-cli/{version}"
    root_items = [{"title": "Overview", "href": f"{url_base}/"}]
    if os.path.isfile(os.path.join(pages_dir, "whats-new.mdx")):
        root_items.append({"title": f"What's New in {version}", "href": f"{url_base}/whats-new"})
    sections = [{"title": "Unity Gaming Services CLI", "items": root_items}]

    general_dir = os.path.join(pages_dir, "manual", "general")
    for subsection in ["get-started", "base-commands", "deployment-definition", "samples", "troubleshooting"]:
        sub_dir = os.path.join(general_dir, subsection)
        if os.path.isdir(sub_dir):
            subsection_url = f"{url_base}/manual/general/{subsection}"
            items = build_sidebar_items(sub_dir, subsection_url)
            if os.path.exists(os.path.join(sub_dir, "_index.mdx")):
                items.insert(0, {"title": "Overview", "href": subsection_url})
            if items:
                label = DIRECTORY_DISPLAY_NAMES.get(subsection, subsection.replace("-", " ").title())
                sections.append({"title": label, "items": items})

    service_items = []
    manual_dir = os.path.join(pages_dir, "manual")
    for entry in sorted(os.listdir(manual_dir)):
        if entry == "general":
            continue
        if os.path.isdir(os.path.join(manual_dir, entry)):
            display_name = display_names.get(entry, entry.replace("-", " ").title())
            service_items.append({"title": display_name, "href": f"{url_base}/manual/{entry}"})

    if service_items:
        sections.append({"title": "CLI Services Documentation", "items": service_items})

    return {"title": "Unity Gaming Services CLI", "sections": sections}


def copy_sources(input_dir, pages_dir, services):
    """Copy source files into the UDP bundle layout."""
    # Root _index.mdx — fix relative links (sources say general/..., output needs ./manual/general/...)
    root_index = os.path.join(input_dir, "_index.mdx")
    if os.path.isfile(root_index):
        with open(root_index, "r", encoding="utf-8") as f:
            content = f.read()
        content = content.replace('href="general/', 'href="./manual/general/')
        with open(os.path.join(pages_dir, "_index.mdx"), "w", encoding="utf-8") as f:
            f.write(content)

    # Root What's New page
    whats_new = os.path.join(input_dir, "whats-new.mdx")
    if os.path.isfile(whats_new):
        shutil.copy2(whats_new, os.path.join(pages_dir, "whats-new.mdx"))

    # General section → manual/general/
    general_src = os.path.join(input_dir, "general")
    general_dst = os.path.join(pages_dir, "manual", "general")
    if os.path.isdir(general_src):
        shutil.copytree(general_src, general_dst, dirs_exist_ok=True,
                        ignore=shutil.ignore_patterns("*.js", "__pycache__"))

    # Services → manual/{service}/
    for service in services:
        src = os.path.join(input_dir, service)
        if os.path.isdir(src):
            dst = os.path.join(pages_dir, "manual", service)
            shutil.copytree(src, dst, dirs_exist_ok=True,
                            ignore=shutil.ignore_patterns("*.js", "__pycache__"))


def rewrite_versioned_links(pages_dir, version):
    """Rewrite /guides/ugs-cli/... absolute links to /services-cli/{version}/manual/..."""
    for root, _, files in os.walk(pages_dir):
        for f in files:
            if not f.endswith(".mdx"):
                continue
            filepath = os.path.join(root, f)
            with open(filepath, "r", encoding="utf-8") as fh:
                content = fh.read()

            def rewrite(match):
                prefix = match.group(1)
                path = match.group(2)
                return f"{prefix}/services-cli/{version}/manual/{path}"

            new_content = re.sub(r'(\[.*?\]\()\s*/guides/ugs-cli/[^/]+/(.*?\))', rewrite, content)
            new_content = re.sub(r'(\]: )/guides/ugs-cli/[^/]+/(.*)', rewrite, new_content)

            if new_content != content:
                with open(filepath, "w", encoding="utf-8") as fh:
                    fh.write(new_content)


def fix_relative_links(pages_dir):
    """Fix relative links that don't resolve to actual files in the output tree."""
    for root, _, files in os.walk(pages_dir):
        for f in files:
            if not f.endswith(".mdx"):
                continue
            filepath = os.path.join(root, f)
            with open(filepath, "r", encoding="utf-8") as fh:
                content = fh.read()
            changed = False

            def try_fix(match):
                nonlocal changed
                prefix = match.group(1)
                target = match.group(2)
                anchor = ""
                if "#" in target:
                    target, anchor = target.split("#", 1)
                    anchor = "#" + anchor
                if target.startswith("http") or target.startswith("/"):
                    return match.group(0)
                resolved = os.path.normpath(os.path.join(os.path.dirname(filepath), target))
                if os.path.exists(resolved + ".mdx") or os.path.exists(os.path.join(resolved, "_index.mdx")):
                    return match.group(0)
                basename = os.path.basename(target)
                for alt in [
                    target.replace("../", "./", 1),
                    re.sub(r'^\.\./', '', target),
                    "../" + target.lstrip("./"),
                    "../../" + basename,
                    "../../../" + basename,
                ]:
                    alt_resolved = os.path.normpath(os.path.join(os.path.dirname(filepath), alt))
                    if os.path.exists(alt_resolved + ".mdx") or os.path.exists(os.path.join(alt_resolved, "_index.mdx")):
                        changed = True
                        return prefix + alt + anchor
                return match.group(0)

            new_content = re.sub(r'(\]\()(\.\./[^\)]+)', try_fix, content)
            new_content = re.sub(r'(\]:\s+)(\.\./\S+)', try_fix, new_content)
            if changed:
                with open(filepath, "w", encoding="utf-8") as fh:
                    fh.write(new_content)


REFERENCE_DEFINITION = re.compile(r'^[ \t]*\[([^\]]+)\]:[ \t]')
REFERENCE_USE = re.compile(r'(?<!\[)\[([^\[\]]+)\](?!\(|:|\[)')
INLINE_CODE = re.compile(r'(`[^`]*`)')


def prose_line_flags(lines):
    """True for each line outside a fenced code block."""
    flags = []
    in_code = False
    for line in lines:
        if line.startswith("```"):
            in_code = not in_code
            flags.append(False)
        else:
            flags.append(not in_code)
    return flags


def outside_inline_code(line, transform):
    """Apply transform to the parts of a line that are not inline code spans."""
    parts = INLINE_CODE.split(line)
    return "".join(part if i % 2 else transform(part) for i, part in enumerate(parts))


def is_unused_definition(line, used_refs):
    match = REFERENCE_DEFINITION.match(line)
    return match is not None and match.group(1) not in used_refs


def remove_unused_definitions(lines, prose, used_refs):
    """Replace each unused definition, with the blank lines directly above it, by one blank line."""
    kept = []
    original_blanks = 0
    for line, is_prose in zip(lines, prose):
        if is_prose and is_unused_definition(line, used_refs):
            del kept[len(kept) - original_blanks:]
            kept.append("")
            original_blanks = 0
            continue
        kept.append(line)
        original_blanks = original_blanks + 1 if line.strip() == "" else 0
    return kept


def apply_build_time_fixes(pages_dir):
    """Orphan bracket escaping and unused reference cleanup, skipping code blocks."""
    for root, _, files in os.walk(pages_dir):
        for f in files:
            if not f.endswith(".mdx"):
                continue
            filepath = os.path.join(root, f)
            with open(filepath, "r", encoding="utf-8") as fh:
                content = fh.read()

            # Code blocks and inline code are left untouched by every pass below:
            # lines such as "[Error]:" in sample output are not reference definitions.
            lines = content.split("\n")
            prose = prose_line_flags(lines)

            # Collect defined references
            defined_refs = set()
            for line, is_prose in zip(lines, prose):
                ref_match = REFERENCE_DEFINITION.match(line)
                if is_prose and ref_match:
                    defined_refs.add(ref_match.group(1))

            def escape_orphan_ref(match):
                if match.group(1) in defined_refs:
                    return match.group(0)
                return f"\\[{match.group(1)}\\]"

            lines = [
                outside_inline_code(line, lambda text: REFERENCE_USE.sub(escape_orphan_ref, text))
                if is_prose else line
                for line, is_prose in zip(lines, prose)]

            # Remove unused reference definitions
            used_refs = set()
            for line, is_prose in zip(lines, prose):
                if not is_prose:
                    continue
                for ref_match in REFERENCE_USE.finditer(line):
                    text = ref_match.group(1)
                    if not text.startswith("\\"):
                        used_refs.add(text)
            lines = remove_unused_definitions(lines, prose, used_refs)
            new_content = "\n".join(lines)

            if not new_content.endswith("\n"):
                new_content += "\n"

            if new_content != content:
                with open(filepath, "w", encoding="utf-8") as fh:
                    fh.write(new_content)


def main():
    parser = argparse.ArgumentParser(description="Build UDP documentation bundle from pre-formatted sources")
    parser.add_argument("--input", required=True, help="Path to docs/ugs-cli/latest/")
    parser.add_argument("--output", required=True, help="Output directory for UDP bundle")
    parser.add_argument("--version", required=True, help="Version string (e.g. 2.0.0-exp.6)")
    parser.add_argument("--exclude-staging", action="store_true", help="Exclude staging-only services")
    parser.add_argument("--services", help="Path to services.yaml (default: <input>/../services.yaml)")
    args = parser.parse_args()

    input_dir = args.input
    output_dir = args.output
    version = args.version

    if os.path.exists(output_dir):
        shutil.rmtree(output_dir)

    pages_dir = os.path.join(output_dir, "en-US", "pages")
    os.makedirs(pages_dir, exist_ok=True)

    # Load services config
    services_path = args.services or os.path.join(os.path.dirname(input_dir), "services.yaml")
    production, staging_only, display_names = load_services_config(services_path)

    services = production[:]
    if not args.exclude_staging:
        services.extend(sorted(staging_only))

    # Copy sources into bundle
    copy_sources(input_dir, pages_dir, services)

    # Generate metadata
    with open(os.path.join(output_dir, "_version.yaml"), "w") as f:
        yaml.dump({
            "title": f"v{version.split('.')[0]}.{version.split('.')[1]}",
            "versionNumber": version,
            "status": "supported",
        }, f, default_flow_style=False, sort_keys=False)

    with open(os.path.join(pages_dir, "_topnav.yaml"), "w") as f:
        yaml.dump({
            "title": "Unity Gaming Services CLI",
            "call-to-action": {
                "title": "GitHub Repository",
                "href": "https://github.com/Unity-Technologies/unity-gaming-services-cli",
            },
        }, f, default_flow_style=False, sort_keys=False)

    # Generate sidebars
    for service in services:
        service_dir = os.path.join(pages_dir, "manual", service)
        if os.path.isdir(service_dir):
            sidebar = generate_service_sidebar(service_dir, service, version, display_names)
            with open(os.path.join(service_dir, "_sidebar.yaml"), "w") as f:
                yaml.dump(sidebar, f, default_flow_style=False, sort_keys=False, allow_unicode=True)

    root_sidebar = generate_root_sidebar(pages_dir, version, display_names)
    with open(os.path.join(pages_dir, "_sidebar.yaml"), "w") as f:
        yaml.dump(root_sidebar, f, default_flow_style=False, sort_keys=False)

    # Build-time link fixes
    rewrite_versioned_links(pages_dir, version)
    fix_relative_links(pages_dir)
    apply_build_time_fixes(pages_dir)

    missing = pages_missing_required_front_matter(pages_dir)
    if missing:
        sys.exit("ERROR: pages missing a title or description in front matter "
                 "(required by the docs-content lint):\n  " + "\n  ".join(missing))

    docs_domain_links = pages_linking_to_docs_domain(pages_dir)
    if docs_domain_links:
        sys.exit("ERROR: links to docs.unity.com must be site paths (e.g. /cloud/automation), "
                 "as required by the docs-content lint:\n  " + "\n  ".join(docs_domain_links))

    print(f"UDP bundle generated at {output_dir}")
    print(f"  Version: {version}")
    print(f"  Services: {len(services)}")


if __name__ == "__main__":
    main()
