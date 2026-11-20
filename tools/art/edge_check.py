"""입상 잘림 검사 — 뽑은 그림을 쓰기 전에 돌린다.

기준(언메이커: 좌우 팔·위 머리가 잘리지 않게):
  · 위: 인물(불투명 화소)이 맨 위에서 TOP 화소 안으로 들어오면 머리가 잘린 것.
  · 좌우: 인물이 왼쪽·오른쪽 끝 SIDE 화소 안에 닿으면 팔·소매·무기가 잘린 것.
  · 아래: 허벅지에서 끊기는 구도라 아래는 닿아도 된다.
누끼(알파)를 딴 PNG 에서 재고, 알파가 없으면 바탕(밝은 종이색)과 다른 화소로 잰다.

  python3 tools/art/edge_check.py 그림.png [그림2.png …]
  잘린 그림이 하나라도 있으면 종료 코드 1 — 그 그림은 다시 뽑는다(그림마다 다섯 번까지).
"""
import sys
from PIL import Image

TOP, SIDE = 24, 16          # 832×1216 기준 여백(화소)
ALPHA_ON = 40               # 이보다 짙은 알파는 인물
BG_DIFF = 38                # 알파가 없을 때: 바탕과 이만큼 다르면 인물


def body_mask(im):
    if im.mode in ("RGBA", "LA") and im.getextrema()[-1][0] < 250:
        a = im.getchannel("A")
        return a.point(lambda v: 255 if v > ALPHA_ON else 0)
    g = im.convert("L")
    w, h = g.size
    corners = [g.getpixel((2, 2)), g.getpixel((w - 3, 2)), g.getpixel((2, h // 2)), g.getpixel((w - 3, h // 2))]
    bg = sorted(corners)[len(corners) // 2]
    return g.point(lambda v: 255 if abs(v - bg) > BG_DIFF else 0)


def check(path):
    im = Image.open(path)
    m = body_mask(im)
    w, h = m.size
    sx = w / 832.0
    top, side = int(TOP * sx), int(SIDE * sx)
    bad = []

    def touches(box):
        return m.crop(box).getbbox() is not None

    if touches((0, 0, w, top)):
        bad.append("위(머리)")
    if touches((0, 0, side, int(h * 0.85))):
        bad.append("왼쪽(팔·소매)")
    if touches((w - side, 0, w, int(h * 0.85))):
        bad.append("오른쪽(팔·소매)")
    return bad


def main(paths):
    worst = 0
    for p in paths:
        bad = check(p)
        if bad:
            worst = 1
            print("잘림  " + p + "  — " + ", ".join(bad))
        else:
            print("통과  " + p)
    return worst


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
