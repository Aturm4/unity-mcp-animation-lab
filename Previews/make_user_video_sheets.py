from pathlib import Path
from PIL import Image, ImageDraw

src = Path(r"D:\game\My project\Previews\UserVideoFrames")
out = Path(r"D:\game\My project\Previews")

groups = {
    "forward": [1, 5, 9, 13, 17, 21, 24],
    "right": [29, 33, 37, 41, 45, 49, 52],
    "backward": [57, 61, 65, 69, 73, 77, 80],
    "left": [85, 89, 93, 97, 101, 105, 108],
    "transitions": [24, 25, 26, 27, 28, 29, 52, 53, 54, 55, 56, 57, 80, 81, 82, 83, 84, 85, 108, 109, 110, 111, 112, 113],
}

def sheet(name, frames, cols):
    tile_w, tile_h = 260, 400
    rows = (len(frames) + cols - 1) // cols
    canvas = Image.new("RGB", (cols * tile_w, rows * tile_h), (25, 25, 25))
    draw = ImageDraw.Draw(canvas)
    for idx, frame in enumerate(frames):
        image = Image.open(src / f"frame_{frame:04d}.png").convert("RGB")
        crop = image.crop((175, 150, 337, 400)).resize((tile_w, tile_h), Image.Resampling.LANCZOS)
        x = (idx % cols) * tile_w
        y = (idx // cols) * tile_h
        canvas.paste(crop, (x, y))
        draw.rectangle((x, y, x + 58, y + 24), fill=(0, 0, 0))
        draw.text((x + 6, y + 4), f"F{frame}", fill=(255, 255, 255))
    canvas.save(out / f"UserVideo_{name}_sheet.png")

for name, frames in groups.items():
    sheet(name, frames, 7 if name != "transitions" else 6)

direct = Path(r"D:\game\My project\Previews\DirectActionQA")
for action in ["Run_Forward", "Run_Right", "Run_Backward", "Run_Left"]:
    frames = list(range(1, 24, 2))
    tile_w, tile_h = 240, 300
    canvas = Image.new("RGB", (6 * tile_w, 2 * tile_h), (25, 25, 25))
    draw = ImageDraw.Draw(canvas)
    for idx, frame in enumerate(frames):
        image = Image.open(direct / f"{action}_{frame:02d}.png").convert("RGB")
        image.thumbnail((tile_w, tile_h), Image.Resampling.LANCZOS)
        x = (idx % 6) * tile_w + (tile_w - image.width) // 2
        y = (idx // 6) * tile_h
        canvas.paste(image, (x, y))
        draw.rectangle(((idx % 6) * tile_w, y, (idx % 6) * tile_w + 55, y + 24), fill=(0, 0, 0))
        draw.text(((idx % 6) * tile_w + 6, y + 4), f"F{frame}", fill=(255, 255, 255))
    canvas.save(out / f"DirectQA_{action}.png")
