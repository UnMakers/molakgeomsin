# -*- coding: utf-8 -*-
"""타이틀 새 시안(2026-11-23, 언메이커: 「지금 진행하는 거 다 하고 타이틀도 지금 컨셉에 맞게 새로 — 시안 여러 개,
엔딩·선택에 따라 타이틀이 변하는 설정은 유지」).

타이틀은 겹쳐 그리는 구조다(MolakTitle.cs): 바탕 한 폭(MolakTitleNight) 위에 코드가 붉은 달·원상·등불·물결을 그리고,
겪은 결말에 따라 실루엣(소아·진백·노인·상복)을 더한다. 그래서 바꾸는 것은 바탕 한 폭과 실루엣 넷이고, 변하는 규칙은 그대로다.
  · 바탕: 지금 바탕의 윤곽(canny)을 붙잡아 구도를 지킨다 — 달 중심(0.494, 0.121) · 물가(0.879) · 검객 자리.
  · 실루엣: 인물 로라로 뒷모습을 그려 누끼 → 흰 가면(코드가 먹빛으로 칠한다).

PC 에서(로라 공장이 '공장 한 바퀴 끝'을 적은 뒤에 돈다 — 그 전에는 5분마다 기다린다):
  C:\\Users\\sjf1t\\wag-train\\venv\\Scripts\\python.exe title_drafts.py
결과: Comfy output\\molak\\title\\<시안>_s<씨앗>.png , output\\molak\\title\\sil_<인물>_s<씨앗>_cut.png
"""
import os, io, json, time, shutil, urllib.request
from PIL import Image

ROOT = r'C:\Users\sjf1t\RuinedSword'
RES = os.path.join(ROOT, 'Assets', 'Resources')
COMFY = r'C:\Users\sjf1t\AppData\Local\Comfy-Desktop\ComfyUI-Shared'
INPUT = os.path.join(COMFY, 'input', 'molak')
OUTPUT = os.path.join(COMFY, 'output')
STATUS = os.path.join(ROOT, 'Build', 'lora', 'factory_status.txt')
TSTATUS = os.path.join(ROOT, 'Build', 'lora', 'title_status.txt')
API = 'http://127.0.0.1:8188'
SEEDS = [8101, 8202, 8303, 8404]

STYLE = 'greyscale, monochrome, spot color, red accents, ink sketch, ink wash painting, traditional media, minimalism, negative space'
NEG = ('lowres, worst quality, low quality, jpeg artifacts, signature, watermark, text, blurry, colorful, '
       'multiple moons, crowd, multiple boys, frame, border')
HERO = '1man, solo, from behind, wuxia swordsman, conical straw hat, sword, standing on water, small figure, wide shot'

# 바탕 시안: (이름, 장면 꼬리표, ControlNet 세기)
BASES = [
    ('still', 'scenery, lake, still water, reflection, mist, vast empty sky, full moon, night', 0.55),
    ('ash', 'scenery, lake, still water, reflection, distant burning mansion on far shore, fire, smoke rising into sky, embers, red flames reflected in water, full moon, night', 0.45),
    ('rain', 'scenery, lake, rain, heavy rain, rain streaks, ripples, mist, dark clouds, full moon behind clouds, night', 0.45),
    ('petals', 'scenery, lake, still water, reflection, falling red petals, wind, mist, vast empty sky, full moon, night', 0.5),
    ('snow', 'scenery, frozen lake, snow, snowing, reflection, mist, vast empty sky, full moon, night', 0.45),
]
# 실루엣: (이름, 로라, 꼬리표)
SILS = [
    ('soa', 'molak_soa_v1', 'molaksoa, 1girl, solo, from behind, full body, standing, black hair, high ponytail, red streak in hair, black robe, red sash, sword on back'),
    ('jinbaek', 'molak_jb_v1', 'molakjb, 1man, solo, from side, full body, standing, very long hair, black hair, grey cloak, sword on back, red cord'),
    ('lawlord', 'molak_old_v1', 'molakold, 1man, solo, from behind, full body, standing, old man, very long hair, white hair, white robe, wide sleeves'),
    ('hajinmu', 'molak_hjm_v1', 'molakhjm, 1man, solo, from side, full body, standing, head down, mourning clothes, white robe, hemp hat'),
]
SIL_STYLE = 'greyscale, monochrome, ink sketch, white background, simple background'


def log (m):
    line = time.strftime('%m-%d %H:%M:%S ') + m
    print(line, flush=True)
    with io.open(TSTATUS, 'a', encoding='utf-8') as f: f.write(line + '\n')

def api (path, body=None):
    data = json.dumps(body).encode('utf-8') if body is not None else None
    req = urllib.request.Request(API + path, data=data, headers={'Content-Type': 'application/json'} if data else {})
    with urllib.request.urlopen(req, timeout=60) as r: t = r.read()
    try: return json.loads(t)
    except Exception: return t

