package util

import "strings"

func Mangle(input string) string {
	return strings.ReplaceAll(strings.ToLower(input), "-", "")
}
