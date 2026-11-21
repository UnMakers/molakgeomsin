#!/usr/bin/env python3
"""0.4.94 인연별 이야기 모드의 선택 기준표 — unity-data/MolakData/storypicks.txt 를 만든다.

기준(손으로 고르지 않는다): 선택지를 고르면 대본이 실제로 무엇을 바꾸는지 따라가 점수를 매긴다.
  · 따라가기: goto 라벨부터 아래로. 무조건 @goto·@gosub 는 따라 들어가고, 조건 붙은 @goto 는 건너뛴다.
    다음 선택지 묶음을 만나면 그중 가장 좋은 갈래의 값을 더한다(앞을 내다본다, 선택 깊이 8).
    하루 흐름으로 돌아가는 자리(Molak02 의 Daytime·AfterChapter·Night·BeatCheck 등)에서 멈춘다.
  · 정: 그 편 히로인의 정 한 칸 +3, 소아의 정은 어느 편이든 +2(본줄기의 끝은 합벽), 다른 히로인 +0.3.
  · 사실·앎(f_·g_): 대본 전체 조건에서 「있으면 열린다(>0)」 수와 「없어야 열린다(<1)」 수의 차(±6에서 자름)×0.5.
    진엔딩 대목(Molak04 True·Epi·LdHap·OldFight, Molak22)이 「있으면」으로 보는 깃발은 +3. 그 편 히로인 이름이 든 깃발 ×2, 다른 히로인 ×0.3.
  · 사람이 죽는 깃발(…dead): −8, 소아 −15.
  · 끝(@ending): 그 편의 진짜 끝(Molak22 True○○2, 소아 편은 합벽의 TrueBoardE·EpiBoardE) +60, 다른 편의 진짜 끝 +15,
    그 편 히로인과 「도망쳐 산 생」(LongTail○○, g_long_○○ 를 남긴다 — 다음 생에 진짜 끝을 여는 열쇠) +30, 그 밖의 끝 −20, 죽음 −50.
  · 되감기(StoryFold): +25. 「처음 화면」: −100.
  · 사술 +1 −0.2, 체력 −1 −0.05, 살기 +1 −0.05, 검술·내공 +1 +0.1.
  · 앎(g_)은 생을 넘어 남는다. 이미 가진 앎을 또 얻는 값은 쳐 주지 않도록, 갈래가 얻는 g_ 깃발과 그 몫을 함께 적는다(게임이 지금 가진 것은 뺀다).
줄 형식: 편<TAB>대본<TAB>점수<TAB>글귀<TAB>g_깃발:몫,…
쓰는 법: python3 tools/story_picks.py   (대본을 고친 뒤 vgen 과 함께 돌린다)
"""
import glob, os, re, sys

sys.setrecursionlimit(10000)
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
NANI = os.path.join(ROOT, 'unity-nani')
OUT = os.path.join(ROOT, 'unity-data', 'MolakData', 'storypicks.txt')

ROUTES = {1: 'soa', 2: 'amh', 3: 'cm', 4: 'cham'}
HER_KEYS = {'soa': ['soa', 'yeon'], 'amh': ['amh'], 'cm': ['cm', 'jbsister'], 'cham': ['cham']}
AFFS = {'aff': 'soa', 'amhaff': 'amh', 'cmaff': 'cm', 'chamaff': 'cham'}
TRUE_LABEL = {'TrueAmh2': 'amh', 'TrueCm2': 'cm', 'TrueCham2': 'cham', 'TrueBoardE': 'soa', 'EpiBoardE': 'soa'}
LONG_LABEL = {'LongTailSoa': 'soa', 'LongTailAmh': 'amh', 'LongTailCm': 'cm', 'LongTailCham': 'cham'}
DEATH_END = {'VictoryCurse', 'RipeAW', 'CultDie', 'DeadM0', 'E_Maeng3JE'}
HUB = {('Molak02', x) for x in ('Daytime', 'AfterChapter', 'DayLoop', 'Night', 'NightEnd', 'BeatCheck', 'RunEvent')}

scripts = {}
for f in sorted(glob.glob(os.path.join(NANI, '*.nani'))):
    scripts[os.path.splitext(os.path.basename(f))[0]] = [l.rstrip('\n').strip() for l in open(f, encoding='utf-8-sig')]
labels, label_at = {}, {}
for s, ls in scripts.items():
    cur = ''
    for i, l in enumerate(ls):
        m = re.match(r'^#\s*(\S+)', l)
        if m: cur = m.group(1); labels[(s, cur)] = i
        label_at[(s, i)] = cur

