# -*- coding: utf-8 -*-
"""출시 점검(0.4.93) — 정적 검사가 못 보는 '흐름' 쪽 구멍을 찾는다.

  python3 tools/review/audit_release.py [--full]

  ① 막다른 선택지: @choice 묶음의 모든 갈래에 if: 가 붙어 있으면, 조건이 다 거짓일 때 @stop 에서 멈춘다(빠져나갈 갈래 없음).
  ② 행낭 물건: 얻는 곳(@set/@fx 로 1 이상)이 없는 것 · 얻고 나서 어디서도 읽지 않는 것(서사 회수 없음).
  ③ 사건 표: 조건이 읽는 변수를 쓰는 곳이 없어 영영 열리지 않는 사건 · 표에 있는데 대본에 라벨이 없는 사건 · 대본에 있는데 표에 없는 E_ 라벨.
  ④ 고아 라벨: 어디서도 오지 않는 라벨(이동·선택지·사건 표·코드 문자열 어디에도 이름이 없다).
  ⑤ 회수 없는 깃발: 선택의 결과로 세우는데(f_…=1) 어디서도 읽지 않는 깃발 — 고른 것이 다시 돌아오지 않는다.
  ⑥ 죽은 사람이 나오는 사건: 일상 사건 본문에 그림으로 서는 사람이 죽은 생에도 그 사건이 열리는가(사건 조건에도 본문 갈래에도 그 사람의 죽음 깃발이 없다).
"""
import io, os, re, sys, glob, collections

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FULL = '--full' in sys.argv
scripts = {os.path.splitext(os.path.basename(p))[0]: io.open(p, encoding='utf-8-sig').read().split('\n')
           for p in sorted(glob.glob(os.path.join(ROOT, 'unity-nani', '*.nani')))}
src = {os.path.basename(p): io.open(p, encoding='utf-8-sig').read() for p in glob.glob(os.path.join(ROOT, 'unity-src', '*.cs'))}
allsrc = '\n'.join(src.values())
alltxt = '\n'.join('\n'.join(L) for L in scripts.values())
out = collections.defaultdict(list)

# ① 막다른 선택지
for s, L in scripts.items():
    i = 0
    while i < len(L):
        if L[i].startswith('@choice'):
            j = i; block = []
            while j < len(L) and (L[j].startswith('@choice') or L[j].strip().startswith(';') or not L[j].strip()):
                if L[j].startswith('@choice'): block.append((j, L[j]))
                j += 1
            nxt = L[j].strip() if j < len(L) else ''
            conds = [re.search(r'if:(\S+)', l).group(1) if re.search(r'\bif:', l) else None for _, l in block]
            def exhaustive_of(cs):
                """조건들이 모든 경우를 덮는가 — 변수마다 조건에 나온 숫자 언저리 값을 다 넣어 본다(조합 4096 까지)."""
                import itertools
                expr = [re.sub(r'!(?!=)', ' not ', c.replace('&&', ' and ').replace('||', ' or ')) for c in cs]
                names = sorted(set(re.findall(r'[A-Za-z_]\w*', ' '.join(expr))) - {'and', 'or', 'not', 'random'})
                vals = {}
                for n in names:
                    nums = {int(float(x)) for x in re.findall(r'\b%s\s*[<>=!]=?\s*(-?\d+(?:\.\d+)?)' % n, ' '.join(cs))} or {0}
                    vals[n] = sorted({v + d for v in nums for d in (-1, 0, 1)} | {0})
                combos = 1
                for n in names: combos *= len(vals[n])
                if combos > 4096 or any(re.search(r'[/*(]', e) for e in expr): return True   # 셈이 들면 판단하지 않는다
                for combo in itertools.product(*[vals[n] for n in names]):
                    env = dict(zip(names, combo))
                    try:
                        if not any(eval(e, {}, env) for e in expr): return False
                    except Exception: return True
                return True
            exhaustive = all(conds) and exhaustive_of(conds)
            if nxt.startswith('@stop') and block and all(conds) and not exhaustive:
                out['막다른 선택지'].append('%s:%d  %d갈래 모두 조건부 — %s' % (s, block[0][0] + 1, len(block), ' / '.join(re.search(r'if:(\S+)', l).group(1) for _, l in block)[:160]))
            i = j
        else:
            i += 1

