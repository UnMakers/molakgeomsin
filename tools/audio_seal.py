# -*- coding: utf-8 -*-
# 0.4.89 인장 소리 한 벌 섞기: python3 tools/audio_seal.py 재료폴더 출력폴더 [brush=0 thud=1 good=2 ...]
#  재료는 tools/audio_jobs_seal.json 으로 뽑은 mp3 (seal_brush_N, seal_thud_N, seal_t_<종류>_N).
#  한 벌 = 붓이 원을 긋는 소리(0~0.55초) + 돌도장이 내려앉는 쿵(SEAL_HIT) + 인장 빛깔의 여운.
#  SEAL_HIT 은 MolakSfx.SealHit 과 같아야 한다(화면의 인장이 내려앉는 때: MolakSeal 글자 0.45→0.9초 사이).
import os, sys, subprocess, numpy as np
SR = 44100
SEAL_HIT = 0.80
# 종류 → (여운 세기 dB, 여운 길이 초). 종류 이름은 MolakSeal.Kind 를 소문자로.
KINDS = {'good': (-5.0, 3.0), 'bad': (-2.5, 2.8), 'know': (-7.0, 3.2), 'skill': (-5.5, 3.2), 'fork': (-4.0, 3.2)}

def load(f):
    raw = subprocess.run(['ffmpeg', '-v', 'quiet', '-i', f, '-ac', '2', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True).stdout
    return np.frombuffer(raw, dtype=np.float32).reshape(-1, 2).copy()

def save(path, y):
    tmp = path + '.raw'; np.clip(y, -1, 1).astype(np.float32).tofile(tmp)
    subprocess.run(['ffmpeg', '-v', 'quiet', '-y', '-f', 'f32le', '-ar', str(SR), '-ac', '2', '-i', tmp, '-c:a', 'libvorbis', '-q:a', '5', path], check=True); os.remove(tmp)

def onset(x, th=0.08):
    m = np.abs(x).max(axis=1); i = np.where(m > m.max() * th)[0]
    return int(i[0]) if len(i) else 0

def peak(x, db):
    return x * (10 ** (db / 20) / (np.abs(x).max() + 1e-9))

def fade(x, fin, fout):
    x = x.copy(); a = int(fin * SR); b = min(len(x), int(fout * SR))
    if a: x[:a] *= np.linspace(0, 1, a)[:, None]
    if b: x[-b:] *= np.linspace(1, 0, b)[:, None] ** 1.6
    return x

def seg(x, start, dur):
    return x[start:start + int(dur * SR)]

def build(brush, thud, tail, kind):
    gdb, tlen = KINDS[kind]
    L = int((SEAL_HIT + tlen + 0.1) * SR); y = np.zeros((L, 2), np.float32)
    # 붓: 원을 긋는 0.5초에 부풀고, 끝은 찍힐 때까지 옅게 끌려 빈 틈(디지털 무음)이 없게
    b = fade(seg(brush, max(0, onset(brush, 0.05) - int(0.01 * SR)), SEAL_HIT + 0.05), 0.02, 0.35)
    y[:len(b)] += peak(b, -14.0)
    # 쿵: 내려앉는 순간에 머리를 맞춘다(머리 앞 3ms 남김)
    t0 = onset(thud, 0.1); h = fade(seg(thud, max(0, t0 - int(0.003 * SR)), 0.7), 0.0, 0.3)
    p = int(SEAL_HIT * SR) - int(0.003 * SR); y[p:p + len(h)] += peak(h, -2.0)[:L - p]
    # 여운: 쿵 바로 뒤에서 피어오른다
    t1 = onset(tail, 0.06); w = fade(seg(tail, max(0, t1 - int(0.005 * SR)), tlen), 0.01, min(1.4, tlen * 0.45))
    q = int((SEAL_HIT + 0.015) * SR); y[q:q + len(w)] += peak(w, gdb)[:L - q]
    return peak(y, -1.0)

if __name__ == '__main__':
    src, out = sys.argv[1], sys.argv[2]; os.makedirs(out, exist_ok=True)
    # 고른 재료(0.4.89 귀로 고르지 못해 포락선·스펙트럼으로: 붓은 0.1~0.5초에 부풀었다 잦아드는 것, 쿵은 머리가 하나뿐인 것,
    # 여운은 머리가 하나고 고르게 잦아드는 것). 쿵은 「깊은 것+또렷한 것」을 겹칠 수 있다: thud=1+0 (뒤의 것은 -5dB).
    pick = {'brush': '0', 'thud': '1+0', 'good': '1', 'bad': '0', 'know': '0', 'skill': '1', 'fork': '0'}
    for a in sys.argv[3:]:
        k, v = a.split('='); pick[k] = v
    f = lambda n: load(os.path.join(src, n + '.mp3'))
    def layered(base, spec):
        xs = [f('%s_%s' % (base, i)) for i in spec.split('+')]
        y = peak(xs[0][onset(xs[0], 0.1):], 0.0)
        for x in xs[1:]:
            x = peak(x[onset(x, 0.1):], -5.0); n = min(len(y), len(x)); y = y.copy(); y[:n] += x[:n]
        return y
    brush, thud = f('seal_brush_' + pick['brush']), layered('seal_thud', pick['thud'])
    for k in KINDS:
        y = build(brush, thud, f('seal_t_%s_%s' % (k, pick[k])), k)
        save(os.path.join(out, 'seal_%s.ogg' % k), y); print('seal_' + k, round(len(y) / SR, 2), 's')
