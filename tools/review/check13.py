# -*- coding: utf-8 -*-
# 선택지 확장 검증: python3 check13.py <원본.nani> <수정본.nani>
import io, re, sys, os, glob
HERE = os.path.dirname(os.path.abspath(__file__))
CANON = os.path.join(HERE, '..', 'canon')
CH = re.compile(r'^@choice "((?:[^"\\]|\\.)*)"(.*)$')
FX_KEYS = {'hp', 'ng', 'sw', 'sl', 'mn', 'curse', 'day'}
NEW_FLAGS = {'f_hat', 'f_whetstone', 'f_salve', 'f_incense', 'f_alms', 'f_thiefkid', 'f_storylore', 'f_herbpulse', 'f_gambleshamed', 'f_smithwait', 'mkv', 'mkthief', 'mksmith', 'mkgamble'}
KW = {'random', 'true', 'false', 'none', 'pow', 'abs', 'min', 'max'}

def is_text(l): return l.strip() != '' and l[0] not in '@#;'
def norm(l):
    m = CH.match(l)
    return ('@choice "<L>"' + m.group(2)) if m else l.rstrip()

def skel(lines):
    return [(i, norm(l)) for i, l in enumerate(lines) if not is_text(l) and l.strip() and l.strip() != '@page']

def known_vars():
    v = set(NEW_FLAGS)
    for f in glob.glob(os.path.join(CANON, '*.nani')):
        t = io.open(f, encoding='utf-8').read()
        t2 = re.sub(r'"[^"]*"', '', t)
        v |= set(re.findall(r'@set ([A-Za-z_][A-Za-z0-9_]*)', t))
        for m in re.finditer(r'if:([^\s]+)', t2): v |= set(re.findall(r'[A-Za-z_][A-Za-z0-9_]*', m.group(1)))
    return v

def all_labels(extra_name, extra_lines):
    lab = {}
    for f in glob.glob(os.path.join(CANON, '*.nani')):
        n = os.path.basename(f)[:-5]
        lab[n] = set(re.findall(r'^# (\S+)', io.open(f, encoding='utf-8').read(), re.M))
    lab.setdefault(extra_name, set()).update(re.findall(r'^# (\S+)', '\n'.join(extra_lines), re.M))
    return lab

