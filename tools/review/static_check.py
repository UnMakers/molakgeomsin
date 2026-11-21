# -*- coding: utf-8 -*-
"""대본 정적 점검(0.4.86) — 빌드 전에 돌린다.

  python3 tools/review/static_check.py unity-nani unity-src

보는 것
  ① 이동: @goto · @gosub · @choice goto: 의 대상 라벨이 있는가(다른 대본 '대본#라벨' 포함).
  ② 그림: @art scene:/char: 이름이 Resources/MolakArt 에 있는가(art_names.txt, none 제외).
  ③ 변수: 조건(if:/lock:/{변수})에서 읽는 이름을 쓰는 곳(@set·@fx·C# 문자열)이 있는가.
     앎 표식(g_kn_·g_know_·g_ev_·g_bt_·g_end_·g_long_)은 켜는 곳과 쓰는 곳의 짝을 따로 센다.
  ④ 생 수 말: '지난 생·몇 번을 되감·몇 생을·또 이 …' 처럼 회귀를 겪은 사람만 할 말이
     첫 생에도 열리는 자리(들어오는 길에 회귀·앎 조건이 하나도 없는 라벨)에 있는가.
  ⑤ 따옴표·색 태그 짝.
  ⑥ 쪽 넘김 뒤 장면 · ⑦ 쪽 넘김 뒤 인물(0.4.91: @page 뒤 그림 없이 앞 인물이 이어 말하면).
  ⑧ 한자 글꼴(0.4.92): 게임이 쓰는 한자·인장 글자가 unity-font 의 MolakHanja·MolakSeal 안에 있는가(없으면 기기 글꼴로 빠진다).
     빠졌으면 python3 tools/font_subset.py 로 다시 뽑는다.
"""
import io, os, re, sys, glob, collections

NANI, SRC = (sys.argv[1], sys.argv[2]) if len(sys.argv) > 2 else ('unity-nani', 'unity-src')
HERE = os.path.dirname(os.path.abspath(__file__))
ART = set(io.open(os.path.join(HERE, 'art_names.txt'), encoding='utf-8').read().strip().split(','))
CAST = set()
_cp = os.path.join(os.path.dirname(os.path.dirname(HERE)), 'unity-data', 'MolakData', 'cast.txt')
if os.path.exists(_cp):
    for _l in io.open(_cp, encoding='utf-8'):
        if '=' in _l and not _l.startswith('#'): CAST.add(_l.split('=')[0].strip())
# 0.4.88 저장소의 그림 원본(unity-art-*/)도 이름으로 센다 — 새 그림을 넣을 때 목록을 손으로 고치지 않게
ROOT = os.path.dirname(os.path.dirname(HERE))
for _p in glob.glob(os.path.join(ROOT, 'unity-art-*', '*.png')):
    ART.add(os.path.splitext(os.path.basename(_p))[0])

scripts = {}
for p in sorted(glob.glob(os.path.join(NANI, '*.nani'))):
    scripts[os.path.splitext(os.path.basename(p))[0]] = io.open(p, encoding='utf-8-sig').read().split('\n')

labels = {s: {} for s in scripts}
for s, L in scripts.items():
    for i, l in enumerate(L):
        m = re.match(r'^#\s*(\S+)', l)
        if m: labels[s][m.group(1)] = i

out = collections.defaultdict(list)
def rep(kind, s, i, msg): out[kind].append('%s:%d  %s' % (s, i + 1, msg))

COND = re.compile(r'\b(?:if|lock):(\S+)')
TARGET = re.compile(r'(?:^@goto\s+|^@gosub\s+|\bgoto:)([\w.]*#?[\w.]*)')
def resolve(s, t):
    if '#' in t:
        sc, lb = t.split('#', 1)
        sc = sc or s
    else:
        sc, lb = t, None
    return sc, lb

