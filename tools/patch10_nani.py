# -*- coding: utf-8 -*-
# 콘텐츠 팩 10: 종장 변주(중반 플래그 → 혈쇄문/검은 산/혈천진/교주전 분기) + 깨달음 상점 확장(비방·외상·육신)
#  실행 순서: … → 8 → 9 → **10** (initvars 재생성 + 라벨 검사 포함)
import io, os, re, sys
D = os.path.dirname(os.path.abspath(__file__))

def R(f): return io.open(os.path.join(D,f),encoding='utf-8').read()
def W(f,s): io.open(os.path.join(D,f),'w',encoding='utf-8').write(s)
def sub1(s, old, new, name):
    assert s.count(old) == 1, '%s: expected exactly one match (%d)' % (name, s.count(old))
    return s.replace(old, new)

m04 = R('out_Molak04.nani'); mov = R('out_MolakOver.nani'); mev = R('out_MolakEvents.nani')
assert u'Wf1C3' not in m04, 'patch10 already applied'

# ---- 1. 혈쇄문 입장 변주 ----
m04 = sub1(m04,
u'''@choice "말없이 검을 뽑는다" goto:#Wf1C2
@stop
# Wf1C0
''',
u'''@choice "말없이 검을 뽑는다" goto:#Wf1C2
@choice "소아의 봉인법으로 문양을 잠그고 들어선다" if:f_soaseal>0 goto:#Wf1C3
@choice "\\"적운의 아이들은 어디 있나.\\" — 먼저 묻는다" if:f_soareq>0 goto:#Wf1C4
@choice "낭인 마충이 뒤에 서 있다 — 사슬 절반은 그의 몫" if:f_sparfriend>0 goto:#Wf1C5
@stop
# Wf1C3
쌍검의 문파가 쓰는 봉인. 소아가 가르쳐 준 대로 숨을 세 번 끊고, 문양 위에 손바닥을 얹는다.
열이 가라앉는다. 가슴이 조용해진 만큼—공력이 온전히 검으로 간다. 오늘만큼은 저주와 둘이 싸우지 않아도 된다.
@fx curse:-2
@set f_wolfseal=1
@goto #Wf2
# Wf1C4
문주의 어깨가 아주 잠깐 창고 쪽으로 기운다. 그것으로 충분하다.
"……산 채로 두었다. 교주께서 쓸 데가 있다 하셔서."
산 채로. 소아의 아이들이 사슬 너머에 있다. 오늘 이 마당에서 져서는 안 되는 이유가 하나 더 생겼다—그리고, 문주의 눈이 창고로 갈 때마다 검이 반 치 늦는다는 것도 보았다.
@fx sl:1
@set f_wolfedge=1
@goto #Wf2
# Wf1C5
"백 합이라 했지."
마충이 등 뒤에서 도를 뽑는다. 사슬의 숲이 두 사람 몫으로 나뉜다. 왼쪽 사슬은 그의 도가 받아낸다. 당신은 오른쪽만 보면 된다.
"돌아오면 백 합—오늘 반쯤 미리 치르지."
@set f_wolfally=1
@goto #Wf2
# Wf1C0
''', 'Wf1 choices')

# 마충 후일담: 승리 시 한 줄
m04 = sub1(m04,
u'''# WolfWin
혈쇄문주가 제 사슬 더미 위에 무릎을 꿇는다.
''',
u'''# WolfWin
혈쇄문주가 제 사슬 더미 위에 무릎을 꿇는다.
@goto #WolfWin2 if:f_wolfally<1
마충이 왼팔을 사슬에 감긴 채 웃고 있다. "…남은 오십 합은 외상이다." 그는 이 팔을 한 달쯤 못 쓸 것이다. 빚이 늘었다. 갚을 만한 빚이다.
# WolfWin2
''', 'WolfWin ally line')

