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
pass "bundle identity and executable are correct"

launcher="$app_path/Contents/MacOS/launcher"
game_dir="$app_path/Contents/Resources/game"
[[ -x "$launcher" ]] || fail "launcher is missing or not executable: $launcher"
[[ -x "$game_dir/TanksRebirth" ]] || fail "TanksRebirth is missing or not executable"
[[ -s "$game_dir/TanksRebirth.dll" ]] || fail "TanksRebirth.dll is missing or empty"
[[ -s "$game_dir/libSDL2-2.0.0.dylib" ]] || fail "libSDL2-2.0.0.dylib is missing or empty"
[[ -s "$game_dir/libopenal.1.dylib" ]] || fail "libopenal.1.dylib is missing or empty"
[[ -s "$game_dir/.dotnet-x64/host/fxr/8.0.30/libhostfxr.dylib" ]] || fail "the required .NET 8.0.30 x64 hostfxr is missing or empty"
pass "launcher, game, native libraries, and private runtime are present"

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
    [[ -s "$game_dir/Content/$relative_xnb" ]] || fail "required XNB is missing or empty: $relative_xnb"
done

xnb_count="$(find "$game_dir/Content" -type f -name '*.xnb' | wc -l | tr -d '[:space:]')"
[[ "$xnb_count" == "41" ]] || fail "expected exactly 41 XNB assets, found $xnb_count"
pass "all 41 XNB assets are present, including all seven shaders"

codesign --verify --deep --strict "$app_path" || fail "ad-hoc code signature verification failed"
pass "deep strict code signature verification succeeded"
pass "local co-op app bundle validation completed"
