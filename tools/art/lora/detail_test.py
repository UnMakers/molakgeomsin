# -*- coding: utf-8 -*-
"""입상을 더 또렷하게 — 두 방법 시험(2026-11-23, 언메이커: 「그 방법으로 한번 해볼래」).
  A 붙잡는 힘 풀기: ControlNet 0.55·0.6까지 → 0.30·0.40까지. 자세는 비슷하게, 옷 주름·머리칼·먹선은 로라가 새로.
  B 세부 다시 그리기: 고른 그림을 1.5배(1248×1824)로 키워 로라 + 윤곽 0.45 로 살짝(denoise 0.38) 다시 그린다 → 832×1216 로 줄여 쓴다.
  A+B: A 결과에 B 를 한 번 더.
대상: 진백 기본·붙잡음, 노인 고요 — 씨앗 둘씩.
공장은 STOP 파일로 하던 인물을 마치고 멈춘 뒤에 돌고, 끝나면 STOP 을 지우고 공장을 다시 띄운다.
결과: Comfy output\\molak\\detail\\<id>_<A|B|AB>_s<씨앗>.png
"""
import os, io, json, time, shutil, subprocess, urllib.request

ROOT = r'C:\Users\sjf1t\RuinedSword'
LORA_DIR = os.path.join(ROOT, 'Build', 'lora')
COMFY = r'C:\Users\sjf1t\AppData\Local\Comfy-Desktop\ComfyUI-Shared'
INPUT = os.path.join(COMFY, 'input', 'molak')
OUTPUT = os.path.join(COMFY, 'output')
STATUS = os.path.join(LORA_DIR, 'factory_status.txt')
API = 'http://127.0.0.1:8188'
STYLE = 'greyscale, monochrome, spot color, red accents, ink sketch, grey background, simple background'
NEG = ('lowres, bad anatomy, bad hands, extra digits, fewer digits, cropped, worst quality, low quality, jpeg artifacts, '
       'signature, watermark, text, blurry, scenery, detailed background, colorful, multiple views')
DETAIL = 'highly detailed, intricate linework, detailed hair, detailed clothes, sharp lines, masterpiece, best quality'
CASES = [  # (id, 로라, 꼬리표, 지금 고른 씨앗)
    ('char_jinbaek', 'molak_jb_v1', 'molakjb, 1man, solo, very long hair, black hair, grey cloak, black clothes, sword on back, red cord, expressionless, looking at viewer', 7404),
    ('char_jinbaek_grab', 'molak_jb_v1', 'molakjb, 1man, solo, very long hair, black hair, grey cloak, black clothes, sword on back, red cord, messy hair, reaching towards viewer, holding red cloth, blood, intense eyes', 7303),
    ('char_oldman_calm', 'molak_old_v1', 'molakold, 1man, solo, old man, very long hair, white hair, white robe, cloth mask, mouth mask, calm, hands in sleeves', 7303),
]
SEEDS = [9101, 9202]


def log (m):
    line = time.strftime('%m-%d %H:%M:%S ') + 'detail ' + m
    print(line, flush=True)
    with io.open(STATUS, 'a', encoding='utf-8') as f: f.write(line + '\n')

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

def wait_idle ():
    while not idle(): time.sleep(15)

def base (lora, pos):
    return {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': 'animagine-xl-4.0-opt.safetensors'}},
        '2': {'class_type': 'LoraLoader', 'inputs': {'model': ['1', 0], 'clip': ['1', 1], 'lora_name': lora + '.safetensors', 'strength_model': 0.9, 'strength_clip': 0.9}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'text': pos, 'clip': ['2', 1]}},
        '4': {'class_type': 'CLIPTextEncode', 'inputs': {'text': NEG, 'clip': ['2', 1]}},
        '12': {'class_type': 'ControlNetLoader', 'inputs': {'control_net_name': 'controlnet-union-sdxl.safetensors'}},
        '13': {'class_type': 'SetUnionControlNetType', 'inputs': {'control_net': ['12', 0], 'type': 'canny/lineart/anime_lineart/mlsd'}},
    }

def cut (g, img, prefix):
    g['20'] = {'class_type': 'LoadBackgroundRemovalModel', 'inputs': {'bg_removal_name': 'birefnet.safetensors'}}
    g['21'] = {'class_type': 'RemoveBackground', 'inputs': {'bg_removal_model': ['20', 0], 'image': img}}
    g['22'] = {'class_type': 'InvertMask', 'inputs': {'mask': ['21', 0]}}
    g['23'] = {'class_type': 'JoinImageWithAlpha', 'inputs': {'image': img, 'alpha': ['22', 0]}}
    g['24'] = {'class_type': 'SaveImage', 'inputs': {'images': ['23', 0], 'filename_prefix': prefix + '_cut'}}
    g['9'] = {'class_type': 'SaveImage', 'inputs': {'images': img, 'filename_prefix': prefix}}

