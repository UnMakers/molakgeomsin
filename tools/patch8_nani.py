# -*- coding: utf-8 -*-
# 콘텐츠 팩 8: 사건 시스템 v2 — 1회성 사건, 낮/밤 슬롯, NPC 단계 사슬, 아침/밤 풍경 풀, 밤 행동 변주
#  실행 순서: build_nani.js → patch → 2 → 3 → 4 → 5 → 6 → 7 → **8** (initvars 재생성 + 라벨 검사 포함)
import io, os, re, sys
D = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, D)
from evdefs import EVENTS, EXISTING, MORNING, NIGHTFLAVOR
from events8 import NEW_EVENTS

def R(f): return io.open(os.path.join(D,f),encoding='utf-8').read()
def W(f,s): io.open(os.path.join(D,f),'w',encoding='utf-8').write(s)

m02 = R('out_Molak02.nani'); mev = R('out_MolakEvents.nani')
assert u'; ===== 팩 8' not in mev, 'patch8 already applied'

# ---- 1. 신규 사건 본문 추가 (사건 핸들러 섹션 앞에) ----
anchor = u'; ===== 사건 핸들러 ====='
assert anchor in mev
mev = mev.replace(anchor, NEW_EVENTS.strip() + u'\n\n' + anchor, 1)

# ---- 2. 기존 사건 소폭 수정: 분기 플래그 ----
old = u'# E_YongK\n"더 할 말은?"'
assert old in mev
mev = mev.replace(old, u'# E_YongK\n@set f_yongkilled=1\n"더 할 말은?"', 1)
for lab in ('E_BloodtaxC0', 'E_BloodtaxC2'):
    a = u'# %s\n' % lab
    assert a in mev
    mev = mev.replace(a, a + u'@set f_taxhelp=1\n', 1)
# 소아의 봉인법: C#(CurseFactor)이 읽는 변수명도 함께 세팅
old = u'@set f_soaseal=1\n'
assert old in mev
mev = mev.replace(old, old + u'@set soaseal=1\n', 1)

# 모든 신규 사건 라벨 존재 확인
labels = set(re.findall(r'^# (\S+)', mev, re.M))
for e in EVENTS:
    assert e[0] in labels, 'event body missing: ' + e[0]
    if e[0] not in EXISTING: assert (u'# %s\n' % e[0]) in NEW_EVENTS, 'not new: ' + e[0]

# ---- 3. Molak02: 낮/밤 슬롯 ----
old = u'# Daytime\n@pickevent chance:60\n'
assert old in m02
m02 = m02.replace(old, u'# Daytime\n@pickevent chance:65 slot:day\n', 1)
old = u'# Evening\n@art char:none\n@pickevent chance:30\n'
assert old in m02
m02 = m02.replace(old, u'# Evening\n@art char:none\n@pickevent chance:40 slot:night\n', 1)

# ---- 4. Molak02: RunEvent 디스패치 전면 교체 ----
i = m02.index(u'# RunEvent\n'); j = m02.index(u'@return\n', i) + len(u'@return\n')
dispatch = u'# RunEvent\n' + u''.join(u'@gosub MolakEvents#%s if:nextevent=="%s"\n' % (e[0], e[0]) for e in EVENTS) + u'@return\n'
m02 = m02[:i] + dispatch + m02[j:]

# ---- 5. Molak02: 아침 풍경 풀 ----
i = m02.index(u'@set mrand=random(1,8)\n'); j = m02.index(u'# Daytime\n')
lines = [u'@pickflavor pool:morning']
lines += [u'@goto #M%d if:mrand==%d' % (k+1, k+1) for k in range(1, len(MORNING))]
lines += [u'@goto #M1']
for k, (mn, mx, cond, txt) in enumerate(MORNING):
    lines += [u'# M%d' % (k+1), txt, u'@goto #Daytime']
m02 = m02[:i] + u'\n'.join(lines) + u'\n\n' + m02[j:]

