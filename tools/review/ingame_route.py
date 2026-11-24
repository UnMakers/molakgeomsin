"""대본(.nani)을 정해 둔 갈래대로 따라가며 게임 화면에 실제로 찍히는 글만 뽑는다(인게임판).
명령은 숨기고, 선택지는 [선택] 목록으로, 고른 것은 ▶ 로 표시한다. 문단 사이에 빈 줄을 넣는다.
쓰는 법: python3 ingame_route.py > out.txt  (아래 ROUTE 를 고쳐 갈래를 바꾼다)"""
import re, os, sys
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '../../unity-nani')
NAME = '연호'
_cache = {}
def load(f):
    if f not in _cache:
        L = open(os.path.join(ROOT, f + '.nani'), encoding='utf-8').read().split('\n')
        idx = {}
        for i, l in enumerate(L):
            if l.startswith('# '): idx[l[2:].strip()] = i
        _cache[f] = (L, idx)
    return _cache[f]

def ev(cond, V):
    e = cond.replace('&&', ' and ').replace('||', ' or ')
    e = re.sub(r'!(?!=)', ' not ', e)
    e = re.sub(r'\b([a-zA-Z_][a-zA-Z0-9_]*)\b', lambda m: m.group(1) if m.group(1) in ('and', 'or', 'not') else f'V.get("{m.group(1)}",0)', e)
    e = e.replace('"none"', '"none"')
    try: return bool(eval(e, {'V': V}))
    except Exception: return False

def clean(t):
    t = re.sub(r'<color=[^>]*>|</color>|<b>|</b>|<i>|</i>|<size=[^>]*>|</size>', '', t)
    right = '<align=right>' in t
    t = re.sub(r'</?align[^>]*>', '', t)
    t = t.replace('{g_pname_a}', NAME).replace('{g_pname}', NAME).replace('{mn}', str(int(V.get('mn', 30))))
    t = t.replace('\\[', '[').replace('\\]', ']')
    return t.strip(), right

def split_if(s):
    m = re.search(r'\sif:(\S+)', s)
    return (s[:m.start()] + s[m.end():], m.group(1)) if m else (s, None)

OUT = []
STOP = {'Night', 'Shop', 'AfterChapter', 'DayLoop', 'SoaGift', 'NightChoice'}
def say(s=''):
    OUT.append(s)

