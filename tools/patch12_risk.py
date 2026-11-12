# -*- coding: utf-8 -*-
"""0.4.12 승부수: 숨은 고정 문턱(if:sw>=8 등)을 보이는 승산(〔검술+1〕 + @risk)으로 바꾸고, 빗나간 판의 장면을 붙인다.
정본(canon) 파일을 입력으로 한다. 같은 자리에 두 번 돌려도 바뀌지 않는다(이미 바뀐 줄은 건너뜀)."""
import io,re,sys,os
C=sys.argv[1] if len(sys.argv)>1 else os.environ.get('MOLAK_CANON','canon')
P=os.path.join(C,'MolakEvents.nani')
s=io.open(P,encoding='utf-8').read()
L=s.split('\n')
def find_label(name):
    for i,l in enumerate(L):
        if l.strip()=='# '+name: return i
    raise KeyError(name)
def block_end(i):
    j=i+1
    while j<len(L) and not L[j].startswith('# '): j+=1
    return j
def tag_choice(target, cond_re, tag):
    """goto:#target 인 @choice 들에서 조건 cond_re 를 지우고 글귀 끝에 tag 를 붙인다."""
    n=0
    for i,l in enumerate(L):
        if not l.startswith('@choice') or ('goto:#'+target) not in l.split(' goto:')[-1].join(['',''])and ('goto:#'+target+' ' not in l+' '): continue
        if '〔' in l: n+=1; continue
        m=re.match(r'@choice "((?:[^"\\]|\\.)*)"(.*)$',l)
        txt,rest=m.groups()
        cm=re.search(r' if:(\S+)',rest)
        if cm:
            cond=cm.group(1)
            parts=[p for p in cond.split('&&') if not re.fullmatch(cond_re,p)]
            rest=rest.replace(' if:'+cond,(' if:'+'&&'.join(parts)) if parts else '')
        L[i]='@choice "'+txt+' '+tag+'"'+rest; n+=1
    assert n>0,('choice not found',target)
def risk_at(label, stat, need, fail, replace_re=None, on_pass=None):
    """label 첫머리에 @risk 와 빗나감 분기를 넣는다. replace_re: 지울 기존 판정 줄(정규식)."""
    i=find_label(label)
    if i+1<len(L) and L[i+1].startswith('@risk'): return
    if replace_re:
        j=i+1; e=block_end(i)
        L[i+1:e]=[x for x in L[i+1:e] if not re.fullmatch(replace_re,x.strip())]
    ins=['@risk on:%s need:%s'%(stat,need)]
    ins.append(('@goto #%s if:pass==1'%on_pass) if on_pass else ('@goto #%s if:pass==0'%fail))
    L[i+1:i+1]=ins
def add_block(after_label, name, body):
    if any(l.strip()=='# '+name for l in L): return
    i=find_label(after_label); e=block_end(i)
    L[e:e]=['# '+name]+body.strip('\n').split('\n')