def idle ():
    try:
        q = api('/queue'); return not q.get('queue_running') and not q.get('queue_pending')
    except Exception: return False

def graph (pos, neg, seed, prefix, lora=None, lw=0.85, ref=None, cn=0.5, cut=False):
    g = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': 'animagine-xl-4.0-opt.safetensors'}},
        '5': {'class_type': 'EmptyLatentImage', 'inputs': {'width': 832, 'height': 1216, 'batch_size': 1}},
        '8': {'class_type': 'VAEDecode', 'inputs': {'samples': ['6', 0], 'vae': ['1', 2]}},
        '9': {'class_type': 'SaveImage', 'inputs': {'images': ['8', 0], 'filename_prefix': prefix}},
    }
    model, clip = ['1', 0], ['1', 1]
    if lora:
        g['2'] = {'class_type': 'LoraLoader', 'inputs': {'model': model, 'clip': clip, 'lora_name': lora + '.safetensors', 'strength_model': lw, 'strength_clip': lw}}
        model, clip = ['2', 0], ['2', 1]
    g['3'] = {'class_type': 'CLIPTextEncode', 'inputs': {'text': pos, 'clip': clip}}
    g['4'] = {'class_type': 'CLIPTextEncode', 'inputs': {'text': neg, 'clip': clip}}
    p, n = ['3', 0], ['4', 0]
    if ref:
        g['10'] = {'class_type': 'LoadImage', 'inputs': {'image': ref}}
        g['11'] = {'class_type': 'Canny', 'inputs': {'image': ['10', 0], 'low_threshold': 0.2, 'high_threshold': 0.6}}
        g['12'] = {'class_type': 'ControlNetLoader', 'inputs': {'control_net_name': 'controlnet-union-sdxl.safetensors'}}
        g['13'] = {'class_type': 'SetUnionControlNetType', 'inputs': {'control_net': ['12', 0], 'type': 'canny/lineart/anime_lineart/mlsd'}}
        g['14'] = {'class_type': 'ControlNetApplyAdvanced', 'inputs': {'positive': p, 'negative': n, 'control_net': ['13', 0], 'image': ['11', 0], 'strength': cn, 'start_percent': 0.0, 'end_percent': 0.55, 'vae': ['1', 2]}}
        p, n = ['14', 0], ['14', 1]
    g['6'] = {'class_type': 'KSampler', 'inputs': {'seed': seed, 'steps': 30, 'cfg': 5.0, 'sampler_name': 'euler_ancestral', 'scheduler': 'normal', 'denoise': 1.0, 'model': model, 'positive': p, 'negative': n, 'latent_image': ['5', 0]}}
    if cut:
        g['20'] = {'class_type': 'LoadBackgroundRemovalModel', 'inputs': {'bg_removal_name': 'birefnet.safetensors'}}
        g['21'] = {'class_type': 'RemoveBackground', 'inputs': {'bg_removal_model': ['20', 0], 'image': ['8', 0]}}
        g['22'] = {'class_type': 'InvertMask', 'inputs': {'mask': ['21', 0]}}
        g['23'] = {'class_type': 'JoinImageWithAlpha', 'inputs': {'image': ['8', 0], 'alpha': ['22', 0]}}
        g['24'] = {'class_type': 'SaveImage', 'inputs': {'images': ['23', 0], 'filename_prefix': prefix + '_cut'}}
    return g


def main (now=False):
    while not now:
        try: done = '공장 한 바퀴 끝' in io.open(STATUS, encoding='utf-8').read()
        except Exception: done = False
        if done and idle(): break
        time.sleep(300)
    while not idle(): time.sleep(20)
    os.makedirs(INPUT, exist_ok=True)
    shutil.copy(os.path.join(RES, 'MolakTitleNight.png'), os.path.join(INPUT, 'ref_title_night.png'))
    n = 0
    for name, tags, cn in BASES:
        for s in SEEDS:
            api('/prompt', {'prompt': graph(', '.join([HERO, tags, STYLE, 'masterpiece, best quality']), NEG, s,
                                             'molak/title/%s_s%d' % (name, s), ref='molak/ref_title_night.png', cn=cn)}); n += 1
    for name, lora, tags in SILS:
        for s in SEEDS:
            api('/prompt', {'prompt': graph(', '.join([tags, SIL_STYLE, 'masterpiece, best quality']), NEG + ', scenery', s,
                                             'molak/title/sil_%s_s%d' % (name, s), lora=lora, cut=True)}); n += 1
    log('타이틀 시안 %d장 맡김' % n)
    t0 = time.time()
    while not idle(): time.sleep(20)
    log('타이틀 시안 끝 (%.0f분)' % ((time.time() - t0) / 60))


if __name__ == '__main__':
    import sys
    main(now='--now' in sys.argv)
