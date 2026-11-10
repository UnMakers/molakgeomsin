# -*- coding: utf-8 -*-
# 윤문 구조 검증: python3 validate.py <원본.nani> <윤문.nani>
#  - 명령/라벨/주석/빈 줄 골격이 순서까지 동일해야 함 (@choice 따옴표 안 문구, 추가된 @page 줄은 예외)
#  - 화자 지정 오해(줄머리 'X: '), 본문 내 '[', 색 태그 짝, {변수}·수치 꼬리 보존
import io, re, sys, collections
CH = re.compile(r'^@choice "((?:[^"\\]|\\.)*)"(.*)$')
def is_text(l): return l.strip() != '' and l[0] not in '@#;'
def skel(lines):
    out = []
    for i, l in enumerate(lines):
        if is_text(l): continue
        if l.strip() == '': continue
        if l.strip() == '@page': continue
        m = CH.match(l)
        if l.startswith('@choice') and not m: out.append((i, '!!BADCHOICE ' + l)); continue
        out.append((i, '@choice "<L>"' + m.group(2) if m else l.rstrip()))
    return out
def labels(lines):
    return [CH.match(l).group(1) for l in lines if l.startswith('@choice') and CH.match(l)]
TAIL = re.compile(r'\(은자 \d+\)|\[\{\w+\}\]|\(\{\w+\}/\d+\)|저주 [+−-]\d+|\{\w+\}')
def main(a, b):
    A = io.open(a, encoding='utf-8').read().split('\n'); B = io.open(b, encoding='utf-8').read().split('\n')
    errs = []
    sa, sb = skel(A), skel(B)
    for k in range(max(len(sa), len(sb))):
        x = sa[k] if k < len(sa) else (None, '<없음>'); y = sb[k] if k < len(sb) else (None, '<없음>')
        if x[1] != y[1]:
            errs.append('골격 불일치 #%d: 원본 %s행 [%s]  /  윤문 %s행 [%s]' % (k, (x[0] or 0) + 1, x[1][:90], (y[0] or 0) + 1, y[1][:90])); break
    la, lb = labels(A), labels(B)
    for i, (p, q) in enumerate(zip(la, lb)):
        tp = collections.Counter(TAIL.findall(p)); tq = collections.Counter(TAIL.findall(q))
        if tp != tq: errs.append('선택지 꼬리 변경: [%s] -> [%s]' % (p, q))
        if '"' in q.replace('\\"', ''): errs.append('선택지 안 따옴표 이스케이프 누락: [%s]' % q)
    for i, l in enumerate(B):
        if not is_text(l): continue
        if re.match(r'^\S{1,24}: ', l): errs.append('%d행 화자 지정으로 오해될 줄머리: %s' % (i + 1, l[:60]))
        if re.search(r'(?<!\\)\[', l): errs.append('%d행 본문에 [ 사용: %s' % (i + 1, l[:60]))
        if l.count('<color=') != l.count('</color>'): errs.append('%d행 색 태그 짝 불일치: %s' % (i + 1, l[:60]))
    va = collections.Counter(re.findall(r'\{\w+\}', '\n'.join(A))); vb = collections.Counter(re.findall(r'\{\w+\}', '\n'.join(B)))
    if va != vb: errs.append('{변수} 집합 변경: %s' % (dict((va - vb) + (vb - va))))
    ta = [l for l in A if is_text(l)]; tb = [l for l in B if is_text(l)]
    changed = len(set(ta) - set(tb))
    dash = lambda T: sum(l.count('—') for l in T)
    you = lambda T: sum(l.count('당신') for l in T)
    pages = sum(1 for l in B if l.strip() == '@page') - sum(1 for l in A if l.strip() == '@page')
    print('본문 %d→%d줄, 바뀐 원문 줄 %d, 대시 %d→%d, "당신" %d→%d, @page +%d' % (len(ta), len(tb), changed, dash(ta), dash(tb), you(ta), you(tb), pages))
    if errs:
        print('FAIL'); [print(' - ' + e) for e in errs[:30]]; sys.exit(1)
    print('PASS')
main(sys.argv[1], sys.argv[2])
