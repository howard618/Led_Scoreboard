#!/usr/bin/env python3

from PIL import Image, ImageEnhance, ImageFilter
import argparse
from pathlib import Path

def make_pixel_logo(input_path, output_path, size):
    input_path = Path(input_path)
    output_path = Path(output_path)

    img = Image.open(input_path).convert("RGBA")

    # Trim transparent/empty border if present
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)

    # Make logo pop before shrinking
    img = ImageEnhance.Contrast(img).enhance(1.45)
    img = ImageEnhance.Color(img).enhance(1.65)
    img = ImageEnhance.Sharpness(img).enhance(2.0)

    # Resize down using high-quality sampling first
    img.thumbnail((size, size), Image.Resampling.LANCZOS)

    # Center on transparent square
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    x = (size - img.width) // 2
    y = (size - img.height) // 2
    canvas.alpha_composite(img, (x, y))

    # Reduce color count for 8-bit / retro look
    quantized = canvas.convert("P", palette=Image.Palette.ADAPTIVE, colors=12)
    quantized = quantized.convert("RGBA")

    # Snap alpha to fully transparent or fully visible
    pixels = quantized.load()
    for y in range(size):
        for x in range(size):
            r, g, b, a = pixels[x, y]
            if a < 90:
                pixels[x, y] = (0, 0, 0, 0)
            else:
                pixels[x, y] = (r, g, b, 255)

    # Add subtle black outline for LED readability
    alpha = quantized.getchannel("A")
    outline = alpha.filter(ImageFilter.MaxFilter(3))
    outline_img = Image.new("RGBA", (size, size), (0, 0, 0, 255))
    outlined = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    outlined.alpha_composite(outline_img)
    outlined.putalpha(outline)

    final = Image.alpha_composite(outlined, quantized)

    # Convert transparent pixels to black for LED matrix
    background = Image.new("RGBA", (size, size), (0, 0, 0, 255))
    background.alpha_composite(final)

    output_path.parent.mkdir(parents=True, exist_ok=True)

    png_path = output_path.with_suffix(".png")
    ppm_path = output_path.with_suffix(".ppm")

    background.save(png_path)
    background.convert("RGB").save(ppm_path)

    print(f"Saved {png_path}")
    print(f"Saved {ppm_path}")

def main():
    parser = argparse.ArgumentParser(description="Convert a logo into a Tecmo-style LED pixel logo.")
    parser.add_argument("input", help="Input logo PNG")
    parser.add_argument("output", help="Output path without extension, or with .png/.ppm")
    parser.add_argument("--size", type=int, default=16, help="Output size, default 16")

    args = parser.parse_args()

    output = Path(args.output)
    if output.suffix:
        output = output.with_suffix("")

    make_pixel_logo(args.input, output, args.size)

if __name__ == "__main__":
    main()
