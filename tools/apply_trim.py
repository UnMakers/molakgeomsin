# 줄 번호 기반 본문 축약 적용: edits 파일 형식 "N=새 문장" / "N-" (삭제). 명령(@ # ;) 줄은 건드리지 못한다.
import sys, re, io
src, ed = sys.argv[1], sys.argv[2]
L = io.open(src, encoding='utf-8').read().split('\n')
E = {}
for raw in io.open(ed, encoding='utf-8').read().split('\n'):
    if not raw.strip() or raw.startswith('//'): continue
    m = re.match(r'^(\d+)(=|-)(.*)$', raw)
    if not m: sys.exit('bad edit line: ' + raw[:60])
    n = int(m.group(1))
    if n in E: sys.exit('dup %d' % n)
    t = L[n-1]
    if not t.strip() or re.match(r'\s*[@#;]', t): sys.exit('line %d is not prose: %s' % (n, t[:60]))
    E[n] = None if m.group(2) == '-' else m.group(3)
out = []
for i, t in enumerate(L, 1):
    if i in E:
        if E[i] is None: continue
        out.append(E[i])
    else: out.append(t)
io.open(src, 'w', encoding='utf-8').write('\n'.join(out))
def prose(ls): return [l for l in ls if l.strip() and not re.match(r'\s*[@#;]', l)]
b, a = prose(L), prose(out)
print('edits', len(E), 'prose lines %d -> %d, chars %d -> %d (%.0f%%)' % (len(b), len(a), sum(map(len,b)), sum(map(len,a)), 100.0*sum(map(len,a))/max(1,sum(map(len,b)))))
