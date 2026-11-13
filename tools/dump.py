import json,sys,re
sys.path.insert(0,'tools3')
import importlib.util
spec=importlib.util.spec_from_file_location('cm','tools3/choicemap.py')
B=json.load(open('choicemap.json',encoding='utf-8'))
want=sys.argv[1].split(',')
import glob,os
files={os.path.basename(f)[:-5]:open(f,encoding='utf-8').read().split('\n') for f in glob.glob('canon/*.nani')}
labels={}
for fb,L in files.items():
    for i,l in enumerate(L):
        if l.strip().startswith('# '): labels[(fb,l.strip()[2:].strip())]=i
def body(fb,t,n=6):
    if not t: return []
    if t.startswith('#'): k=(fb,t[1:])
    else: a,b=t.split('#',1); k=(a,b)
    if k not in labels: return ['?']
    L=files[k[0]]; i=labels[k]+1; out=[]
    while i<len(L) and len(out)<n:
        s=L[i].strip(); i+=1
        if s.startswith('# '): continue
        if s: out.append(s[:70])
        if s.startswith('@return') or s.startswith('@goto') and 'if:' not in s: break
    return out
for b in B:
    if b['label'] in want:
        print('==',b['file'],b['line'],b['label'])
        for j,o in enumerate(b['opts']):
            fx=[x for x in body(b['file'],o['goto'],12) if x.startswith('@fx') or x.startswith('@set') or x.startswith('@goto')]
            print('  ',j,o['text'][:45],'| if:',o['cond'],'|',fx[:3])
