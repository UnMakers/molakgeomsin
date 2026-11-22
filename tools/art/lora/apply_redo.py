# -*- coding: utf-8 -*-
"""로라로 다시 그린 입상을 게임 그림으로 다듬는다(2026-11-23).

  python3 tools/art/lora/apply_redo.py 뽑은_cut.png 출력.png [--good]

순서: 바탕 덩어리 걷기(머리칼 사이 남은 회색) → post_v21(붉은 점색 밖은 회색) → 누끼 가장자리 정리(birefnet 반투명 테두리의 밝은 띠를 걷는다)
      → (선한 쪽이면 eye_gray 로 눈동자 붉은 덩어리를 회색으로) → fit_portrait(832×1216, 위 40·좌우 24 여백) → edge_check
"""
import os, sys, subprocess, tempfile
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)


def drop_bg (src, dst, tol=9):
    """birefnet 이 머리칼 사이에 남긴 바탕 회색 덩어리를 걷는다.
    바탕 빛 = 누끼 밖(알파 0) 화소의 중앙값(뽑은 그림은 고른 회색이다). 누끼 밖에서 시작해 바탕 빛과 tol 안인 화소로만 번진 자리를 비운다.
    인물 안의 회색 옷은 먹선으로 막혀 있어 번지지 않는다."""
    from scipy import ndimage
    im = np.asarray(Image.open(src).convert('RGBA')).copy()
    a = im[..., 3]; rgb = im[..., :3].astype(np.int16)
    out = a < 8
    if out.sum() < 1000:
        Image.fromarray(im, 'RGBA').save(dst); return 0
    bg = np.median(rgb[out], 0)
    near = np.abs(rgb - bg).max(2) <= tol
    reach = ndimage.binary_propagation(out, mask=near | out)
    gone = reach & ~out
    # 걷힌 자리 둘레 1화소는 반투명으로(계단 없이)
    ring = ndimage.binary_dilation(reach, iterations=1) & ~reach
    a = a.astype(np.float32); a[gone] = 0; a[ring] = np.minimum(a[ring], 150)
    im[..., 3] = a.astype(np.uint8)
    Image.fromarray(im, 'RGBA').save(dst)
    return int(gone.sum())


def clean_edge (src, dst):
    """반투명 테두리(알파 8~200)에서 바탕 회색이 비친 밝은 화소를 안쪽 빛으로 바꾸고, 알파 8 미만은 비운다."""
    im = Image.open(src).convert('RGBA')
    a = np.asarray(im)[..., 3].astype(np.float32)
    rgb = np.asarray(im)[..., :3].astype(np.float32)
    inner = np.asarray(im.filter(ImageFilter.MinFilter(5)))[..., :3].astype(np.float32)
    rim = (a > 8) & (a < 200)
    lum = rgb.mean(2)
    halo = rim & (lum > 150)
    rgb[halo] = np.minimum(rgb[halo], inner[halo])
    a[a < 8] = 0
    out = np.dstack([rgb, a]).clip(0, 255).astype(np.uint8)
    Image.fromarray(out, 'RGBA').save(dst)


def main ():
    src, dst = sys.argv[1], sys.argv[2]
    good = '--good' in sys.argv
    t = tempfile.mkdtemp()
    z, a, b, c = os.path.join(t, 'z.png'), os.path.join(t, 'a.png'), os.path.join(t, 'b.png'), os.path.join(t, 'c.png')
    print('바탕 덩어리 걷음', drop_bg(src, z))
    subprocess.check_call([sys.executable, os.path.join(ART, 'post_v21.py'), z, a])
    clean_edge(a, b)
    if good:
        subprocess.check_call([sys.executable, os.path.join(ART, 'eye_gray.py'), b, c])
        b = c
    subprocess.check_call([sys.executable, os.path.join(ART, 'fit_portrait.py'), b, dst])
    rc = subprocess.call([sys.executable, os.path.join(ART, 'edge_check.py'), dst])
    sys.exit(rc)


if __name__ == '__main__':
    main()
