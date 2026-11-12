# -*- coding: utf-8 -*-
# 삽화 v2 후처리: 낙관/서명 제거, 종이색 정규화, (인물) 고립 조각 제거, (장면) 가장자리 페이드
import os, sys, glob, numpy as np
from PIL import Image
from scipy import ndimage

SRC = '/tmp/claude-0/-home-claude/86bad543-6f16-5694-8653-95ae7a680642/scratchpad/art11src'
OUT = '/mnt/user-data/outputs/art_v11'
os.makedirs(OUT, exist_ok=True)

def paper_color(rgb):
    # 밝은 픽셀(상위 3%)의 채널별 97 백분위 = 종이색
    lum = rgb.mean(axis=2)
    thr = np.percentile(lum, 85)
    sel = rgb[lum >= thr]
    return np.percentile(sel, 97, axis=0)

def normalize(rgb, paper):
    out = np.clip(rgb / np.maximum(paper, 1e-3), 0, 1)
    return out

def margin_mask(h, w, frac):
    m = np.zeros((h, w), bool)
    mh, mw = int(h*frac), int(w*frac)
    m[:mh, :] = True; m[-mh:, :] = True; m[:, :mw] = True; m[:, -mw:] = True
    return m

def red_mask(rgb):
    r, g, b = rgb[...,0], rgb[...,1], rgb[...,2]
    return (r > 0.45) & (r - g > 0.22) & (r - b > 0.22)

def dark_mask(rgb):
    return rgb.mean(axis=2) < 0.55

def fill_from_surroundings(rgb, comp_mask, paper):
    # 구멍 주변 고리의 중앙값으로 채움 (없으면 종이색)
    ring = ndimage.binary_dilation(comp_mask, iterations=12) & ~ndimage.binary_dilation(comp_mask, iterations=3)
    if ring.sum() > 20:
        col = np.median(rgb[ring], axis=0)
    else:
        col = paper
    noise = (np.random.rand(comp_mask.sum(), 3) - 0.5) * 0.01
    rgb[comp_mask] = np.clip(col + noise, 0, 1)

def remove_seals(rgb, paper, is_char_alpha=None):
    """가장자리 띠(18%)에 있는 작은 붉은 낙관 / 작은 검정 서명 덩어리 제거. 반환: 제거된 마스크"""
    h, w, _ = rgb.shape
    band = margin_mask(h, w, 0.18)
    removed = np.zeros((h, w), bool)
    area = h * w
    # 붉은 낙관
    rm = red_mask(rgb)
    lab, n = ndimage.label(ndimage.binary_dilation(rm, iterations=2))
    for i in range(1, n+1):
        comp = lab == i
        a = comp.sum()
        if a < 2 or a > area * 0.012: continue
        ys, xs = np.nonzero(comp)
        cy, cx = ys.mean(), xs.mean()
        # v11: 붉은 낙관은 좌우 가장자리 띠에서만 지운다(위쪽 깃발 같은 붉은 강조는 남김)
        if cx < w*0.18 or cx > w*0.82:
            removed |= ndimage.binary_dilation(comp, iterations=3)
    # 검정 서명(세로로 긴 작은 글자 기둥)
    dm = dark_mask(rgb) & band
    lab, n = ndimage.label(ndimage.binary_dilation(dm, iterations=4))
    for i in range(1, n+1):
        comp = lab == i
        a = comp.sum()
        if a > area * 0.006: continue
        ys, xs = np.nonzero(comp)
        bh, bw = np.ptp(ys)+1, np.ptp(xs)+1
        cy, cx = ys.mean(), xs.mean()
        if not band[int(cy), int(cx)]: continue
        tall = bh / max(bw, 1) > 1.8 and bh < h * 0.3
        tiny = a < area * 0.0012
        if tall or tiny:
            removed |= ndimage.binary_dilation(comp, iterations=3)
    if removed.any():
        lab, n = ndimage.label(removed)
        for i in range(1, n+1):
            fill_from_surroundings(rgb, lab == i, paper)
    return removed

