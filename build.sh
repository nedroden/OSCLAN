#!/usr/bin/env bash

set -e

echo "[] Build compiler"
dotnet build

echo "[] Build runner"
go build -C Runner -o ../osclan runner.go

echo "Finished building project."