# -*- coding: utf-8 -*-
"""로라로 다시 그린 입상을 게임 그림으로 다듬는다(2026-11-23).

  python3 tools/art/lora/apply_redo.py 뽑은_cut.png 출력.png [--good] [--close 4|8]

순서: 누끼 다시 따기(바탕 빛 기준 — 머리칼 사이 바탕 덩어리·옅은 소매 구멍) → post_v21(붉은 점색 밖은 회색) → 누끼 가장자리 정리(birefnet 반투명 테두리의 밝은 띠를 걷는다)
      → (선한 쪽이면 eye_gray 로 눈동자 붉은 덩어리를 회색으로) → fit_portrait(832×1216, 위 40·좌우 24 여백) → edge_check
"""
import os, sys, subprocess, tempfile
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.dirname(HERE)


def drop_bg (src, dst, tol=9, close=4, fill=False):
    """누끼를 바탕 빛으로 다시 딴다(birefnet 의 실수 두 가지를 고친다).
    뽑은 그림의 바탕은 한 빛의 고른 회색이다(그림마다 150~220 사이로 다르다 — 누끼 밖 화소의 중앙값으로 잰다).
      · 머리칼 사이에 남은 바탕 덩어리(소아): 바탕 빛이고 바깥과 이어져 있으면 지운다.
      · 바탕과 같은 빛으로 옅게 칠한 소매(참새): 먹선을 close 화소만큼 이어 붙여 닫힌 안쪽은 인물로 본다.
        기본 4(머리칼 틈은 열어 둔다). 옷 속이 뚫리면 --close 8(머리칼 틈도 닫혀 바탕 덩어리가 남을 수 있으니 그림마다 보고 고른다).
      · 선을 이어 붙이며 바깥 둘레에 끼어든 바탕 빛 띠는 다시 걷고, 바깥에 닿은 2화소는 빛 차이만큼 반투명으로."""
    from scipy import ndimage
    im = np.asarray(Image.open(src).convert('RGBA')).copy()
    a = im[..., 3]; rgb = im[..., :3].astype(np.int16)
    out = a < 8
    if out.sum() < 1000:
        Image.fromarray(im, 'RGBA').save(dst); return 0
    bg = np.median(rgb[out], 0)
    dist = np.abs(rgb - bg).max(2)
    near = dist <= tol
    closed = ndimage.binary_closing(~near, structure=np.ones((3, 3)), iterations=close)
    # 입상은 아래가 잘려 있어 옷자락이 아래 끝에 닿는다 — 아래 끝에서는 바탕을 번지게 하지 않는다(옷 속이 바닥에서부터 뚫렸다: 할멈)
    border = np.zeros_like(near); border[0, :] = border[:, 0] = border[:, -1] = True
    reach = ndimage.binary_propagation(border & ~closed, mask=~closed)
    key = ~reach
    key &= ~(near & ndimage.binary_dilation(reach, iterations=close + 1))
    key = ndimage.binary_opening(key, iterations=1)
    # birefnet 누끼와 합친다: birefnet 이 놓친 옷 속(바탕빛, 먹선으로 닫힘)은 더하고,
    # birefnet 이 인물로 본 바탕빛 중 '작은 덩어리'(머리칼 틈)만 뺀다. 크거나 아래 끝에 닿는 덩어리는 옷자락이다(할멈 치마가 통째로 뚫렸다).
    bfg = a > 127
    cand = bfg & ~key & near
    lab, k = ndimage.label(cand)
    drop = np.zeros_like(cand)
    if k:
        sizes = ndimage.sum(cand, lab, range(1, k + 1))
        h = cand.shape[0]
        low = set(np.unique(lab[int(h * 0.88):])) - {0}
        lim = 0.02 * cand.size
        for i, sz in enumerate(sizes, 1):
            if sz < lim and i not in low: drop |= lab == i
    fig = ((bfg | key) if fill else bfg) & ~drop   # fill: birefnet 이 놓친 닫힌 옷 속까지 더한다(--close 를 줬을 때만 — 팔과 몸 사이 바탕이 덩어리로 낄 수 있다)
    edge = fig & ndimage.binary_dilation(~fig, iterations=2)
    alpha = np.where(fig, np.where(bfg, np.maximum(a, 0).astype(np.float64), 255.0), 0.0)
    alpha[fig & ~bfg & edge] = np.clip(dist[fig & ~bfg & edge] * 255.0 / 60.0, 0, 255)
    alpha[drop] = 0
    changed = int(((alpha > 127) != (a > 127)).sum())
    im[..., 3] = alpha.astype(np.uint8)
    Image.fromarray(im, 'RGBA').save(dst)
    return changed


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
    close = int(sys.argv[sys.argv.index('--close') + 1]) if '--close' in sys.argv else 4
    print('누끼 고침', drop_bg(src, z, close=close, fill='--close' in sys.argv))
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