def process_char(name):
    cut = np.asarray(Image.open(os.path.join(SRC, name)).convert('RGBA')).astype(np.float32) / 255.0
    opq = np.asarray(Image.open(os.path.join(SRC, name + '.opaque.png')).convert('RGB')).astype(np.float32) / 255.0
    h, w, _ = opq.shape
    paper = paper_color(opq)
    rgb = normalize(opq, paper)
    alpha = cut[..., 3].copy()
    # 1) 낙관/서명 제거 (불투명 원본 기준) → 해당 영역 알파 0
    removed = remove_seals(rgb, paper) if 'oldman' not in name else np.zeros(alpha.shape, bool)
    alpha[removed] = 0
    if 'oldman' in name:  # 배경까지 잡힌 컷아웃: 가장자리 페이드로 사각 경계 제거
        hh, ww = alpha.shape
        fx, fy = int(ww*0.10), int(hh*0.08)
        ax = np.ones(ww); ax[:fx] = np.linspace(0,1,fx); ax[-fx:] = np.linspace(1,0,fx)
        ay = np.ones(hh); ay[:fy] = np.linspace(0,1,fy); ay[-fy:] = np.linspace(1,0,fy)
        alpha = alpha * np.outer(ay, ax)
    # 2) 고립 조각: 가장 큰 덩어리 기준, 가장자리 띠에 있는 작은 조각 제거
    solid = alpha > 0.08
    lab, n = ndimage.label(solid)
    if n > 1:
        sizes = ndimage.sum(solid, lab, range(1, n+1))
        big = sizes.max()
        band = margin_mask(h, w, 0.16)
        for i, s in enumerate(sizes, 1):
            if s == big: continue
            comp = lab == i
            ys, xs = np.nonzero(comp)
            cy, cx = ys.mean(), xs.mean()
            if s < big * 0.04 and band[int(cy), int(cx)]:
                alpha[comp] = 0
    # 2b) 본체에 붙지 않은 작은 붉은 조각(낙관 잔재·튄 점) 제거
    solid = alpha > 0.08
    lab, n = ndimage.label(solid)
    if n > 1:
        sizes = ndimage.sum(solid, lab, range(1, n+1))
        bigi = int(np.argmax(sizes)) + 1
        rm = red_mask(rgb)
        for i in range(1, n+1):
            if i == bigi: continue
            comp = lab == i
            if sizes[i-1] < h*w*0.002 and rm[comp].mean() > 0.5:
                alpha[comp] = 0
    # 3) 잉크 밀도 기반 알파 보강: 종이색에 가까운 픽셀은 투명하게 (후광 제거)
    ink = 1.0 - rgb.mean(axis=2)            # 0=종이, 1=먹
    alpha = np.minimum(alpha, np.clip(ink * 3.2, 0, 1))
    # 붉은 악센트는 밝아도 유지
    alpha = np.where(red_mask(rgb) & (cut[..., 3] > 0.3), np.maximum(alpha, 0.9), alpha)
    out = np.dstack([rgb, alpha])
    Image.fromarray((out * 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, name))
    return removed.sum()

def process_scene(name):
    opq = np.asarray(Image.open(os.path.join(SRC, name)).convert('RGB')).astype(np.float32) / 255.0
    h, w, _ = opq.shape
    paper = paper_color(opq)
    rgb = normalize(opq, paper)
    removed = remove_seals(rgb, paper)
    # 가장자리 알파 페이드 (좌우 7%, 상하 9%)
    fx, fy = int(w * 0.07), int(h * 0.09)
    ax = np.ones(w); ax[:fx] = np.linspace(0, 1, fx); ax[-fx:] = np.linspace(1, 0, fx)
    ay = np.ones(h); ay[:fy] = np.linspace(0, 1, fy); ay[-fy:] = np.linspace(1, 0, fy)
    alpha = np.outer(ay, ax)
    alpha = alpha ** 0.8
    out = np.dstack([rgb, alpha])
    Image.fromarray((out * 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, name))
    return removed.sum()

np.random.seed(1)
for f in sorted(glob.glob(os.path.join(SRC, 'char_*.png'))):
    n = os.path.basename(f)
    if n.endswith('.opaque.png'): continue
    print(n, 'removed px:', process_char(n))
for f in sorted(glob.glob(os.path.join(SRC, 'scene_*.png'))):
    n = os.path.basename(f)
    print(n, 'removed px:', process_scene(n))
print('done')
