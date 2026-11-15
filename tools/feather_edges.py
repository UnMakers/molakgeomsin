# 0.4.63 입상 가장자리 깃털 — 그림 틀에 잘린 소매·머리칼이 세로줄로 뚝 끊겨 '카드'처럼 보이던 것을 부드럽게 사라지게 한다.
# 실제 그림이 시작되는 칸(알파>0인 첫/끝 열·행)에서 안쪽으로 좌우 72px, 위 48px, 아래 0px(아래는 화면 밖으로 잘림) 동안 알파를 smoothstep 으로 올린다.
# python3 feather_edges.py <입력폴더> <출력폴더> [파일...]
import sys, os, glob
import numpy as np
from PIL import Image
def ss(t): t = np.clip(t, 0, 1); return t * t * (3 - 2 * t)
def main():
    src, dst = sys.argv[1], sys.argv[2]; os.makedirs(dst, exist_ok=True)
    files = sys.argv[3:] or [os.path.basename(f) for f in sorted(glob.glob(os.path.join(src, 'char_*.png')))]
    for f in files:
        im = np.asarray(Image.open(os.path.join(src, f)).convert('RGBA')).astype(np.float32)
        a = im[..., 3]; h, w = a.shape
        cols = np.where(a.max(0) > 8)[0]; rows = np.where(a.max(1) > 8)[0]
        if len(cols) == 0: continue
        x0, x1, y0 = cols[0], cols[-1], rows[0]
        xs = np.arange(w, dtype=np.float32); ys = np.arange(h, dtype=np.float32)
        # 틀에 잘린 쪽만 — 그 끝 열(행)에 진하게 칠해진 칸이 높이(너비)의 8% 넘게 있을 때. 손끝 하나 닿은 쪽은 건드리지 않는다.
        cutL = (a[:, min(x0 + 2, w - 1)] > 128).mean() > 0.08
        cutR = (a[:, max(x1 - 2, 0)] > 128).mean() > 0.08
        cutT = (a[min(y0 + 2, h - 1), :] > 128).mean() > 0.08
        fx = np.ones(w, np.float32)
        if cutL: fx = np.minimum(fx, ss((xs - x0) / 72.0))
        if cutR: fx = np.minimum(fx, ss((x1 - xs) / 72.0))
        fy = ss((ys - y0) / 48.0) if cutT else np.ones(h, np.float32)
        im[..., 3] = a * fx[None, :] * fy[:, None]
        Image.fromarray(im.clip(0, 255).astype(np.uint8), 'RGBA').save(os.path.join(dst, f))
        print(f, 'L' if cutL else '-', 'R' if cutR else '-', 'T' if cutT else '-')
main()
