"""앞 줄의 생각을 마저 잇는 '조각 줄'(서술어 없이 끝나는 줄) 찾기. 같은 생각이면 한 줄 기준."""
import re, sys, glob, os
VERB_END = tuple('다요오소까네군라')
def narr(l): return l and l[0] not in '@#;"\'<{「(' and not l.startswith('*')
def frag(b):
    b = re.sub(r'<[^>]+>', '', b)
    m = re.match(r'(.+?[.!?…])(\s|$)', b)
    if m: b = m.group(1)
    if '\'' in b or '"' in b: return False
    t = b.rstrip('.!…』」 ')
    t = re.sub(r'<[^>]+>', '', t).rstrip('.!…』」)" ')
    return bool(t) and not t.endswith(VERB_END) and not b.endswith('?')
def run(path, apply=False, skip=()):
    L = open(path, encoding='utf-8').read().split('\n')
    hits = []
    i = 0
    while i < len(L)-1:
        a, b = L[i], L[i+1]
        if narr(a.strip()) and narr(b.strip()) and a.rstrip().endswith(('.', '다', '…')) and frag(b.strip()) and len(a)+len(b) <= 95:
            hits.append((i+1, a.strip(), b.strip()))
        i += 1
    if apply:
        for n, _, _ in reversed(hits):
            if f"{os.path.basename(path)}:{n}" in skip: continue
            L[n-1] = L[n-1].rstrip() + ' ' + L[n].strip(); del L[n]
        open(path, 'w', encoding='utf-8').write('\n'.join(L))
    return hits
if __name__ == '__main__':
    apply = '--apply' in sys.argv
    skipf = [a for a in sys.argv[1:] if a.startswith('--skip=')]
    skip = set(open(skipf[0][7:]).read().split()) if skipf else set()
    files = sorted(glob.glob(os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../unity-nani/*.nani')))
    tot = 0
    for f in files:
        h = run(f, apply, skip); tot += len(h)
        for n, a, b in h: print(f"{os.path.basename(f)}:{n}\t{a}  ⏎  {b}")
    print('total', tot, file=sys.stderr)