SITES=[
 # (choice target, 지울 조건, 표시, 판정 넣을 라벨, stat, need, 빗나감 라벨, 빗나감 글)
 ('E_CultistsC0',r'sw>=\d+','〔검술+1〕','E_CultistsC0','sw','+1','E_CultFail','''@vfx kind:heavy
셋째의 칼이 옆구리를 판다. 첫 칼을 흘린 손목이 아직 저리다.
"그날 밤 살아남은 놈이 이놈이냐!"
피를 뿌리며 골목 셋을 굴러 빠져나온다. 등 뒤로 호루라기가 운다.
@fx hp:-5 sl:1
@set injury=3
@set f_wanted=1
@return'''),
 ('E_ThugsC2',r'sl>=\d+','〔살기〕','E_ThugsC2','sl','0','E_ThugsSFail','''눈을 맞춘다. 곰 가죽이 히죽 웃는다.
"눈깔 사납네. 그런 눈은 산에서 질리게 봤다."
몽둥이가 정강이를 친다. 통행세는 두 배로 뜯긴다.
@fx hp:-2 mn:-4
@return'''),
 ('E_SpyC1',r'sw>=\d+','〔검술-1〕','E_SpyC1','sw','-1','E_Spy2Fail','''뒷골목으로 꾀어 들였는데, 사내가 먼저 돌아선다. 소매에서 쇳가루가 날아든다.
눈을 비비는 사이 발소리가 멀어진다. 밀서는 그대로 교로 간다.
@fx hp:-1
@set f_wanted=1
@return'''),
 ('E_HunterC1',r'sw>=\d+','〔검술〕','E_HunterC1','sw','0','E_HunterFFail','''담 위에서 뛰어내린 순간, 사냥꾼이 이미 돌아서 있다.
"수배서 그림보다 굼뜨군."
칼등이 어깨를 부순다. 구르고, 일어나고, 생선 좌판을 엎으며 달아난다.
@fx hp:-3
@set injury=3
@return'''),
 ('E_ColdspringX',r'ng>=\d+','〔내공〕','E_ColdspringX','ng','0','E_ColdspringFail','''기를 한 바퀴 돌리기도 전에 냉기가 단전을 움켜쥔다. 진기가 얼음 조각처럼 혈도에 걸린다.
이를 딱딱 부딪치며 기어 나온다. 문양은 식지 않았다. 몸만 식었다.
@fx hp:-4 curse:-1
@return'''),
 ('E_TalismanC1',r'ng>=\d+','〔내공+1〕','E_TalismanC1','ng','+1','E_TalismanFail','''@fx mn:-3
기를 눈에 모았는데, 빛이 도는 것은 두 장이다. 고른 쪽은 틀렸다.
품에 넣은 부적이 밤새 아무 일도 하지 않는다.
@return'''),
 ('E_GambleC1',r'sl>=\d+','〔살기〕','E_GambleC1','sl','0','E_Gamble2Fail','''타짜의 손목을 노려본다. 타짜는 눈 한 번 깜빡이지 않고 패를 돌린다.
"객, 노름판에서 눈싸움은 하수나 하는 짓이오."
판이 끝날 즈음 전낭이 가볍다.
@fx mn:-3
@return'''),
 ('E_DokRevengeC0',r'sw>=\d+','〔검술-1〕','E_DokRevengeC0','sw','-1','E_DokRevengeFail','''@vfx kind:heavy
두 합이면 족할 줄 알았다. 셋째 합에서 뼈 장식 단 도끼가 갈비를 친다.
놈들이 웃으며 전낭을 턴다. "통행세에 이자까지."
@fx hp:-4 mn:-3
@return'''),
 ('E_BloodtaxC0',r'sw>=\d+','〔검술+2〕','E_BloodtaxC0','sw','+2','E_BloodtaxFail','''골목으로 꾀어 하나를 눕혔다. 둘째가 호각을 분다.
징세조 칼잡이 넷이 골목 양끝을 막는다. 담을 넘으며 등에 한 칼을 받는다.
전대는 주인에게 돌아가지 못했다. 당신의 얼굴만 장부에 올랐다.
@fx hp:-4 sl:1
@set f_wanted=1
@return'''),
 ('E_Cultists2C0',r'sw>=\d+','〔검술+1〕','E_Cultists2C0','sw','+1','E_Cultists2Fail','''@vfx kind:heavy
첫째를 눕히는 사이 둘째가 밧줄 끝을 당긴다. 묶인 청년들이 한꺼번에 넘어진다.
셋째의 칼이 허벅지를 긋는다. 청년들은 다시 일으켜 세워져 끌려간다.
돌아보던 그 얼굴이, 이번엔 당신을 보지 않는다.
@fx hp:-4 curse:1
@set f_wanted=1
@return'''),
 ('E_Yong3C0',r'sw>=\d+','〔검술-1〕','E_Yong3C0','sw','-1','E_Yong3Fail','''두 합으로 끝날 판이 다섯 합으로 늘어진다. 뼈 장식 하나가 짐꾼의 등짐을 강물에 차 넣는다.
놈들은 물러갔지만, 그날 품삯은 강물이 먹었다. 용개가 말없이 젖은 짐을 건져 올린다.
@fx hp:-3
@return'''),
 ('E_YongK2C1',r'sw>=\d+','〔검술-2〕','E_YongK2C1','sw','-2','E_YongK2Fail','''맨손으로 칼을 꺾으려다 손바닥이 갈라진다. 사내가 미친 듯이 휘두른다.
결국 검을 뽑는다. 사내의 팔에 한 줄이 그어진다. 베지 않으려 했는데, 베었다.
"아우를 죽인 것도 너 같은 놈이었다!"
@fx hp:-2 sl:1
@return'''),
 ('E_Beggar3Y',r'sl>=\d+','〔살기〕','E_Beggar3Y','sl','0','E_Beggar3Fail','''멱살을 쥐었는데 거지가 웃는다. "칼 찬 나리들 멱살은 하루에 세 번은 잡히오."
악을 쓰자 담 밑의 거지들이 일제히 일어선다. 아무것도 듣지 못하고 골목을 나온다.
@fx sl:1
@return'''),
 ('E_Beggar4C1',r'sw>=\d+','〔검술〕','E_Beggar4C1','sw','0','E_Beggar4Fail','''@art scene:teahouse
주막에서 붉은 끈 하나를 끌어낸다. 안에서 다섯이 더 나온다.
"거지 편 드는 칼이라. 이름값 좀 하겠군."
기둥을 등지고 버티다 겨우 빠져나온다. 거지 아이가 멀리서 그 꼴을 본다.
@fx hp:-4
@set f_wanted=1
@return'''),
 ('E_WolfhowlY',r'sl>=\d+','〔살기〕','E_WolfhowlY','sl','0','E_WolfhowlFail','''창을 열고 진기를 실어 운다. 목에서 쇳소리가 갈라진다.
북쪽 울음이 잠시 멎더니, 이번엔 객잔 쪽으로 가까이 운다. 제 무리가 아닌 울음이 어디서 났는지 들은 것이다.
@fx hp:-1 curse:1
@return'''),
 ('E_CupC',r'sl>=\d+','〔살기〕','E_CupC','sl','0','E_CupFail','''잔을 쏟고 눈을 본다. 사내는 눈을 피하지 않는다. 쏟아진 술이 거품을 무는데도 웃는다.
"독인 걸 알았으면, 다음 잔은 칼이오."
사내가 일어서며 탁자 밑으로 비수를 긋는다. 정강이에서 피가 번진다.
@fx hp:-3
@set f_wanted=1
@return'''),
 ('E_FloodB',r'sw>=\d+','〔검술+1〕','E_FloodB','sw','+1','E_FloodBFail','''버드나무 가지를 베어 눕힌다. 아이의 손이 가지 끝에 닿았다가, 미끄러진다.
흙탕물이 작은 머리를 삼킨다. 하류에서 누군가 울부짖는다.
가지를 쥔 손을 한참 펴지 못한다.
@fx hp:-1 curse:2
@return'''),
 ('E_FloodC',r'ng>=\d+','〔내공+1〕','E_FloodC','ng','+1','E_FloodCFail','''둑길을 달린다. 숨이 목까지 찬다. 여울목에 닿았을 때, 물살은 이미 아이를 지나 보냈다.
젖은 짚신 한 짝이 바위에 걸려 있다.
@fx hp:-2 curse:2
@return'''),
]
for tgt,cre,tag,lab,stat,need,fail,body in SITES:
    tag_choice(tgt,cre,tag)
    risk_at(lab,stat,need,fail)
    add_block(lab,fail,body)