# ① 이동 / ② 그림 / ⑤ 짝
reads = collections.defaultdict(list); writes = collections.Counter()
for s, L in scripts.items():
    for i, l in enumerate(L):
        st = l.strip()
        if not st or st.startswith(';'): continue
        for t in TARGET.findall(st):
            if not t: continue
            sc, lb = resolve(s, t)
            if sc not in scripts: rep('이동', s, i, '없는 대본 ' + t)
            elif lb and lb not in labels[sc]: rep('이동', s, i, '없는 라벨 ' + t)
        if st.startswith('@art'):
            # 0.4.88 새 인물은 인물 표(unity-data/MolakData/cast.txt)에 한 줄 — 이름 인장·자리 그림이 거기서 온다
            for k in ('char', 'with'):
                m = re.search(r'\b' + k + r':([a-z_0-9]+)', st)
                if m and m.group(1) not in ('none', 'off') and m.group(1) not in CAST and m.group(1).split('_')[0] not in CAST:
                    rep('인물표', s, i, '인물 표에 없는 그림 ' + m.group(1))
            for k, pre in (('scene', 'scene_'), ('char', 'char_'), ('with', 'char_')):
                m = re.search(r'\b' + k + r':(\S+)', st)
                if m and m.group(1) not in ('none', 'off') and pre + m.group(1) not in ART:
                    rep('그림', s, i, '없는 그림 ' + pre + m.group(1))
        for c in COND.findall(st):
            c = re.sub(r'"[^"]*"', '', c)
            for v in re.findall(r'[A-Za-z_]\w*', c):
                if v in ('random', 'true', 'false', 'and', 'or', 'not'): continue
                reads[v].append((s, i))
        for v in re.findall(r'\{(\w+)\}', st): reads[v].append((s, i))
        m = re.match(r'^@set\s+(.*?)(?:\s+if:\S+)?$', st)
        if m:
            for a in m.group(1).split(';'):
                n = re.match(r'\s*(\w+)\s*[+\-*/]?=', a)
                if n: writes[n.group(1)] += 1
        if st.startswith('@fx'):
            for n in re.findall(r'\b(\w+):', st): writes[n] += 1
        if st[0] not in '@#':
            if st.count('<color') != st.count('</color>'): rep('짝', s, i, '색 태그 짝: ' + st[:50])
            q = st.replace('\\"', '').count('"')
            if q % 2: rep('짝', s, i, '따옴표 짝: ' + st[:50])

# C# 에서 쓰는 이름(문자열로 Set/SetStr/Ensure 하는 것, 표에 적힌 것)
cs_names = set()
for p in glob.glob(os.path.join(SRC, '*.cs')):
    t = io.open(p, encoding='utf-8-sig').read()
    cs_names.update(re.findall(r'"([a-z][a-z0-9_]{1,40})"', t))
# 표에서 이름을 이어 붙여 만드는 것(g_know_<상대>, g_died_<상대>, g_end_<말로>, g_gr_<사람> …)은 머리말로 본다
prefix_built = set()
for p in glob.glob(os.path.join(SRC, '*.cs')):
    t = io.open(p, encoding='utf-8-sig').read()
    prefix_built.update(re.findall(r'"(g_[a-z]+_)"\s*\+', t))
    prefix_built.update(re.findall(r'"([a-z]+_)"\s*\+', t))

NUMLIT = re.compile(r'^\d')
for v, where in sorted(reads.items()):
    if NUMLIT.match(v) or v in writes or v in cs_names: continue
    if any(v.startswith(pf) for pf in prefix_built): continue
    s, i = where[0]
    rep('변수', s, i, '읽기만 하고 쓰는 곳이 없음: %s (%d곳)' % (v, len(where)))

# 앎 표식 짝: 켜는 곳은 있는데 아무도 안 읽는 것
KN = re.compile(r'^g_(kn|know|ev|bt|long|end)_')
readset = set(reads)
for v in sorted(writes):
    if KN.match(v) and v not in readset and v not in cs_names:
        out['앎(켜기만 함)'].append(v)

# ④ 생 수 말 — 라벨마다 '들어오는 길'에 회귀·앎 조건이 있는지
PAST = re.compile(r'지난 생|몇 번을 되감|몇 생을|몇 생이고|되감을 때마다|지난 생들|또 이 (?:골목|아침|장면)|전생에')
GATE = re.compile(r'\bg_\w+|\bloop\b|\bregress\w*|\bf_ev_\w+|\bf_bt_\w+|\bknow\w*|\bmemwarn\b')   # memwarn: 지난 생에 맞붙은 상대일 때 코드가 켠다
inbound = collections.defaultdict(list)   # (script,label) -> [조건 문자열 or '']
for s, L in scripts.items():
    cur = None; uncond_flow = True
    for i, l in enumerate(L):
        st = l.strip()
        m = re.match(r'^#\s*(\S+)', st)
        if m:
            if cur is not None and uncond_flow: inbound[(s, m.group(1))].append(('', (s, cur)))   # 앞 라벨에서 흘러 들어옴
            cur = m.group(1); uncond_flow = True; continue
        if not st or st.startswith(';'): continue
        cm = COND.search(st); cond = cm.group(1) if cm else ''
        for t in TARGET.findall(st):
            if not t: continue
            sc, lb = resolve(s, t)
            if lb: inbound[(sc, lb)].append((cond, (s, cur)))
        if (st.startswith('@goto') or st.startswith('@stop') or st.startswith('@return')) and not cm: uncond_flow = False
    # 대본 첫 라벨은 외부(코드)에서 들어올 수 있다 — 들어오는 길을 모르니 그대로 둔다

