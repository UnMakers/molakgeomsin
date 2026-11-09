# -*- coding: utf-8 -*-
# 콘텐츠 팩 11: 되감긴 자의 아침(회차 서두 변주·교주의 기억·전생의 기억 경고) + 저주의 얼굴(노인의 목소리·사술 폭주 선택) + 사설 비무판
#  (사건 4종·아침 풍경은 evdefs/events11 → patch8 경유)  실행 순서: … → 9 → 10 → **11**
import io, os, re, sys
D = os.path.dirname(os.path.abspath(__file__))
def R(f): return io.open(os.path.join(D,f),encoding='utf-8').read()
def W(f,s): io.open(os.path.join(D,f),'w',encoding='utf-8').write(s)
def sub1(s, old, new, name):
    assert s.count(old) == 1, '%s: expected exactly one match (%d)' % (name, s.count(old))
    return s.replace(old, new)

m01 = R('out_Molak01.nani'); m02 = R('out_Molak02.nani'); m04 = R('out_Molak04.nani'); mdu = R('out_MolakDuel.nani')
assert u'# RbCommon' not in m01, 'patch11 already applied'

# ---- 1. 회차 서두 변주 (직전 말로 g_lastend) ----
m01 = sub1(m01,
u'''# Rebirth
눈을 뜬다.
운락객잔의 그 방. 그 천장. 창호지로 스며드는 그 빛.
"…깨어나셨소!"
하진무의 그 목소리. 전낭의 그 무게. 은자 서른 냥.
전부 그대로다. 당신의 기억만 빼고.
''',
u'''# Rebirth
눈을 뜬다.
운락객잔의 그 방. 그 천장. 창호지로 스며드는 그 빛.
@goto #RbWin if:g_lastend=="sicheon"||g_lastend=="donghaeng"||g_lastend=="sungeom"
@goto #RbSimma if:g_lastend=="simma"||g_lastend=="seongsa"
@goto #RbChain if:g_lastend=="pobak"||g_lastend=="soseul"
@goto #RbBlade if:g_lastend=="hoengsa"||g_lastend=="hyeolcheon"||g_lastend=="sahyeong"||g_lastend=="dokni"||g_lastend=="gyeona"
@goto #RbBody if:g_lastend=="gwaro"||g_lastend=="gongbok"
@goto #RbLeave if:g_lastend=="wonhaeng"
@goto #RbKneel if:g_lastend=="ipma"
@goto #RbCommon
# RbWin
"…깨어나셨소?"
하진무의 목소리가 반 박자 늦다. 문간에 선 영감이 당신을 오래 본다—꿈에서 본 얼굴을 확인하듯.
"……이상한 꿈을 꿨소. 깃발이 타는 꿈을."
그도 꾸었다. 되감기는 것은 당신뿐인데, 세상이 조금씩 기억을 흘린다.
@goto #RbCommon2
# RbSimma
가슴이 아직 뜨겁다. 지난밤—아니, 지난 생의 열이 문양 안에 고여 있다.
손끝을 본다. 깨끗하다. 먹빛은 없다. 그러나 손이 기억한다, 검게 물들어 가던 감각을.
@goto #RbCommon
# RbChain
손목이 아프다. 사슬 자국은 없다. 없는데 아프다.
목을 만진다. 베인 자리도 없다. 거기도 아프다.
@goto #RbCommon
# RbBlade
옆구리에 손이 먼저 간다. 상처는 없다. 지난 생의 검이 들어온 자리를 몸이 외운 것이다.
검을 쥔다. 손이 조금 더 빨리 자루를 찾는다. 죽음은 수업료가 비싼 대신, 환불이 없다.
@goto #RbCommon
# RbBody
배가 고프다. 지독하게. 지난 생의 마지막 허기가 따라왔다.
아래층 국밥 냄새가 그 어느 아침보다 선명하다. 이번 생은 밥부터 먹기로 한다.
@goto #RbCommon
# RbLeave
강물 소리가 귓가에 남아 있다. 이름 모를 강가. 옆자리의 온기.
창밖은 운락이다. 남쪽이 아니다. 베개 밑을 더듬는다—거기 있어야 할 쌍검 한 자루는, 없다.
@goto #RbCommon
# RbKneel
무릎이 저리다. 돌바닥에 닿았던 무릎.
"존명." 입이 멋대로 그 말을 되뇌려다 멈춘다. 아직 아무에게도 무릎 꿇지 않은 아침이다. 이번에는.
@goto #RbCommon
# RbCommon
"…깨어나셨소!"
하진무의 그 목소리.
# RbCommon2
전낭의 그 무게. 은자 서른 냥.
전부 그대로다. 당신의 기억만 빼고.
''', 'Rebirth variants')