# 기존의 무작위 실패(35%)를 승부수로: 등 줄 끊기
tag_choice('E_LanternX',r'sw>=\d+','〔검술-1〕')
i=find_label('E_LanternX')
if not L[i+1].startswith('@risk'):
    assert L[i+1].startswith('@set tr=random') and L[i+2].startswith('@goto #E_LanternZ')
    L[i+1:i+3]=['@risk on:sw need:-1','@goto #E_LanternZ if:pass==0']
# 숨은 자동 판정 → 보이는 승산: 표국 호위, 사냥꾼 넷
tag_choice('E_Guard2C0',r'sw>=\d+','〔검술-1〕')
risk_at('E_Guard2C0','sw','-1',None,replace_re=r'@goto #E_Guard2W if:sw>=\d+',on_pass='E_Guard2W')
tag_choice('E_Hunter3C0',r'sw>=\d+','〔검술+1〕')
risk_at('E_Hunter3C0','sw','+1',None,replace_re=r'@goto #E_Hunter3W if:sw>=\d+',on_pass='E_Hunter3W')
io.open(P,'w',encoding='utf-8').write('\n'.join(L))
print('ok', sum(1 for l in L if l.startswith('@risk')), 'risks')
# 지난 생의 앎으로 여는 길은 승산이 아니라 앎이다: 여울목 — 판정을 건너뛰는 입구를 따로 둔다
L=io.open(P,encoding='utf-8').read().split('\n')
for i,l in enumerate(L):
    if l.startswith('@choice "물에 들지 않고 하류로 달린다') and 'goto:#E_FloodC' in l and 'E_FloodCK' not in l:
        L[i]='@choice "물에 들지 않고 하류로 달린다 — 이 물이 어디서 얕아지는지 안다" if:g_end_ik>0 goto:#E_FloodCK'
if not any(l.strip()=='# E_FloodCK' for l in L):
    i=[k for k,l in enumerate(L) if l.strip()=='# E_FloodC'][0]
    assert L[i+1].startswith('@risk') and L[i+2].startswith('@goto #E_FloodCFail')
    L.insert(i+3,'# E_FloodCK')
io.open(P,'w',encoding='utf-8').write('\n'.join(L))
print('floodck ok')
