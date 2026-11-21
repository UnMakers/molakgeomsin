#!/usr/bin/env python3
"""장면 그림 후처리(0.4.88) — post_art_v11.process_scene 을 파일 하나씩 돌린다.
종이색 정규화 · 좌우 가장자리 낙관/서명 지우기 · 가장자리 알파 페이드. 1344×768 그대로.
사용: post_scene.py <in.png> <out.png> [--erase x0,y0,x1,y1 ...]  (--erase: 그 네모를 둘레 색으로 메운다 — 섞여 나온 인물 등)
"""
import sys, os, importlib.util
import numpy as np
from PIL import Image
from scipy import ndimage
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location('v11', os.path.join(HERE, '..', 'post_art_v11.py'))
src = open(spec.origin, encoding='utf-8').read().split('np.random.seed(1)')[0]   # 아래 일괄 실행부는 빼고 함수만
v11 = {}
exec(compile(src.replace("os.makedirs(OUT, exist_ok=True)", ""), spec.origin, 'exec'), v11)

def main():
    a = sys.argv[1:]; boxes = []
    while '--erase' in a:
        i = a.index('--erase'); boxes.append([int(v) for v in a[i + 1].split(',')]); del a[i:i + 2]
    inp, out = a[0], a[1]
    np.random.seed(1)
    opq = np.asarray(Image.open(inp).convert('RGB')).astype(np.float32) / 255.0
    h, w, _ = opq.shape
    paper = v11['paper_color'](opq)
    rgb = v11['normalize'](opq, paper)
    for x0, y0, x1, y1 in boxes:
        m = np.zeros((h, w), bool); m[y0:y1, x0:x1] = True
        v11['fill_from_surroundings'](rgb, m, paper)
        # 메운 자리 경계를 흐려 네모 자국을 없앤다
        soft = ndimage.gaussian_filter(m.astype(np.float32), 6)[..., None]
        blur = np.stack([ndimage.gaussian_filter(rgb[..., c], 8) for c in range(3)], -1)
        rgb = rgb * (1 - soft) + blur * soft
    v11['remove_seals'](rgb, paper)
    fx, fy = int(w * 0.07), int(h * 0.09)
    ax = np.ones(w); ax[:fx] = np.linspace(0, 1, fx); ax[-fx:] = np.linspace(1, 0, fx)
    ay = np.ones(h); ay[:fy] = np.linspace(0, 1, fy); ay[-fy:] = np.linspace(1, 0, fy)
    alpha = np.outer(ay, ax) ** 0.8
    Image.fromarray((np.dstack([rgb, alpha]) * 255).astype(np.uint8), 'RGBA').save(out)
    print('ok', out)

if __name__ == '__main__':
    main()
