---
description: "Generate animated pixel art assets for the isometric tactics game using Gemini keyframes + PixelLab interpolation"
---

# Asset Pipeline

Generate animated pixel art assets for an isometric tactics game (Final Fantasy Tactics style).

**Pipeline**: Gemini generates two keyframe images (start + end poses) → PixelLab interpolates intermediate animation frames.

**User input**: `$ARGUMENTS`

## Step 1: Parse the Asset Request

Analyze `$ARGUMENTS` to determine:

- **Asset name**: A short slug for file/directory naming (e.g., `knight-attack`, `fire-spell`, `goblin-walk`)
- **Asset type**: `character`, `spell`, `effect`, `object`, or `environment`
- **Action/animation**: What the animation depicts (e.g., "sword slash", "casting fireball", "walking")
- **Style notes**: Any specific style details mentioned

Create the output directory:
```bash
mkdir -p assets/<asset-name>/keyframes assets/<asset-name>/frames
```

## Step 2: Generate Keyframe Prompts

Craft TWO prompts for Gemini — one for the START pose and one for the END pose. Both prompts MUST include these style constraints:

**Mandatory style prefix** (prepend to every prompt):
```
Pixel art sprite, 64x64 pixels, isometric 3/4 top-down view, transparent background,
Final Fantasy Tactics art style, limited color palette, clean pixel edges,
no anti-aliasing, game-ready sprite.
```

**Start keyframe prompt**: The character/object at the BEGINNING of the action.
Example: "...A knight in steel armor, standing in ready stance, sword raised behind shoulder, about to swing"

**End keyframe prompt**: The character/object at the END of the action.
Example: "...A knight in steel armor, follow-through stance after sword swing, sword extended forward and down"

CRITICAL: Both prompts must describe the SAME character/object with the SAME colors, equipment, and proportions — only the pose/state changes.

## Step 3: Generate Keyframes with Gemini

Run the generation script for each keyframe. The script outputs the base64 data to stdout (last line) and saves the PNG file.

```bash
# Generate start keyframe
START_B64=$(bash scripts/gemini-generate.sh "assets/<asset-name>/keyframes/start.png" "<start_prompt>")
# Capture just the base64 (last line)
START_B64=$(echo "$START_B64" | tail -1)

# Generate end keyframe
END_B64=$(bash scripts/gemini-generate.sh "assets/<asset-name>/keyframes/end.png" "<end_prompt>")
END_B64=$(echo "$END_B64" | tail -1)
```

After generation, READ both keyframe images to visually verify:
- Both show the same character/object
- Colors and proportions are consistent
- Poses clearly represent start and end of the animation
- Art style is consistent pixel art

If the keyframes look inconsistent, regenerate with adjusted prompts. You may need to reference the first image in the second prompt for consistency.

## Step 4: Resize Keyframes for PixelLab

PixelLab interpolation requires images between 16-128px. The keyframes from Gemini may be larger. Resize them to 64x64 while preserving pixel art crispness (nearest-neighbor scaling):

```bash
# Check if ImageMagick is available for resizing
if command -v convert &>/dev/null; then
  convert "assets/<asset-name>/keyframes/start.png" -resize 64x64 -filter point "assets/<asset-name>/keyframes/start_64.png"
  convert "assets/<asset-name>/keyframes/end.png" -resize 64x64 -filter point "assets/<asset-name>/keyframes/end_64.png"
  # Re-encode to base64
  START_B64=$(base64 -w0 "assets/<asset-name>/keyframes/start_64.png")
  END_B64=$(base64 -w0 "assets/<asset-name>/keyframes/end_64.png")
elif command -v ffmpeg &>/dev/null; then
  ffmpeg -i "assets/<asset-name>/keyframes/start.png" -vf "scale=64:64:flags=neighbor" "assets/<asset-name>/keyframes/start_64.png" -y
  ffmpeg -i "assets/<asset-name>/keyframes/end.png" -vf "scale=64:64:flags=neighbor" "assets/<asset-name>/keyframes/end_64.png" -y
  START_B64=$(base64 -w0 "assets/<asset-name>/keyframes/start_64.png")
  END_B64=$(base64 -w0 "assets/<asset-name>/keyframes/end_64.png")
fi
```

If neither tool is available, install ImageMagick: `sudo apt-get install -y imagemagick`

## Step 5: Interpolate with PixelLab

Submit the interpolation job using the Python script. The `<action>` should be a short description of the motion between the two keyframes. The script handles job submission, polling, and converting RGBA frames to PNG.

```bash
python3 scripts/pixellab-interpolate.py \
  "assets/<asset-name>/keyframes/start_64.png" \
  "assets/<asset-name>/keyframes/end_64.png" \
  "<action description>" \
  64 64 \
  "assets/<asset-name>/frames"
```

This script submits the job, polls until complete, and saves all frames as PNG files.

## Step 6: Create Asset Manifest

After frames are saved, create a manifest file describing the asset:

Write `assets/<asset-name>/manifest.json`:
```json
{
  "name": "<asset-name>",
  "description": "<original user description>",
  "type": "<asset-type>",
  "animation": "<action>",
  "frame_size": {"width": 64, "height": 64},
  "keyframes": ["keyframes/start.png", "keyframes/end.png"],
  "frames": ["frames/frame_001.png", "frames/frame_002.png", "..."],
  "frame_count": <N>,
  "created": "<ISO timestamp>",
  "pipeline": {
    "keyframe_model": "gemini-2.5-flash-image",
    "interpolation": "pixellab-v2"
  }
}
```

## Step 7: Report Results

Display a summary:

```
Asset Created: <asset-name>
  Type: <asset-type>
  Animation: <action>
  Keyframes: assets/<asset-name>/keyframes/
  Frames: assets/<asset-name>/frames/ (<N> frames)
  Manifest: assets/<asset-name>/manifest.json
```

Then READ the keyframe images and a sample frame (if available) to show the user what was generated.

## Error Handling

- If Gemini fails to generate an image, retry once with a simplified prompt
- If PixelLab returns 402 (insufficient credits), inform the user
- If PixelLab returns 422 (validation error), check image dimensions and format
- If polling times out, provide the job ID so the user can check later
- If keyframes are visually inconsistent, note this and offer to regenerate

## Alternative: PixelLab-Native Characters

If the user's request is better served by PixelLab's native character tools (standard humanoid/quadruped with template animations), suggest using those instead:

- `mcp__pixellab__create_character` for character creation with directional views
- `mcp__pixellab__animate_character` for template-based animations (walk, run, attack, etc.)

These are cheaper (1 generation vs 20+) and produce more consistent results for standard character animations. Use the Gemini+interpolation pipeline for:
- Custom/unique animations not covered by templates
- Spell effects and particles
- Environmental animations
- Non-standard character types
- Complex multi-stage animations
