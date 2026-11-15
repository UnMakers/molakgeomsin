# 0.4.63 입상 누끼 다시 따기 — isnet-anime(애니풍 인물 분할) 마스크로 남은 흰 바탕을 걷는다.
# 현재 입상(종이색 정규화본)을 흰 바탕에 얹어 마스크를 뽑고, 원래 알파와 겹친다(둘 다 인물이라 한 곳만 남김).
# 마스크 경계는 1px 흐리게, 옅은 안개(반투명·밝음)는 지운다. python3 recut_anime.py <입력폴더> <출력폴더> [파일...]
import sys, os, glob
import numpy as np
from PIL import Image, ImageFilter
from rembg import new_session, remove
def main():
    src, dst = sys.argv[1], sys.argv[2]; os.makedirs(dst, exist_ok=True)
    files = sys.argv[3:] or [os.path.basename(f) for f in sorted(glob.glob(os.path.join(src, 'char_*.png')))]
    s = new_session('isnet-anime')
    for f in files:
        im = Image.open(os.path.join(src, f)).convert('RGBA')
        white = Image.new('RGBA', im.size, (255, 255, 255, 255)); white.alpha_composite(im)
        m = remove(white.convert('RGB'), session=s, only_mask=True).filter(ImageFilter.GaussianBlur(0.8))
        m = np.asarray(m).astype(np.float32) / 255
        a = np.asarray(im).astype(np.float32) / 255
        lum = a[..., :3].mean(2)
        al = np.minimum(a[..., 3], np.clip((m - 0.08) / 0.84, 0, 1))
        al[(al < 0.35) & (lum > 0.6)] = 0
        al[0, :] = al[-1, :] = 0; al[:, 0] = al[:, -1] = 0
        Image.fromarray((np.dstack([a[..., :3], al]) * 255).astype(np.uint8), 'RGBA').save(os.path.join(dst, f))
        print(f, round(float((a[..., 3] - al).clip(0).mean()), 4))
main()
