# -*- coding: utf-8 -*-
# canon 전체: 변수 선초기화 재생성 + goto/choice 대상·형식·fx·본문 함정 검사
import io, re, glob, os, sys
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
files = sorted(glob.glob(os.path.join(C, '*.nani')))
outs = {os.path.basename(f)[:-5]: io.open(f, encoding='utf-8').read() for f in files}
idset = set()
for s in outs.values():
    s = re.sub(r'; \(자동 생성\) 변수 선초기화[\s\S]*?(?=@initrun)', '', s)
    t = re.sub(r'"[^"]*"', '', s)
    for m in re.finditer(r'if:([^\s]+)', t): idset.update(re.findall(r'[A-Za-z_][A-Za-z0-9_]*', m.group(1)))
    for m in re.finditer(r'@set ([A-Za-z_][A-Za-z0-9_]*)', t): idset.add(m.group(1))
    for m in re.finditer(r'\{([A-Za-z_][A-Za-z0-9_]*)\}', s): idset.add(m.group(1))
drop = {'random', 'true', 'false', 'pow', 'abs', 'min', 'max'}
strings = {'knownew', 'knowsnap', 'nextevent', 'dkind', 'deathcause', 'foename', 'foerank', 'foetier', 'oldrank', 'newrank', 'tiername','prepbond','prepitem','lifesum','lifecmp','deathtip','truehint','vowline','dejapre','byeolho','dx_pretext','dx_telltext','dx_restext','dx_momtext'}
nums = sorted(x for x in idset if x not in drop and not x.startswith('g_') and x[0].islower() and x not in strings)
init = ['; (자동 생성) 변수 선초기화'] + ['@set %s=0' % v for v in nums] + ['@set %s="none"' % v for v in sorted(strings)]
m01 = outs['Molak01']
old_init = re.search(r'; \(자동 생성\) 변수 선초기화[\s\S]*?(?=@initrun)', m01).group(0)
old_vars = set(re.findall(r'@set (\w+)=', old_init))
m01 = m01.replace(old_init, '\n'.join(init) + '\n')
outs['Molak01'] = m01
io.open(os.path.join(C, 'Molak01.nani'), 'w', encoding='utf-8').write(m01)
print('vars', len(nums), 'new:', sorted(set(nums) - old_vars), 'gone:', sorted(old_vars - set(nums) - strings))
labels = {n: re.findall(r'^# (\S+)', c, re.M) for n, c in outs.items()}
bad = []
for n, ls in labels.items():
    seen = set()
    for l in ls:
        if l in seen: bad.append('%s: 라벨 중복 %s' % (n, l))
        seen.add(l)
lab = {n: set(v) for n, v in labels.items()}
FX = {'hp', 'ng', 'sw', 'sl', 'mn', 'curse', 'day'}
CH = re.compile(r'^@choice "((?:[^"\\]|\\.)*)"(.*)$')
for n, c in outs.items():
    for i, l in enumerate(c.split('\n')):
        for m in re.finditer(r'(?:@goto|@gosub) (\S+)|goto:(\S+)', l):
            t = m.group(1) or m.group(2)
            if '{' in t: continue
            if t.startswith('#'): f2, lb = n, t[1:]
            elif '#' in t: f2, lb = t.split('#')
            else: continue
            if f2 not in lab or lb not in lab[f2]: bad.append('%s:%d goto 대상 없음 %s' % (n, i + 1, t))
        if l.startswith('@choice') and (not CH.match(l) or not re.match(r'^((?: (?:if|goto|set):\S+)*)\s*$', CH.match(l).group(2)) or CH.match(l).group(1) == ''): bad.append('%s:%d choice 형식' % (n, i + 1))
        if l.startswith('@fx'):
            for kv in l.split()[1:]:
                if kv.startswith('if:'): continue
                if ':' not in kv or kv.split(':')[0] not in FX: bad.append('%s:%d fx 키 %s' % (n, i + 1, kv))
        if l.strip() and l[0] not in '@#;':
            if re.match(r'^\S{1,24}: ', l): bad.append('%s:%d 화자 오해 %s' % (n, i + 1, l[:40]))
            if re.search(r'(?<!\\)\[', l): bad.append('%s:%d 본문 [ %s' % (n, i + 1, l[:40]))
            if l.count('<color=') != l.count('</color>'): bad.append('%s:%d color 짝' % (n, i + 1))
print('\n'.join(bad) if bad else 'OK')