# ---- 6. Molak02: 밤 서두 풀 ----
i = m02.index(u'@set nrand=random(1,4)\n'); j = m02.index(u'# NightChoice\n')
lines = [u'@pickflavor pool:night']
lines += [u'@goto #Nv%d if:nrand==%d' % (k+1, k+1) for k in range(1, len(NIGHTFLAVOR))]
lines += [u'@goto #Nv1']
for k, (mn, mx, cond, txt) in enumerate(NIGHTFLAVOR):
    lines += [u'# Nv%d' % (k+1), txt, u'@goto #NightChoice']
m02 = m02[:i] + u'\n'.join(lines) + u'\n' + m02[j:]

# ---- 7. Molak02: 밤 행동 변주 + 수련 막간(T_) 1회성 훅 ----
old = u'''# NSw
@set r=random(1,3)
@goto #NSwB if:f_bilokhalf>0||f_biloklearn>0
달빛 아래 천 번의 횡소(橫掃). 손바닥이 터지고, 감각이 쌓인다.
@fx sw:1 hp:-1
@goto #NightEnd
# NSwB
「운검비록」의 구결이 손끝을 이끈다. 어제의 반 치, 오늘의 반 치—그것이 쌓여 벽이 된다.
@fx sw:2 hp:-1
@goto #NightEnd
# NQi
@goto #NQiB if:f_breath>0
조식(調息), 취기(聚氣), 운기(運氣)… 토탁(吐濁)에서 막힌다. 뱉어야 할 탁기를, 가슴의 <color=#9A70C4>문양이 먼저 삼킨다</color>.
@fx ng:2 hp:1 curse:3
@goto #NightEnd
# NQiB
들숨 여섯, 날숨 아홉. 거지 중의 구결대로 숨을 바꾸자, 탁기가 덜 쌓인다.
@fx ng:2 hp:1 curse:2
@goto #NightEnd
'''
assert old in m02, 'night actions anchor'
new = u'''# NSw
@set tr=random(1,100)
@goto #NSwT1 if:tn_sw1<1&&day>=4&&tr<=30
@goto #NSwT2 if:tn_sw2<1&&day>=9&&tr<=30
@goto #NSwB if:f_bilokhalf>0||f_biloklearn>0
@set sv=random(1,4)
@goto #NSw2 if:sv==2
@goto #NSw3 if:sv==3
@goto #NSw4 if:sv==4
달빛 아래 천 번의 횡소(橫掃). 손바닥이 터지고, 감각이 쌓인다.
@fx sw:1 hp:-1
@goto #NightEnd
# NSw2
횡소 천 번, 자세는 하나. 사형이 세던 숫자를 당신이 센다. 팔백에서 어깨가 울고, 구백에서 조용해진다.
@fx sw:1 hp:-1
@goto #NightEnd
# NSw3
마당의 느티나무를 상대로 천 번. 나무는 피하지 않는다. 피하지 않는 상대를 베는 연습은—결국 제 검을 보는 연습이다.
@fx sw:1 hp:-1
@goto #NightEnd
# NSw4
달이 없는 밤. 보이지 않는 검끝을 손끝으로 더듬는다. 천 번째에—검이 어디 있는지 눈 없이 안다.
@fx sw:1 hp:-1
@goto #NightEnd
# NSwT1
@set tn_sw1=1
@gosub MolakEvents#T_Sw1
@goto #NightEnd
# NSwT2
@set tn_sw2=1
@gosub MolakEvents#T_Sw2
@goto #NightEnd
# NSwB
@set sv=random(1,3)
@goto #NSwB2 if:sv==2
@goto #NSwB3 if:sv==3
「운검비록」의 구결이 손끝을 이끈다. 어제의 반 치, 오늘의 반 치—그것이 쌓여 벽이 된다.
@fx sw:2 hp:-1
@goto #NightEnd
# NSwB2
비록의 구결이 몸에 든다. 손목이 아니라 허리, 허리가 아니라 발. 어제 몰랐던 반 치가 오늘 손에 있다.
@fx sw:2 hp:-1
@goto #NightEnd
# NSwB3
운검비록 제삼결. 획 하나를 천 번 긋는다. 종이가 아니라 공기에. 공기가 베이는 소리를, 당신은 이제 안다.
@fx sw:2 hp:-1
@goto #NightEnd
# NQi
@set tr=random(1,100)
@goto #NQiT1 if:tn_qi1<1&&day>=5&&tr<=30
@goto #NQiT2 if:tn_qi2<1&&day>=12&&curse>=25&&tr<=30
@goto #NQiB if:f_breath>0
@set sv=random(1,4)
@goto #NQi2 if:sv==2
@goto #NQi3 if:sv==3
@goto #NQi4 if:sv==4
조식(調息), 취기(聚氣), 운기(運氣)… 토탁(吐濁)에서 막힌다. 뱉어야 할 탁기를, 가슴의 <color=#9A70C4>문양이 먼저 삼킨다</color>.
@fx ng:2 hp:1 curse:3
@goto #NightEnd
# NQi2
운기 한 바퀴. 단전에서 올라온 기가 가슴에서 한 번 멈춘다. 문양이 통행세를 받는 자리다. 내고, 지나간다.
@fx ng:2 hp:1 curse:3
@goto #NightEnd
# NQi3
들숨, 날숨. 벽 너머 술꾼들의 노래가 박자가 된다. 강호의 밤에 맞춰 숨 쉬는 법—그것도 운기다. 문양은 제 몫을 챙긴다.
@fx ng:2 hp:1 curse:3
@goto #NightEnd
# NQi4
운기 도중 사형의 목소리가 들린 것 같았다. '막내야, 숨이 급하다.' 숨을 늦춘다. 기가 한 뼘 깊이 내려가고—문양도 한 뼘 따라온다.
@fx ng:2 hp:1 curse:3
@goto #NightEnd
# NQiT1
@set tn_qi1=1
@gosub MolakEvents#T_Qi1
@goto #NightEnd
# NQiT2
@set tn_qi2=1
@gosub MolakEvents#T_Qi2
@goto #NightEnd
# NQiB
@goto #NQiB3 if:f_breath2>0
@set sv=random(1,2)
@goto #NQiB2 if:sv==2
들숨 여섯, 날숨 아홉. 거지 중의 구결대로 숨을 바꾸자, 탁기가 덜 쌓인다.
@fx ng:2 hp:1 curse:2
@goto #NightEnd
# NQiB2
여섯, 아홉. 구결대로 숨을 바꾼다. 문양이 삼키는 몫이 손톱만큼 준다. 손톱만큼이 쌓여 목숨이 된다.
@fx ng:2 hp:1 curse:2
@goto #NightEnd
# NQiB3
여섯, 아홉—그리고 반 호흡의 멈춤. 거지 중의 두 번째 구결. 탁기가 씨앗에 닿기 전에 입에서 나간다. 문양이 굶는다.
@fx ng:2 hp:1 curse:1
@goto #NightEnd
'''
m02 = m02.replace(old, new, 1)