# 회귀 뒤에만 오는 라벨(회귀 아침 Rb·Rg, 잃은 사람의 아침 Gr, 회귀한 생 갈래 …K·…Re·…R2) — 코드가 회귀 뒤에만 부른다
REBIRTH = re.compile(r'^(Rb|Rg|Gr|P4R|P4AL)|K\d*$|Re[A-Z]?\d*$|R\d$')
# 진말로(합벽) 길: 검보(g_hapbo)는 지난 생의 적운 구결로만 열려 첫 생엔 닿지 않는다
# 비무 연습(타이틀)은 이야기 밖 — 지난 생을 고르는 선택지가 있다
ALLOW_EXTRA = {('MolakDuel', 'PF_hochiS')}
ALLOW = ALLOW_EXTRA | {('Molak04', l) for l in ('TrueEpi', 'OldFight', 'OldF1A', 'OldF1B', 'OldF1C', 'OldF1Miss', 'OldF2', 'OldF2A', 'OldF2B', 'OldF2Miss', 'OldF2C', 'OldF3', 'OldF3Go', 'OldF3Weak', 'OldF3End')}
memo = {}
def gated(s, lb, depth=0):
    key = (s, lb)
    if key in memo: return memo[key]
    if key in ALLOW: return True
    if depth > 25: return False
    memo[key] = True   # 순환은 '조건 있음'으로 본다
    ins = inbound.get(key, [])
    if not ins: memo[key] = False; return False
    ok = True
    for c, src in ins:
        if c and GATE.search(c): continue
        if src[1] and (REBIRTH.search(src[1]) or gated(src[0], src[1], depth + 1)): continue   # 조건 없이 오더라도, 오는 곳이 이미 걸러진 라벨이면
        ok = False; break
    memo[key] = ok
    return ok

for s, L in scripts.items():
    cur = None; skipguard = False
    for i, l in enumerate(L):
        st = l.strip()
        m = re.match(r'^#\s*(\S+)', st)
        if m: cur = m.group(1); skipguard = False; continue
        if not st or st[0] in ';#': continue
        # 같은 라벨 안, 이 줄 위에 '앎이 없으면 건너뛴다'(@goto … if:g_…) 가 있으면 그 아래는 앎이 있는 생만 온다
        if st.startswith('@goto') and COND.search(st) and GATE.search(COND.search(st).group(1)): skipguard = True
        if skipguard or (cur and REBIRTH.search(cur)): continue
        if st.startswith('@') and not st.startswith('@print') and not st.startswith('@choice') and not st.startswith('@deja'): continue
        body = re.sub(r'<[^>]+>', '', st)
        if not PAST.search(body): continue
        cm = COND.search(st)
        if cm and GATE.search(cm.group(1)): continue
        if st.startswith('@deja'): continue   # @deja 는 지난 생에 읽은 줄일 때만 찍힌다
        if '9A70C4' in st: pass   # 보랏빛은 앎 표기 — 그래도 첫 생에 열리면 어색하다
        if cur and gated(s, cur): continue
        rep('생 수 말', s, i, '[%s] %s' % (cur, body[:70]))

