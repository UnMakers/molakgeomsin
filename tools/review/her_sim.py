# 히로인 사건 연달아 나오기 어림(0.4.98) — 사건 표를 읽어 생 1500번을 돌려 옛/새 추첨을 견준다. 조건은 확률로 어림(cond열림).
#   python3 tools/review/her_sim.py
import re, random, sys
src=open('unity-src/MolakEventDefs.cs',encoding='utf-8').read()
tbl=src[src.index('Def[] Table'):src.index('Flav[] Morning')]
defs=[]
for m in re.finditer(r'new Def\("(\w+)",\s*(\d+),\s*(\d+),\s*\'(\w)\',\s*(null|"[^"]*"),\s*(null|"[^"]*"),\s*(\d+),\s*(\d+)\)',tbl):
    i,mn,mx,sl,cond,aft,gap,w=m.groups()
    defs.append(dict(id=i,mn=int(mn),mx=int(mx),sl=sl,after=None if aft=='null' else aft.strip('"'),gap=int(gap),w=int(w),cond=None if cond=='null' else cond.strip('"')))
KEYS=["Soa","Amh","Cm","Cham","TempleBell","JinSister"]
her=lambda i: any(k in i for k in KEYS)
print(len(defs),'defs', sum(her(d['id']) for d in defs),'heroine')
def life(new, pcond):
    seen={}; dry=0; last=False; lastday=-9; picks=her_n=back=0; sameday=0
    avail={d['id']: (random.random()<pcond if d['cond'] else True) for d in defs}   # 이번 생에 조건이 열리는 사건(어림)
    for day in range(1,61):
        hd=0
        for slot,ch in (('d',85),('d',40),('n',55)):
            c=[d for d in defs if d['id'] not in seen and avail[d['id']] and day>=d['mn'] and (d['mx']==0 or day<=d['mx']) and (d['sl']=='a' or d['sl']==slot)
               and (not d['after'] or (d['after'] in seen and day-seen[d['after']]>=d['gap']))]
            if not c: continue
            just= last and day-lastday<=1
            if new and just and any(her(d['id']) for d in c) and random.random()>0.2:
                c=[d for d in c if not her(d['id'])]
                if not c: continue
            eff=min(100,ch+20*dry)
            if random.randint(1,100)>eff: dry+=1; continue
            tot=sum(d['w'] for d in c); r=random.uniform(0,tot)
            for d in c:
                r-=d['w']
                if r<0: break
            seen[d['id']]=day; dry=0; picks+=1
            h=her(d['id'])
            if h:
                her_n+=1; hd+=1
                if just: back+=1
                lastday=day
            last=h
        if hd>=2: sameday+=1
    return picks,her_n,back,sameday
for pc in (0.5,0.8):
  for new in (False,True):
    random.seed(1); T=[0,0,0,0]
    for _ in range(1500):
        r=life(new,pc); T=[a+b for a,b in zip(T,r)]
    n=1500
    print('cond열림 %.1f %s: 생당 사건 %.1f · 히로인 %.1f · 히로인 바로 뒤 히로인 %.2f회 · 하루 두 번 이상 히로인인 날 %.2f' % (pc,'새' if new else '옛', T[0]/n, T[1]/n, T[2]/n, T[3]/n))
