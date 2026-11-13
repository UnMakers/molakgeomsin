import json,re,os,glob,collections
CAN='canon'
AUTO=['S_WM','E_ThugsM','E_ThugsF','E_Simma','E_Sutra','E_Dream','E_Funeral','T_Sw1','T_Qi1','T_Qi2','T_Wk1','T_Rest1',
      'E_Swordgrave','E_Childplay','E_Lostblade','E_Echoes','E_Talisman2','E_Simma3','E_Rain2','E_Echoes2','E_Eclipse',
      'E_Caravan','E_Wine','E_Lastletter']
B=json.load(open('choicemap.json',encoding='utf-8'))
files={os.path.basename(f)[:-5]:open(f,encoding='utf-8').read().split('\n') for f in glob.glob(CAN+'/*.nani')}
labels={}
for fb,L in files.items():
    for i,l in enumerate(L):
        if l.strip().startswith('# '): labels[(fb,l.strip()[2:].strip())]=i
def first_fx(fb,t):
    if t.startswith('#'): k=(fb,t[1:])
    else: a,b=t.split('#',1); k=(a,b)
    L=files[k[0]]; i=labels[k]+1; d=collections.Counter()
    while i<len(L):
        s=L[i].strip(); i+=1
        if s.startswith('# '): continue
        if s.startswith('@fx'):
            for m in re.finditer(r'([a-z]+):(-?\d+)',s): d[m.group(1)]+=int(m.group(2))
            break
        if s.startswith('@return') or s.startswith('@goto') or s=='@stop': break
    return d
def narr(t):
    t=t.replace('\\"','"')
    t=re.sub(r'〔[^〕]*〕','',t); t=re.sub(r'\s*\((?:은자|[^)]*냥)[^)]*\)','',t)
    t=t.replace(' — ',' ').strip()
    if not re.search(r'[.!?"」…]$',t): t+='.'
    return t.replace('"','\\"') if False else t
def firsttext(fb,t,depth=0):
    if depth>4: return ''
    if t.startswith('#'): k=(fb,t[1:])
    else: a,b=t.split('#',1); k=(a,b)
    L=files[k[0]]; i=labels[k]+1
    while i<len(L):
        s=L[i].strip(); i+=1
        if not s or s.startswith(';') or s.startswith('# '): continue
        if s.startswith('@'):
            if s.startswith('@return'): return ''
            if s.startswith('@goto') and 'if:' not in s: return firsttext(k[0],s.split()[1],depth+1)
            continue
        return s
    return ''
def overlap(a,b):
    f=lambda s:set(re.sub(r'[^가-힣]','',s)[i:i+2] for i in range(max(0,len(re.sub(r'[^가-힣]','',s))-1)))
    A,Bs=f(a),f(b or '')
    return len(A&Bs)/max(1,len(A))
SC={
 'v1':lambda d: d['sw']*2+d['ng']*2+d['sl'],
 'v2':lambda d: -d['sl']*1.5+d['hp']*0.5-d['curse']*0.3,
 'v3':lambda d: d['hp']*1.5+d['mn']*0.7-d['curse']*0.8,
 'cu':lambda d: -d['curse']*2+d['hp']*0.3,
 'hp':lambda d: d['hp']*2-d['curse']*0.2,
}
edits=[]
for b in B:
    if b['label'] not in AUTO or b['file']!='MolakEvents' and b['label'] not in ('S_WM',): continue
    fb=b['file']; L=files[fb]
    opts=b['opts']
    gated=[(j,o) for j,o in enumerate(opts) if o['cond'] and re.search(r'\b[fg]_',o['cond'])]
    free=[(j,o) for j,o in enumerate(opts) if not o['cond']]
    assert free, b['label']
    fx={j:first_fx(fb,o['goto']) for j,o in enumerate(opts)}
    def best(key):
        return max(free,key=lambda jo:(SC[key](fx[jo[0]]), -jo[0]))[0]
    lab=b['label']
    out=['; 0.4.48 선택지를 걷었다(일상 사건은 글로 흐른다). 앎·물건이 있으면 그것을 쓰고, 다음은 몸(문양·체력), 그다음은 이번 생의 뜻을 따른다.']
    for j,o in gated: out.append(f'@goto #{lab}_au{j} if:{o["cond"]}')
    used=set(j for j,_ in gated)
    for key,cond in (('cu','curse>=60'),('hp','hp<=hpmax/3'),('v1','vow==1'),('v2','vow==2'),('v3','vow==3')):
        j=best(key); used.add(j); out.append(f'@goto #{lab}_au{j} if:{cond}')
    dj=free[0][0]; used.add(dj)
    out.append(f'@goto #{lab}_au{dj}')
    for j in sorted(used):
        out.append(f'# {lab}_au{j}')
        n=narr(opts[j]['text'])
        ft=firsttext(fb,opts[j]['goto'])
        if overlap(n,ft)<0.36: out.append(n)   # 이어지는 첫 줄이 같은 행동을 말하면 겹쳐 쓰지 않는다
        out.append(f'@goto {opts[j]["goto"]}')
    start=b['line']-1; end=b['end']-1   # end = index of first non-choice line
    endcmd=L[end].strip()
    stop_incl = endcmd=='@stop'
    edits.append((fb,start,end+1 if stop_incl else end,out,lab))
# apply bottom-up per file
for fb in set(e[0] for e in edits):
    L=files[fb]
    for e in sorted([e for e in edits if e[0]==fb],key=lambda e:-e[1]):
        _,s,t,out,lab=e
        L[s:t]=out
    open(f'{CAN}/{fb}.nani','w',encoding='utf-8').write('\n'.join(L))
print('auto blocks',len(edits),[e[4] for e in edits])
