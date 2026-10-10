from PIL import Image, ImageDraw
from pathlib import Path

out = Path("Nexos Network System/wwwroot/img/home/contact-map.jpg")
out.parent.mkdir(parents=True, exist_ok=True)

W, H = 1200, 480
img = Image.new("RGB", (W, H), "#eef2f8")
d = ImageDraw.Draw(img)

# grid
for x in range(0, W, 40):
    d.line([x, 0, x, H], fill="#dfe5ee", width=1)
for y in range(0, H, 40):
    d.line([0, y, W, y], fill="#dfe5ee", width=1)

# Shahrah-e-Faisal
d.line([60, 80, 1140, 360], fill="#c9d2e0", width=20)
# Clifton Road
d.line([470, 40, 520, 440], fill="#cdd6e2", width=14)
# secondary streets
d.line([0, 220, 1200, 240], fill="#dde3ec", width=8)
d.line([220, 0, 260, 480], fill="#dde3ec", width=8)
d.line([900, 0, 940, 480], fill="#dde3ec", width=8)
d.line([0, 380, 1200, 360], fill="#dde3ec", width=8)

# blocks
for i in range(8):
    bx = 120 + i * 110
    d.rectangle([bx, 150, bx + 60, 190], fill="#f3f6fb", outline="#cdd6e2")
for i in range(6):
    bx = 620 + i * 70
    d.rectangle([bx, 300, bx + 50, 334], fill="#f3f6fb", outline="#cdd6e2")

# labels
d.text((60, 108), "Shahrah", fill="#0b1f3a")
d.text((478, 410), "Clifton", fill="#111827")
d.text((120, 228), "Street", fill="#111827")
d.text((880, 60), "Road", fill="#111827")

# pin
px, py = 720, 240
d.ellipse([px - 6, py - 6, px + 6, py + 6], fill="#ff4d4d")
d.ellipse([px - 9, py - 9, px + 9, py + 9], outline="#ff4d4d", width=2)
d.line([px, py, px, py + 18], fill="#ff4d4d", width=3)

img.save(out, "JPEG", quality=88)
print("OK", out, out.stat().st_size)
