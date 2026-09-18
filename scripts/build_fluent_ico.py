import math
from PIL import Image, ImageDraw, ImageFilter

def build_fluent_ico(output_path):
    size = 1024
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    
    # 1. Soft background radial ambient glow
    glow = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_glow = ImageDraw.Draw(glow)
    draw_glow.ellipse([140, 140, 884, 884], fill=(0, 120, 215, 110))
    glow = glow.filter(ImageFilter.GaussianBlur(60))
    img.alpha_composite(glow)

    # 2. Back Violet Ribbon Loop
    violet_layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_violet = ImageDraw.Draw(violet_layer)
    draw_violet.ellipse([220, 160, 800, 740], fill=(130, 45, 210, 240))
    
    # Gradient overlay on violet loop
    grad_v = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_grad_v = ImageDraw.Draw(grad_v)
    for y in range(160, 740):
        t = (y - 160) / 580.0
        r = int(194 * (1 - t) + 45 * t)
        g = int(133 * (1 - t) + 11 * t)
        b = int(255 * (1 - t) + 102 * t)
        draw_grad_v.line([(220, y), (800, y)], fill=(r, g, b, 230))
    violet_layer = Image.composite(grad_v, violet_layer, violet_layer.split()[3])
    
    # Violet dropshadow
    v_shadow = violet_layer.filter(ImageFilter.GaussianBlur(24))
    v_shadow_shifted = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    v_shadow_shifted.paste(v_shadow, (0, 20), v_shadow)
    img.alpha_composite(v_shadow_shifted)
    img.alpha_composite(violet_layer)

    # 3. Main 3D Blue Torus (Front Outer Shell)
    torus = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_torus = ImageDraw.Draw(torus)
    draw_torus.ellipse([160, 180, 864, 884], fill=(0, 120, 212, 255))
    
    # Vertical volumetric lighting gradient
    torus_grad = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_tgrad = ImageDraw.Draw(torus_grad)
    for y in range(180, 884):
        t = (y - 180) / 704.0
        r = int(71 * (1 - t) + 0 * t)
        g = int(197 * (1 - t) + 41 * t)
        b = int(251 * (1 - t) + 82 * t)
        draw_tgrad.line([(160, y), (864, y)], fill=(r, g, b, 255))
    torus = Image.composite(torus_grad, torus, torus.split()[3])

    # Inner cavity cutout
    cavity_mask = Image.new("L", (size, size), 255)
    draw_cav = ImageDraw.Draw(cavity_mask)
    draw_cav.ellipse([304, 324, 720, 740], fill=0)
    torus.putalpha(Image.composite(torus.split()[3], Image.new("L", (size, size), 0), cavity_mask))

    # Torus dropshadow
    t_shadow = torus.filter(ImageFilter.GaussianBlur(30))
    t_shadow_shifted = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    t_shadow_shifted.paste(t_shadow, (0, 28), t_shadow)
    img.alpha_composite(t_shadow_shifted)
    img.alpha_composite(torus)

    # 4. Dark Cavity Center
    center_cav = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_cc = ImageDraw.Draw(center_cav)
    draw_cc.ellipse([304, 324, 720, 740], fill=(19, 27, 38, 255))
    img.alpha_composite(center_cav)

    # 5. Central Prismatic Core ("A" apex shape)
    prism = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_prism = ImageDraw.Draw(prism)
    draw_prism.polygon([(512, 320), (360, 620), (664, 620)], fill=(0, 210, 255, 255))
    
    # Left facet highlight, right facet shading
    draw_prism.polygon([(512, 320), (360, 620), (512, 620)], fill=(128, 234, 255, 240))
    draw_prism.polygon([(512, 320), (512, 620), (664, 620)], fill=(0, 90, 158, 240))
    # Inner hole
    draw_prism.polygon([(512, 420), (448, 560), (576, 560)], fill=(19, 27, 38, 255))
    
    p_shadow = prism.filter(ImageFilter.GaussianBlur(16))
    p_shadow_shifted = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    p_shadow_shifted.paste(p_shadow, (0, 12), p_shadow)
    img.alpha_composite(p_shadow_shifted)
    img.alpha_composite(prism)

    # 6. Vibrant Amber Horizon Crossbar
    bar = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_bar = ImageDraw.Draw(bar)
    draw_bar.rounded_rectangle([390, 520, 634, 552], radius=16, fill=(255, 140, 0, 255))
    draw_bar.rounded_rectangle([420, 524, 604, 548], radius=12, fill=(255, 215, 60, 255))
    img.alpha_composite(bar)

    # 7. Specular bulb lighting
    spec = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw_spec = ImageDraw.Draw(spec)
    draw_spec.ellipse([340, 260, 480, 400], fill=(255, 255, 255, 200))
    draw_spec.ellipse([380, 290, 430, 340], fill=(255, 255, 255, 255))
    spec = spec.filter(ImageFilter.GaussianBlur(8))
    img.alpha_composite(spec)

    # Save multi-resolution Windows ICO
    sizes_list = [256, 128, 64, 48, 32, 16]
    img_256 = img.resize((256, 256), Image.Resampling.LANCZOS)
    img_256.save(output_path, format="ICO", sizes=[(s, s) for s in sizes_list])
    print(f"Created authentic multi-resolution ICO at: {output_path}")

if __name__ == "__main__":
    import os
_here = os.path.dirname(os.path.abspath(__file__))
build_fluent_ico(os.path.join(_here, "..", "src", "AizenSearch.App", "AizenSearch.ico"))