old = u'''# NWork
물지게 마흔 번. 어깨에 밧줄 자국. 삯은 정직하고, 정직한 것들은 대개 아프다.
@set r=random(2,3)
@fx mn:{r} hp:-1
@goto #NightEnd
'''
assert old in m02, 'NWork anchor'
new = u'''# NWork
@set tr=random(1,100)
@goto #NWorkT1 if:tn_wk1<1&&day>=6&&tr<=30
@set r=random(2,3)
@set sv=random(1,3)
@goto #NWork2 if:sv==2
@goto #NWork3 if:sv==3
물지게 마흔 번. 어깨에 밧줄 자국. 삯은 정직하고, 정직한 것들은 대개 아프다.
@fx mn:{r} hp:-1
@goto #NightEnd
# NWork2
장작 패기 이백 개. 도끼질에도 검법이 있다—라고 생각하면 덜 지겹다. 삯은 정직하고, 어깨는 아프다.
@fx mn:{r} hp:-1
@goto #NightEnd
# NWork3
저자 창고에서 가마니를 나른다. 창고지기가 당신 손바닥의 굳은살을 보고 품삯을 반 닢 얹어 준다. 손이 이력서다.
@fx mn:{r} hp:-1
@goto #NightEnd
# NWorkT1
@set tn_wk1=1
@gosub MolakEvents#T_Wk1
@goto #NightEnd
'''
m02 = m02.replace(old, new, 1)

