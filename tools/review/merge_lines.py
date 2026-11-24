"""검토를 마친 줄 잇기 목록(파일:줄 — 앞 줄 번호)을 적용한다. 같은 동작·같은 생각이면 한 줄."""
import sys, os, collections
keys = open(sys.argv[1]).read().split()
by = collections.defaultdict(list)
for k in keys:
    f, n = k.split(':'); by[f].append(int(n))
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../unity-nani')
for f, ns in by.items():
    p = os.path.join(root, f + '.nani'); L = open(p, encoding='utf-8').read().split('\n')
    for n in sorted(set(ns), reverse=True):
        a, b = L[n-1], L[n]
        assert a.strip() and b.strip() and a.strip()[0] not in '@#;' and b.strip()[0] not in '@#;', (f, n, a, b)
        L[n-1] = a.rstrip() + ' ' + b.strip(); del L[n]
    open(p, 'w', encoding='utf-8').write('\n'.join(L))
    print(f, len(set(ns)))
