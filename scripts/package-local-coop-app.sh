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
original_content="$original_app/Contents/Resources/game/Content"
artifacts_root="$repo_root/artifacts"
local_coop_root="$artifacts_root/local-coop"
publish_dir="$local_coop_root/publish"
app_dir="$artifacts_root/Tanks Rebirth Local Co-op.app"
contents_dir="$app_dir/Contents"
game_dir="$contents_dir/Resources/game"

[[ -x "$dotnet" ]] || fail "private x64 .NET SDK is missing: $dotnet"
[[ -d "$original_app" ]] || fail "original app is missing: $original_app"
[[ "$repo_root" == "/Users/andrewbowlus/Documents/Codex/2026-08-20/run/work/TanksRebirth-source" ]] || fail "unexpected repository root: $repo_root"

repo_real="$(cd "$repo_root" && pwd -P)"
[[ "$repo_real" == "$repo_root" ]] || fail "repository root is not the expected canonical path: $repo_real"

[[ ! -L "$artifacts_root" ]] || fail "artifacts root must not be a symlink: $artifacts_root"
[[ ! -e "$artifacts_root" || -d "$artifacts_root" ]] || fail "artifacts root is not a directory: $artifacts_root"
if [[ ! -e "$artifacts_root" ]]; then
    mkdir "$artifacts_root"
fi
artifacts_real="$(cd "$artifacts_root" && pwd -P)"
[[ "$artifacts_real" == "$repo_real/artifacts" ]] || fail "artifacts root escapes the repository: $artifacts_real"

[[ ! -L "$local_coop_root" ]] || fail "local co-op artifacts directory must not be a symlink: $local_coop_root"
[[ ! -e "$local_coop_root" || -d "$local_coop_root" ]] || fail "local co-op artifacts path is not a directory: $local_coop_root"
if [[ ! -e "$local_coop_root" ]]; then
    mkdir "$local_coop_root"
fi
local_coop_real="$(cd "$local_coop_root" && pwd -P)"
[[ "$local_coop_real" == "$artifacts_real/local-coop" ]] || fail "local co-op artifacts directory escapes the artifacts root: $local_coop_real"

[[ "$publish_dir" == "$local_coop_real/publish" ]] || fail "unsafe publish path: $publish_dir"
[[ "$app_dir" == "$artifacts_real/Tanks Rebirth Local Co-op.app" ]] || fail "unsafe staging path: $app_dir"
[[ ! -L "$publish_dir" ]] || fail "publish target must not be a symlink: $publish_dir"
[[ ! -L "$app_dir" ]] || fail "staging target must not be a symlink: $app_dir"
[[ "${app_dir:A}" != "${original_app:A}" ]] || fail "staging path resolves to the original app"

if [[ -e "$publish_dir" ]]; then
    rm -rf -- "$publish_dir"
fi
if [[ -e "$app_dir" ]]; then
    rm -rf -- "$app_dir"
fi
mkdir "$publish_dir"
mkdir -p "$game_dir" "$contents_dir/MacOS"

required_xnbs=(
    Assets/christmas/block_stack_alt_snowy.xnb
    Assets/christmas/block_stack_snowy.xnb
    Assets/christmas/outer_bounds_snowy.xnb
    Assets/models/armor.xnb
    Assets/models/box_face.xnb
    Assets/models/bullet.xnb
    Assets/models/chest.xnb
    Assets/models/cosmetics/army_hat.xnb
    Assets/models/cosmetics/blender_default_cube.xnb
    Assets/models/cosmetics/crown.xnb
    Assets/models/cosmetics/crown_tex_0.xnb
    Assets/models/cosmetics/halo.xnb
    Assets/models/cosmetics/horns.xnb
    Assets/models/cosmetics/santa_hat.xnb
    Assets/models/cosmetics/witch_hat.xnb
    Assets/models/dome.xnb
    Assets/models/flat_face.xnb
    Assets/models/key.xnb
    Assets/models/logo/logo.xnb
    Assets/models/medal.xnb
    Assets/models/mine.xnb
    Assets/models/ping.xnb
    Assets/models/plane.xnb
    Assets/models/rebirth_tanks/tank_necro.xnb
    Assets/models/scene/block_stack.xnb
    Assets/models/scene/block_stack_alt.xnb
    Assets/models/scene/outer_bounds.xnb
    Assets/models/scene/scene_floor.xnb
    Assets/models/scene/skybox/room.xnb
    Assets/models/smoke.xnb
    Assets/models/smoke_grenade.xnb
    Assets/models/tank_e.xnb
    Assets/models/tank_p.xnb
    Assets/models/teleporter.xnb
    Assets/shaders/ExampleVxShader.xnb
    Assets/shaders/black_hue.xnb
    Assets/shaders/controlled_gradient.xnb
    Assets/shaders/gaussian_blur.xnb
    Assets/shaders/lantern.xnb
    Assets/shaders/mouse.xnb
    Assets/shaders/rainbow_grad_anim.xnb
)

[[ -d "$original_content" ]] || fail "original Content directory is missing: $original_content"
original_xnb_count="$(find "$original_content" -type f -name '*.xnb' | wc -l | tr -d '[:space:]')"
[[ "$original_xnb_count" == "41" ]] || fail "expected exactly 41 original XNB assets, found $original_xnb_count"
for relative_xnb in "${required_xnbs[@]}"; do
    [[ -s "$original_content/$relative_xnb" ]] || fail "required original XNB is missing or empty: $relative_xnb"
done
print -- "Packaging: validated all 41 original XNB assets, including all seven shaders"

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

publish_content="$publish_dir/Content"
[[ "$publish_content" == "$local_coop_real/publish/Content" ]] || fail "unsafe publish Content path: $publish_content"
[[ ! -L "$publish_content" ]] || fail "publish Content target must not be a symlink: $publish_content"
if [[ -e "$publish_content" ]]; then
    rm -rf -- "$publish_content"
fi
ditto "$original_content" "$publish_content"

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

macho_files=()
while IFS= read -r -d '' candidate; do
    description="$(file -b "$candidate")"
    if [[ "$description" == *"Mach-O"* ]]; then
        macho_files+=("$candidate")
    fi
done < <(find "$contents_dir" -depth -type f -print0)

(( ${#macho_files[@]} > 0 )) || fail "no nested Mach-O files were found to sign"
print -- "Packaging: ad-hoc signing ${#macho_files[@]} nested Mach-O files"
for macho_file in "${macho_files[@]}"; do
    codesign --force --sign - "$macho_file"
done

print -- "Packaging: applying the top-level ad-hoc app signature"
codesign --force --sign - "$app_dir"

"$repo_root/scripts/test-local-coop-app.sh" "$app_dir"
print -- "PASS: packaged local co-op app at $app_dir"
