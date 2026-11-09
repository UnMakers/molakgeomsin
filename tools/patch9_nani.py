# -*- coding: utf-8 -*-
# 콘텐츠 팩 9: 갈림길의 종장 — 성사/입마/원행/순검/포박 엔딩 + 상점 문구 분기 + 에필로그 변주
#  실행 순서: build_nani.js → patch → 2 … → 8 → **9** (initvars 재생성 + 라벨 검사 포함)
import io, os, re, sys
D = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, D)
from endings9 import MOLAK06_ADD, MOLAK04_VICTORYCURSE, MOLAK04_EPIJINDEAD, MOLAKOVER_SHOP, MOLAKOVER_AFTERS, END_ENTRIES

def R(f): return io.open(os.path.join(D,f),encoding='utf-8').read()
def W(f,s): io.open(os.path.join(D,f),'w',encoding='utf-8').write(s)
def sub1(s, old, new, name):
    assert s.count(old) == 1, '%s: expected exactly one match (%d)' % (name, s.count(old))
    return s.replace(old, new)

m02 = R('out_Molak02.nani'); m04 = R('out_Molak04.nani'); m06 = R('out_Molak06.nani'); mov = R('out_MolakOver.nani')
assert u'; ===== 팩 9' not in m06, 'patch9 already applied'

# ---- 1. Molak06: 종장 본문 + 집법전주 '응한다' 분기 ----
m06 = sub1(m06,
    u'@choice "무릎을 꿇는 척 틈을 본다" goto:#Hunt3K\n@stop\n',
    u'@choice "무릎을 꿇는 척 틈을 본다" goto:#Hunt3K\n@choice "\\"…가지. 교주께.\\" — 부름에 응한다" goto:#Hunt3Y\n@stop\n',
    'Hunt3 choice')
m06 = m06.rstrip('\n') + u'\n# Hunt3Y\n@goto #Ipma1\n' + MOLAK06_ADD

# ---- 2. Molak02: 낮 진입 분기 (성사 / 포박) ----
m02 = sub1(m02,
    u'@goto Molak04#Ld0 if:f_wolfdown>0&&rank>0&&rank<=10\n',
    u'@goto Molak04#Ld0 if:f_wolfdown>0&&rank>0&&rank<=10\n'
    u'@goto Molak06#Ripe1 if:curse>=88&&f_oldseen>0&&b_ripe<1\n'
    u'@goto Molak06#Capture if:day>=60&&b_capture<1\n',
    'DayLoop gates')

# ---- 3. Molak02: 밤 선택지 — 소아와 떠남 ----
m02 = sub1(m02,
    u'@choice "혈쇄문의 초대에 응한다" if:f_wolfinvite>0&&f_wolfdown<1 goto:#NWolf\n@choice "몸을 쉰다" goto:#NRest\n',
    u'@choice "혈쇄문의 초대에 응한다" if:f_wolfinvite>0&&f_wolfdown<1 goto:#NWolf\n'
    u'@choice "소아와 떠난다 — 강호를 등진다" if:f_soabond>0&&day>=38&&b_leave<1 goto:#NLeave\n'
    u'@choice "몸을 쉰다" goto:#NRest\n',
    'NightChoice leave')
m02 = sub1(m02,
    u'# NWolf\n@goto Molak04#Wf1\n',
    u'# NWolf\n@goto Molak04#Wf1\n# NLeave\n@set b_leave=1\n@goto Molak06#Leave1\n',
    'NLeave label')

# ---- 4. Molak04: 승리 직후 저주 분기 + 에필로그 변주 ----
m04 = sub1(m04,
    u'\\[ 終 \\] — 혈마교의 깃발이 내려간다.\n@clearrun\n',
    u'\\[ 終 \\] — 혈마교의 깃발이 내려간다.\n@goto #VictoryCurse if:curse>=80\n@clearrun\n',
    'Victory curse gate')
m04 = sub1(m04, u'# Epilogue\n', MOLAK04_VICTORYCURSE + u'# Epilogue\n', 'VictoryCurse block')
m04 = sub1(m04,
    u'@goto #EpiJinTruth if:f_jinbaektruth>0\n',
    u'@goto #EpiJinTruth if:f_jinbaektruth>0\n@goto #EpiJinDead if:f_jinbaekdead>0\n',
    'Epilogue jindead gate')
