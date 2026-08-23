#!/bin/zsh
set -eu

fail() {
    print -u2 -- "FAIL: $1"
    exit 1
}

script_dir="${0:A:h}"
repo_root="${script_dir:h}"
sdk_root="/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/.dotnet-sdk-x64"
dotnet="$sdk_root/dotnet"
original_app="/Users/andrewbowlus/Applications/Tanks Rebirth.app"
publish_dir="$repo_root/artifacts/local-coop/publish"
app_dir="$repo_root/artifacts/Tanks Rebirth Local Co-op.app"
contents_dir="$app_dir/Contents"
game_dir="$contents_dir/Resources/game"

[[ -x "$dotnet" ]] || fail "private x64 .NET SDK is missing: $dotnet"
[[ -d "$original_app" ]] || fail "original app is missing: $original_app"
[[ "$repo_root" == "/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/TanksRebirth-source" ]] || fail "unexpected repository root: $repo_root"
[[ "$publish_dir" == "$repo_root/artifacts/local-coop/publish" ]] || fail "unsafe publish path: $publish_dir"
[[ "$app_dir" == "$repo_root/artifacts/Tanks Rebirth Local Co-op.app" ]] || fail "unsafe staging path: $app_dir"
[[ "${app_dir:A}" != "${original_app:A}" ]] || fail "staging path resolves to the original app"

if [[ -e "$publish_dir" ]]; then
    rm -rf -- "$publish_dir"
fi
if [[ -e "$app_dir" ]]; then
    rm -rf -- "$app_dir"
fi
mkdir -p "$publish_dir" "$game_dir" "$contents_dir/MacOS"

export DOTNET_CLI_HOME="$sdk_root/cli-home"
export DOTNET_ROOT="$sdk_root"
export DOTNET_ROLL_FORWARD=Major
export PATH="$sdk_root:/usr/bin:/bin:/usr/sbin:/sbin"

print -- "Packaging: publishing Tanks Rebirth for osx-x64"
arch -x86_64 "$dotnet" publish "$repo_root/TanksRebirth.csproj" \
    --configuration Release \
    --runtime osx-x64 \
    --self-contained false \
    --output "$publish_dir" \
    --disable-build-servers \
    --property:DotnetCommand=/usr/bin/true \
    --property:NuGetAudit=false \
    --property:ErrorOnDuplicatePublishOutputFiles=false \
    --property:UseSharedCompilation=false \
    --maxcpucount:1 \
    --nodeReuse:false \
    --consoleLoggerParameters:ErrorsOnly

[[ -s "$publish_dir/TanksRebirth.dll" ]] || fail "publish did not produce TanksRebirth.dll"
[[ -d "$publish_dir/Content" ]] || fail "publish did not include the precompiled Content cache"

ditto "$publish_dir" "$game_dir"
ditto "$original_app/Contents/Resources/game/.dotnet-x64" "$game_dir/.dotnet-x64"

native_libraries=(
    libMonoPosixHelper.dylib
    libSDL2-2.0.0.dylib
    libSystem.IO.Ports.Native.dylib
    libnfd.dylib
    libopenal.1.dylib
)
for library in "${native_libraries[@]}"; do
    if [[ ! -s "$game_dir/$library" ]]; then
        [[ -s "$original_app/Contents/Resources/game/$library" ]] || fail "original native library is missing: $library"
        ditto "$original_app/Contents/Resources/game/$library" "$game_dir/$library"
    fi
done

ditto "$repo_root/packaging/macos/Info.plist" "$contents_dir/Info.plist"
ditto "$repo_root/packaging/macos/launcher" "$contents_dir/MacOS/launcher"
chmod +x "$contents_dir/MacOS/launcher" "$game_dir/TanksRebirth"

print -- "Packaging: applying an ad-hoc signature"
codesign --force --deep --sign - "$app_dir"

"$repo_root/scripts/test-local-coop-app.sh" "$app_dir"
print -- "PASS: packaged local co-op app at $app_dir"
