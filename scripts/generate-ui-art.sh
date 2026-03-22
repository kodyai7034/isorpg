#!/usr/bin/env bash
# Generate UI art assets via Gemini and post-process for Unity import.
# Usage: ./generate-ui-art.sh
# Requires: GEMINI_API_KEY, ImageMagick (convert)
set -uo pipefail
# Note: not using -e because gemini-generate.sh outputs large base64 to stdout
# and head -1 causes SIGPIPE which would exit the script

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RAW_DIR="$SCRIPT_DIR/../assets/ui-raw"
UNITY_UI="$SCRIPT_DIR/../UnityProject/Assets/Sprites/UI"

STYLE_BLOCK="Pixel art game UI element, 32-bit era JRPG style inspired by Final Fantasy Tactics and Tactics Ogre. Rich detailed pixel art with depth. Limited color palette: navy #1a1a3e, royal blue #2a2a6e, gold accent #d4a017, cream #f5e6ca, dark stone #3a3a4a, shadow black #0a0a0a. Clean crisp pixel edges, NO anti-aliasing, NO 3D rendering. Isolated element on transparent background, PNG with alpha channel. Medieval fantasy aesthetic, ornate stone and metal craftsmanship. 32-bit PlayStation era pixel art quality and detail level."

generate() {
    local output="$1"
    local prompt="$2"
    echo ">>> Generating: $output"
    bash "$SCRIPT_DIR/gemini-generate.sh" "$output" "$STYLE_BLOCK $prompt" 2>&1 | head -1
}

downscale() {
    local input="$1"
    local output="$2"
    local percent="${3:-25}"
    echo "    Downscaling $percent% → $output"
    convert "$input" -filter Point -resize "${percent}%" "$output"
}

copy_to_unity() {
    local src="$1"
    local dest_subdir="$2"
    local filename="$(basename "$src")"
    mkdir -p "$UNITY_UI/$dest_subdir"
    cp "$src" "$UNITY_UI/$dest_subdir/$filename"
    echo "    Copied → Assets/Sprites/UI/$dest_subdir/$filename"
}

# ============================================================
# PANELS — frame and background only, text/buttons layered by Unity
# ============================================================
generate_panels() {
    echo "=== PANELS ==="

    generate "$RAW_DIR/panels/action_menu_4x.png" \
        "A 880x1040 pixel UI panel frame and background. Vertical rectangle, taller than wide. Ornate carved dark stone border frame about 50px wide with crossed-sword corner flourishes. Thin gold accent line along inner edge of border. Interior fill: deep navy blue with subtle pixel dithering. Empty inside — no text, no icons, no buttons. Just the decorated ornate stone frame and navy fill."

    generate "$RAW_DIR/panels/combat_menu_4x.png" \
        "A 880x1160 pixel UI panel frame and background. Vertical rectangle, taller than wide. Same ornate carved dark stone border frame about 50px wide with crossed-sword corner flourishes. Thin gold accent line along inner edge. Interior fill: deep navy blue with subtle dithering. Slightly taller than wide. Empty inside — no text, no icons. Just the decorated frame and fill."

    generate "$RAW_DIR/panels/ability_menu_4x.png" \
        "A 1000x1280 pixel UI panel frame and background. Vertical rectangle, taller than wide. Same ornate carved dark stone border about 50px wide with crossed-sword corner flourishes. Thin gold accent line along inner edge. Interior fill: deep navy blue with subtle dithering. Taller and slightly wider panel. Empty inside — just the decorated frame and fill."

    generate "$RAW_DIR/panels/selection_context_4x.png" \
        "A 1040x560 pixel UI panel frame and background. Horizontal rectangle, wider than tall. Same ornate carved dark stone border about 40px wide with small shield corner flourishes. Thin gold accent line along inner edge. Interior fill: deep navy blue with subtle dithering. Empty inside — just the decorated frame and fill."

    generate "$RAW_DIR/panels/turn_banner_blue_4x.png" \
        "A 1024x128 pixel UI banner frame. Very wide horizontal strip. Ornate carved dark stone border with scrollwork along top and bottom edges. Gold accent trim. Interior fill: deep navy blue (#1a1a3e) with royal blue (#2a2a6e) gradient. Corner flourishes are small shield motifs. Empty inside — just the decorated banner frame. This is a full-width announcement banner."

    generate "$RAW_DIR/panels/turn_banner_red_4x.png" \
        "A 1024x128 pixel UI banner frame. Very wide horizontal strip. Ornate carved dark stone border with scrollwork along top and bottom edges. Gold accent trim. Interior fill: deep dark red (#3a1010) with crimson (#5a1a1a) gradient. Corner flourishes are small skull motifs. Empty inside — just the decorated banner frame. This is a full-width enemy announcement banner."

    generate "$RAW_DIR/panels/battle_result_4x.png" \
        "A 1600x800 pixel UI panel frame and background. Horizontal rectangle, wider than tall. Extra ornate carved dark stone border about 60px wide with elaborate crossed-sword and shield corner flourishes. Double gold accent lines along inner edge. Interior fill: deep navy blue with subtle pixel dithering. This is a prestigious results panel — more elaborate than other panels. Empty inside — just the decorated frame and fill."

    generate "$RAW_DIR/panels/unit_info_4x.png" \
        "A 1040x720 pixel UI panel frame and background. Slightly wider than tall. Ornate carved dark stone border about 45px wide with small shield corner flourishes. Thin gold accent line along inner edge. Interior fill: deep navy blue with subtle pixel dithering. This panel will display unit information — but generate it empty, just the decorated frame and fill."
}