# ⑥ 쪽 넘김 뒤 장면: @page 는 바탕(낮·밤)으로 되돌리고, 인물만 세우면 그 사람의 자리 그림이 깔린다.
#    앞 장면이 기본 장면(아침·밤·비·객잔)이 아닌데 @page 뒤 글 전에 인물만 세우면 장소가 엉뚱하게 바뀐다 → 장면을 다시 적는다.
BASE = {'morning', 'night', 'rain', 'innhall', 'innroom', 'yard', 'market', 'marketday', 'alley'}
for s, L in scripts.items():
    last = None
    for i, l in enumerate(L):
        st = l.strip()
        m = re.match(r'@art scene:(\S+)', st)
        if m: last = m.group(1)
        if st != '@page': continue
        j = i + 1; sawscene = False; sawchar = None
        while j < len(L):
            t = L[j].strip()
            if not t or t.startswith(';'): j += 1; continue
            if t.startswith('@art'):
                if 'scene:' in t: sawscene = True
                mm = re.search(r'char:(\S+)', t)
                if mm and mm.group(1) != 'none': sawchar = mm.group(1)
                j += 1; continue
            if t.startswith('@scene'): sawscene = True; j += 1; continue
            if t.startswith(('@goto', '@gosub', '@choice', '@stop', '@return')) or not t.startswith('@') or t.startswith('@print'): break
            j += 1
        if sawchar and not sawscene and last and last not in BASE:
            rep('쪽 넘김 장면', s, i, '@page 뒤 인물(%s)만 — 앞 장면 %s 를 다시 적을 것' % (sawchar, last))

# ⑦ 0.4.91 쪽 넘김 뒤 인물: @page 는 그림을 바탕으로 되돌린다. 그런데 앞에 서 있던 인물이 이어서 말하면(@page 뒤 그림 없이 대사가 나오면)
#    대화가 안 끝났는데 그림이 넘어간다(언메이커: 참새 첫 만남). 기준: 같은 장면이 이어지면 @page 뒤에 장면·인물을 다시 적고,
#    장면이 바뀌면 새 장면을 적는다 — 어느 쪽이든 그림을 적는다. 시간이 흐른 쪽(「밤.」「새벽.」 같은 첫 줄)도 사람이 말하면 누구 앞인지 적는다.
for s, L in scripts.items():
    for i, l in enumerate(L):
        if not (l.strip() == '@page' or l.strip().startswith('@page ')): continue
        j = i + 1; art = False; talk = None
        while j < len(L):
            t = L[j].strip()
            if t.startswith('@art') or t.startswith('@scene'): art = True; break
            if t.startswith(('#', '@page', '@goto', '@gosub', '@return', '@stop', '@choice')): break
            if t and not t.startswith(('@', ';')) and t[0] in '"“' and talk is None: talk = t
            j += 1
        if art or not talk: continue
        k = i - 1; pc = None
        while k > 0:
            t = L[k].strip()
            if t.startswith('@art char:'): pc = t.split(':', 1)[1].split()[0]; break
            if t.startswith(('@art scene', '@page', '@scene')) or re.match(r'^#\s*\S', t) and False: break
            if t.startswith(('@art scene', '@page', '@scene')): break
            k -= 1
        if pc and pc != 'none':
            rep('쪽 넘김 인물', s, i, '@page 뒤 그림 없이 대사 — 앞 인물 %s: %s' % (pc, talk[:30]))

# ⑧ 0.4.92 한자 글꼴: 인장 通 이 기기 고딕으로 찍히던 것(통·실·억·성·관·기·세·선이 글꼴에 없었다). 기준: 쓰는 한자는 전부 글꼴 안에.
try:
    sys.path.insert(0, os.path.join(ROOT, 'tools'))
    import font_subset as _fs
    _mh, _ms = _fs.cmap(os.path.join(ROOT, 'unity-font', 'MolakHanja.otf')), _fs.cmap(os.path.join(ROOT, 'unity-font', 'MolakSeal.otf'))
    _miss = sorted(c for c in _fs.used_hanja() if ord(c) not in _mh)
    if _miss: out['글꼴'].append('MolakHanja 에 없는 한자 %d: %s → python3 tools/font_subset.py' % (len(_miss), ''.join(_miss)))
    _miss = sorted(c for c in _fs.seal_hanja() if ord(c) not in _ms)
    if _miss: out['글꼴'].append('MolakSeal 에 없는 인장 글자 %d: %s → python3 tools/font_subset.py' % (len(_miss), ''.join(_miss)))
except ImportError as e:
    out['글꼴'].append('fontTools 없음 — 글꼴 검사 건너뜀 (%s)' % e)

order = ['이동', '그림', '변수', '앎(켜기만 함)', '생 수 말', '쪽 넘김 장면', '쪽 넘김 인물', '짝', '글꼴']
total = 0
for k in order:
    v = out.get(k, [])
    total += len(v)
    print('== %s %d' % (k, len(v)))
    for x in v[:400]: print('  ' + x)
print('합계', total)
