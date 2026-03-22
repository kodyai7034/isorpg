#!/usr/bin/env python3
"""Interpolate between two keyframe images using PixelLab API.

Usage: python3 scripts/pixellab-interpolate.py <start_img> <end_img> <action> [width] [height] [output_dir]

Requires: PIXELLAB_API_KEY env var, Pillow
"""
import sys
import os
import json
import base64
import time
import requests
from PIL import Image

def main():
    if len(sys.argv) < 4:
        print("Usage: pixellab-interpolate.py <start_img> <end_img> <action> [width] [height] [output_dir]")
        sys.exit(1)

    start_path = sys.argv[1]
    end_path = sys.argv[2]
    action = sys.argv[3]
    width = int(sys.argv[4]) if len(sys.argv) > 4 else 64
    height = int(sys.argv[5]) if len(sys.argv) > 5 else 64
    output_dir = sys.argv[6] if len(sys.argv) > 6 else "."

    api_key = os.environ.get("PIXELLAB_API_KEY")
    if not api_key:
        print("ERROR: PIXELLAB_API_KEY not set", file=sys.stderr)
        sys.exit(1)

    os.makedirs(output_dir, exist_ok=True)

    # Read and encode images
    with open(start_path, "rb") as f:
        start_b64 = base64.b64encode(f.read()).decode()
    with open(end_path, "rb") as f:
        end_b64 = base64.b64encode(f.read()).decode()

    # Build payload
    payload = {
        "start_image": {
            "image": {"type": "base64", "base64": start_b64, "format": "png"},
            "size": {"width": width, "height": height},
        },
        "end_image": {
            "image": {"type": "base64", "base64": end_b64, "format": "png"},
            "size": {"width": width, "height": height},
        },
        "action": action,
        "image_size": {"width": width, "height": height},
        "no_background": True,
    }

    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }

    # Submit job
    print("Submitting interpolation job...")
    resp = requests.post(
        "https://api.pixellab.ai/v2/interpolation-v2",
        headers=headers,
        json=payload,
    )

    if resp.status_code == 202:
        data = resp.json()
        job_id = data.get("background_job_id") or data.get("job_id") or data.get("id")
        print(f"Job submitted: {job_id}")
    elif resp.status_code == 200:
        data = resp.json()
        job_id = None
        print("Job completed immediately")
    else:
        print(f"ERROR: HTTP {resp.status_code}", file=sys.stderr)
        print(resp.text, file=sys.stderr)
        sys.exit(1)

    # Poll for completion
    if job_id:
        print("Polling for completion...")
        for attempt in range(120):
            time.sleep(5)
            poll_resp = requests.get(
                f"https://api.pixellab.ai/v2/background-jobs/{job_id}",
                headers={"Authorization": f"Bearer {api_key}"},
            )
            if poll_resp.status_code != 200:
                print(f"  Poll {attempt+1}: HTTP {poll_resp.status_code}")
                continue

            poll_data = poll_resp.json()
            status = poll_data.get("status", "unknown")
            print(f"  Poll {attempt+1}: {status}")

            if status == "completed":
                data = poll_data
                break
        else:
            print("ERROR: Timed out", file=sys.stderr)
            sys.exit(1)

    # Extract and save frames
    images = data.get("last_response", data).get("images", [])
    if not images:
        # Save raw result for debugging
        result_path = os.path.join(output_dir, "job_result.json")
        with open(result_path, "w") as f:
            json.dump(data, f, indent=2)
        print(f"No images found. Raw result saved to {result_path}")
        sys.exit(1)

    for i, img in enumerate(images):
        img_type = img.get("type", "")
        w = img.get("width", width)
        h = img.get("height", height)
        raw = base64.b64decode(img["base64"])

        frame_path = os.path.join(output_dir, f"frame_{i+1:03d}.png")

        if img_type == "rgba_bytes":
            pil_img = Image.frombytes("RGBA", (w, h), raw)
            pil_img.save(frame_path)
        else:
            with open(frame_path, "wb") as f:
                f.write(raw)

        print(f"  Saved {frame_path}")

    print(f"\nDone! {len(images)} frames saved to {output_dir}/")
    # Output metadata for manifest
    seed = data.get("last_response", data).get("seed", None)
    if seed:
        print(f"Seed: {seed}")


if __name__ == "__main__":
    main()