# ============================================================
# BUTTONS — 4 states each, frame only, text layered by Unity
# ============================================================
generate_buttons() {
    echo "=== BUTTONS ==="

    # Standard button (160x42 target → 640x168 at 4x)
    generate "$RAW_DIR/buttons/btn_standard_normal_4x.png" \
        "A 640x168 pixel rectangular button. Raised stone/metal surface with subtle carved texture and bevel. Dark navy-blue base (#1a1a3e) with 2px border: outer dark (#0a0a0a), inner gold (#d4a017) highlight. Top edge slightly lighter for depth. Empty — no text. Just the button shape."

    generate "$RAW_DIR/buttons/btn_standard_hover_4x.png" \
        "A 640x168 pixel rectangular button in HOVER state. Same stone/metal button shape but glowing — gold border is brighter and thicker, interior is slightly lighter royal blue (#2a2a6e), warm golden highlight across surface. Empty — no text. Just the glowing button shape."

    generate "$RAW_DIR/buttons/btn_standard_pressed_4x.png" \
        "A 640x168 pixel rectangular button in PRESSED state. Same stone/metal button shape but depressed/inset — darker interior (#0f0f2a), inner shadow at top edge, gold border slightly dimmer. Feels pushed in. Empty — no text. Just the pressed button shape."

    generate "$RAW_DIR/buttons/btn_standard_disabled_4x.png" \
        "A 640x168 pixel rectangular button in DISABLED state. Same button shape but desaturated and dim — flat grey stone (#3a3a3a), no gold accent, very low contrast, looks inactive and weathered. Empty — no text. Just the disabled button shape."

    # Ability entry button (200x40 target → 800x160 at 4x)
    generate "$RAW_DIR/buttons/btn_ability_normal_4x.png" \
        "A 800x160 pixel rectangular button. Slightly flatter than an action button — more like a list entry. Dark navy surface (#1a1a3e) with thin 1px gold border and subtle inner bevel. Narrower/flatter proportions. Empty — no text. Just the entry shape."

    generate "$RAW_DIR/buttons/btn_ability_hover_4x.png" \
        "A 800x160 pixel rectangular list entry button in HOVER state. Same flat entry shape, gold border glows brighter, interior slightly lighter royal blue. Empty — no text."

    generate "$RAW_DIR/buttons/btn_ability_pressed_4x.png" \
        "A 800x160 pixel rectangular list entry button in PRESSED state. Darker interior, inset shadow. Empty — no text."

    generate "$RAW_DIR/buttons/btn_ability_disabled_4x.png" \
        "A 800x160 pixel rectangular list entry button in DISABLED state. Grey, flat, desaturated, no gold. Empty — no text."

    # Wide result button (240x50 target → 960x200 at 4x)
    generate "$RAW_DIR/buttons/btn_wide_normal_4x.png" \
        "A 960x200 pixel wide rectangular button. Ornate stone/metal surface, wider than standard buttons. Gold border with small decorative end caps. Dark navy interior. Empty — no text. Just the wide button shape."

    generate "$RAW_DIR/buttons/btn_wide_hover_4x.png" \
        "A 960x200 pixel wide button in HOVER state. Gold border glows, interior lighter. Empty — no text."

    generate "$RAW_DIR/buttons/btn_wide_pressed_4x.png" \
        "A 960x200 pixel wide button in PRESSED state. Darker, inset. Empty — no text."

    generate "$RAW_DIR/buttons/btn_wide_disabled_4x.png" \
        "A 960x200 pixel wide button in DISABLED state. Grey, flat, no gold. Empty — no text."
}

