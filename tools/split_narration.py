import re,glob,sys
LONG=44      # 이보다 긴 서술 줄만(휴대폰 한 줄 약 22자 — 두 줄을 넘으면)
MAXC=40      # 한 줄 목표 상한
MINC=24      # 이보다 짧으면 다음 문장과 붙인다
TAG=re.compile(r'<[^>]+>')
def plain(s): return TAG.sub('',s)
def sentences(s):
    # 따옴표·꺾쇠 안은 끊지 않는다. 문장 끝: . ? ! … 다음 공백
    out=[];cur='';depth=0;intag=False;i=0
    while i<len(s):
        c=s[i];cur+=c
        if c=='<':intag=True
        elif c=='>':intag=False
        elif not intag and c in '"“「\'':
            if c in '“「':depth+=1
            elif c in '”」':depth=max(0,depth-1)
            else: depth=1-depth if c=='"' else depth
        if not intag and depth==0 and c in '.?!…' and i+1<len(s) and s[i+1]==' ' and (i+2>=len(s) or s[i+2] not in '.…'):
            out.append(cur.strip());cur=''
        i+=1
    if cur.strip():out.append(cur.strip())
    return out
def balanced(ch):
    for t in ['color','b','i','align','size','u','mark','s']:
        if len(re.findall('<'+t+r'[=>]',ch))!=len(re.findall('</'+t+'>',ch)):return False
    return True
def split_line(line):
    s=line.rstrip('\n')
    st=s.lstrip()
    if not st or st[0] in '@#;' or st[0] in '"“「\'' or '[' in st or st.startswith('<align'): return None
    if len(plain(st))<=LONG: return None
    sents=sentences(st)
    if len(sents)<2: return None
    chunks=[];cur=''
    for x in sents:
        if not cur: cur=x; continue
        if len(plain(cur))>=MINC and len(plain(cur))+1+len(plain(x))>MAXC: chunks.append(cur);cur=x
        else: cur+=' '+x
    if cur: chunks.append(cur)
    if len(chunks)<2: return None
    # 마지막 조각이 너무 짧으면 앞에 붙인다
    if len(plain(chunks[-1]))<12: chunks[-2]+=' '+chunks[-1]; chunks.pop()
    if len(chunks)<2 or not all(balanced(c) for c in chunks): return None
    # 따옴표(대사)로 시작하는 조각이 생기면 대사 줄이 되므로 그대로 둔다
    if any(c[0] in '"“「\'' for c in chunks[1:]): return None
    ind=s[:len(s)-len(st)]
    return [ind+c for c in chunks]
if __name__=='__main__':
    apply='--apply' in sys.argv
    tot=0;ex=[]
    for f in sorted(glob.glob(sys.argv[1]+'/*.nani')):
        L=open(f,encoding='utf-8').read().split('\n');out=[];n=0
        for l in L:
            r=split_line(l)
            if r: out.extend(r); n+=1
            else: out.append(l)
            if r and len(ex)<6: ex.append((f,l,r))
        tot+=n
        if apply and n: open(f,'w',encoding='utf-8').write('\n'.join(out))
        print(f.split('/')[-1],n)
    print('total',tot)
    for f,l,r in ex: print('\n'+l+'\n  =>\n  '+'\n  '.join(r))