old = u'''# NRest
침상에 눕는다. 아무 일도 없는 밤—지금의 당신에게는 그것이 사치다.
@fx hp:3
@goto #NightEnd
'''
assert old in m02, 'NRest anchor'
new = u'''# NRest
@set tr=random(1,100)
@goto #NRestT1 if:tn_rest1<1&&day>=5&&tr<=30
@set sv=random(1,4)
@goto #NRest2 if:sv==2
@goto #NRest3 if:sv==3
@goto #NRest4 if:sv==4
침상에 눕는다. 아무 일도 없는 밤—지금의 당신에게는 그것이 사치다.
@fx hp:3
@goto #NightEnd
# NRest2
일찍 눕는다. 천장의 얼룩을 센다. 일 년 동안 센 얼룩이다. 오늘은 하나가 더 늘어 보인다. 착각일 것이다.
@fx hp:3
@goto #NightEnd
# NRest3
잠이 오지 않아 창을 연다. 달이 마당의 느티나무에 걸려 있다. 아무 일도 없는 밤. 그것이 사치라는 것을 몸이 안다.
@fx hp:3
@goto #NightEnd
# NRest4
점소이가 두고 간 식은 국밥을 마저 먹고 눕는다. 배부른 것도 수련이다—라고 사형이 말했었다. 믿기로 한다.
@fx hp:3
@goto #NightEnd
# NRestT1
@set tn_rest1=1
@gosub MolakEvents#T_Rest1
@goto #NightEnd
'''
m02 = m02.replace(old, new, 1)

W('out_Molak02.nani', m02); W('out_MolakEvents.nani', mev)

# ---- 8. C# 정의표 생성 ----
def cs_str(s): return 'null' if s is None else '"%s"' % s
rows = []
for (eid, slot, mn, mx, cond, after, gap, w) in EVENTS:
    rows.append('            new Def("%s", %d, %d, \'%s\', %s, %s, %d, %d),' % (eid, mn, mx, slot, cs_str(cond), cs_str(after), gap, w))
def flav(name, pool):
    return '        public static readonly Flav[] %s = {\n%s\n        };' % (name, '\n'.join(
        '            new Flav(%d, %d, %s),' % (mn, mx, cs_str(cond)) for (mn, mx, cond, txt) in pool))
cs = u'''// (자동 생성: patch8_nani.py) 사건 정의표 v2 — 수정은 evdefs.py 에서.
public static class MolakEventDefs
{
    public struct Def
    {
        public string Id; public int MinDay, MaxDay; public char Slot; public string Cond, After; public int Gap, Weight;
        public Def (string id, int minDay, int maxDay, char slot, string cond, string after, int gap, int weight)
        { Id = id; MinDay = minDay; MaxDay = maxDay; Slot = slot; Cond = cond; After = after; Gap = gap; Weight = weight; }
    }
    public struct Flav
    {
        public int MinDay, MaxDay; public string Cond;
        public Flav (int minDay, int maxDay, string cond) { MinDay = minDay; MaxDay = maxDay; Cond = cond; }
    }
    public static readonly Def[] Table = {
%s
    };
%s
%s
}
''' % ('\n'.join(rows), flav('Morning', MORNING), flav('Night', NIGHTFLAVOR))
W('MolakEventDefs.cs', cs)

# ---- 9. initvars 재생성 (patch5 와 동일 로직) ----
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

# ---- 10. 라벨 무결성 ----
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
# 중복 라벨 검사
dups=[]
for f,c in outs.items():
    ls=re.findall(r'^# (\S+)',c,re.M)
    seen=set()
    for l in ls:
        if l in seen: dups.append(f+':'+l)
        seen.add(l)
print('DUP LABELS:', dups if dups else 'none')
print('BAD:\n'+'\n'.join(sorted(set(bad))) if bad else 'ALL LABELS OK')
print('patch8 OK: events=%d new=%d morning=%d night=%d' % (len(EVENTS), len([e for e in EVENTS if e[0] not in EXISTING]), len(MORNING), len(NIGHTFLAVOR)))
