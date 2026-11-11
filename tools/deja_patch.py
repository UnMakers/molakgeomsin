# -*- coding: utf-8 -*-
# 0.4.9: 지난 생에 지나온 장면이면 첫 본문 줄 앞에 한 줄(@deja → {dejapre}). 사건 전부 + 주요 이야기 마디.
import sys, re, io, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from deja_lib import *
names=['Molak01','Molak02','Molak03','Molak04','Molak05','Molak06','MolakEvents','MolakDuel','MolakOver']
files={n:load(n) for n in names}
SPEC = {   # (파일, 라벨): (need, text)
 ('Molak02','ChSoa'): (None, '젓가락을 쥔 손이 떨린다. 맞은편에 누가 앉을지, 그 칼이 어느 각도로 올지 목이 먼저 안다.'),
 ('Molak02','B5'): (None, '\\"객, 얼굴이 어찌 그리 질렸소. 귀신이라도 본 사람처럼.\\" 귀신은 아니다. 지난 생의 이 밤을 한 번 더 볼 뿐이다.'),
 ('Molak02','B7'): (None, '게시벽 앞 탁자. 지난 생, 이 앞에서 댄 이름이 끝내 당신의 목을 불렀다.'),
 ('Molak02','B8'): (None, '은자 삼백 냥. 지난 생엔 이 종이 한 장 값에 칼 든 자들이 객잔 지붕에 올랐다.'),
 ('Molak02','B11'): ('g_know_jangsell', '\\"지나던 길이오.\\" 첫마디까지 안다. 지난 생, 이 사람이 서른 냥에 당신 방을 팔았다.'),
 ('Molak02','B13'): ('g_soadied', '꽂힌 쌍검을 보는 순간 숨이 멎는다. 지난 생, 지붕 위에 남은 것은 이 검 한 자루뿐이었다.'),
 ('Molak02','B32'): ('g_hajindied', '기운 버선을 쥔 손이 떨린다. 지난 생, 이 버선을 기운 손은 마당에서 식어 갔다.'),
 ('Molak02','ChOld'): (None, '용마루 위의 복면. 몇 번을 겪어도 저 시선 아래에서는 익어 가는 열매가 된다.'),
 ('Molak02','KDebt'): ('g_end_chae', '\\"오늘이오.\\" 지난 생에도 이 말 다음에 쇠고리가 철컥 닫혔다.'),
 ('Molak02','A17'): ('g_know_taxday', '붉은 끈 둘. 지난 생, 이 한낮이 지나고 하진무의 손목이 꺾였다.'),
 ('Molak02','BS1'): ('g_soadied', '담 위에서 술병이 흔들린다. 이 웃음을 안다. 이 웃음이 어느 밤에 끊기는지도 안다.'),
 ('Molak02','BS2'): ('g_soadied', '나란히 걷는 이 저잣거리가, 지난 생에는 그녀와의 마지막 나들이였다.'),
 ('Molak02','BS5'): ('g_soadied', '같은 달이다. 지난 생에도 이 기와 위에서 이 달을 봤고, 며칠 뒤 이 지붕에 그녀의 피가 고였다.'),
 ('Molak02','ChWolf'): ('g_died_wolf', '녹슨 사슬 고리를 보자 손목부터 저린다. 지난 생, 이 초대장 끝에서 사슬에 감겼다.'),
 ('Molak06','Capture'): ('g_end_pobak', '또 이 아침이다. 성문 빗장이 걸리는 소리를 귀가 먼저 기억한다.'),
 ('Molak02','ChShr'): ('g_redverse', '낡은 비석. 피 묻은 숨으로 받은 열두 줄이 혀끝에서 맴돈다.'),
}
entries=[]
for l in files['Molak02']:
    m=re.match(r'^@gosub MolakEvents#(E_\w+) if:',l)
    if m: entries.append(('MolakEvents',m.group(1)))
for lb in ['B5','B7','B8','B11','B13','B20','B32','ChSoa','ChUnju','ChShr','ChHm','ChOld','KDebt','KGrudge','A17','A19','A23','BS1','BS2','BS3','BS4','BS5','BS6','BS7','BR1','ChWolf','BNetHint']:
    entries.append(('Molak02',lb))
entries.append(('Molak06','Capture'))
done=set(); ins={}; spec_n=0; gen_n=0; miss=[]
for f,lb in entries:
    r=find_first_text(files,f,lb)
    if not r: miss.append(lb); continue
    tf,j=r
    if (tf,j) in done or files[tf][j].startswith('{dejapre}'): continue
    done.add((tf,j))
    need,text=SPEC.get((f,lb),(None,None))
    cmd='@deja'
    if need: cmd+=' need:'+need
    if text: cmd+=' text:"'+text+'"'; spec_n+=1
    else: gen_n+=1
    ins.setdefault(tf,[]).append((j,cmd))
for tf,lst in ins.items():
    L=files[tf]
    for j,cmd in sorted(lst,reverse=True):
        L[j]='{dejapre}'+L[j]
        L.insert(j,cmd)
    io.open(os.path.join(C,tf+'.nani'),'w',encoding='utf-8').write('\n'.join(L))
print('specific',spec_n,'generic',gen_n,'miss',len(miss),miss[:20])
