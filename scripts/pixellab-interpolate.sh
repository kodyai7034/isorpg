#!/usr/bin/env bash
# Interpolate between two keyframe images using PixelLab API
# Usage: ./pixellab-interpolate.sh <start_image_b64> <end_image_b64> <action> <width> <height> [output_dir]
# Requires: PIXELLAB_API_KEY env var, jq

set -euo pipefail

START_B64="$1"
END_B64="$2"
ACTION="$3"
WIDTH="${4:-64}"
HEIGHT="${5:-64}"
OUTPUT_DIR="${6:-.}"

if [[ -z "${PIXELLAB_API_KEY:-}" ]]; then
  echo "ERROR: PIXELLAB_API_KEY not set" >&2
  exit 1
fi

API_URL="https://api.pixellab.ai/v2/interpolation-v2"

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL" \
  -H "Authorization: Bearer ${PIXELLAB_API_KEY}" \
  -H "Content-Type: application/json" \
  -d "$(jq -n \
    --arg start "$START_B64" \
    --arg end "$END_B64" \
    --arg action "$ACTION" \
    --argjson width "$WIDTH" \
    --argjson height "$HEIGHT" \
    '{
      start_image: {image: $start},
      end_image: {image: $end},
      action: $action,
      image_size: {width: $width, height: $height},
      no_background: true
    }')")

HTTP_CODE=$(echo "$RESPONSE" | tail -1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [[ "$HTTP_CODE" == "202" ]]; then
  JOB_ID=$(echo "$BODY" | jq -r '.job_id // .data.job_id // .id // empty')
  if [[ -n "$JOB_ID" ]]; then
    echo "JOB_ID=$JOB_ID"
    echo "Job submitted. Poll with: scripts/pixellab-poll.sh $JOB_ID $OUTPUT_DIR"
  else
    echo "BODY=$BODY"
    echo "202 accepted but no job_id found in response"
  fi
elif [[ "$HTTP_CODE" == "200" ]]; then
  echo "$BODY" | jq -r '.data' > "$OUTPUT_DIR/interpolation_result.json"
  echo "Interpolation complete. Result saved to $OUTPUT_DIR/interpolation_result.json"
  echo "BODY=$BODY"
else
  echo "ERROR: HTTP $HTTP_CODE" >&2
  echo "$BODY" >&2
  exit 1
fi
