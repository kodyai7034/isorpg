#!/usr/bin/env bash
# Generate a pixel art keyframe image using Gemini API
# Usage: ./gemini-generate.sh <output_path> <prompt>
# Requires: GEMINI_API_KEY env var, jq

set -euo pipefail

OUTPUT_PATH="$1"
shift
PROMPT="$*"

if [[ -z "${GEMINI_API_KEY:-}" ]]; then
  echo "ERROR: GEMINI_API_KEY not set" >&2
  exit 1
fi

MODEL="gemini-2.5-flash-image"

RESPONSE=$(curl -s -X POST \
  "https://generativelanguage.googleapis.com/v1beta/models/${MODEL}:generateContent" \
  -H "x-goog-api-key: ${GEMINI_API_KEY}" \
  -H "Content-Type: application/json" \
  -d "$(jq -n --arg prompt "$PROMPT" '{
    contents: [{
      parts: [{text: $prompt}]
    }],
    generationConfig: {
      responseModalities: ["TEXT", "IMAGE"]
    }
  }')")

# Check for errors
ERROR=$(echo "$RESPONSE" | jq -r '.error.message // empty')
if [[ -n "$ERROR" ]]; then
  echo "ERROR: Gemini API error: $ERROR" >&2
  exit 1
fi

# Extract base64 image data from response parts
IMAGE_DATA=$(echo "$RESPONSE" | jq -r '
  .candidates[0].content.parts[]
  | select(.inlineData != null)
  | .inlineData.data' | head -1)

if [[ -z "$IMAGE_DATA" || "$IMAGE_DATA" == "null" ]]; then
  echo "ERROR: No image data in response" >&2
  echo "Response: $(echo "$RESPONSE" | jq -r '.candidates[0].content.parts[] | select(.text != null) | .text' 2>/dev/null)" >&2
  exit 1
fi

# Decode and save
echo "$IMAGE_DATA" | base64 -d > "$OUTPUT_PATH"

MIME=$(echo "$RESPONSE" | jq -r '
  .candidates[0].content.parts[]
  | select(.inlineData != null)
  | .inlineData.mimeType' | head -1)

echo "Saved ${MIME} to ${OUTPUT_PATH}"
echo "$IMAGE_DATA"
