# -*- coding: utf-8 -*-
"""0.4.12 움직이는 연출: 죽음·이별·결전·회귀의 순간에 화면을 움직인다(흔들림·섬광·어둠·불티·핏빛 비). 비무는 칼이 맞부딪는 한 박자를 둔다."""
import io,sys,os
C=sys.argv[1] if len(sys.argv)>1 else 'canon'
EDITS={
 'MolakOver.nani':[
  ('before','검이 스쳤다고 생각했다.','@vfx kind:hit'),
  ('before','쌍검 중 하나가 막고, 하나가 들어온다.','@vfx kind:heavy'),
  ('before','그녀의 외눈이 젖어 있다.','@vfx kind:beat'),
  ('before','계단 셋째 칸에서 무릎이 꺾인다.','@vfx kind:hit'),
  ('after','계단 셋째 칸에서 무릎이 꺾인다.','@vfx kind:embers'),
  ('before','칼이 셋이었는데 들어온 것은 하나다.','@vfx kind:hit'),
  ('before','사내의 칼은 서두르지 않는다.','@vfx kind:hit'),
  ('before','붉은 끈 다섯이 당신을 내려다본다.','@vfx kind:hit'),
  ('before','금 밖으로 걸어 나가는 사람은 하나다.','@vfx kind:hit'),
  ('before','차가운 흙바닥. 하늘이 보인다.','@vfx kind:beat'),
  ('before','일어서려는데 다리에 감각이 없다.','@vfx kind:beat'),
  ('before','열이 머리까지 오른다.','@vfx kind:beat'),
  ('before','굶주림은 검보다 느리지만, 더 집요하다.','@vfx kind:beat'),
  ('after','# Dead','@vfx kind:beat'),
  ('beforeall','시간이 거꾸로 흐른다. 뽑히지 않은 칼이','@vfx kind:rewind'),
 ],
 'Molak06.nani':[
  ('before','창호지에 횃불이 번진다.','@vfx kind:embers'),
  ('before','쇳소리가 아니라 살 소리가 난다.','@vfx kind:hit'),
  ('before','"……등 뒤를, 맡긴다고, 했잖아."','@vfx kind:beat'),
  ('after','@art scene:bloodroof','@vfx kind:bloodrain'),
  ('before','맨 끝에 한 글자. 蓮.','@vfx kind:beat'),
  ('before','가슴의 상처 결을 안다.','@vfx kind:beat'),
  ('before','뒷문. 골목. 등 뒤로 객잔이 불탄다.','@vfx kind:embers'),
  ('before','열넷 중 열하나가 계단에 눕고','@vfx kind:heavy'),
 ],
 'Molak02.nani':[
  ('before','맨 앞의 칼이, 당신 앞을 막아선 노인의 가슴에 들어간다.','@vfx kind:hit'),
  ('before','"……셈이…… 이제 맞소.','@vfx kind:beat'),
 ],
 'MolakEvents.nani':[
  ('before','마당에 내려놓았을 때 할멈은 숨을 쉬지 않는다.','@vfx kind:beat'),
  ('before','불은 새벽까지 탄다. 잿더미 앞에서','@vfx kind:embers'),
 ],
 'Molak04.nani':[
  ('before','문양이 갈라진다. 열매가 아니라, 뿌리째.','@vfx kind:heavy'),
  ('before','아침이 온다. 되감기지 않는 아침이.','@vfx kind:dawn'),
  ('after','아침이 온다. 되감기지 않는 아침이.','@vfx kind:petals'),
  ('before','복수는 끝났다.<br>이야기는, 끝나지 않았다.','@vfx kind:dawn'),
 ],
}
for fn,eds in EDITS.items():
    P=os.path.join(C,fn); L=io.open(P,encoding='utf-8').read().split('\n')
    for mode,anchor,ins in eds:
        idx=[i for i,l in enumerate(L) if (l.strip()==anchor if anchor.startswith('#') or anchor.startswith('@') else l.startswith(anchor))]
        assert idx,(fn,anchor)
        if mode!='beforeall': idx=idx[:1]
        for i in reversed(idx):
            if mode in('before','beforeall'):
                if i>0 and L[i-1].strip()==ins: continue
                L.insert(i,ins)
            else:
                if i+1<len(L) and L[i+1].strip()==ins: continue
                L.insert(i+1,ins)
    io.open(P,'w',encoding='utf-8').write('\n'.join(L))
# 비무: 초식을 고르면 칼이 맞부딪는 한 박자 → 결과
P=os.path.join(C,'MolakDuel.nani'); L=io.open(P,encoding='utf-8').read().split('\n'); out=[]
for i,l in enumerate(L):
    if l.startswith('@duel style:') and not (out and out[-1].startswith('@wait')):
        heavy=l.split(':')[1].strip() in ('killer','curse2')
        out+=['@vfx kind:%s'%('heavy' if heavy else 'clash'),'@wait 0.35']
    out.append(l)
io.open(P,'w',encoding='utf-8').write('\n'.join(out))
print('ok')
