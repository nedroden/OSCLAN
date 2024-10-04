#!/usr/bin/env bash
set -e
dotnet clean
cp README.md Docs/index.md
docfx Docs/docfx.json