# ---- 조건에서 깃발의 쓰임 ----
pos, neg, truepos = {}, {}, set()
TERM = re.compile(r'\b([fg]_[A-Za-z0-9_]+)\s*(>=|>|<=|<|==|!=)\s*(-?\d+)')
for s, ls in scripts.items():
    for i, l in enumerate(ls):
        lab = label_at[(s, i)]
        region = s == 'Molak22' or (s == 'Molak04' and re.match(r'(True|Epi|LdHap|OldFight)', lab or '')) or (s == 'Molak12' and lab == 'EveEnd')
        for c in re.findall(r'(?:if|lock):(\S+)', l):
            for v, op, n in TERM.findall(c):
                n = int(n)
                p = op in ('>', '>=') or (op == '==' and n > 0) or (op == '!=' and n == 0)
                q = (op == '<' and n <= 1) or (op == '<=' and n == 0) or (op == '==' and n == 0)
                if p: pos[v] = pos.get(v, 0) + 1
                if q: neg[v] = neg.get(v, 0) + 1
                if p and region: truepos.add(v)

def heroine_of(v):
    low = v.lower()
    for her, keys in HER_KEYS.items():
        if any(k in low for k in keys): return her
    return None

def flag_w(v, her):
    if re.match(r'g_long_', v):
        h = v[7:]
        return 30.0 if h == her and her != 'soa' else -20.0   # 소아 편의 진짜 끝은 합벽이라 「도망쳐 산 생」이 열쇠가 아니다
    p, n = pos.get(v, 0), neg.get(v, 0)
    w = max(-6, min(6, p - n)) * 0.5
    if v in truepos: w += 3
    if v.endswith('dead') or v.endswith('died'):
        w = -15.0 if 'soa' in v else -8.0
    h = heroine_of(v)
    if h and h != 'soa':
        w *= 2 if h == her else 0.3
    elif h == 'soa' and her == 'soa':
        w *= 1.5
    return w

FX = re.compile(r'\b(hp|sw|ng|sl|mn|curse|aff|amhaff|cmaff|chamaff):(-?\d+)')
SETS = re.compile(r'([A-Za-z_][A-Za-z0-9_]*)\s*=\s*([^;]+)')
WK = {'g_wk_curse': -0.2, 'g_wk_sl': -0.05, 'g_wk_sw': 0.1}
CH = re.compile(r'^@choice\s+"((?:[^"\\]|\\.)*)"(.*)$')

def target(s, tgt):
    ts, tl = (tgt.split('#', 1) if '#' in tgt else (s, tgt))
    return (ts or s), tl.lstrip('#')

