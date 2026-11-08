import os
import shutil
from PyQt6.QtSvg import QSvgRenderer
from PyQt6.QtGui import QImage, QPainter, QColor
from PyQt6.QtCore import QSize, Qt, QRectF
from PIL import Image

SVG_SOURCE = r"C:\Users\ColdLuv\Desktop\Logo_SilkThread.svg"
ASSETS_DIR = r"c:\Users\ColdLuv\Desktop\SilkThread\src\SilkThread.App\Assets"

def render_svg_to_png(svg_path, out_png_path, width, height, bg_color=None):
    renderer = QSvgRenderer(svg_path)
    if not renderer.isValid():
        raise ValueError(f"Invalid SVG file: {svg_path}")
    
    img = QImage(width, height, QImage.Format.Format_ARGB32)
    if bg_color:
        img.fill(QColor(bg_color))
    else:
        img.fill(0) # transparent
    
    painter = QPainter(img)
    painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
    painter.setRenderHint(QPainter.RenderHint.SmoothPixmapTransform, True)
    renderer.render(painter)
    painter.end()
    
    img.save(out_png_path)
    print(f"Rendered {width}x{height} -> {out_png_path}")

def render_splash_screen(svg_path, out_png_path, width=1240, height=600, logo_size=200, bg_hex="#0B0D13"):
    renderer = QSvgRenderer(svg_path)
    img = QImage(width, height, QImage.Format.Format_ARGB32)
    img.fill(QColor(bg_hex))
    
    painter = QPainter(img)
    painter.setRenderHint(QPainter.RenderHint.Antialiasing, True)
    painter.setRenderHint(QPainter.RenderHint.SmoothPixmapTransform, True)
    
    target_x = float((width - logo_size) // 2)
    target_y = float((height - logo_size) // 2)
    renderer.render(painter, QRectF(target_x, target_y, float(logo_size), float(logo_size)))
    painter.end()
    
    img.save(out_png_path)
    print(f"Rendered Splash {width}x{height} -> {out_png_path}")

def create_ico(source_png, out_ico_path):
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
    img = Image.open(source_png)
    img.save(out_ico_path, format="ICO", sizes=sizes)
    print(f"Created multi-res ICO -> {out_ico_path}")

def main():
    os.makedirs(ASSETS_DIR, exist_ok=True)
    
    # 1. Copy raw SVG
    dest_svg = os.path.join(ASSETS_DIR, "Logo_SilkThread.svg")
    shutil.copy2(SVG_SOURCE, dest_svg)
    print(f"Copied SVG -> {dest_svg}")
    
    # 2. Render high-res PNGs
    logo_512 = os.path.join(ASSETS_DIR, "Logo_SilkThread_512.png")
    logo_256 = os.path.join(ASSETS_DIR, "Logo_SilkThread.png")
    render_svg_to_png(SVG_SOURCE, logo_512, 512, 512)
    render_svg_to_png(SVG_SOURCE, logo_256, 256, 256)
    
    # 3. Create AppIcon.ico
    app_ico = os.path.join(ASSETS_DIR, "AppIcon.ico")
    create_ico(logo_512, app_ico)
    
    # 4. Standard WinUI Assets
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "Square44x44Logo.scale-200.png"), 88, 88)
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "Square150x150Logo.scale-200.png"), 300, 300)
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "StoreLogo.png"), 50, 50)
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "Square44x44Logo.targetsize-24_altform-unplated.png"), 24, 24)
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "Square44x44Logo.targetsize-48_altform-lightunplated.png"), 48, 48)
    render_svg_to_png(SVG_SOURCE, os.path.join(ASSETS_DIR, "LockScreenLogo.scale-200.png"), 48, 48)
    
    # 5. Splash and Wide banners
    render_splash_screen(SVG_SOURCE, os.path.join(ASSETS_DIR, "SplashScreen.scale-200.png"), 1240, 600, 200, "#0B0D13")
    render_splash_screen(SVG_SOURCE, os.path.join(ASSETS_DIR, "Wide310x150Logo.scale-200.png"), 620, 300, 120, "#0B0D13")
    
    print("All assets successfully generated!")

if __name__ == "__main__":
    main()