def run(f, label, V, picks, stop_files=()):
    """label 부터 실행. 다른 파일로 가는 goto/대본 끝/@stop 에서 멈추고 (file,label) 을 돌려준다."""
    L, idx = load(f)
    i = idx[label] + 1
    choices = []
    while i < len(L):
        raw = L[i].rstrip(); i += 1
        s = raw.strip()
        if not s or s.startswith(';'): continue
        if s.startswith('# '): continue          # 다음 라벨로 흘러 들어감
        if s.startswith('@'):
            body, cond = split_if(s)
            if cond and not ev(cond, V): continue
            cmd = body.split()[0]
            if cmd == '@goto':
                tgt = body.split()[1]
                if '#' in tgt and not tgt.startswith('#'):
                    return tgt.split('#')
                if tgt[1:] in STOP: return None
                return run(f, tgt[1:], V, picks)
            if cmd == '@set':
                for a in re.findall(r'(\w+)=("[^"]*"|\S+)', body[4:]):
                    k, v = a
                    if v.startswith('"'): V[k] = v.strip('"')
                    else:
                        try: V[k] = eval(re.sub(r'\b([a-zA-Z_]\w*)\b', lambda m: f'V.get("{m.group(1)}",0)' if m.group(1) != 'random' else 'rnd', v), {'V': V, 'rnd': lambda a, b: a})
                        except Exception: pass
                continue
            if cmd == '@regress': V['g_runs'] = V.get('g_runs', 0) + 1; continue
            if cmd == '@chapter':
                t = re.search(r'text:"([^"]*)"', body); sub = re.search(r'sub:"([^"]*)"', body); d = re.search(r'desc:"([^"]*)"', body)
                say(f'【{t.group(1) if t else ""}{" · " + sub.group(1) if sub else ""}】' + (f' {d.group(1)}' if d else '')); say(); continue
            if cmd == '@black':
                t = re.search(r'text:"([^"]*)"', body)
                if t:
                    say('— 먹 화면 —'); [ (say(x), say()) for x in t.group(1).split('|') ]
                continue
            if cmd == '@print':
                t = re.search(r'"(.*)"', body)
                if t:
                    txt, right = clean(t.group(1).replace('\\"', '"'))
                    if '{' not in txt: say(('　　　　(나) ' if right else '') + txt); say()
                continue
            if cmd == '@choice':
                t = re.search(r'"((?:[^"\\]|\\.)*)"', body)
                g = re.search(r'goto:(\S+)', body)
                choices.append((t.group(1).replace('\\"', '"'), g.group(1) if g else None))
                continue
            if cmd == '@stop':
                if not choices: return None
                if not picks:
                    say('[선택 — 남은 고르기 없음]'); [say('　 '+clean(c)[0]) for c,_ in choices]; return None
                k = picks.pop(0)
                say('[선택]')
                for j, (c, _) in enumerate(choices):
                    say(('▶ ' if j == k else '　 ') + clean(c)[0])
                say()
                tgt = choices[k][1]; choices = []
                if '#' in tgt and not tgt.startswith('#'): return tgt.split('#')
                if tgt[1:] in STOP: return None
                return run(f, tgt[1:], V, picks)
            if cmd == '@gosub' or cmd == '@return' and False: continue
            if cmd == '@return': return None
            continue
        if s == '{dejapre}': continue
        txt, right = clean(s)
        if not txt or re.fullmatch(r'\{[^}]*\}', txt): continue
        if re.search(r'\{(?!g_pname)[a-z_]+\}', txt):
            txt = re.sub(r'\{[a-z_]+\}', '…', txt)
        say(('　　　　(나) ' if right else '') + txt); say()
    return None

def part(title):
    say(); say('━' * 20); say(title); say('━' * 20); say()

# 갈래: (파일, 라벨, 고를 선택지 번호 목록, 미리 깔 변수)
V = {'g_runs': 0, 'tr': 1, 'g_bestday': 0, 'theme': 0, 'g_lastend': 'none', 'g_sac_hajin': 0, 'g_nxform': 'none', 'mn': 30, 'hp': 10, 'hpmax': 10}
ROUTE = [
    ('첫 번째 생 — 서장', 'Molak01', 'Prologue', [0, 0, 1, 1]),        # 날짜 세기 · 대추를 던짐 · 맞선다 · (저자 대신) 객잔으로
    ('(밤이 몇 번 지나고, 저잣거리 국밥집)', 'Molak03', 'Soa1', [0, 1]),  # 내력을 밝힐 까닭이 없소 · 입을 다문다
    ('두 번째 생 — 다시, 그 아침', 'Molak01', 'Rebirth', [3, 0, 0, 1]),  # 대답하지 않는다 · 좌판을 눌러 준다 · 쥐눈이 장 · 객잔으로
    ('(국밥집, 그 자리)', 'Molak03', 'Soa1', [0, 0]),                    # 청운검파요 · 국밥값
    ('(보름날)', 'Molak03', 'Unju0', [0, 1, 0]),                         # 가명 운객 · 먼저 둘러본다 · 서열전에 나선다
    ('(그날 밤)', 'Molak02', 'NvSoa', []),
    ('', 'MolakEvents', 'E_Soa2', []),
]
for title, f, lab, picks in ROUTE:
    if title: part(title)
    if lab == 'Soa1' and V.get('g_runs', 0) > 0: V['g_fac_jeok'] = 1
    pk = list(picks)
    r = run(f, lab, V, pk)
    while r and r[1] not in STOP:
        r = run(r[0], r[1], V, pk)
    if lab == 'Unju0': say('[비무 — 판마다 상대와 합이 달라진다]'); say()
print('\n'.join(OUT).replace('\n\n\n\n', '\n\n'))
