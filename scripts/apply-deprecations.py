#!/usr/bin/env python3
"""Apply deprecation notices to a previous version's pages in docs-content.

Reads deprecations.yaml from the CLI repo and modifies the target version's
MDX files in a local clone of docs-content by prepending warning banners.
"""
import argparse
import os
import re
import yaml


def load_deprecations(filepath):
    with open(filepath, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


def add_warning_to_page(filepath, message, redirect_to=None, new_version_base=None):
    if not os.path.isfile(filepath):
        print(f"  SKIP (not found): {filepath}")
        return False

    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    if "<Warning>" in content:
        print(f"  SKIP (already has warning): {filepath}")
        return False

    match = re.match(r'^(---\n.*?\n---\n)', content, re.DOTALL)
    if not match:
        print(f"  SKIP (no frontmatter): {filepath}")
        return False

    frontmatter = match.group(1)
    body = content[match.end():]

    warning_block = f"\n<Warning>\n  {message}"
    if redirect_to and new_version_base:
        redirect_url = f"{new_version_base}/{redirect_to}"
        warning_block += f"\n\n  See [{redirect_to.split('/')[-1]}]({redirect_url}) for the replacement command."
    warning_block += "\n</Warning>\n"

    new_content = frontmatter + warning_block + body

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(new_content)

    print(f"  UPDATED: {filepath}")
    return True


def main():
    parser = argparse.ArgumentParser(description="Apply deprecation notices to docs-content pages")
    parser.add_argument("--deprecations", required=True, help="Path to deprecations.yaml")
    parser.add_argument("--docs-content", required=True, help="Path to local docs-content clone")
    parser.add_argument("--namespace", default="services-cli", help="Namespace in docs-content")
    parser.add_argument("--new-version", dest="new_version", help="Override new_version from deprecations.yaml (use the workflow's resolved version)")
    args = parser.parse_args()

    config = load_deprecations(args.deprecations)
    target_version = config["target_version"]
    new_version = args.new_version or config["new_version"]
    new_version_base = f"/{args.namespace}/{new_version}"

    base_path = os.path.join(args.docs_content, "content", args.namespace, target_version, "en-US", "pages")

    if not os.path.isdir(base_path):
        print(f"ERROR: Target version directory not found: {base_path}")
        return 1

    updated_count = 0

    for entry in config["entries"]:
        page_path = entry["path"]
        action = entry["action"]
        message = entry["message"]
        redirect_to = entry.get("redirect_to")
        apply_to_all = entry.get("apply_to_all", False)

        print(f"\nProcessing: {page_path} ({action})")

        if apply_to_all:
            target_dir = os.path.join(base_path, page_path)
            if not os.path.isdir(target_dir):
                print(f"  SKIP (directory not found): {target_dir}")
                continue
            for root, _, files in os.walk(target_dir):
                for f in files:
                    if f.endswith(".mdx"):
                        filepath = os.path.join(root, f)
                        if add_warning_to_page(filepath, message, redirect_to, new_version_base):
                            updated_count += 1
        else:
            filepath = os.path.join(base_path, page_path + ".mdx")
            if not os.path.isfile(filepath):
                filepath = os.path.join(base_path, page_path, "_index.mdx")
            if add_warning_to_page(filepath, message, redirect_to, new_version_base):
                updated_count += 1

    print(f"\nDone. Updated {updated_count} pages in {target_version}.")
    return 0


if __name__ == "__main__":
    exit(main())