# ---- 2. 검은 산 입구 변주 ----
m04 = sub1(m04,
u'''@choice "지나쳐 바로 산으로 들어간다" goto:#Ld0C2
@stop
''',
u'''@choice "지나쳐 바로 산으로 들어간다" goto:#Ld0C2
@choice "소금길로 돌아 들어간다 — 교의 눈이 없는 길" if:f_valleyroute>0||f_roadinfo>0 goto:#Ld0C3
@choice "교 안의 불만 세력과 접선한다" if:f_spyinfo>0 goto:#Ld0C4
@stop
# Ld0C3
장사치들만 아는 소금길. 관도를 버리고 나루 윗길로, 폐사 뒤로.
순찰 한 번 마주치지 않고 산자락에 닿는다. 주막의 소란도, 취객도 없다. 하룻밤을 바위 그늘에서 온전히 잔다.
새벽, 교도 둘이 바위 그늘로만 걷는 것을 본다. 같은 보폭, 같은 꺾임. 눈에 새긴다.
@fx hp:2
@set f_taverninfo=1
@goto #Ld1
# Ld0C4
주막 뒤꼍. 교의(敎衣) 자락이 먼저 보인다. 세작이다—운락에서 당신을 지켜보던 그 눈.
"교주를 원치 않는 자들이 있다 했지. 오늘이 그날이오."
그가 모래 위에 세 걸음을 긋는다. 왼발 사선, 멈춤, 오른발 반 보. 혈천진의 틈.
"대전까지는 우리가 길을 비우겠소. 그 뒤는—그대 몫이오. 지면, 우리는 그대를 모른다."
@set f_janginfo=1
@set f_spyally=1
@goto #Ld1
''', 'Ld0 choices')

# ---- 3. 혈천진 돌파 변주 ----
m04 = sub1(m04,
u'''@choice "반나절, 진의 숨을 더듬는다" if:f_janginfo<1 goto:#Ld1C3
@stop
''',
u'''@choice "반나절, 진의 숨을 더듬는다" if:f_janginfo<1 goto:#Ld1C3
@choice "봉인법으로 문양을 잠그고 걷는다" if:f_soaseal>0&&f_janginfo<1 goto:#Ld1C4
@choice "사형의 패를 쥔다 — 청운의 보법으로" if:f_relicpae>0&&f_janginfo<1 goto:#Ld1C5
@stop
# Ld1C4
진이 깨무는 것은 혈도다. 그리고 진이 가장 먼저 찾는 것은—가슴의 문양이다.
봉인법으로 문양을 잠근다. 진이 더듬을 것이 없어진다. 아지랑이가 당신을 지나쳐, 뒤에 선 바위를 깨문다.
서른 걸음. 피 한 방울 없이.
@fx curse:-1
@set f_lordseal=1
@goto #Ld2
# Ld1C5
일곱째 사형의 패를 쥔다. 청운의 보법은 여덟 초식의 발이다—마당을 쓸며 천 번 밟은 그 발.
진이 혈도를 깨물 때마다 반 보씩 비껴 선다. 사형이 살아 있었다면 이렇게 걸었을 것이다.
스무 걸음에 한 번 피를 삼킨다. 그래도 설 수 있다.
@fx hp:-1
@goto #Ld2
''', 'Ld1 choices')