def job_a (vid, lora, tags, seed):
    g = base(lora, ', '.join([tags, STYLE, 'cowboy shot', DETAIL]))
    g['10'] = {'class_type': 'LoadImage', 'inputs': {'image': 'molak/ref_%s.png' % vid}}
    g['11'] = {'class_type': 'Canny', 'inputs': {'image': ['10', 0], 'low_threshold': 0.3, 'high_threshold': 0.7}}
    g['14'] = {'class_type': 'ControlNetApplyAdvanced', 'inputs': {'positive': ['3', 0], 'negative': ['4', 0], 'control_net': ['13', 0], 'image': ['11', 0], 'strength': 0.30, 'start_percent': 0.0, 'end_percent': 0.40, 'vae': ['1', 2]}}
    g['5'] = {'class_type': 'EmptyLatentImage', 'inputs': {'width': 832, 'height': 1216, 'batch_size': 1}}
    g['6'] = {'class_type': 'KSampler', 'inputs': {'seed': seed, 'steps': 30, 'cfg': 5.0, 'sampler_name': 'euler_ancestral', 'scheduler': 'normal', 'denoise': 1.0, 'model': ['2', 0], 'positive': ['14', 0], 'negative': ['14', 1], 'latent_image': ['5', 0]}}
    g['8'] = {'class_type': 'VAEDecode', 'inputs': {'samples': ['6', 0], 'vae': ['1', 2]}}
    cut(g, ['8', 0], 'molak/detail/%s_A_s%d' % (vid, seed))
    return g

def job_b (src_input, vid, lora, tags, seed, tag):
    """src_input: input 폴더 기준 그림(회색 바탕 RGB). 1.5배로 키워 살짝 다시 그린다."""
    g = base(lora, ', '.join([tags, STYLE, 'cowboy shot', DETAIL]))
    g['10'] = {'class_type': 'LoadImage', 'inputs': {'image': src_input}}
    g['15'] = {'class_type': 'ImageScale', 'inputs': {'image': ['10', 0], 'upscale_method': 'lanczos', 'width': 1248, 'height': 1824, 'crop': 'disabled'}}
    g['11'] = {'class_type': 'Canny', 'inputs': {'image': ['15', 0], 'low_threshold': 0.3, 'high_threshold': 0.7}}
    g['14'] = {'class_type': 'ControlNetApplyAdvanced', 'inputs': {'positive': ['3', 0], 'negative': ['4', 0], 'control_net': ['13', 0], 'image': ['11', 0], 'strength': 0.45, 'start_percent': 0.0, 'end_percent': 0.8, 'vae': ['1', 2]}}
    g['16'] = {'class_type': 'VAEEncode', 'inputs': {'pixels': ['15', 0], 'vae': ['1', 2]}}
    g['6'] = {'class_type': 'KSampler', 'inputs': {'seed': seed, 'steps': 26, 'cfg': 5.0, 'sampler_name': 'euler_ancestral', 'scheduler': 'normal', 'denoise': 0.38, 'model': ['2', 0], 'positive': ['14', 0], 'negative': ['14', 1], 'latent_image': ['16', 0]}}
    g['8'] = {'class_type': 'VAEDecode', 'inputs': {'samples': ['6', 0], 'vae': ['1', 2]}}
    g['17'] = {'class_type': 'ImageScale', 'inputs': {'image': ['8', 0], 'upscale_method': 'lanczos', 'width': 832, 'height': 1216, 'crop': 'disabled'}}
    cut(g, ['17', 0], 'molak/detail/%s_%s_s%d' % (vid, tag, seed))
    g['18'] = {'class_type': 'SaveImage', 'inputs': {'images': ['8', 0], 'filename_prefix': 'molak/detail/%s_%s_s%d_big' % (vid, tag, seed)}}
    return g


def post (g):
    try: api('/prompt', {'prompt': g})
    except Exception as e: log('맡김 실패 %s' % e)


def run ():
    open(os.path.join(LORA_DIR, 'STOP'), 'w').close()
    log('STOP 놓음 — 하던 인물을 마치길 기다린다')
    while True:
        st = io.open(STATUS, encoding='utf-8').read()
        if st.rstrip().endswith('STOP 파일 — 멈춘다') or 'STOP 파일 — 멈춘다' in st.splitlines()[-1]: break
        time.sleep(30)
    wait_idle()
    os.makedirs(INPUT, exist_ok=True)
    # B 의 재료: 지금 고른 그림(회색 바탕 원본)을 input 으로
    for vid, lora, tags, pick in CASES:
        src = os.path.join(OUTPUT, 'molak', 'redo', vid, '%s_s%d_00001_.png' % (vid, pick))
        shutil.copy(src, os.path.join(INPUT, 'detail_%s.png' % vid))
    for vid, lora, tags, pick in CASES:
        for s in SEEDS:
            post(job_a(vid, lora, tags, s))
            post(job_b('molak/detail_%s.png' % vid, vid, lora, tags, s, 'B'))
    log('A·B 맡김'); wait_idle()
    for vid, lora, tags, pick in CASES:
        for s in SEEDS:
            a = os.path.join(OUTPUT, 'molak', 'detail', '%s_A_s%d_00001_.png' % (vid, s))
            if os.path.exists(a):
                shutil.copy(a, os.path.join(INPUT, 'detail_%s_A%d.png' % (vid, s)))
                post(job_b('molak/detail_%s_A%d.png' % (vid, s), vid, lora, tags, s, 'AB'))
    log('A+B 맡김'); wait_idle()


def main ():
    try: run()
    except Exception as e: log('시험 오류 %s' % e)
    log('시험 끝 — 공장 다시 띄움')
    try: os.remove(os.path.join(LORA_DIR, 'STOP'))
    except Exception: pass
    subprocess.Popen('cmd /c start "molak_lora_factory" /min "%s"' % os.path.join(LORA_DIR, 'run_factory.bat'), shell=True)


if __name__ == '__main__':
    main()