# ② 행낭 물건
bag = re.findall(r'new Entry\("(\w+)",\s*"([^"]*)"', src.get('MolakBag.cs', ''))
effects = dict(re.findall(r'new Entry\("(\w+)",\s*"[^"]*",\s*"[^"]*",\s*"([^"]*)"', src.get('MolakBag.cs', '')))
def setters(v):
    n = len(re.findall(r'@set\s+[^\n]*\b%s\s*=\s*(?!0\b)' % re.escape(v), alltxt))
    n += len(re.findall(r'@fx[^\n]*\b%s:' % re.escape(v), alltxt))
    n += len(re.findall(r'"%s"\s*,\s*(?!0f?\))' % re.escape(v), allsrc.replace(src.get('MolakBag.cs', ''), '').replace(src.get('MolakVarList.cs', ''), '')))
    return n
def readers(v):
    n = len(re.findall(r'(?:if|lock):[^\s]*\b%s\b' % re.escape(v), alltxt))
    n += len(re.findall(r'\{%s\}' % re.escape(v), alltxt))
    other = allsrc.replace(src.get('MolakBag.cs', ''), '').replace(src.get('MolakVarList.cs', ''), '')
    n += len(re.findall(r'"%s"' % re.escape(v), other))
    n += len(re.findall(r'"[^"\n]*\b%s\b[^"\n]*"' % re.escape(v), src.get('MolakEventDefs.cs', '')))
    return n
for flag, name in bag:
    st, rd = setters(flag), readers(flag)
    if st == 0: out['물건 얻는 곳 없음'].append('%s (%s)' % (flag, name))
    elif rd == 0 and not re.match(r'^(검술|내공|체력|저주|살기|은자)[^,]*[+−-]\d', effects.get(flag, '')):   # 얻는 순간 수치로 끝나는 물건은 제외
        out['물건 쓰임 없음'].append('%s (%s) — 얻는 곳 %d' % (flag, name, st))

def falls_into(L, lb):
    """라벨 바로 앞 줄이 흐름을 끊지 않으면(@stop·@return·조건 없는 @goto 가 아니면) 위에서 흘러 들어온다."""
    for i, l in enumerate(L):
        if re.match(r'^#\s*%s\s*$' % re.escape(lb), l):
            k = i - 1
            while k >= 0 and (not L[k].strip() or L[k].strip().startswith(';')): k -= 1
            if k < 0: return True
            t = L[k].strip()
            if t.startswith('@stop') or t.startswith('@return') or (t.startswith('@goto') and ' if:' not in t) or t.startswith('@choice'): return False
            return True
    return False

# ③ 사건 표
defs = re.findall(r'new Def\("(\w+)",\s*(\d+),\s*(\d+),\s*\'(\w)\',\s*(null|"[^"]*")', src.get('MolakEventDefs.cs', ''))
labels = {s: {m.group(1) for l in L for m in [re.match(r'^#\s*(\S+)', l)] if m} for s, L in scripts.items()}
evlabels = labels.get('MolakEvents', set())
def written(v):
    if v in ('day', 'aff', 'rank', 'curse', 'mn', 'hp', 'hpmax', 'sw', 'ng', 'sl', 'spared', 'arenawin'): return True
    if re.search(r'(?:@set\s+|[\s&,])%s\s*=' % re.escape(v), alltxt) or re.search(r'@fx[^\n]*\b%s:' % re.escape(v), alltxt): return True
    if re.search(r'"%s"' % re.escape(v), allsrc.replace(src.get('MolakVarList.cs', ''), '').replace(src.get('MolakEventDefs.cs', ''), '')): return True
    if v.startswith('seen_') or v.startswith('b_') and re.search(r'%s\s*=' % re.escape(v), alltxt): return True
    return False
for d, mi, ma, slot, cond in defs:
    if d not in evlabels: out['사건 라벨 없음'].append(d)
    if cond != 'null':
        for v in set(re.findall(r'[A-Za-z_]\w*', cond.strip('"'))):
            if not written(v): out['사건 조건 변수 안 씀'].append('%s: %s' % (d, v))
