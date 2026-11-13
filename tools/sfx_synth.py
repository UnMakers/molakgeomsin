# 0.4.32 효과음 합성(외부 생성 없이): 칼 쇳소리, 인장, 심장, 사술의 숨
import numpy as np, wave
SR = 44100
rng = np.random.default_rng(7)
def save(name, x):
    x = x / (np.abs(x).max() + 1e-9) * 0.89
    fade = int(SR*0.01); x[-fade:] *= np.linspace(1, 0, fade)
    with wave.open(name + '.wav', 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype(np.int16).tobytes())
def t_(d): return np.arange(int(SR*d)) / SR
def lp(x, a):  # 한 극 저역 통과
    y = np.zeros_like(x); acc = 0.0
    for i, v in enumerate(x): acc += a*(v-acc); y[i] = acc
    return y
# 1) 칼끼리 맞는 쇳소리: 비조화 배음 + 짧은 충격 잡음
t = t_(1.4)
parts = [(1210,1.0,2.6),(2633,0.7,3.4),(3987,0.55,4.5),(5521,0.4,6.0),(7340,0.3,8.0),(1870,0.5,3.0),(9120,0.18,11)]
ring = sum(a*np.sin(2*np.pi*f*t*(1+0.0008*np.sin(2*np.pi*6*t)) + rng.uniform(0,6.28))*np.exp(-d*t) for f,a,d in parts)
hit = rng.normal(0,1,len(t))*np.exp(-t*90)
hit = hit - lp(hit, 0.15)   # 높은 쪽만
clash = ring*0.8 + hit*1.6
save('sword_clash', clash)
# 2) 인장: 낮은 둔탁음(음높이가 떨어지는 사인) + 종이 바스락
t = t_(0.7)
f = 95*np.exp(-t*6)+55
thud = np.sin(2*np.pi*np.cumsum(f)/SR)*np.exp(-t*14)
paper = rng.normal(0,1,len(t)); paper = (paper - lp(paper,0.3))*np.exp(-t*35)*0.35
knock = lp(rng.normal(0,1,len(t)),0.08)*np.exp(-t*60)*2.5
save('seal_stamp', thud*1.2 + paper + knock)
# 3) 심장: 둥-둥 두 번
t = t_(1.9)
def beat(t0, amp):
    tt = np.clip(t - t0, 0, None); on = (t >= t0)
    f = 62*np.exp(-tt*9)+38
    return on*amp*np.sin(2*np.pi*np.cumsum(f*on)/SR)*np.exp(-tt*11)
heart = beat(0.05,1.0) + beat(0.33,0.75) + beat(1.05,0.6) + beat(1.33,0.45)
heart = lp(heart, 0.05)
save('heartbeat', heart)
# 4) 사술의 숨: 걸러진 잡음이 부풀었다 가라앉는다 + 낮은 맥놀이
t = t_(2.6)
env = np.sin(np.pi*np.clip(t/2.6,0,1))**2
n = rng.normal(0,1,len(t))
band = lp(n, 0.06) - lp(n, 0.012)
drone = 0.25*(np.sin(2*np.pi*73*t)+np.sin(2*np.pi*77.5*t))
whis = (n - lp(n, 0.5))*0.12*np.sin(np.pi*np.clip((t-0.3)/1.8,0,1))**2
save('curse_breath', (band*2.2 + drone + whis)*env)
print('ok')