def main(a, b):
    A = io.open(a, encoding='utf-8').read().split('\n'); B = io.open(b, encoding='utf-8').read().split('\n')
    name = os.path.basename(b)[:-5]
    fname = 'MolakEvents' if name.startswith('ev_') else name
    errs = []
    sa, sb = skel(A), skel(B)
    # 원본 골격이 수정본의 부분수열인지 (추가만 허용)
    j = 0; matched = set()
    for i, (ai, x) in enumerate(sa):
        while j < len(sb) and sb[j][1] != x: j += 1
        if j >= len(sb): errs.append('원본 줄이 사라졌거나 순서가 바뀜: 원본 %d행 [%s]' % (ai + 1, x[:90])); break
        matched.add(sb[j][0]); j += 1
    new_lines = [bi for bi, _ in sb if bi not in matched]
    # 선택지 블록 크기 (새 선택지가 들어간 블록만 ≤4)
    blk = []; has_new = False
    for i, l in enumerate(B):
        if l.startswith('@choice'):
            blk.append(i); has_new = has_new or (i in new_lines)
        elif l.strip() == '@stop':
            if blk and has_new and len(blk) > 4: errs.append('%d행 근처 선택지 %d개(>4)' % (blk[0] + 1, len(blk)))
            blk = []; has_new = False
    # 새 라벨 끝맺음
    orig_labels = set(re.findall(r'^# (\S+)', '\n'.join(A), re.M))
    idx = [i for i, l in enumerate(B) if l.startswith('# ')]
    for k, i in enumerate(idx):
        lab = B[i][2:].strip()
        if lab in orig_labels: continue
        end = idx[k + 1] if k + 1 < len(idx) else len(B)
        cmds = [B[t].strip() for t in range(i + 1, end) if B[t].strip() and not is_text(B[t]) and not B[t].startswith(';')]
        last = cmds[-1] if cmds else ''
        ok = last in ('@return', '@stop') or (last.startswith('@goto ') and ' if:' not in last)
        if not ok: errs.append('새 라벨 #%s 이 끝나지 않고 흘러내림(마지막 명령: %s)' % (lab, last or '없음'))
        if lab in orig_labels: errs.append('라벨 중복 #' + lab)
    seen = {}
    for i in idx:
        lab = B[i][2:].strip()
        if lab in seen: errs.append('라벨 중복 #%s (%d행, %d행)' % (lab, seen[lab] + 1, i + 1))
        seen[lab] = i
    # goto 대상
    labs = all_labels(fname, B)
    for i, l in enumerate(B):
        for m in re.finditer(r'(?:@goto|@gosub) (\S+)|goto:(\S+)', l):
            t = m.group(1) or m.group(2)
            if '{' in t: continue
            if t.startswith('#'): f2, lb = fname, t[1:]
            elif '#' in t: f2, lb = t.split('#')
            else: continue
            if f2 not in labs or lb not in labs[f2]: errs.append('%d행 goto 대상 없음: %s' % (i + 1, t))
    # @fx 키, 조건 변수
    kv = known_vars()
    for i in new_lines + [t for t, l in enumerate(B) if l.startswith('@choice')]:
        l = B[i]
        if l.startswith('@fx'):
            for kvp in l.split()[1:]:
                if ':' not in kvp or kvp.split(':')[0] not in FX_KEYS: errs.append('%d행 @fx 키 오류: %s' % (i + 1, kvp))
        m = re.search(r'if:(\S+)', re.sub(r'"(?:[^"\\]|\\.)*"', '""', l))
        if m:
            for v in re.findall(r'[A-Za-z_][A-Za-z0-9_]*', m.group(1)):
                if v in KW or v.startswith('g_') or v.startswith('seen_'): continue
                if v not in kv: errs.append('%d행 모르는 조건 변수: %s' % (i + 1, v))
        cm = CH.match(l)
        if l.startswith('@choice') and not cm: errs.append('%d행 @choice 형식 오류' % (i + 1))
        if cm and '"' in cm.group(1).replace('\\"', ''): errs.append('%d행 선택지 따옴표 이스케이프 누락' % (i + 1))
    for i, l in enumerate(B):
        if not is_text(l): continue
        if re.match(r'^\S{1,24}: ', l): errs.append('%d행 화자 지정 오해 줄머리: %s' % (i + 1, l[:50]))
        if re.search(r'(?<!\\)\[', l): errs.append('%d행 본문에 [ : %s' % (i + 1, l[:50]))
        if l.count('<color=') != l.count('</color>'): errs.append('%d행 색 태그 짝 불일치' % (i + 1))
    nc = sum(1 for i in new_lines if B[i].startswith('@choice'))
    nl = sum(1 for i in new_lines if B[i].startswith('# '))
    oc = sum(1 for l in A if l.startswith('@choice')); bc = sum(1 for l in B if l.startswith('@choice'))
    two_a = 0; two_b = 0
    for L, acc in ((A, 'a'), (B, 'b')):
        cnt = 0; n2 = 0
        for l in L:
            if l.startswith('@choice'): cnt += 1
            elif l.strip() == '@stop':
                if cnt == 2: n2 += 1
                cnt = 0
        if acc == 'a': two_a = n2
        else: two_b = n2
    print('선택지 %d→%d (새 %d), 새 라벨 %d, 두 갈래 장면 %d→%d' % (oc, bc, nc, nl, two_a, two_b))
    if errs:
        print('FAIL'); [print(' - ' + e) for e in errs[:40]]; sys.exit(1)
    print('PASS')
main(sys.argv[1], sys.argv[2])