m04 = sub1(m04, u'# EpiTail\n', MOLAK04_EPIJINDEAD + u'# EpiTail\n', 'EpiJinDead block')

# ---- 5. MolakOver: 상점 문구 분기 + 엔딩별 복귀 ----
mov = re.sub(u'# Shop\n@shopinfo\n[^\n]*\n(?=@choice)', MOLAKOVER_SHOP, mov, count=1)
assert u'# ShopList' in mov, 'Shop header replace failed'
mov = mov.rstrip('\n') + u'\n' + MOLAKOVER_AFTERS
W('out_Molak02.nani', m02); W('out_Molak04.nani', m04); W('out_Molak06.nani', m06); W('out_MolakOver.nani', mov)

# ---- 6. C# 말로록 패치 조각 (MolakEnd.cs 에 적용할 텍스트) ----
entries = u'\n'.join(u'        new Entry("%s",%s"%s", "%s", "%s", "%s"),' % (i, ' ' * max(1, 11 - len(i)), h, k, t, n) for i, h, k, t, n in END_ENTRIES)
W('MolakEnd_entries9.txt', entries + u'\n')

# ---- 7. initvars 재생성 (patch8 과 동일 로직) ----
files = ['out_Molak01.nani','out_Molak02.nani','out_Molak03.nani','out_Molak04.nani',
         'out_Molak05.nani','out_Molak06.nani','out_MolakEvents.nani','out_MolakDuel.nani','out_MolakOver.nani']
outs = {f: R(f) for f in files}
idset=set()
for s in outs.values():
    t=re.sub(r'"[^"]*"','',s)
    for m in re.finditer(r'if:([^\s]+)',t): idset.update(re.findall(r'[A-Za-z_][A-Za-z0-9_]*',m.group(1)))
    for m in re.finditer(r'@set ([A-Za-z_][A-Za-z0-9_]*)',t): idset.add(m.group(1))
    for m in re.finditer(r'\{([A-Za-z_][A-Za-z0-9_]*)\}',t): idset.add(m.group(1))
drop={'random','true','false','pow','abs','min','max'}
strings={'nextevent','dkind','deathcause','foename','foerank','foetier','oldrank','newrank','tiername'}
nums=sorted(x for x in idset if x not in drop and not x.startswith('g_') and x[0].islower() and x not in strings)
init_lines=['; (자동 생성) 변수 선초기화']+['@set %s=0'%v for v in nums]+['@set %s="none"'%v for v in sorted(strings)]
m01 = outs['out_Molak01.nani']
m01 = re.sub(r'; \(자동 생성\) 변수 선초기화[\s\S]*?(?=@initrun)', '\n'.join(init_lines)+'\n', m01)
assert '@initrun' in m01
W('out_Molak01.nani', m01); outs['out_Molak01.nani'] = m01
print('vars:', len(nums))

# ---- 8. 라벨 무결성 ----
labels={}
for f,c in outs.items():
    name=f.replace('out_','').replace('.nani','')
    labels[name]=set(re.findall(r'^# (\S+)',c,re.M))
bad=[]
for f,c in outs.items():
    name=f.replace('out_','').replace('.nani','')
    for m in re.finditer(r'@(?:goto|gosub) (\S+)',c):
        t=m.group(1)
        if '{' in t: continue
        file,lab=name,t
        if t.startswith('#'): lab=t[1:]
        elif '#' in t: file,lab=t.split('#')
        else: file,lab=t,None
        if lab and (file not in labels or lab not in labels[file]): bad.append('%s: %s'%(name,t))
        if lab is None and file not in labels and file not in('Entry','Title'): bad.append('%s: script %s'%(name,t))
    for m in re.finditer(r'goto:(\#?[\w#]+)',c):
        t=m.group(1); file,lab=name,t
        if t.startswith('#'): lab=t[1:]
        elif '#' in t: file,lab=t.split('#')
        if file not in labels or lab not in labels[file]: bad.append('%s choice: %s'%(name,t))
dups=[]
for f,c in outs.items():
    ls=re.findall(r'^# (\S+)',c,re.M)
    seen=set()
    for l in ls:
        if l in seen: dups.append(f+':'+l)
        seen.add(l)
print('DUP LABELS:', dups if dups else 'none')
print('BAD:\n'+'\n'.join(sorted(set(bad))) if bad else 'ALL LABELS OK')
print('patch9 OK')
