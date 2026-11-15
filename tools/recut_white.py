# 0.4.63 입상 누끼 다시 따기 — 남은 흰 바탕(인물 둘레·팔과 몸 사이·사각으로 잘린 흰 덩어리)을 지운다.
# 이미 투명한 곳과 그림 테두리에서 출발해, 밝고 색이 없는 픽셀만 따라 번진다(먹 선에서 멈춘다).
# 지운 자리의 경계는 밝기에 따라 부드럽게 빼서 흰 테가 남지 않게 한다. 옅은 안개(반투명·밝음)도 지운다.
import sys, os, glob
import numpy as np
from PIL import Image
from scipy import ndimage
def recut(src, dst):
    im = Image.open(src).convert('RGBA'); a = np.asarray(im).astype(np.float32) / 255
    rgb = a[..., :3]; al = a[..., 3].copy()
    lum = rgb.mean(2); sat = rgb.max(2) - rgb.min(2)
    whiteish = (lum > 0.86) & (sat < 0.10)
    seeds = (al < 0.05)
    seeds[0, :] = seeds[-1, :] = True; seeds[:, 0] = seeds[:, -1] = True
    # 번질 수 있는 곳: 이미 투명 or 흰 기운
    passable = whiteish | (al < 0.05)
    lab, n = ndimage.label(passable)
    keep_lab = np.unique(lab[seeds & passable])
    bg = np.isin(lab, keep_lab[keep_lab > 0])
    # 흰 덩어리 중 아주 작은 것은 살린다(눈 흰자·이빨 등은 바깥과 안 이어지지만, 혹시 이어져도 작은 건 둔다)
    newal = al.copy()
    newal[bg & whiteish] = 0.0
    # 경계 부드럽게: 지운 곳과 맞닿은 1~2px 의 밝은 픽셀은 밝기만큼 뺀다
    ring = ndimage.binary_dilation(bg & whiteish, iterations=2) & ~(bg & whiteish)
    soft = np.clip((0.95 - lum) / 0.30, 0, 1)
    newal[ring] = np.minimum(newal[ring], np.maximum(soft[ring], 0.0) * al[ring] + (1 - al[ring]) * 0)
    # 옅은 안개 지우기
    newal[(newal < 0.35) & (lum > 0.55)] = 0.0
    # 테두리 1px 투명
    newal[0, :] = newal[-1, :] = 0; newal[:, 0] = newal[:, -1] = 0
    out = np.dstack([rgb, newal]); Image.fromarray((out * 255).astype(np.uint8), 'RGBA').save(dst)
    return float((al - newal).clip(0).sum() / al.size)
if __name__ == '__main__':
    src, dst = sys.argv[1], sys.argv[2]
    os.makedirs(dst, exist_ok=True)
    for f in sorted(glob.glob(os.path.join(src, 'char_*.png'))):
        r = recut(f, os.path.join(dst, os.path.basename(f)))
        print(os.path.basename(f), round(r, 4))
