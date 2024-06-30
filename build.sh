#!/usr/bin/env bash

set -e

echo "[ ] Building compiler..."
go build -C Compiler -o ../neocc cmd/cli/main.go
echo "Done"

echo "[ ] Building analyzer..."
dotnet build Neoc.OutputAnalyzer
echo "Done"