#!/usr/bin/env python3
"""0.4.92 한자 글꼴 두 벌을 다시 뽑는다 — 기준: 게임이 쓰는 한자는 전부 글꼴 안에 있다(운영체제 글꼴로 빠지지 않는다).

  unity-font/MolakHanja.otf : Noto Serif CJK KR Regular 부분집합(Noto Serif KR 에 없는 丐 같은 글자도 들어 있다). 대본·코드·데이터에 나오는 한자 전부 + 지금 글꼴에 있던 글자.
  unity-font/MolakSeal.otf  : Noto Serif CJK KR Bold 부분집합. 인장 글자(코드의 한 글자짜리 한자 문자열 "通" 등) + 「」.
                              인장은 이 글꼴의 진짜 굵은 획으로 찍는다(가짜 굵게 없음).

빠졌던 것(0.4.91까지): 通·失·憶·醒·觀·岐·勢·先 이 MolakHanja 에 없어 인장이 기기 글꼴(윈도 맑은 고딕 등)로 찍혔다 —
같은 인장인데 境 은 명조, 通 은 고딕이었다.

쓰는 법: python3 tools/font_subset.py [--src-kr <NotoSerifCJK-Regular.ttc>] [--src-bold <NotoSerifCJK-Bold.ttc>]
원본 글꼴(SIL OFL)은 저장소에 두지 않는다. 없으면 기본 경로를 찾는다.
--check 만 주면 글꼴은 그대로 두고 빠진 글자만 보고한다(정적 검사가 부른다).
"""
import argparse, glob, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HAN = re.compile(r'[㐀-䶿一-鿿豈-﫿]')
SEAL_LIT = re.compile(r'"([一-鿿])"')


def used_hanja():
    s = set()
    files = glob.glob(os.path.join(ROOT, 'unity-src', '*.cs')) + glob.glob(os.path.join(ROOT, 'unity-nani', '*.nani')) \
        + glob.glob(os.path.join(ROOT, 'unity-data', 'MolakData', '*.txt'))
    for f in files:
        s.update(HAN.findall(open(f, encoding='utf-8-sig').read()))
    return s


def seal_hanja():
    """인장 글자: 코드 안의 한 글자짜리 한자 문자열(MolakVfx.Stamp("通", …), KindOf 의 case "失" 등)."""
    s = set()
    for f in glob.glob(os.path.join(ROOT, 'unity-src', '*.cs')):
        s.update(SEAL_LIT.findall(open(f, encoding='utf-8-sig').read()))
    return s


def cmap(path):
    from fontTools.ttLib import TTFont
    return set(TTFont(path, fontNumber=0).getBestCmap()) if os.path.exists(path) else set()


def first(paths):
    for p in paths:
        for q in glob.glob(os.path.expanduser(p)):
            return q
    return None


def kr_index(path):
    """모음 글꼴(.ttc)이면 KR 얼굴의 번호."""
    if not path.endswith('.ttc'): return 0
    from fontTools.ttLib import TTCollection
    names = [f['name'].getDebugName(1) for f in TTCollection(path).fonts]
    return next((i for i, n in enumerate(names) if n.split(' ')[-1] == 'KR' or n.endswith(' KR')), 0)


def subset(src, out, chars, font_number=0):
    from fontTools import subset as fs
    opt = fs.Options(); opt.font_number = font_number; opt.layout_features = ['*']; opt.name_IDs = ['*']; opt.name_languages = ['*']
    font = fs.load_font(src, opt)
    sub = fs.Subsetter(opt); sub.populate(unicodes=sorted(ord(c) for c in chars)); sub.subset(font)
    fs.save_font(font, out, opt)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--src-kr'); ap.add_argument('--src-bold'); ap.add_argument('--check', action='store_true')
    a = ap.parse_args()
    hanja_path = os.path.join(ROOT, 'unity-font', 'MolakHanja.otf')
    seal_path = os.path.join(ROOT, 'unity-font', 'MolakSeal.otf')
    used, seals = used_hanja(), seal_hanja()
    if a.check:
        mh, ms = cmap(hanja_path), cmap(seal_path)
        miss_h = sorted(c for c in used if ord(c) not in mh)
        miss_s = sorted(c for c in seals if ord(c) not in ms)
        print('한자 글꼴에 없는 글자', len(miss_h), ''.join(miss_h))
        print('인장 글꼴에 없는 글자', len(miss_s), ''.join(miss_s))
        return 1 if miss_h or miss_s else 0
    kr = a.src_kr or first(['/usr/share/fonts/opentype/noto/NotoSerifCJK-Regular.ttc'])
    bold = a.src_bold or first(['/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc'])
    if not kr or not bold:
        print('원본 글꼴을 찾지 못했다', kr, bold); return 2
    keep = {chr(u) for u in cmap(hanja_path)}
    subset(kr, hanja_path, keep | used | set('「」·…—'), kr_index(kr))
    subset(bold, seal_path, seals | set('「」'), kr_index(bold))
    print('MolakHanja', len(cmap(hanja_path)), '자 · MolakSeal', len(cmap(seal_path)), '자 ←', ''.join(sorted(seals)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
