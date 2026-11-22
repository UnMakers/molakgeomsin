# -*- coding: utf-8 -*-
"""나니노벨 없이 돌리기 — 0단계 준비(0.4.96).

몰락검신 대본(unity-nani/*.nani)을 우리 쪽 중간 표현(IR)으로 읽어 들인다. 나니노벨 없이 자체 실행기(MolakRunner)로
옮길 때 대본은 손대지 않고 그대로 쓰는 것이 목표라, 이 도구가 대본 전체를 빠짐없이 읽어 내는지가 첫 관문이다.

  python3 tools/engine/nani_ir.py            # 전수 파싱 + 모르는 문법 보고(합계 0 이어야 한다)
  python3 tools/engine/nani_ir.py --dump out.json   # IR 을 JSON 으로(실행기 시험용)

IR 한 줄 = {"k": 종류, ...}
  label  {"k":"label","name"}
  text   {"k":"text","text","cond":None}                     — 따옴표 대사·서술·독백 모두(꾸밈 태그 그대로)
  cmd    {"k":"cmd","name","args":{이름:값},"pos":위치값|None,"cond":조건|None}
  choice {"k":"choice","text","goto","cond","lock"}
조건·식은 문자열로 둔다(실행기의 식 평가기가 읽는다). 쓰는 연산: && || == != >= <= < > + - * / ! , 함수 random(a,b), 문자열 "…".
"""
import io, os, re, sys, glob, json, collections

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
NANI = os.path.join(ROOT, 'unity-nani')

# 몰락검신이 쓰는 명령과 받는 인자(나니노벨 기본 + 우리 명령). 새 명령을 만들면 여기에도 적는다.
KNOWN = {
    'set': None, 'goto': None, 'gosub': None, 'return': None, 'stop': None, 'print': None, 'choice': None, 'wait': None,
    'art': {'scene', 'char', 'with', 'off'}, 'fx': None, 'vfx': {'kind'}, 'page': set(), 'chapter': {'text', 'sub', 'desc'},
    'deja': {'text', 'need'}, 'risk': {'on', 'need', 'know'}, 'scene': {'id'}, 'mark': {'text', 'note'}, 'black': {'text', 'hold', 'red', 'turn', 'marks'},
    'giyeon': None, 'ending': None, 'frenzy': None, 'duelx': None, 'regress': None, 'turn': {'title', 'lines', 'kicker'},
    'stance': None, 'realm': None, 'bgm': {'mood'}, 'practice': None, 'duel': None, 'daytick': None, 'clearrun': None,
    'pname': None, 'pickevent': None, 'pickflavor': None, 'nextbody': None, 'companion': None, 'bridge': None, 'autolife': None,
    'teaser': None, 'lifenote': None, 'spk': {'id'}, 'gesture': {'kind'}, 'duelinfo': None, 'initrun': None, 'ensurevars': None,
}
ARG = re.compile(r'(\w+):("(?:[^"\\]|\\.)*"|\S+)')

def parse_args (rest):
    """'@cmd 위치값 이름:값 이름:"값"' → (위치값, {이름:값}). 위치값은 첫 낱말이 이름:값 꼴이 아니면."""
    rest = rest.strip(); pos = None; args = collections.OrderedDict()
    if rest and not re.match(r'\w+:', rest):
        if rest.startswith('"'):
            m = re.match(r'"((?:[^"\\]|\\.)*)"\s*', rest); pos = m.group(1); rest = rest[m.end():]
        else:
            m = re.match(r'(\S+)\s*', rest); pos = m.group(1); rest = rest[m.end():]
    for k, v in ARG.findall(rest):
        if v.startswith('"'): v = v[1:-1]
        args[k] = v
    return pos, args

def parse_file (path):
    out = []; bad = []
    name = os.path.splitext(os.path.basename(path))[0]
    for i, raw in enumerate(io.open(path, encoding='utf-8-sig').read().split('\n')):
        l = raw.rstrip('\r')
        s = l.strip()
        if not s or s.startswith(';'): continue
        if s.startswith('#'):
            out.append({'k': 'label', 'name': s[1:].strip(), 'line': i + 1}); continue
        if s.startswith('@'):
            m = re.match(r'@(\w+)\s*(.*)$', s)
            if not m: bad.append((name, i + 1, '명령 꼴이 아니다: ' + s[:40])); continue
            c, rest = m.group(1), m.group(2)
            pos, args = parse_args(rest)
            cond = args.pop('if', None)
            if c == 'choice':
                out.append({'k': 'choice', 'text': pos or '', 'goto': args.get('goto'), 'cond': cond, 'lock': args.get('lock'), 'line': i + 1})
                if not args.get('goto'): bad.append((name, i + 1, '선택지에 goto 가 없다'))
                continue
            if c not in KNOWN: bad.append((name, i + 1, '모르는 명령 @' + c))
            elif KNOWN[c] is not None:
                for k in args:
                    if k not in KNOWN[c]: bad.append((name, i + 1, '@%s 이 모르는 인자 %s' % (c, k)))
            out.append({'k': 'cmd', 'name': c, 'pos': pos, 'args': args, 'cond': cond, 'line': i + 1})
            continue
        if re.search(r'(?<!\\)\[\w+', s): bad.append((name, i + 1, '본문 속 [인라인 명령] — 실행기가 아직 모른다'))
        out.append({'k': 'text', 'text': l, 'line': i + 1})
    return name, out, bad

def main ():
    scripts = {}; bad = []
    for p in sorted(glob.glob(os.path.join(NANI, '*.nani'))):
        n, ir, b = parse_file(p); scripts[n] = ir; bad += b
    # 이동 대상이 실제로 있는가(실행기는 라벨 표로 뛴다)
    labels = {n: {x['name'] for x in ir if x['k'] == 'label'} for n, ir in scripts.items()}
    def target_ok (cur, t):
        if not t: return True
        if '#' in t:
            s, lb = t.split('#', 1); s = s or cur
            return s in scripts and (lb == '' or lb in labels[s] or '{' in lb)
        return t in scripts
    for n, ir in scripts.items():
        for x in ir:
            t = x.get('goto') if x['k'] == 'choice' else (x['pos'] if x['k'] == 'cmd' and x['name'] in ('goto', 'gosub') else None)
            if t and not target_ok(n, t): bad.append((n, x['line'], '이동 대상 없음 ' + t))
    kinds = collections.Counter(x['k'] for ir in scripts.values() for x in ir)
    cmds = collections.Counter(x['name'] for ir in scripts.values() for x in ir if x['k'] == 'cmd')
    print('대본 %d개 · 줄 %d (글 %d · 명령 %d · 선택지 %d · 라벨 %d) · 명령 종류 %d' % (len(scripts), sum(kinds.values()), kinds['text'], kinds['cmd'], kinds['choice'], kinds['label'], len(cmds)))
    for n, line, msg in bad[:200]: print('  %s:%d  %s' % (n, line, msg))
    print('모르는 문법 합계', len(bad))
    if '--dump' in sys.argv:
        dst = sys.argv[sys.argv.index('--dump') + 1]
        json.dump(scripts, io.open(dst, 'w', encoding='utf-8'), ensure_ascii=False)
        print('IR →', dst)

if __name__ == '__main__':
    main()