defids = {d for d, *_ in defs}
for lb in sorted(evlabels):
    if re.match(r'^E_[A-Za-z0-9]+$', lb) and lb not in defids and not re.search(r'#%s\b' % lb, alltxt) and ('"%s"' % lb) not in allsrc and not falls_into(scripts['MolakEvents'], lb):
        out['사건 표에 없음'].append(lb)

# ④ 고아 라벨
refs = set(re.findall(r'#([\w.]+)', alltxt)) | set(re.findall(r'"([A-Za-z_][\w]*)"', allsrc))
for s, ls in labels.items():
    for lb in ls:
        if lb not in refs and not lb.endswith('K') and not falls_into(scripts[s], lb):
            out['고아 라벨'].append('%s#%s' % (s, lb))

# ⑤ 회수 없는 깃발
_init = re.sub(r'(?s); =+ ?변수 선초기화.*', '', '\n'.join(scripts.get('Molak01', [])))
_txt = '\n'.join('\n'.join(L) for k, L in scripts.items() if k != 'Molak01') + '\n' + _init
_src = '\n'.join(v for k, v in src.items() if k != 'MolakVarList.cs')
_w = set(re.findall(r'@set\s+(f_\w+)\s*=\s*(?!0\b)', _txt)) | set(re.findall(r'@fx[^\n]*\b(f_\w+):', _txt))
_r = set(re.findall(r'f_\w+', _src)) | set(re.findall(r'\{(f_\w+)\}', _txt))
for m in re.finditer(r'(?:if|lock):(\S+)', _txt): _r |= set(re.findall(r'f_\w+', m.group(1)))
for m in re.finditer(r'@set\s+\w+\s*=([^\n]*)', _txt): _r |= set(re.findall(r'f_\w+', m.group(1)))
for v in sorted(_w - _r):
    if not v.startswith(('f_ev_', 'f_bt_')): out['회수 없는 깃발'].append(v)

# ⑥ 죽은 사람이 나오는 사건
DEAD = {'soa': 'f_soadead', 'hajinmu': 'f_hajindead', 'amhyang': 'f_amhdead', 'granny': 'f_herbdead', 'jinbaek': 'f_jinbaekdead',
        'hyeok': 'f_hyeokdead', 'chamsae': 'f_chamgone', 'gyeol': 'f_gyeoldead', 'baekri': 'f_baekridead'}
_ev = scripts.get('MolakEvents', [])
_cond = {d: (c.strip('"') if c != 'null' else '') for d, mi, ma, slot, c in defs}
_after = dict(re.findall(r'new Def\("(\w+)",\s*\d+,\s*\d+,\s*\'\w\',\s*(?:null|"[^"]*"),\s*"(\w+)"', src.get('MolakEventDefs.cs', '')))
for d in _cond:
    body, on = [], False
    for l in _ev:
        m = re.match(r'^#\s*(\S+)', l)
        if m: on = m.group(1) == d or (m.group(1).startswith(d) and not re.match(r'^%s\d' % re.escape(d), m.group(1)) and m.group(1)[len(d):len(d)+1] in ('', 'A', 'B', 'C', 'D', 'G', 'K', 'R', 'X', 'Y', 'W', 'L', 'T', 'S', 'P', 'F', 'M', 'N', 'O', 'E', '_'))
        if on: body.append(l)
    txt = '\n'.join(body)
    who = set(re.findall(r'@art[^\n]*char:([a-z]+)', txt)) | set(re.findall(r'@art[^\n]*with:([a-z]+)', txt))
    for w in who:
        base = w.split('_')[0]
        fl = DEAD.get(base)
        if not fl: continue
        if fl in _cond[d] or fl in txt: continue
        # 앞 사건(after)의 조건이 막아 주면 통과
        if d in _after and fl in _cond.get(_after[d], ''): continue
        out['죽은 사람이 나오는 사건'].append('%s: %s 가 서는데 %s 를 보지 않는다 (조건: %s)' % (d, w, fl, _cond[d] or '없음'))

order = ['막다른 선택지', '물건 얻는 곳 없음', '물건 쓰임 없음', '사건 라벨 없음', '사건 조건 변수 안 씀', '사건 표에 없음', '고아 라벨', '회수 없는 깃발', '죽은 사람이 나오는 사건']
for k in order:
    v = out.get(k, [])
    print('== %s %d' % (k, len(v)))
    for x in v[: (9999 if FULL else 60)]: print('  ' + x)
