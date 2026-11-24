"""줄 기준 후보 찾기: 같은 동작·같은 생각이면 한 줄, 눈길이 옮겨 가면 새 줄.
연달아 나오는 서술 두 줄 가운데 '같은 주어가 이어지는' 것 / '주어 없이 앞 줄을 잇는' 것을 후보로 뽑는다.
판단은 사람이(Claude가) 한다. 이 도구는 후보만 낸다."""
import re, sys, glob, os
SUBJ = re.compile(r'^(\S+?)(이|가|은|는|께서)\s')
CONT = ('그러더니','그러다','그래도','그러고','그리고','이내','곧 ','다시 ','또 ','한참','그대로','그 뒤','그러자')
def narr(l):
    return l and l[0] not in '@#;"\'<{' and not l.startswith('「')
def subj(l):
    m = SUBJ.match(l); return m.group(1) if m else None
def cands(path):
    L = open(path, encoding='utf-8').read().split('\n')
    out = []
    for i in range(len(L)-1):
        a, b = L[i].strip(), L[i+1].strip()
        if not (narr(a) and narr(b)): continue
        if len(a) + len(b) > 80: continue
        sa, sb = subj(a), subj(b)
        why = None
        if sa and sb and sa == sb: why = 'same-subj'
        elif b.startswith(CONT): why = 'cont'
        elif sb is None and len(b) <= 18 and not b.endswith(('?', '.')) is False and len(a) <= 40: why = 'short-tail'
        if why: out.append((i+1, why, a, b))
    return out
if __name__ == '__main__':
    files = sys.argv[1:] or sorted(glob.glob(os.path.join(os.path.dirname(__file__), '../../unity-nani/*.nani')))
    tot = 0
    for f in files:
        c = cands(f); tot += len(c)
        for n, w, a, b in c: print(f"{os.path.basename(f)}:{n}\t{w}\t{a}  ⏎  {b}")
    print('total', tot, file=sys.stderr)
