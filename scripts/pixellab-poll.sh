#!/usr/bin/env bash
# Poll a PixelLab background job until complete and save frame images
# Usage: ./pixellab-poll.sh <job_id> [output_dir]
# Requires: PIXELLAB_API_KEY env var, jq

set -euo pipefail

JOB_ID="$1"
OUTPUT_DIR="${2:-.}"

if [[ -z "${PIXELLAB_API_KEY:-}" ]]; then
  echo "ERROR: PIXELLAB_API_KEY not set" >&2
  exit 1
fi

API_URL="https://api.pixellab.ai/v2/background-jobs/${JOB_ID}"
MAX_ATTEMPTS=60
INTERVAL=5

mkdir -p "$OUTPUT_DIR"

for i in $(seq 1 $MAX_ATTEMPTS); do
  RESPONSE=$(curl -s -w "\n%{http_code}" -X GET "$API_URL" \
    -H "Authorization: Bearer ${PIXELLAB_API_KEY}")

  HTTP_CODE=$(echo "$RESPONSE" | tail -1)
  BODY=$(echo "$RESPONSE" | sed '$d')

  STATUS=$(echo "$BODY" | jq -r '.status // .data.status // "unknown"')

  if [[ "$HTTP_CODE" == "200" && ("$STATUS" == "completed" || "$STATUS" == "complete" || "$STATUS" == "done") ]]; then
    echo "Job complete!"

    # Extract frame images from response
    FRAME_COUNT=$(echo "$BODY" | jq '[.data.images // .images // .data.frames // .frames // [] | .[] ] | length')

    if [[ "$FRAME_COUNT" -gt 0 ]]; then
      echo "$BODY" | jq -r '(.data.images // .images // .data.frames // .frames // [])[] | .image // .data // .' | \
      while IFS= read -r frame_b64; do
        FRAME_IDX=$((${FRAME_IDX:-0} + 1))
        PADDED=$(printf "%03d" $FRAME_IDX)
        echo "$frame_b64" | base64 -d > "$OUTPUT_DIR/frame_${PADDED}.png"
        echo "  Saved frame_${PADDED}.png"
      done
      echo "Saved $FRAME_COUNT frames to $OUTPUT_DIR/"
    else
      echo "$BODY" | jq '.' > "$OUTPUT_DIR/job_result.json"
      echo "No frame images found in standard locations. Raw result saved to job_result.json"
    fi

    echo "RESULT=$BODY"
    exit 0
  elif [[ "$HTTP_CODE" == "404" ]]; then
    echo "ERROR: Job $JOB_ID not found" >&2
    exit 1
  fi

  echo "Poll $i/$MAX_ATTEMPTS: status=$STATUS (waiting ${INTERVAL}s...)"
  sleep $INTERVAL
done

echo "ERROR: Timed out after $((MAX_ATTEMPTS * INTERVAL))s" >&2
exit 1
