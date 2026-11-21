#!/usr/bin/env python3
"""대사 한 줄 띄기 — 서술 끝에 붙은 대사를 제 줄로 뗀다.

기준(0.4.88): 대사는 제 줄을 가진다. 그래야 MolakFlow 가 대사 앞뒤로 빈 줄을 넣고,
말하는 사람 그림·오른쪽 정렬 규칙도 그 줄에 걸린다.
  · 「서술. "대사"」         → 서술 / "대사"
  · 「서술. "대사" 서술.」   → 서술 / "대사" / 서술.
  · 「하진무가 "밤마실이오?" 하고 묻기에」처럼 따옴표 뒤에 조사·'하고'가 붙거나,
    「"산 채로"라는 말」처럼 낱말을 인용한 것은 그대로 둔다(문장 가운데 인용).
사용: split_dialogue.py <nani 폴더> [--dry]
"""
import glob, io, os, re, sys

Q = re.compile(r'"[^"]+"')
END = re.compile(r'[.!?…다요죠네군가지]\s*$')          # 앞 서술이 문장으로 끝났는가
PART = re.compile(r'^(?:하고|하며|라고|라는|라며|이라|이라고|란|를|을|은|는|이|가|로|으로|에|와|과|도|만|까지|처럼|하는|하던|한|하자|하기에|하면)')


def split_line(t):
    if not t or t[0] in '@#;' or t.lstrip().startswith(('"', '“', '「', "'", '<')):
        return [t]
    out, rest = [], t
    while True:
        m = Q.search(rest)
        if not m: break
        pre, q, post = rest[:m.start()], m.group(0), rest[m.end():]
        if not pre.strip() or not END.search(pre):
            return [t] if not out else out + [rest]
        p2 = post.lstrip()
        if p2 and PART.match(p2):
            return [t] if not out else out + [rest]
        if len(q) < 6:          # 짧은 낱말 인용("예", "응")은 서술 속에 둔다
            return [t] if not out else out + [rest]
        out += [pre.rstrip(), q]
        rest = p2
        if not rest: return out
        if not Q.search(rest):
            return out + [rest]
    return [t] if not out else out + [rest]


def main():
    d = sys.argv[1]; dry = '--dry' in sys.argv; tot = 0
    for p in sorted(glob.glob(os.path.join(d, '*.nani'))):
        raw = io.open(p, encoding='utf-8-sig').read()
        L = raw.split('\n'); N = []; n = 0
        for l in L:
            parts = split_line(l)
            if len(parts) > 1:
                n += 1
                if dry and n <= 3: print('  ', os.path.basename(p), '|', ' ⏎ '.join(parts)[:150])
            N += parts
        if n:
            tot += n; print(os.path.basename(p), n)
            if not dry: io.open(p, 'w', encoding='utf-8').write('\n'.join(N))
    print('합계', tot)


if __name__ == '__main__':
    main()
