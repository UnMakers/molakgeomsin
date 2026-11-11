import io,re,os
C=os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','canon')
def load(n): return io.open(os.path.join(C,n+'.nani'),encoding='utf-8').read().split('\n')
def find_first_text(files, f, label, depth=0):
    L=files[f]
    try: k=L.index('# '+label)
    except ValueError: return None
    for j in range(k+1,len(L)):
        l=L[j]
        if l.startswith('# '): return None
        if l.startswith('@choice') or l.startswith('@stop') or l.startswith('@return'): return None
        m=re.match(r"^@(?:goto|gosub) (\S+)$",l)
        if m and depth<3:
            t=m.group(1)
            f2,lb=(f,t[1:]) if t.startswith('#') else t.split('#')
            return find_first_text(files,f2,lb,depth+1)
        if l.startswith('@goto') or l.startswith('@gosub'):
            if ' if:' in l: continue
            return None
        if l and l[0] not in '@#;': return (f,j)
    return None