# ============================================================
# GAUNTLET SELECTOR — 4-frame idle animation
# ============================================================
generate_gauntlet() {
    echo "=== GAUNTLET SELECTOR ==="

    generate "$RAW_DIR/icons/gauntlet_frame1_4x.png" \
        "A 128x128 pixel icon of an armored medieval gauntlet hand pointing to the right. Iron/steel armor with gold trim on the knuckles. Index finger extended pointing right, other fingers curled. Side view. This is a menu cursor/selector for a tactical RPG. Frame 1 of 4: neutral idle position."

    generate "$RAW_DIR/icons/gauntlet_frame2_4x.png" \
        "A 128x128 pixel icon of an armored medieval gauntlet hand pointing to the right. Iron/steel armor with gold trim. Index finger extended pointing right. Side view. Frame 2 of 4: finger slightly more extended forward, subtle forward motion."

    generate "$RAW_DIR/icons/gauntlet_frame3_4x.png" \
        "A 128x128 pixel icon of an armored medieval gauntlet hand pointing to the right. Iron/steel armor with gold trim. Index finger extended pointing right. Side view. Frame 3 of 4: hand shifted slightly forward (2-3 pixels), maximum extension of the pointing gesture."

    generate "$RAW_DIR/icons/gauntlet_frame4_4x.png" \
        "A 128x128 pixel icon of an armored medieval gauntlet hand pointing to the right. Iron/steel armor with gold trim. Index finger extended pointing right. Side view. Frame 4 of 4: returning to idle, finger relaxing back toward frame 1 position."
}

# ============================================================
# ABILITY & STATUS ICONS
# ============================================================
generate_icons() {
    echo "=== ABILITY ICONS ==="

    generate "$RAW_DIR/icons/icon_attack_4x.png" \
        "A 128x128 pixel icon of a silver steel sword for a tactical RPG. Diagonal orientation, blade pointing upper-right. Simple hilt with gold crossguard. Clean bold silhouette readable at 32x32."

    generate "$RAW_DIR/icons/icon_fire_4x.png" \
        "A 128x128 pixel icon of a magical fire spell for a tactical RPG. Orange-red flame with yellow core, swirling upward. Bold shape, high contrast against transparent background. Readable at 32x32."

    generate "$RAW_DIR/icons/icon_cure_4x.png" \
        "A 128x128 pixel icon of a healing spell for a tactical RPG. White and green sparkle/starburst with soft glow. A plus or cross shape suggested in the sparkle. Readable at 32x32."

    generate "$RAW_DIR/icons/icon_poison_strike_4x.png" \
        "A 128x128 pixel icon of a venomous blade attack for a tactical RPG. Purple-tinged short sword with green poison dripping from the blade. Bold silhouette. Readable at 32x32."

    echo "=== STATUS ICONS ==="

    generate "$RAW_DIR/icons/status_poison_4x.png" \
        "A 64x64 pixel icon for Poison status effect. Green bubbling skull or vial with toxic fumes. Very simple, bold, high contrast. Must be readable at 16x16 pixels."

    generate "$RAW_DIR/icons/status_haste_4x.png" \
        "A 64x64 pixel icon for Haste/Speed status effect. Yellow speed lines or small clock with fast-forward arrow. Very simple, bold. Readable at 16x16."

    generate "$RAW_DIR/icons/status_slow_4x.png" \
        "A 64x64 pixel icon for Slow status effect. Blue hourglass or snail silhouette. Very simple, bold. Readable at 16x16."

    generate "$RAW_DIR/icons/status_protect_4x.png" \
        "A 64x64 pixel icon for Protect/Defense status effect. Orange-brown shield. Very simple, bold shape. Readable at 16x16."

    generate "$RAW_DIR/icons/status_shell_4x.png" \
        "A 64x64 pixel icon for Shell/Magic Defense status effect. Blue-purple dome barrier or shell shape. Very simple, bold. Readable at 16x16."

    generate "$RAW_DIR/icons/status_regen_4x.png" \
        "A 64x64 pixel icon for Regen/Heal-over-time status effect. Green glowing heart or pulsing life symbol. Very simple, bold. Readable at 16x16."

    generate "$RAW_DIR/icons/cursor_selector_4x.png" \
        "A 256x128 pixel isometric diamond cursor for a tactical RPG tile grid. Glowing gold outline in a diamond/rhombus shape (isometric tile shape, 2:1 width to height ratio). Semi-transparent interior with subtle glow. Used to highlight the tile under the mouse cursor."
}