# ---- 4. 교주 대면 변주 ----
m04 = sub1(m04,
u'''@choice "대답 대신 검을 뽑는다" goto:#Ld2C1
@stop
# Ld2C0
''',
u'''@choice "대답 대신 검을 뽑는다" goto:#Ld2C1
@choice "\\"당신도 누군가의 밭이었다지.\\"" if:f_monklore>0||f_sutraknow>0 goto:#Ld2C2
@choice "\\"북두를 거꾸로.\\" — 대사형의 말을 되뇐다" if:f_jinbaektruth>0 goto:#Ld2C3
@stop
# Ld2C2
@set f_lordedge=1
@fx curse:-2
교주의 얼굴에서 표정이 사라진다. 지겨움조차.
"……산사의 중놈이 아직 살아 있었군."
단을 내려서는 걸음이 처음으로 흐트러진다. 밭. 그 한 글자가 서열록의 일위를 흔든다.
"거두는 자가 되지 마라—그랬겠지. 그 중놈은 늘 반만 말한다. 거두지 않으면, 거둬진다."
@goto #Ld3c
# Ld2C3
@set f_lordedge=1
@set f_northstep=1
"……누가 가르쳤나."
"대사형이."
교주가 웃지 않는다. 혈천진의 눈이 북두를 거꾸로 밟는다는 것—교주 뒤의 일곱 걸음—그것을 아는 자는 교 안에서도 셋뿐이다.
"진백. 그 개가 끝까지 청운이었군."
대사형이 남긴 일곱 걸음을 머릿속으로 밟는다. 교주가 선 자리가, 진의 눈이 보인다.
@goto #Ld3d
# Ld3c
"그 늙은이가 나를 심은 것은 삼십 년 전이다."
교주가 제 가슴을 두드린다. 흉터조차 없는 자리.
"익기 전에 베어냈다. 그래서 나는 거두어지지 않았고—이 자리에 앉았다. 네 가슴의 그것은 아직 뿌리가 있군. 가엾게."
가엾다. 서열록의 일위가 당신을 가엾다 한다. 그 말이 검보다 먼저 가슴을 친다—그리고 검보다 먼저, 틈을 보여준다. 교주는 지금 당신이 아니라 삼십 년 전의 제 가슴을 보고 있다.
@set dkind="lord"
@gosub MolakDuel#Run
@goto #LordAfter
# Ld3d
"오너라, 막내."
그가 대사형의 말투를 흉내 낸다. 모욕이다. 그러나 당신은 모욕을 듣는 대신—그의 뒤를 본다.
일곱 걸음. 북두의 자루 끝. 진의 눈이 거기서 빛을 머금고 있다. 교주가 선 자리를 지나 그곳에 닿으면, 대전의 공기는 그의 것이 아니게 된다.
천 번을 따라 했던 기수식으로, 대사형이 마지막으로 가르친 길을 간다.
@set dkind="lord"
@gosub MolakDuel#Run
@goto #LordAfter
# Ld2C0
''', 'Ld2 choices')

# ---- 5. 깨달음 상점 확장 ----
mov = sub1(mov,
u'''@choice "다 시 그 날 로" goto:#Restart
''',
u'''@choice "청심차 비방 — 처음부터 매일 저주 −0.15 \\[{cost_tea}\\]" if:g_know_tea>0&&max_tea<1&&g_pts>=cost_tea goto:#BuyTea
@choice "빈칸 방값 — 하진무의 외상 \\[{cost_lodge}\\]" if:g_know_lodge>0&&max_lodge<1&&g_pts>=cost_lodge goto:#BuyLodge
@choice "구르고 또 구른 몸 — 시작 체력 +1 ({lv_vigor}/10) \\[{cost_vigor}\\]" if:max_vigor<1&&g_pts>=cost_vigor goto:#BuyVigor
@choice "다 시 그 날 로" goto:#Restart
''', 'Shop new choices')
mov = sub1(mov,
u'''# BuyMem
@buyup kind:mem
@goto #Shop
''',
u'''# BuyMem
@buyup kind:mem
@goto #Shop
# BuyTea
@buyup kind:tea
@goto #Shop
# BuyLodge
@buyup kind:lodge
@goto #Shop
# BuyVigor
@buyup kind:vigor
@goto #Shop
''', 'Buy labels')

# 비방/외상을 한 번 얻으면 회귀해도 기억 (전역 플래그)
mev = sub1(mev, u'@set f_tea=1\n', u'@set f_tea=1\n@set g_know_tea=1\n', 'tea know')
mev = sub1(mev, u'@set f_hajinfree=1\n', u'@set f_hajinfree=1\n@set g_know_lodge=1\n', 'lodge know')

W('out_Molak04.nani', m04); W('out_MolakOver.nani', mov); W('out_MolakEvents.nani', mev)

# ---- 6. initvars 재생성 + 라벨 검사 (patch8/9 와 동일) ----
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
print('patch10 OK')