# ---- 2. 교주의 기억 (등극 이력) ----
m04 = sub1(m04,
u'''# Ld2
@art char:lord
골짜기 최심부. 검은 전각.
''',
u'''# Ld2
@art char:lord
@goto #Ld2Again if:g_clears>0
골짜기 최심부. 검은 전각.
''', 'Ld2 gate')
m04 = sub1(m04,
u'''"일 년을 자고, 삼류에서 여기까지. 사람의 속도가 아니지. …누구의 솜씨냐."
@choice "\\"복면의 백발. 당신도 아는 자겠지.\\"" goto:#Ld2C0
''',
u'''"일 년을 자고, 삼류에서 여기까지. 사람의 속도가 아니지. …누구의 솜씨냐."
@goto #Ld2Q
# Ld2Again
골짜기 최심부. 검은 전각. 문은 열려 있다.
붉은 단 위의 사내가 당신을 본다. 오래. 처음 보는 얼굴을 보듯이가 아니라—잊은 꿈을 더듬듯이.
"……또 그 얼굴이군."
"……."
"이번엔 몇 번째냐."
세지 않는다. 그도 안다, 당신이 세지 않는다는 것을. 되감기는 자들끼리의 예의다.
교주가 천천히 일어선다. 지겨운 눈. 그 안쪽에—아주 얇게—무언가가 있다. 두려움이라 부르기엔 오래된 것.
# Ld2Q
@choice "\\"복면의 백발. 당신도 아는 자겠지.\\"" goto:#Ld2C0
''', 'Ld2Again block')

# ---- 3. 노인의 목소리 (저주 80~87, 1회) ----
m02 = sub1(m02,
u'''# Night
@art char:none
@scene id:night
@pickflavor pool:night
''',
u'''# Night
@art char:none
@scene id:night
@goto #NvOld if:curse>=80&&curse<88&&f_oldseen>0&&tn_oldvoice<1
@pickflavor pool:night
''', 'Night gate')
m02 = sub1(m02,
u'''# Nv1
해가 저문다. 온전히 당신의 것인 반시진이 남았다.
@goto #NightChoice
''',
u'''# Nv1
해가 저문다. 온전히 당신의 것인 반시진이 남았다.
@goto #NightChoice
# NvOld
@set tn_oldvoice=1
해가 저문다. 그런데 마당이 조용하다. 바람조차.
'거의 다 익었다.'
뼛속의 못이 운다. 노인의 목소리. 어디에도 그는 없다—있을 필요가 없다. 그는 당신 안에 있다.
'서두르지 마라. 익는 것은 네가 아니라 열매다.'
소리가 돌아온다. 술꾼들의 노래, 주판 소리. 반시진이 남았다—전보다 훨씬 짧게 느껴지는 반시진이.
@fx curse:1
@goto #NightChoice
''', 'NvOld block')

# ---- 4. 사설 비무판 (비무판 폐쇄 사흘) ----
m02 = sub1(m02,
u'''@choice "비무판을 찾아간다" if:day>=4 goto:#NDuel
''',
u'''@choice "비무판을 찾아간다" if:day>=4&&f_ringclosed<1 goto:#NDuel
@choice "뒷골목 사설 비무판 — 판돈 셋, 눈 없음" if:f_ringclosed>0 goto:#NDuelBack
''', 'NightChoice back ring')
m02 = sub1(m02,
u'''# NWolf
@goto Molak04#Wf1
''',
u'''# NDuelBack
폐가 뒷마당. 횃불 둘. 심판도 방(榜)도 없다. 이기면 판돈, 지면—묻는 자가 없다.
@set dkind="back"
@gosub MolakDuel#Run
@goto #NightEnd
# NWolf
@goto Molak04#Wf1
''', 'NDuelBack label')

# ---- 5. 비무: 전생의 기억 경고 + 75 이상 사술 폭주 선택 ----
mdu = sub1(mdu,
u'''<color=#C03434>{foename}</color> — 강호 서열 {foerank}위 · {foetier}.<br>상대의 기세를 읽는다. 승산, 어림잡아 <color=#C9A227>열에 {odds10}</color>.
@choice "정면으로 벤다" goto:#StFront
''',
u'''<color=#C03434>{foename}</color> — 강호 서열 {foerank}위 · {foetier}.<br>상대의 기세를 읽는다. 승산, 어림잡아 <color=#C9A227>열에 {odds10}</color>.
@goto #RunChoices if:memwarn<1
<color=#9A70C4>지난 생, 이 상대의 검 아래서 죽었다.</color> 전생의 기억이 그 순간을 되감아 보여준다—상대의 첫 수를.
# RunChoices
@choice "정면으로 벤다" goto:#StFront
''', 'Run memwarn')
mdu = sub1(mdu,
u'''@choice "사술을 끌어낸다 — 저주 +5" if:cancurse>0 goto:#StCurse
''',
u'''@choice "사술을 끌어낸다 — 저주 +5" if:cancurse>0&&curse<75 goto:#StCurse
@choice "사술이 먼저 나선다 — 막지 못한다. 저주 +8" if:cancurse>0&&curse>=75 goto:#StCurse2
''', 'curse2 choice')
mdu = sub1(mdu,
u'''# Result
''',
u'''# StCurse2
막으려 했다. 손이 먼저 나갔다—당신의 손이 아닌 것처럼.
문양이 <color=#C03434>스스로</color> 타오른다. 상대의 빈틈이 아니라, 상대의 목숨 줄이 보인다.
@duel style:curse2
@goto #Result
# Result
''', 'StCurse2 block')

W('out_Molak01.nani', m01); W('out_Molak02.nani', m02); W('out_Molak04.nani', m04); W('out_MolakDuel.nani', mdu)

# ---- 6. initvars 재생성 + 라벨 검사 ----
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
print('patch11 OK')
