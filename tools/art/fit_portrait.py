"""입상을 832×1216 판에 맞춰 앉힌다 — 잘림 방지 여백을 판 안에서 확보한다.

기준(edge_check 와 같은 여백):
  · 인물(알파) 덩어리를 잘라 낸 뒤, 좌우 SIDE·위 TOP 여백 안에 들어가도록 줄인다(키우지는 않는다 — 해상도 지킴).
  · 아래는 허벅지에서 끊기는 구도이므로 판 아래 끝에 붙인다(공중에 뜬 잘린 단면이 생기지 않게).
  · 좌우 가운데는 인물 덩어리의 가운데에 맞춘다.
  · 원본 맨 위에 좁게 끊긴 덩어리(상투·창 자루, CAP_MAX 이하)는 반타원 지붕을 위로 이어 그려 닫는다 — 납작한 단면이 남지 않게.
  · 넓게 끊긴 위 단면(흩날린 머리칼)과 좌우 끝에 끊긴 단면은 FADE 화소에 걸쳐 흐려 끝낸다.
  · 판 크기는 832×1216 고정. 해상도를 낮춰 뽑지 않는다.

  python3 tools/art/fit_portrait.py 입력.png 출력.png
"""
import sys
import numpy as np
from PIL import Image

W, H = 832, 1216
TOP, SIDE = 40, 24          # edge_check(24·16)보다 넉넉히
CAP_MAX = 200               # 이보다 좁게 위에 끊긴 덩어리만 지붕을 씌운다(원본 화소)
FADE = 22                   # 넓게 끊긴 위·좌우 단면을 흐리는 폭


def dome_top(arr):
    """맨 윗줄에 닿아 끊긴 덩어리마다 그 위로 반타원 지붕을 이어 그려 닫는다.
    지붕 빛은 끊긴 줄 바로 아래(2~5줄)의 빛을 열마다 그대로 쓴다 — 검은 상투는 검게, 창 자루는 자루 빛으로.
    반환: 지붕을 얹은 새 배열(위로 늘어난 높이만큼 커진다)"""
    a = arr[..., 3]
    on = a[:3].max(axis=0) > 40
    runs, s, e, gap = [], None, 0, 0
    for x in range(len(on) + 1):
        v = x < len(on) and on[x]
        if v:
            if s is None:
                s = x
            gap = 0; e = x
        elif s is not None:
            gap += 1
            if gap > 3 or x == len(on):
                runs.append((s, e + 1)); s = None; gap = 0
    # 넓은 덩어리(흩날린 머리칼 등)는 지붕을 씌우면 투구처럼 보인다 → 흐려 끝낸다. 좁은 것(상투·자루)만 지붕.
    wide = [(x0, x1) for x0, x1 in runs if x1 - x0 > CAP_MAX]
    runs = [(x0, x1) for x0, x1 in runs if x1 - x0 <= CAP_MAX]
    if wide:
        ramp = np.linspace(0.0, 1.0, FADE)[:, None]
        for x0, x1 in wide:
            arr[:FADE, x0:x1, 3] = (arr[:FADE, x0:x1, 3] * ramp).astype(np.uint8)
    if not runs:
        return arr
    caps = [(x0, x1, (x1 - x0) / 2.0, int(max(3, (x1 - x0) / 2.0 * 0.8))) for x0, x1 in runs]
    ext = max(c[3] for c in caps)
    h, w = a.shape
    out = np.zeros((h + ext, w, 4), np.uint8)
    out[ext:] = arr
    src = arr[2:6]
    col = src[..., :3].mean(axis=0)               # 열마다 끊긴 줄 바로 아래 빛
    alp = src[..., 3].astype(np.float32).max(axis=0)
    for x0, x1, r, hgt in caps:
        cx = (x0 + x1) / 2.0
        xs = np.arange(max(0, x0 - 2), min(w, x1 + 2))
        # 끊긴 줄(0~1줄)의 덜 마른 알파도 지붕 빛으로 채운다
        for y in range(2):
            out[ext + y, xs, :3] = col[xs]; out[ext + y, xs, 3] = np.maximum(out[ext + y, xs, 3], alp[xs]).astype(np.uint8)
        for j in range(1, hgt + 1):
            t = j / float(hgt)
            half = r * np.sqrt(max(0.0, 1 - t * t))
            k = np.clip(1 - (np.abs(xs + 0.5 - cx) - half) / 1.5, 0, 1)
            y = ext - j
            out[y, xs, :3] = col[xs]
            out[y, xs, 3] = (alp[xs] * k).astype(np.uint8)
    return out


def fit(src, dst):
    im = Image.open(src).convert("RGBA")
    a = np.asarray(im)[..., 3]
    ys, xs = np.nonzero(a > 40)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    body = im.crop((x0, y0, x1, y1))
    bw, bh = body.size
    # 위에 닿은 끊긴 단면은 둥글게 깎는다(상투는 둥근 상투로, 머리칼 끝은 뾰족하게, 창 자루는 둥근 끝으로)
    if y0 <= 2:
        body = Image.fromarray(dome_top(np.asarray(body).copy()), "RGBA")
    # 좌우 끝에 닿아 끊긴 단면(머리칼·소매)은 흐려 끝낸다
    arr = np.asarray(body).copy()
    ramp = np.linspace(0.0, 1.0, FADE)[None, :]
    if x0 <= 2:
        arr[:, :FADE, 3] = (arr[:, :FADE, 3] * ramp).astype(np.uint8)
    if x1 >= im.size[0] - 2:
        arr[:, -FADE:, 3] = (arr[:, -FADE:, 3] * ramp[:, ::-1]).astype(np.uint8)
    body = Image.fromarray(arr, "RGBA")
    s = min(1.0, (W - 2 * SIDE) / bw, (H - TOP) / bh)
    nw, nh = max(1, round(bw * s)), max(1, round(bh * s))
    if s < 1.0:
        body = body.resize((nw, nh), Image.LANCZOS)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.alpha_composite(body, ((W - nw) // 2, H - nh))
    out.save(dst)
    return s


if __name__ == "__main__":
    print("배율 %.3f" % fit(sys.argv[1], sys.argv[2]))
