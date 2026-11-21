#!/usr/bin/env python3
"""붉은 눈 걷기 — 선한 쪽 민간인 입상은 눈을 붉게 두지 않는다.

기준(0.4.88): 교·사술·악역과 상관없는 객잔·약방·저잣거리 사람과 아군(하진무·할멈·청묘·곽산·용개)은
눈동자를 붉게 두지 않는다. 머리띠·상투 끈 같은 붉은 소품은 남긴다.

방법: 불투명 영역 맨 위에서 --band(기본 0.27) 비율까지의 머리 띠 안에서 붉은 화소를 덩어리로 묶고,
작은 덩어리(눈동자 크기: 넓이 ≤ --maxarea, 폭·높이 ≤ --maxw)만 같은 밝기의 회색으로 바꾼다.
넓고 긴 덩어리(머리띠·끈)는 그대로 둔다. 알파는 건드리지 않는다.

사용: eye_gray.py <in.png> <out.png> [--band 0.27] [--maxarea 1500] [--maxw 70]
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage


def arg(a, k, d):
    if k in a:
        i = a.index(k); v = float(a[i + 1]); del a[i:i + 2]; return v
    return d


def main():
    a = sys.argv[1:]
    band = arg(a, '--band', 0.27); maxarea = arg(a, '--maxarea', 1500); maxw = arg(a, '--maxw', 70)
    src, dst = a[0], a[1]
    im = np.array(Image.open(src).convert('RGBA')).astype(np.int32)
    r, g, b, al = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    ys = np.where((al > 40).any(axis=1))[0]
    top, bot = ys[0], ys[-1]
    y1 = top + int((bot - top) * band)
    mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1), 0)
    red = (al > 10) & (r > g * 1.35) & (r > b * 1.25) & (sat > 0.28)
    red[y1:, :] = False
    lab, n = ndimage.label(ndimage.binary_dilation(red, iterations=2))
    keep = np.zeros_like(red)
    hit = 0
    for i, sl in enumerate(ndimage.find_objects(lab), 1):
        h = sl[0].stop - sl[0].start; w = sl[1].stop - sl[1].start
        m = (lab[sl] == i) & red[sl]
        if m.sum() <= maxarea and w <= maxw and h <= maxw:
            keep[sl] |= m; hit += 1
    l = (0.30 * r + 0.59 * g + 0.11 * b).astype(np.int32)
    out = im.copy()
    for c in range(3):
        out[..., c] = np.where(keep, l, im[..., c])
    Image.fromarray(out.astype(np.uint8), 'RGBA').save(dst)
    print(f'{src}: 머리 띠 y<{y1} 붉은 덩어리 {n}개 중 눈 크기 {hit}개({int(keep.sum())}화소) 회색')


if __name__ == '__main__':
    main()