memo = {}
def walk(s, i, her, depth, stack, known=()):
    """(점수, {g_깃발: 몫}) — s 대본 i 줄부터 아래로. known: 이 길에서 숫자로 정해진 변수(@set f_lw=2 → if:f_lw==2 갈래를 안다)."""
    key = (s, i, her, depth, known)
    if key in memo: return memo[key]
    if (s, i) in stack or depth > 8: return (0.0, {})
    stack = stack | {(s, i)}
    ls = scripts.get(s)
    sc, gs = 0.0, {}
    if ls is None: return (0.0, {})
    n = 0
    kn = dict(known)
    while i < len(ls) and n < 400:
        l = ls[i]; n += 1
        cond = re.search(r'\sif:', l) is not None
        mc = re.search(r'\sif:([A-Za-z_][A-Za-z0-9_]*)==(-?\d+)$', l)
        if mc and mc.group(1) in kn:
            if kn[mc.group(1)] != int(mc.group(2)): i += 1; continue
            cond = False   # 이 길에서 참인 조건
        if l.startswith('@choice'):
            best = None
            j = i
            while j < len(ls) and (ls[j].startswith('@choice') or ls[j] == ''):
                m = CH.match(ls[j])
                if m:
                    v = choice_value(s, j, m, her, depth + 1, stack, tuple(sorted(kn.items())))
                    if best is None or v[0] > best[0]: best = v
                j += 1
            if best:
                sc += best[0]
                for k, w in best[1].items(): gs[k] = gs.get(k, 0) + w
            break
        if l.startswith('@stop') or l.startswith('@return'): break
        if l.startswith('@ending'):
            lab = label_at[(s, i)]
            if lab in TRUE_LABEL: sc += 60 if TRUE_LABEL[lab] == her else 15
            elif lab in LONG_LABEL: pass   # 몫은 g_long_ 깃발이 진다
            elif lab in DEATH_END: sc -= 50
            else: sc -= 20
            break
        if l.startswith('@fx') and not cond:
            for k, v in FX.findall(l):
                v = int(v)
                if k in AFFS: sc += v * (3 if AFFS[k] == her else 2 if AFFS[k] == 'soa' else 0.3)
                elif k == 'curse': sc -= 0.2 * v
                elif k == 'hp':
                    sc += 0.05 * v if v < 0 else 0.02 * v
                    if v <= -20: sc -= 50
                elif k == 'sl': sc -= 0.05 * v
                elif k in ('sw', 'ng'): sc += 0.1 * v
        if l.startswith('@set') and not cond:
            body = re.sub(r'\s(if|lock):\S+', '', l[4:]).strip()
            for var, expr in SETS.findall(body):
                expr = expr.strip()
                if re.match(r'^-?\d+$', expr): kn[var] = int(expr)
                m = re.match(r'%s\s*([+-])\s*(\d+)' % re.escape(var), expr)
                if var in AFFS and m:
                    d = int(m.group(2)) * (1 if m.group(1) == '+' else -1)
                    sc += d * (3 if AFFS[var] == her else 2 if AFFS[var] == 'soa' else 0.3)
                elif var == 'hp' and expr == '0': sc -= 50
                elif var in WK and m:   # 다음 생으로 넘어가는 몸(g_wk_*)
                    sc += int(m.group(2)) * (1 if m.group(1) == '+' else -1) * WK[var]
                elif re.match(r'^[fg]_', var) and not var.startswith('g_end_'):
                    up = (re.match(r'^-?\d+$', expr) and int(expr) > 0) or (m and m.group(1) == '+')
                    if up:
                        w = flag_w(var, her); sc += w
                        if var.startswith('g_') and w > 0: gs[var] = gs.get(var, 0) + w
        if l.startswith('@goto') or l.startswith('@gosub'):
            m = re.match(r'@(goto|gosub)\s+(\S+)', l)
            ts, tl = target(s, m.group(2))
            if ts == 'MolakOver' and s != 'MolakOver':
                if not cond:
                    sc += 25 if tl == 'StoryFold' else -50
                    break
            elif (ts, tl) in HUB:
                if not cond and m.group(1) == 'goto': break
            elif not cond and (ts, tl) in labels:
                v = walk(ts, labels[(ts, tl)] + 1, her, depth, stack, tuple(sorted(kn.items())))
                sc += v[0]
                for k, w in v[1].items(): gs[k] = gs.get(k, 0) + w
                if m.group(1) == 'goto': break
            elif not cond and m.group(1) == 'goto':
                break
        i += 1
    memo[key] = (sc, gs)
    return memo[key]

def choice_value(s, j, m, her, depth, stack, known=()):
    text = m.group(1).replace('\\"', '"')
    if '처음 화면' in text: return (-100.0, {})
    g = re.search(r'goto:(\S+)', m.group(2))
    if not g: return (0.0, {})
    ts, tl = target(s, g.group(1))
    if ts == 'MolakOver' and s != 'MolakOver': return (25.0 if tl == 'StoryFold' else -50.0, {})
    if (ts, tl) not in labels or (ts, tl) in HUB: return (0.0, {})
    return walk(ts, labels[(ts, tl)] + 1, her, depth, stack, known)

rows = []
for s, ls in scripts.items():
    for i, l in enumerate(ls):
        m = CH.match(l)
        if not m: continue
        text = m.group(1).replace('\\"', '"')
        for r, her in ROUTES.items():
            sc, gs = choice_value(s, i, m, her, 0, frozenset())
            rows.append((r, s, round(sc, 2), text, ','.join('%s:%g' % (k, round(w, 1)) for k, w in sorted(gs.items()) if w > 0.5)))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with open(OUT, 'w', encoding='utf-8') as f:
    f.write('# 0.4.94 인연별 이야기 모드 선택 기준표 — tools/story_picks.py 가 만든다(손으로 고치지 않는다)\n')
    for r, s, sc, t, g in rows:
        f.write('%d\t%s\t%s\t%s\t%s\n' % (r, s, sc, t.replace('\t', ' '), g))
print('선택지', len(rows) // len(ROUTES), '개 × 편', len(ROUTES), '→', os.path.relpath(OUT, ROOT))
