# 0.4.55 누끼 다시 다듬기: 인물 둘레에 남은 흰 띠·흰 안개(그림 테두리 쪽으로 하얗게 번진 자리)를 걷는다.
# 1) 투명한 바깥에서 14px 안쪽까지, '거의 흰' 픽셀이 바깥과 이어져 있으면 지운다(검은 윤곽선에서 멈춘다).
# 2) 반투명 가장자리의 흰 바탕 섞임을 걷는다: C = (C - (1-a)·흰색) / a, 흰 기운만 남은 옅은 픽셀은 지운다.
# 3) 알파를 1px 깎고 0.6px 흐려 헤일로를 없앤다.
import sys
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

def run(src, dst):
    im = Image.open(src).convert('RGBA')
    f = np.asarray(im).astype(np.float32) / 255.0
    rgb, a = f[..., :3].copy(), f[..., 3].copy()
    lum = rgb.mean(-1); sat = rgb.max(-1) - rgb.min(-1)
    whiteish = (lum > 0.78) & (sat < 0.16)
    outside = a < 0.25
    dist = ndi.distance_transform_edt(~outside)
    zone = whiteish & (dist <= 14)
    lab, n = ndi.label(zone | outside)
    touch = np.unique(lab[outside]); touch = touch[touch > 0]
    kill = np.isin(lab, touch) & zone
    a[kill] = 0.0
    # 2) 흰 섞임 걷기
    semi = (a > 0.02) & (a < 0.98)
    aa = np.clip(a, 1e-3, 1.0)[..., None]
    dec = (rgb - (1.0 - aa)) / aa
    rgb = np.where(semi[..., None], np.clip(dec, 0, 1), rgb)
    lum2 = rgb.mean(-1)
    a[(a < 0.55) & (lum2 > 0.8)] = 0.0
    # 3) 1px 깎고 살짝 흐림
    core = a > 0.5
    core = ndi.binary_erosion(core, iterations=1)
    a = np.minimum(a, ndi.gaussian_filter(core.astype(np.float32), 0.6) * 1.15)
    # 4) 그림 테두리(왼쪽·오른쪽·위)에 닿아 잘린 자리는 흰색이 아니라 투명으로 스며들게
    h, w = a.shape
    yy, xx = np.mgrid[0:h, 0:w]
    edge = np.minimum(np.minimum(xx, w - 1 - xx), yy).astype(np.float32)
    ramp = np.clip(edge / 40.0, 0, 1); ramp = ramp * ramp * (3 - 2 * ramp)
    a = np.clip(a * ramp, 0, 1)
    out = np.dstack([rgb, a])
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), 'RGBA').save(dst)

if __name__ == '__main__':
    for s in sys.argv[1:]:
        run(s, s.replace('.png', '') + '_nk2.png' if False else None) if False else None
