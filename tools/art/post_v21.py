"""v21 입상 후처리 — 붉은 점색 밖의 색을 회색으로(하늘색 눈·금장식 같은 새는 색을 뺀다). 알파는 그대로.
  python3 tools/art/post_v21.py 입력.png 출력.png"""
import sys, numpy as np
from PIL import Image
im = np.asarray(Image.open(sys.argv[1]).convert("RGBA")).astype(np.float32)
rgb = im[..., :3] / 255.0
mx, mn = rgb.max(2), rgb.min(2); sat = (mx - mn) / np.maximum(mx, 1e-3)
r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
red = (r > g * 1.35) & (r > b * 1.35) & (sat > 0.30)
gray = (rgb * np.array([0.299, 0.587, 0.114])).sum(2, keepdims=True).repeat(3, 2)
out = np.where(red[..., None], rgb, gray)
im[..., :3] = out * 255.0
Image.fromarray(im.clip(0, 255).astype(np.uint8), "RGBA").save(sys.argv[2])
