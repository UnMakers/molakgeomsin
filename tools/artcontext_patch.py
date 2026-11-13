# 0.4.44: 다른 대본에서 넘어와 장소가 바뀌는데 장면 그림이 없던 첫머리 25곳에 장면을 넣는다(맥락 전환 시 바탕 그림 되돌리기와 짝)
import re
C='canon/'
M={
 ('Molak04','LordAfter'):'valley', ('Molak04','Wf1'):'smithy',
 ('Molak03','Soa1'):'innhall', ('Molak03','Unju0'):'valley', ('Molak03','Shr1'):'shrine', ('Molak03','Hm1'):'pass', ('Molak03','H_Amhyang'):'teahouse',
 ('Molak06','SoaN2'):'market', ('Molak06','SoaN4'):'alley', ('Molak06','SoaN7'):'innroom',
 ('Molak06','Rev1'):'well', ('Molak06','Rev2'):'alley', ('Molak06','Rev3'):'forest', ('Molak06','Rev4'):'herbshop',
 ('Molak05','Yong2'):'yard', ('Molak05','Hajin2'):'yard', ('Molak05','Jang2'):'alley', ('Molak05','Soa3'):'yard',
 ('Molak05','Herb2'):'herbshop', ('Molak05','Chain1'):'crossroad', ('Molak05','Rival2'):'yard', ('Molak05','Soa4'):'innhall',
 ('Molak05','Amh2'):'innroom', ('Molak05','Ochi1'):'innroom', ('Molak05','Old2'):'forest',
}
done=0
for (sc,lb),scene in M.items():
    p=C+sc+'.nani'; L=open(p,encoding='utf-8').read().split('\n')
    hit=[i for i,l in enumerate(L) if l.strip()=='# '+lb]
    if not hit: print('missing',sc,lb); continue
    i=hit[0]
    if L[i+1].startswith('@art scene:'): continue
    L.insert(i+1,'@art scene:'+scene); done+=1
    open(p,'w',encoding='utf-8').write('\n'.join(L))
print('inserted',done)
