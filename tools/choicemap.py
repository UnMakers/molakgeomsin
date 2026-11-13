import re,glob,collections,json,sys,os
CAN=os.environ.get('CAN','canon')
files={f:open(f,encoding='utf-8').read().split('\n') for f in glob.glob(CAN+'/*.nani')}
def base(f): return os.path.basename(f)[:-5]
# label index
labels={}
for f,L in files.items():
    for i,l in enumerate(L):
        s=l.strip()
        if s.startswith('# '): labels[(base(f),s[2:].strip())]=i
# references of vars in conditions anywhere
condrefs=collections.Counter()
for f,L in files.items():
    for l in L:
        for m in re.finditer(r'(?:if:|\{)([^\s]+)',l):
            for v in re.findall(r'\b([a-z][a-z0-9_]*)\b',m.group(1)): condrefs[v]+=1
def parse_choice(s):
    t=re.search(r'@choice\s+"((?:[^"\\]|\\.)*)"',s)
    g=re.search(r'goto:([^\s]+)',s)
    c=re.search(r'if:([^\s]+)',s)
    return dict(text=t.group(1) if t else '', goto=g.group(1) if g else None, cond=c.group(1) if c else None)
def resolve(fb,target):
    if target is None: return None
    if target.startswith('#'): return (fb,target[1:])
    if '#' in target: a,b=target.split('#',1); return (a,b)
    return (target,None)
def walk(key,depth=0,seen=None):
    """collect commands along path from label until @return/@stop or uncond goto (followed)."""
    if seen is None: seen=set()
    out=[]
    if key is None or key in seen or depth>6: return out
    seen.add(key)
    fb,lab=key
    f=CAN+'/'+fb+'.nani'
    if f not in files or (fb,lab) not in labels: return out
    L=files[f]; i=labels[(fb,lab)]+1
    while i<len(L):
        s=L[i].strip(); i+=1
        if not s or s.startswith(';'): continue
        if s.startswith('# '):  # fallthrough into next label
            continue
        out.append(s)
        if s.startswith('@return') or s=='@stop' or s.startswith('@choice'):
            if s.startswith('@choice'): 
                # nested choice: stop scanning
                pass
            break
        if s.startswith('@goto') or s.startswith('@gosub'):
            m=re.match(r'@go(?:to|sub)\s+([^\s]+)',s)
            tgt=resolve(fb,m.group(1)) if m else None
            cond='if:' in s
            out+=walk(tgt,depth+1,seen)
            if s.startswith('@goto') and not cond: break
    return out
def classify(cmds):
    tags=set()
    for s in cmds:
        if s.startswith('@risk'): tags.add('risk')
        if s.startswith('@mark'): tags.add('mark')
        if 'MolakDuel' in s or 'dkind' in s: tags.add('duel')
        if s.startswith('@choice'): tags.add('nested')
        if re.match(r'@(end|ending)',s,re.I) or 'Ending' in s: tags.add('end')
        for v in re.findall(r'@set\s+([a-z][a-z0-9_]*)\s*=',s):
            if re.match(r'(f_|g_|b_)',v) and condrefs[v]>0: tags.add('flag')
            if v in ('debt','vow','grudge','chain','rank'): tags.add('flag')
        if s.startswith('@fx'): tags.add('fx')
        if not s.startswith('@'): tags.add('text')
    return tags
blocks=[]
for f,L in sorted(files.items()):
    fb=base(f); cur=None; lab=None
    for i,l in enumerate(L):
        s=l.strip()
        if s.startswith('# '): lab=s[2:].strip()
        if s.startswith('@choice'):
            if cur is None: cur=dict(file=fb,line=i+1,label=lab,opts=[])
            cur['opts'].append(parse_choice(s))
        elif cur is not None and s and not s.startswith(';'):
            cur['end']=i+1; cur['endcmd']=s; blocks.append(cur); cur=None
for b in blocks:
    for o in b['opts']:
        o['tags']=sorted(classify(walk(resolve(b['file'],o['goto']))))
    alltags=set(t for o in b['opts'] for t in o['tags'])
    if alltags & {'risk','duel','mark','end','nested','flag'}: b['kind']='keep'
    else: b['kind']='auto'
    b['alltags']=sorted(alltags)
json.dump(blocks,open('choicemap.json','w',encoding='utf-8'),ensure_ascii=False,indent=0)
c=collections.Counter((b['file']=='MolakEvents',b['kind']) for b in blocks)
print(c)
