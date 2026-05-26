#!/usr/bin/env python3
"""Retro logo converter for LED scoreboard logos.

This script converts input team logo images into small retro-style images
that work better on low-resolution LED matrix displays.

It is intentionally sandboxed and writes output to a separate folder by default.
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path
from typing import Iterable

try:
    from PIL import Image, ImageOps
except ImportError as exc:
    raise SystemExit(
        "Pillow is required. Install it with: pip install pillow"
    ) from exc


DEFAULT_INPUT_DIR = Path("scoreboard/newlogos")
DEFAULT_OUTPUT_DIR = Path("scoreboard/pixel-logos-retro")
DEFAULT_SIZE = 16
DEFAULT_COLORS = 8


def normalize_image(image: Image.Image, size: int) -> Image.Image:
    image = image.convert("RGBA")

    # Trim transparent edges
    bbox = image.getbbox()
    if bbox is not None:
        image = image.crop(bbox)

    # Preserve aspect ratio and pad to square
    width, height = image.size
    square = max(width, height)
    base = Image.new("RGBA", (square, square), (0, 0, 0, 0))
    offset = ((square - width) // 2, (square - height) // 2)
    base.paste(image, offset, image)
    image = base

    # Make sure the smallest dimension is at least 1 before resize
    if square == 0:
        raise ValueError("Input image has no content")

    image = image.resize((size, size), resample=Image.LANCZOS)
    return image


def make_retro_palette(image: Image.Image, colors: int) -> Image.Image:
    # Convert to solid background if there is alpha
    if image.mode == "RGBA":
        background = Image.new("RGBA", image.size, (0, 0, 0, 0))
        background.paste(image, mask=image.split()[3])
        image = background.convert("RGB")
    else:
        image = image.convert("RGB")

    image = ImageOps.autocontrast(image)
    image = ImageOps.posterize(image, 3)

    palette_image = image.quantize(colors=colors, method=Image.MEDIANCUT, dither=Image.FLOYDSTEINBERG)
    return palette_image.convert("RGB")


def save_output(image: Image.Image, output_path: Path, use_ppm: bool) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)

    if use_ppm:
        image.save(output_path, format="PPM")
    else:
        image.save(output_path, format="PNG")


def find_source_files(input_dir: Path, extensions: Iterable[str]) -> list[Path]:
    return sorted(
        [path for path in input_dir.rglob("*") if path.suffix.lower() in extensions]
    )


def convert_logos(
    input_dir: Path,
    output_dir: Path,
    size: int,
    colors: int,
    use_ppm: bool,
    overwrite: bool,
) -> None:
    source_files = find_source_files(input_dir, {".png", ".jpg", ".jpeg", ".bmp", ".gif"})

    if not source_files:
        raise FileNotFoundError(f"No source logo files found in {input_dir}")

    print(f"Found {len(source_files)} input logos in {input_dir}")

    for source_path in source_files:
        relative = source_path.relative_to(input_dir)
        output_path = output_dir / relative.with_suffix(".ppm" if use_ppm else ".png")

        if output_path.exists() and not overwrite:
            print(f"Skipping existing output: {output_path}")
            continue

        print(f"Processing {source_path} -> {output_path}")

        with Image.open(source_path) as image:
            normalized = normalize_image(image, size)
            retro = make_retro_palette(normalized, colors)
            save_output(retro, output_path, use_ppm)

    print("Conversion complete.")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Convert ESPN team logos into retro LED matrix logos."
    )
    parser.add_argument(
        "--input-dir",
        type=Path,
        default=DEFAULT_INPUT_DIR,
        help=f"Source logo directory (default: {DEFAULT_INPUT_DIR})",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help=f"Output logo directory (default: {DEFAULT_OUTPUT_DIR})",
    )
    parser.add_argument(
        "--size",
        type=int,
        default=DEFAULT_SIZE,
        help="Target output logo size in pixels (width and height)",
    )
    parser.add_argument(
        "--colors",
        type=int,
        default=DEFAULT_COLORS,
        help="Number of colors in the generated retro palette",
    )
    parser.add_argument(
        "--ppm",
        action="store_true",
        help="Export output as PPM files instead of PNG",
    )
    parser.add_argument(
        "--overwrite",
        action="store_true",
        help="Overwrite existing output files",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()

    print("Retro logo converter")
    print(f"Input directory: {args.input_dir}")
    print(f"Output directory: {args.output_dir}")
    print(f"Size: {args.size}x{args.size}")
    print(f"Colors: {args.colors}")
    print(f"Export format: {'PPM' if args.ppm else 'PNG'}")
    print(f"Overwrite existing: {args.overwrite}")

    convert_logos(
        input_dir=args.input_dir,
        output_dir=args.output_dir,
        size=args.size,
        colors=args.colors,
        use_ppm=args.ppm,
        overwrite=args.overwrite,
    )


if __name__ == "__main__":
    main()