# ============================================================
# BARS & FRAMES
# ============================================================
generate_bars_frames() {
    echo "=== BARS & FRAMES ==="

    generate "$RAW_DIR/bars/hp_bar_fill_4x.png" \
        "A 512x32 pixel horizontal bar fill for an HP health bar. Left side is green, transitions through yellow to red on the right side. Subtle pixel texture on the gradient. Clean rectangular shape, no border."

    generate "$RAW_DIR/bars/mp_bar_fill_4x.png" \
        "A 512x32 pixel horizontal bar fill for an MP magic bar. Blue gradient from bright royal blue on left to darker navy on right. Subtle pixel texture. Clean rectangular shape, no border."

    generate "$RAW_DIR/bars/bar_background_4x.png" \
        "A 512x32 pixel horizontal bar background/track. Dark recessed channel, very dark grey (#1a1a1a) with subtle inner shadow at top edge. Clean rectangular shape. This sits behind HP/MP bar fills."

    generate "$RAW_DIR/frames/portrait_frame_blue_4x.png" \
        "A 256x256 pixel ornate portrait frame. Stone border with gold accent, matching the panel style. Interior is empty/transparent (portrait goes behind). Blue accent color (#2a2a6e) on the inner edge. Square frame."

    generate "$RAW_DIR/frames/portrait_frame_red_4x.png" \
        "A 256x256 pixel ornate portrait frame. Stone border with gold accent, matching the panel style. Interior is empty/transparent. Red accent color (#5a1a1a) on the inner edge. Square frame."
}

# ============================================================
# POST-PROCESSING — downscale all 4x assets to 1x
# ============================================================
postprocess() {
    echo ""
    echo "=== POST-PROCESSING ==="
    mkdir -p "$UNITY_UI/Panels" "$UNITY_UI/Buttons" "$UNITY_UI/Icons" "$UNITY_UI/Bars" "$UNITY_UI/Frames"

    # Panels → 25% downscale
    for f in "$RAW_DIR/panels/"*_4x.png; do
        [ -f "$f" ] || continue
        base=$(basename "$f" _4x.png)
        downscale "$f" "$UNITY_UI/Panels/${base}.png" 25
    done

    # Buttons → 25% downscale
    for f in "$RAW_DIR/buttons/"*_4x.png; do
        [ -f "$f" ] || continue
        base=$(basename "$f" _4x.png)
        downscale "$f" "$UNITY_UI/Buttons/${base}.png" 25
    done

    # Icons (ability 128→32 = 25%, status 64→16 = 25%, gauntlet 128→32 = 25%)
    for f in "$RAW_DIR/icons/"*_4x.png; do
        [ -f "$f" ] || continue
        base=$(basename "$f" _4x.png)
        downscale "$f" "$UNITY_UI/Icons/${base}.png" 25
    done

    # Bars 512→128 = 25%
    for f in "$RAW_DIR/bars/"*_4x.png; do
        [ -f "$f" ] || continue
        base=$(basename "$f" _4x.png)
        downscale "$f" "$UNITY_UI/Bars/${base}.png" 25
    done

    # Frames 256→64 = 25%
    for f in "$RAW_DIR/frames/"*_4x.png; do
        [ -f "$f" ] || continue
        base=$(basename "$f" _4x.png)
        downscale "$f" "$UNITY_UI/Frames/${base}.png" 25
    done

    echo ""
    echo "=== DONE ==="
    echo "Raw 4x assets: $RAW_DIR/"
    echo "Final 1x assets: $UNITY_UI/"
    find "$UNITY_UI" -name "*.png" | wc -l
    echo "total PNG files imported"
}

# ============================================================
# MAIN
# ============================================================
case "${1:-all}" in
    panels) generate_panels ;;
    buttons) generate_buttons ;;
    gauntlet) generate_gauntlet ;;
    icons) generate_icons ;;
    bars) generate_bars_frames ;;
    process) postprocess ;;
    all)
        generate_panels
        generate_buttons
        generate_gauntlet
        generate_icons
        generate_bars_frames
        postprocess
        ;;
    *) echo "Usage: $0 {panels|buttons|gauntlet|icons|bars|process|all}" ;;
esac
