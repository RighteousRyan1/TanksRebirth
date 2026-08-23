#!/bin/zsh
set -eu

fail() {
    print -u2 -- "FAIL: $1"
    exit 1
}

pass() {
    print -- "PASS: $1"
}

(( $# == 1 )) || fail "usage: $0 <app-path>"

app_path="${1:A}"
original_app="/Users/andrewbowlus/Applications/Tanks Rebirth.app"

[[ -d "$app_path" ]] || fail "app bundle is missing: $app_path"
[[ "$app_path" != "${original_app:A}" ]] || fail "refusing to validate the original app bundle: $app_path"
pass "target is separate from the original app"

plist="$app_path/Contents/Info.plist"
[[ -f "$plist" ]] || fail "Info.plist is missing: $plist"
plutil -lint "$plist" >/dev/null || fail "Info.plist failed plutil validation"
pass "Info.plist is valid"

bundle_id="$(plutil -extract CFBundleIdentifier raw -o - "$plist")" || fail "CFBundleIdentifier is missing or unreadable"
[[ "$bundle_id" == "com.andrewbowlus.tanksrebirth.localcoop" ]] || fail "unexpected CFBundleIdentifier: $bundle_id"
executable="$(plutil -extract CFBundleExecutable raw -o - "$plist")" || fail "CFBundleExecutable is missing or unreadable"
[[ "$executable" == "launcher" ]] || fail "unexpected CFBundleExecutable: $executable"
short_version="$(plutil -extract CFBundleShortVersionString raw -o - "$plist")" || fail "CFBundleShortVersionString is missing or unreadable"
[[ "$short_version" == "1.8.1" ]] || fail "unexpected CFBundleShortVersionString: $short_version"
build_version="$(plutil -extract CFBundleVersion raw -o - "$plist")" || fail "CFBundleVersion is missing or unreadable"
[[ "$build_version" == "1" ]] || fail "unexpected CFBundleVersion: $build_version"
pass "bundle identity, executable, and versions are correct"

launcher="$app_path/Contents/MacOS/launcher"
game_dir="$app_path/Contents/Resources/game"
dotnet="$game_dir/.dotnet-x64/dotnet"
hostfxr="$game_dir/.dotnet-x64/host/fxr/8.0.30/libhostfxr.dylib"
coreclr="$game_dir/.dotnet-x64/shared/Microsoft.NETCore.App/8.0.30/libcoreclr.dylib"
corelib="$game_dir/.dotnet-x64/shared/Microsoft.NETCore.App/8.0.30/System.Private.CoreLib.dll"
runtimeconfig="$game_dir/TanksRebirth.runtimeconfig.json"
[[ -x "$launcher" ]] || fail "launcher is missing or not executable: $launcher"
[[ -x "$game_dir/TanksRebirth" ]] || fail "TanksRebirth is missing or not executable"
[[ -s "$game_dir/TanksRebirth.dll" ]] || fail "TanksRebirth.dll is missing or empty"
[[ -s "$game_dir/libSDL2-2.0.0.dylib" ]] || fail "libSDL2-2.0.0.dylib is missing or empty"
[[ -s "$game_dir/libopenal.1.dylib" ]] || fail "libopenal.1.dylib is missing or empty"
[[ -x "$dotnet" ]] || fail "the private dotnet host is missing or not executable"
[[ -s "$hostfxr" ]] || fail "the required .NET 8.0.30 x64 hostfxr is missing or empty"
[[ -s "$coreclr" ]] || fail "the required .NET 8.0.30 x64 coreclr is missing or empty"
[[ -s "$corelib" ]] || fail "the required .NET 8.0.30 System.Private.CoreLib.dll is missing or empty"
[[ -s "$runtimeconfig" ]] || fail "TanksRebirth.runtimeconfig.json is missing or empty"

framework_name="$(plutil -extract runtimeOptions.framework.name raw -o - "$runtimeconfig")" || fail "runtimeconfig framework name is missing or unreadable"
[[ "$framework_name" == "Microsoft.NETCore.App" ]] || fail "unexpected runtime framework: $framework_name"
framework_version="$(plutil -extract runtimeOptions.framework.version raw -o - "$runtimeconfig")" || fail "runtimeconfig framework version is missing or unreadable"
[[ "$framework_version" == "8.0.0" ]] || fail "unexpected runtime framework version: $framework_version"
pass "launcher, game, native libraries, private runtime, and runtimeconfig are present"

require_x86_64() {
    local code_path="$1"
    local architectures
    architectures="$(lipo -archs "$code_path" 2>/dev/null)" || fail "not valid Mach-O code: $code_path"
    [[ " $architectures " == *" x86_64 "* ]] || fail "x86_64 architecture is missing from: $code_path"
}

required_x86_code=(
    "$game_dir/TanksRebirth"
    "$dotnet"
    "$hostfxr"
    "$coreclr"
    "$game_dir/libSDL2-2.0.0.dylib"
    "$game_dir/libopenal.1.dylib"
)
for code_path in "${required_x86_code[@]}"; do
    require_x86_64 "$code_path"
done
pass "required game, runtime, SDL, and OpenAL code contains x86_64"

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

for relative_xnb in "${required_xnbs[@]}"; do
    original_xnb="$original_app/Contents/Resources/game/Content/$relative_xnb"
    staged_xnb="$game_dir/Content/$relative_xnb"
    [[ -s "$original_xnb" ]] || fail "required original XNB is missing or empty: $relative_xnb"
    [[ -s "$staged_xnb" ]] || fail "required staged XNB is missing or empty: $relative_xnb"
    original_hash="$(shasum -a 256 "$original_xnb" | cut -d ' ' -f 1)"
    staged_hash="$(shasum -a 256 "$staged_xnb" | cut -d ' ' -f 1)"
    [[ "$staged_hash" == "$original_hash" ]] || fail "staged XNB hash differs from original: $relative_xnb"
done

original_xnb_count="$(find "$original_app/Contents/Resources/game/Content" -type f -name '*.xnb' | wc -l | tr -d '[:space:]')"
[[ "$original_xnb_count" == "41" ]] || fail "expected exactly 41 original XNB assets, found $original_xnb_count"
staged_xnb_count="$(find "$game_dir/Content" -type f -name '*.xnb' | wc -l | tr -d '[:space:]')"
[[ "$staged_xnb_count" == "41" ]] || fail "expected exactly 41 staged XNB assets, found $staged_xnb_count"
pass "all 41 staged XNB hashes match the original, including all seven shaders"

macho_files=()
while IFS= read -r -d '' candidate; do
    description="$(file -b "$candidate")"
    if [[ "$description" == *"Mach-O"* ]]; then
        macho_files+=("$candidate")
    fi
done < <(find "$app_path/Contents" -depth -type f -print0)

(( ${#macho_files[@]} > 0 )) || fail "no nested Mach-O files were found"
for macho_file in "${macho_files[@]}"; do
    require_x86_64 "$macho_file"
    codesign --verify --strict "$macho_file" >/dev/null 2>&1 || fail "nested Mach-O signature verification failed: $macho_file"
done
pass "all ${#macho_files[@]} nested Mach-O files contain x86_64 and have valid signatures"

codesign --verify --deep --strict "$app_path" || fail "ad-hoc code signature verification failed"
pass "deep strict code signature verification succeeded"
pass "local co-op app bundle validation completed"
