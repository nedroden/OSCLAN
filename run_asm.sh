#!/usr/bin/env bash

set -e

TARGET=$1

if [ -z "$TARGET" ]; then
    echo "Usage: $0 <target>"
    exit 1
fi

as -o "$TARGET.o" "$TARGET.s" -arch arm64
ld -arch arm64 "$TARGET.o" -o "$TARGET"
"./$TARGET"