# -*- coding: utf-8 -*-
# 0.4.9: 대사와 서술이 한 줄에 붙은 것을 나눈다. "대사" 서술 → "대사" / 서술,  서술 "대사" → 서술 / "대사",  "대사" "대사" → 각각.
import io, re, glob, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
def split_line(l):
    out = []
    rest = l
    while True:
        rest = rest.strip()
        if not rest: break
        if rest.startswith('"'):
            m = re.match(r'^("(?:[^"\\]|\\.)*")(\s+)(\S.*)$', rest)
            if m and not re.match(r'^[가-힣]', m.group(3)[:0] or 'x') :
                out.append(m.group(1)); rest = m.group(3); continue
            out.append(rest); break
        # 서술 뒤에 대사가 붙은 경우: 문장 끝(.!?…」) 뒤 공백 + 따옴표
        m = re.match(r'^(.*?[.!?…」])(\s+)("(?:[^"\\]|\\.)*".*)$', rest)
        if m and '"' not in m.group(1):
            out.append(m.group(1)); rest = m.group(3); continue
        out.append(rest); break
    return out
total = 0
for p in sorted(glob.glob(os.path.join(C, '*.nani'))):
    L = io.open(p, encoding='utf-8').read().split('\n'); out = []; n = 0
    for l in L:
        if not l.strip() or l[0] in '@#;' or l.startswith('{dejapre}') or '<color' in l or '\\[' in l:
            out.append(l); continue
        parts = split_line(l)
        # 따옴표 짝이 안 맞는 조각이 생기면 나누지 않는다
        if len(parts) > 1 and all(pp.count('"') % 2 == 0 for pp in parts):
            out.extend(parts); n += 1
        else: out.append(l)
    if n: io.open(p, 'w', encoding='utf-8').write('\n'.join(out))
    print(os.path.basename(p), n); total += n
print('total', total)
