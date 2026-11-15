# -*- coding: utf-8 -*-
# 뽑은 소리 다듬기: python3 audio_post.py sfx|amb|bgm OUTDIR file.mp3 ...
import subprocess, numpy as np, os, sys
SR = 44100
def load(f):
    raw = subprocess.run(['ffmpeg','-v','quiet','-i',f,'-ac','2','-ar',str(SR),'-f','f32le','-'],capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1,2).copy()
def save(path, y, q):
    tmp = path + '.raw'; np.clip(y,-1,1).astype(np.float32).tofile(tmp)
    subprocess.run(['ffmpeg','-v','quiet','-y','-f','f32le','-ar',str(SR),'-ac','2','-i',tmp,'-c:a','libvorbis','-q:a',str(q),path], check=True); os.remove(tmp)
def sfx(x, ring=False):
    mono = np.abs(x).max(axis=1); idx = np.where(mono > mono.max()*(0.004 if ring else 0.03))[0]
    a = max(0, idx[0]-int(0.01*SR)); b = min(len(x), idx[-1]+int((0.2 if ring else 0.35)*SR))
    y = x[a:b].copy(); fi = int(0.005*SR); y[:fi] *= np.linspace(0,1,fi)[:,None]
    fo = min(len(y)//3, int((0.6 if ring else 0.25)*SR)); y[-fo:] *= np.linspace(1,0,fo)[:,None]
    return y*(10**(-1.5/20)/(np.abs(y).max()+1e-9))
def amb(x):
    N = 2*SR
    if len(x) > 3*N:
        f = np.linspace(0,1,N)[:,None]; y = x[:-N].copy(); y[:N] = x[:N]*f + x[-N:]*(1-f)
    else: y = x
    y = y*(10**(-24/20)/(np.sqrt(np.mean(y**2))+1e-9)); pk = np.abs(y).max()
    return y*(0.89/pk) if pk > 0.89 else y
def bgm(x):
    # 앞 무음 자르기, 세기 맞추기(RMS -18dBFS, 봉우리 -1dB), 앞 0.5초·끝 4초 서서히
    mono = np.abs(x).max(axis=1); idx = np.where(mono > mono.max()*0.01)[0]
    y = x[max(0, idx[0]-int(0.05*SR)): idx[-1]+int(0.5*SR)].copy()
    y = y*(10**(-18/20)/(np.sqrt(np.mean(y**2))+1e-9)); pk = np.abs(y).max()
    if pk > 0.89: y = np.tanh(y/0.89*1.0)*0.89   # 부드럽게 눌러 찌그러짐 없이
    fi = int(0.5*SR); y[:fi] *= np.linspace(0,1,fi)[:,None]
    fo = min(len(y)//4, int(4*SR)); y[-fo:] *= np.linspace(1,0,fo)[:,None]
    return y
mode, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
RING = {'bell_rise','chapter_pluck','true_bell','win_gong','death_drum','lose_drum','sword_clash_heavy','momentum_drum'}
for f in sys.argv[3:]:
    name = os.path.splitext(os.path.basename(f))[0]; x = load(f)
    if len(x) == 0: print('EMPTY', name); continue
    y = amb(x) if mode == 'amb' else bgm(x) if mode == 'bgm' else sfx(x, name in RING)
    save(os.path.join(out, name + '.ogg'), y, 4 if mode == 'bgm' else 5)
    print(name, round(len(y)/SR, 1), 's')
