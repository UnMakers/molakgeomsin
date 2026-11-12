# 0.4.29 누끼 다듬기 (BiRefNet 컷아웃 결과를 받아)
# 1) 흰 바탕이 섞인 가장자리 색을 걷어 낸다(색 오염 제거: C = (C - (1-a)*흰색)/a)
# 2) 알파를 1px 깎고 살짝 흐려 흰 테두리(헤일로)를 없앤다
# 3) 작은 떠돌이 조각(먼지 같은 알파 섬)은 지운다
# 4) 인물이 그림 테두리에 닿아 잘린 곳만 부드럽게 흐린다(네모로 끊겨 보이지 않게)
import sys, os, glob
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

def nukki(src, dst, flip=False):
    im = Image.open(src).convert('RGBA')
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    a8 = np.asarray(im).astype(np.float32) / 255.0
    rgb, a = a8[..., :3], a8[..., 3]
    h, w = a.shape
    # 3) 떠돌이 조각
    solid = a > 0.5
    lab, n = ndi.label(solid)
    if n > 1:
        sizes = ndi.sum(solid, lab, range(1, n + 1))
        keep = np.zeros(n + 1, bool); keep[1:] = sizes >= max(400, sizes.max() * 0.004)
        mask = keep[lab]
        near = ndi.binary_dilation(mask, iterations=3)
        a = np.where(near, a, 0)
    # 안쪽 작은 구멍 메우기(알파가 비어 있는 인물 안쪽)
    filled = ndi.binary_fill_holes(a > 0.5)
    holes = filled & (a <= 0.5)
    hl, hn = ndi.label(holes)
    if hn:
        hs = ndi.sum(holes, hl, range(1, hn + 1))
        small = np.zeros(hn + 1, bool); small[1:] = hs < 2500
        a = np.where(small[hl], 1.0, a)
    # 2) 알파 1px 깎고 살짝 흐림
    a = ndi.grey_erosion(a, size=(3, 3))
    a = ndi.gaussian_filter(a, 0.6)
    # 1) 흰 바탕 섞임 걷기
    safe = np.maximum(a, 1e-3)[..., None]
    fg = (rgb - (1 - a[..., None]) * 1.0) / safe
    fg = np.where(a[..., None] > 0.02, np.clip(fg, 0, 1), rgb)
    # 가장자리(알파 0.05~0.95)는 조금 더 먹색 쪽으로 — 밝은 테두리가 남지 않게
    edge = (a > 0.02) & (a < 0.95)
    fg[edge] = fg[edge] * 0.85
    # 4) 테두리에 닿은 곳만 흐림
    m = int(w * 0.09)
    x = np.arange(w); y = np.arange(h)
    fx = np.clip(np.minimum(x, w - 1 - x) / m, 0, 1); fx = fx * fx * (3 - 2 * fx)
    fy = np.clip(y / (m * 0.8), 0, 1); fy = fy * fy * (3 - 2 * fy)
    a = a * fy[:, None] * fx[None, :]
    out = np.dstack([fg, a])
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').save(dst, optimize=True)

if __name__ == '__main__':
    sd, od = sys.argv[1], sys.argv[2]
    flips = set(sys.argv[3].split(',')) if len(sys.argv) > 3 else set()
    os.makedirs(od, exist_ok=True)
    for p in sorted(glob.glob(os.path.join(sd, 'char_*.png'))):
        n = os.path.basename(p)
        nukki(p, os.path.join(od, n), n in flips)
        print('ok', n)
