#!/usr/bin/env bash
# Every version-bearing file agrees, or this fails.
#
# The release workflow overrides versions at pack time from the tag, which
# means the files below can drift without anything noticing — except Unity
# git-URL consumers, who receive exactly what package.json says. So: CI runs
# this with no argument (files agree with each other); the release runs it
# with the tag (files agree with the tag, and the libpathime submodule is
# pinned at the core release of the same name — the repos release in
# lockstep, libpathime's RELEASING.md has the order).
#
# Usage: scripts/check-release-version.sh [vX.Y.Z]
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"

fail=0
complain() { echo "check-release-version: $*" >&2; fail=1; }

csproj=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' \
    src/PathimeSharp/PathimeSharp.csproj)
nuspec_win=$(sed -n 's/.*<version>\(.*\)<\/version>.*/\1/p' \
    packaging/PathimeSharp.NativeAssets.win-x64/PathimeSharp.NativeAssets.win-x64.nuspec)
nuspec_linux=$(sed -n 's/.*<version>\(.*\)<\/version>.*/\1/p' \
    packaging/PathimeSharp.NativeAssets.linux-x64/PathimeSharp.NativeAssets.linux-x64.nuspec)
unity=$(sed -n 's/.*"version": *"\([^"]*\)".*/\1/p' \
    unity/com.ben.pathime/package.json)

[ -n "$csproj" ] || complain "no <Version> found in PathimeSharp.csproj"
echo "PathimeSharp.csproj:  $csproj"
echo "win-x64 nuspec:       $nuspec_win"
echo "linux-x64 nuspec:     $nuspec_linux"
echo "Unity package.json:   $unity"

for v in "$nuspec_win" "$nuspec_linux" "$unity"; do
    [ "$v" = "$csproj" ] || complain "version-bearing files disagree"
done

if [ $# -ge 1 ]; then
    tag="$1"
    [ "v$csproj" = "$tag" ] || complain "tag $tag but the files say $csproj"

    pin=$(git rev-parse HEAD:libpathime)
    # ls-remote needs no submodule checkout; the ^{} line is the peeled
    # commit when the tag is annotated, absent when it is lightweight.
    remote=https://github.com/bjj/libpathime.git
    # The ^{} line sorts after the plain ref, so the last line is the
    # peeled commit for an annotated tag and the ref itself for a
    # lightweight one — a commit either way.
    want=$(git ls-remote "$remote" "refs/tags/$tag" "refs/tags/$tag^{}" \
        | awk 'END {print $1}')
    [ -n "$want" ] || complain "libpathime has no tag $tag"
    if [ -n "$want" ] && [ "$pin" != "$want" ]; then
        complain "submodule pinned at $pin but libpathime $tag is $want"
    fi
fi

exit $fail
