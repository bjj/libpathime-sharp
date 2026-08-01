#!/usr/bin/env bash
# Stage a local libpathime CMake install into this repo's package layouts.
# Linux counterpart of stage-native.ps1; see that file for the layout notes.
#
# Usage: scripts/stage-native.sh <cmake-install-prefix> [targets...]
#        targets default to: nuget unity tests
set -euo pipefail

prefix="${1:?usage: stage-native.sh <cmake-install-prefix> [nuget|unity|tests ...]}"
shift
if [ $# -eq 0 ]; then
    targets=(nuget unity tests)
else
    targets=("$@")
fi

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
rid=linux-x64

lib_dir="$prefix/lib"
data_dir="$lib_dir/pathime-data"
vendored_dir="$lib_dir/pathime"

ls "$lib_dir"/libpathime.so* >/dev/null 2>&1 || {
    echo "error: no libpathime.so under '$lib_dir' — point at a cmake --install prefix" >&2; exit 1; }
[ -d "$data_dir" ] || {
    echo "error: no pathime-data under '$lib_dir' — a bare build tree stages no dictionaries; run 'cmake --install'" >&2; exit 1; }

# One real file per SONAME, all flat: every library carries RUNPATH $ORIGIN
# and names its siblings by soname, so a single directory resolves the whole
# closure. cp -L dereferences the soname symlink into a real file under the
# soname's name (packages cannot carry symlinks); the fully-versioned real
# names and the dev symlinks stay behind. libpathime.so ships too, as a real
# file: DllImport default probing tries only that name, never the soname.
copy_libs() { # dest
    local dest="$1" name
    cp -L "$lib_dir/libpathime.so" "$dest/libpathime.so"
    for dir in "$lib_dir" "$vendored_dir"; do
        [ -d "$dir" ] || continue
        for f in "$dir"/*.so.*; do
            name="$(basename "$f")"
            case "$name" in
                *.so.*.*) continue ;; # fully-versioned; its soname link covers it
            esac
            cp -L "$f" "$dest/$name"
        done
    done
}

stage_flat() { # dest data_name
    local dest="$1" data_name="$2"
    rm -rf "$dest"
    mkdir -p "$dest"
    copy_libs "$dest"
    cp -r "$data_dir" "$dest/$data_name"
    echo "Staged from $prefix at $(date -Iseconds)" > "$dest/STAGED.txt"
    echo "  $dest"
}

for target in "${targets[@]}"; do
    case "$target" in
        nuget)
            root="$repo_root/artifacts/nuget/$rid"
            rm -rf "$root"
            mkdir -p "$root/native" "$root/data"
            copy_libs "$root/native"
            cp -r "$data_dir" "$root/data/pathime-data"
            echo "  $root"
            ;;
        unity)
            stage_flat "$repo_root/unity/com.ben.pathime/Plugins/Linux/x86_64" "pathime-data~"
            ;;
        tests)
            stage_flat "$repo_root/artifacts/native/$rid" "pathime-data"
            ;;
        *)
            echo "unknown target: $target" >&2; exit 1
            ;;
    esac
done

echo
echo "To use this build directly:"
echo "  export PATHIME_LIBRARY=$lib_dir/libpathime.so"
