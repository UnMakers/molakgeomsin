# 0.4.28 인물 그림 다시 오리기
# 예전 매트는 '밝을수록 투명'이라 얼굴·흰 옷 같은 인물 안쪽까지 비쳐, 뒤 배경이 인물을 뚫고 보였다.
# 이제: 테두리에서 이어진 바탕만 투명, 인물 윤곽 안쪽은 종이색으로 채워 불투명하게.
import sys, os, glob
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

PAPER = np.array([244, 237, 224], dtype=np.float32) / 255.0

def disk(r):
    y, x = np.ogrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= r * r

def process(src, dst):
    im = np.asarray(Image.open(src).convert('RGBA')).astype(np.float32) / 255.0
    h, w = im.shape[:2]
    rgb, a = im[..., :3], im[..., 3]
    # 원래 보이던 모습(종이 위에 곱해 얹은 것과 같게): 투명한 곳은 종이색
    on_paper = rgb * a[..., None] + PAPER * (1 - a[..., None])
    s = w / 832.0
    # 잉크가 있는 곳(어느 정도 불투명)
    ink = a > 0.22
    # 윤곽의 틈을 메운다 → 안쪽 구멍 채우기
    closed = ndi.binary_closing(ink, structure=disk(max(3, int(round(11 * s)))), iterations=1)
    closed = ndi.binary_fill_holes(closed)
    # 테두리에 닿은 덩어리 중 아주 작은 얼룩은 버린다
    lab, n = ndi.label(closed)
    if n:
        sizes = ndi.sum(closed, lab, range(1, n + 1))
        keep = np.zeros(n + 1, bool); keep[1:] = sizes >= (1500 * s * s)
        closed = keep[lab]
    # 가장자리는 부드럽게: 마스크를 조금 깎았다 흐리게 하고, 원래 알파와 큰 쪽을 쓴다
    soft = ndi.gaussian_filter(ndi.binary_erosion(closed, structure=disk(max(1, int(round(2 * s))))).astype(np.float32), sigma=1.6 * s)
    alpha = np.maximum(a, np.clip(soft, 0, 1))
    out = np.dstack([on_paper, alpha])
    Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').save(dst, optimize=True)
    return float((closed.mean()) * 100), float(((a < 0.1) & closed).mean() * 100)

if __name__ == '__main__':
    sd, od = sys.argv[1], sys.argv[2]
    os.makedirs(od, exist_ok=True)
    for p in sorted(glob.glob(os.path.join(sd, 'char_*.png'))):
        cov, filled = process(p, os.path.join(od, os.path.basename(p)))
        print('%-24s 인물 %4.1f%%  메운 안쪽 %4.1f%%' % (os.path.basename(p), cov, filled))
