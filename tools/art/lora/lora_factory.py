# -*- coding: utf-8 -*-
"""몰락검신 인물 로라 공장(2026-11-23).

언메이커: 「몰락검신의 이미지에 대해 완벽히 할 수 있는 한 최대한 학습시켜. 학습이 쉬지 않게 돌아가도록.」
소아 로라(molak_soa_v1)처럼 인물마다 로라를 굽고, 그 로라 + ControlNet(지금 입상의 윤곽)으로
같은 각도·같은 자세의 입상을 다시 그린다. 끝난 인물은 건너뛰므로 몇 번을 다시 띄워도 이어서 돈다.

PC(wag-train venv)에서 돈다:
  C:\\Users\\sjf1t\\wag-train\\venv\\Scripts\\python.exe lora_factory.py
  멈추기: Build\\lora\\STOP 파일을 만든다(인물 하나를 마친 뒤 멈춘다).
  상태: Build\\lora\\factory_status.txt

순서(인물마다):
  1) 학습 자료: 지금 입상(투명 배경)을 회색(190) 위에 펴서 전신·상반신·얼굴 세 장, 꼬리표(trigger + 생김새 + 화풍)
  2) Comfy 비우기(/free) — 6GB 카드라 학습과 그림을 같이 못 돌린다
  3) sdxl_train_network.py — 소아와 같은 값(dim16·alpha8·unet만·fp8·AdamW8bit·cosine), 걸음 수만 인물 무게에 맞춘다
  4) 표정·자세마다 씨앗 4개: 로라 + union ControlNet(canny 0.55, 0.6까지) → birefnet 오려 내기
"""
import io, os, sys, json, time, subprocess, urllib.request, traceback
from PIL import Image
import numpy as np

ROOT = r'C:\Users\sjf1t\RuinedSword'
ART = os.path.join(ROOT, 'Assets', 'Resources', 'MolakArt')
LORA_DIR = os.path.join(ROOT, 'Build', 'lora')
COMFY = r'C:\Users\sjf1t\AppData\Local\Comfy-Desktop\ComfyUI-Shared'
CKPT = os.path.join(COMFY, 'models', 'checkpoints', 'animagine-xl-4.0-opt.safetensors')
LORAS = os.path.join(COMFY, 'models', 'loras')
INPUT = os.path.join(COMFY, 'input', 'molak')
OUTPUT = os.path.join(COMFY, 'output')
SD = r'C:\Users\sjf1t\wag-train\sd-scripts'
PY = r'C:\Users\sjf1t\wag-train\venv\Scripts\python.exe'
API = 'http://127.0.0.1:8188'
STATUS = os.path.join(LORA_DIR, 'factory_status.txt')
GRAY = (190, 190, 190)
SEEDS = [7101, 7202, 7303, 7404]

STYLE = 'greyscale, monochrome, spot color, red accents, ink sketch, grey background, simple background'
NEG = ('lowres, bad anatomy, bad hands, extra digits, fewer digits, cropped, worst quality, low quality, jpeg artifacts, '
       'signature, watermark, text, blurry, scenery, detailed background, colorful, multiple views')

# 생김새 표. good=True 면 붉은 눈을 막는다(선한 쪽 민간인·아군 — STORY_SESSION 붉은 눈 기준).
# v: {입상 id: (꼬리표, 학습에 쓰나, 얼굴 자르기 되나)}
#   학습에 안 쓰는 것: 지금 그림의 생김새가 다른 사람처럼 어긋난 것(로라로 다시 그려 맞춘다).
# steps: 학습 걸음 목표. lw: 그릴 때 로라 세기(기본 0.9).
def V (tags, train=True, face=True, lw=None, shot='cowboy shot'):
    return {'tags': tags, 'train': train, 'face': face, 'lw': lw, 'shot': shot}

CAST = [
    # ── 주요 인물 ──
    ('amh', dict(good=False, steps=1300, base='1girl, solo, black hair, long hair, hair bun, red hair ornament, hairpin, red lips, forehead mark, grey robe, red sash', v={
        'char_amhyang': V('seductive smile, hand on own cheek, looking at viewer'),
        'char_amhyang_pain': V('pained expression, leaning forward, blood, poisoned, sweat', face=False),
        'char_amhyang_soft': V('gentle smile, hands together, standing straight')})),
    ('cm', dict(good=True, steps=1300, base='1girl, solo, white hood, nun, red prayer beads, white robe, holding axe, axe on shoulder', v={
        'char_cheongmyo': V('calm, looking at viewer'),
        'char_cheongmyo_robe': V('gentle smile, head tilt'),
        'char_cheongmyo_smile': V('smile, closed mouth'),
        'char_cheongmyo_tears': V('tears, sad smile')})),
    ('cham', dict(good=False, steps=1200, base='1girl, solo, black hair, short hair, grey robe, red sash', v={
        'char_chamsae': V('smirk, one eye closed, wink, hand near face'),
        'char_chamsae_serious': V('serious, looking down, shadow over eyes')})),
    ('hjm', dict(good=True, steps=1300, base='1man, solo, middle-aged man, grey hair, tied hair, goatee, grey robe', v={
        'char_hajinmu': V('serious, holding abacus, looking at viewer'),
        'char_hajinmu_collar': V('nervous, sweat, hand on own collar, white robe', train=False),
        'char_hajinmu_grave': V('grave expression, frown, holding abacus'),
        'char_hajinmu_kneel': V('kneeling, hands clasped, begging, crying', train=False, face=False, shot='full body'),
        'char_hajinmu_laugh': V('laughing, open mouth, holding abacus'),
        'char_hajinmu_tears': V('tears, crying, holding abacus')})),
    ('jb', dict(good=False, steps=1300, base='1man, solo, very long hair, black hair, grey cloak, black clothes, sword on back, red cord', v={
        'char_jinbaek': V('expressionless, looking at viewer'),
        'char_jinbaek_grab': V('messy hair, reaching towards viewer, holding red cloth, blood, intense eyes', face=False),
        'char_jinbaek_smile': V('slight smile'),
        'char_jinbaek_sorrow': V('sad, looking down, grey robe')})),
    ('old', dict(good=True, steps=1300, base='1man, solo, old man, very long hair, white hair, white robe', v={
        'char_oldman': V('cloth mask, mouth mask, raising arm, red hand, glowing hand'),
        'char_oldman_calm': V('cloth mask, mouth mask, calm, hands in sleeves'),
        'char_oldman_face': V('calm face, half-closed eyes'),
        'char_oldman_face_sorrow': V('sad, looking down'),
        'char_oldman_young': V('young man, black hair, long hair, holding red cord', train=False, lw=0.5)})),
    ('lord', dict(good=False, steps=1300, base='1man, solo, long white hair, black robe, shadowed face, red eyes', v={
        'char_lord': V('red halo, halo behind head, hands clasped, red orb'),
        'char_lord_kneel': V('kneeling, hand on own face, blood', face=False, shot='full body'),
        'char_lord_rage': V('angry, clenched fist'),
        'char_lord_wrath': V('red cape, glowing eyes, arms spread')})),
    ('gyeol', dict(good=True, steps=1200, base='1man, solo, black hair, high ponytail, grey shirt, black vest, holding sword, red tassel', v={
        'char_gyeol': V('calm, looking at viewer'),
        'char_gyeol_angry': V('angry, frown'),
        'char_gyeol_pain': V('pained expression, sweat')})),
    # ── 표정이 여럿인 조연 ──
    ('granny', dict(good=True, steps=1200, base='1girl, solo, old woman, grey hair, wrinkles, grey robe, basket on back, holding basket, red berries', v={
        'char_granny': V('smile'),
        'char_granny_grave': V('serious, frown'),
        'char_granny_kind': V('gentle smile'),
        'char_granny_scold': V('angry, shouting, grin'),
        'char_granny_tremble': V('trembling, frightened, white robe, hair bun', train=False)})),
    ('gwaksan', dict(good=True, steps=1100, base='1man, solo, muscular, light hair, messy hair, red headband, torn robe, holding spear, red tassel', v={
        'char_gwaksan': V('confident, looking at viewer'),
        'char_gwaksan_bow': V('head down, arm over face, covering face', face=False),
        'char_gwaksan_grief': V('sad, looking down')})),
    ('jang', dict(good=True, steps=1100, base='1man, solo, long black hair, thin, oversized robe, hands together', v={
        'char_jang': V('smirk'),
        'char_jang_fear': V('frightened, hands on own face, hair bun, mustache, sweat', train=False),
        'char_jang_uneasy': V('uneasy smile, sweat')})),
    ('yonggae', dict(good=True, steps=1000, base='1man, solo, messy black hair, ponytail, grey changshan, red sash across chest, red wristband, finger on own cheek', v={
        'char_yonggae': V('smirk'),
        'char_yonggae_grave': V('serious')})),
    ('hochi', dict(good=False, steps=1000, base='1man, solo, muscular, red headband, open vest', v={
        'char_hochi': V('grin, holding wooden club, club on shoulder'),
        'char_hochi_kneel': V('kneeling, on one knee, head down, bleeding, buzz cut', face=False, shot='full body')})),
    ('hyeok', dict(good=True, steps=1000, base='1boy, solo, black hair, ponytail, dark grey changshan, red sash, bandaged hand', v={
        'char_hyeok': V('smile'),
        'char_hyeok_grim': V('expressionless, serious')})),
]
SINGLES = {
    'abbot': (True, '1man, solo, old man, bald, monk, grey robe, hands together, calm'),
    'baekri': (False, '1man, solo, black hair, topknot, mustache, black robe, red belt, holding dagger, stern'),
    'beggarmonk': (True, '1man, solo, bald, monk, smile, ragged grey robe, prayer beads, holding bowl, gourd'),
    'cheolsan': (True, '1man, solo, muscular, red headband, beard, torn clothes, broadsword, sword planted, stern'),
    'cultist': (False, '1boy, solo, bald, circlet, white robe, red stripe, bandaged arm, holding stick'),
    'ferryman': (True, '1man, solo, old man, conical hat, straw hat, grey cloak, red sash, holding long pole'),
    'ggoji': (True, '1boy, solo, child, messy black hair, smile, white shirt, holding red string'),
    'inspector': (True, '1man, solo, bald, topknot, goatee, white robe, hands together, stern'),
    'ironmaster': (True, '1man, solo, old man, grey hair, topknot, beard, grey robe, hands on sword hilt, sword planted'),
    'jeomsoi': (True, '1boy, solo, short hair, headband, apron, white clothes, holding tray, teapot, smile'),
    'kyona': (False, '1man, solo, messy black hair, muscular, fur vest, scar over eye, blood, grin, head down'),
    'lawlord': (False, '1man, solo, old man, very long beard, white beard, topknot, black and white robe, holding red heart'),
    'machung': (True, '1man, solo, messy black hair, grey robe, red belt, slouching'),
    'mourner': (True, '1man, solo, white headband, white mourning clothes, hands together, red cord'),
    'mugo': (True, '1man, solo, old man, beard, topknot, apron, arms crossed, red cloth'),
    'namgung': (True, '1man, solo, black hair, hair bun, white robe, red armband, holding sword, grey eyes'),
    'ochi': (True, '1man, solo, black hair, topknot, white clothes, red cord, slim'),
    'odam': (True, '1boy, solo, black hair, ponytail, grey shirt, red sash, red wristband, holding broom, determined'),
    'panju': (True, '1man, solo, long black hair, stern, dark robe, red sash, white robe'),
    'porter': (True, '1man, solo, topknot, mustache, beard, muscular, red strap across chest, angry'),
    'prophet': (False, '1man, solo, black hair, topknot, mustache, dark robe, red tassels, broad shoulders, smile'),
    'scout': (False, 'solo, hood, black hood, face mask, red scarf, black bodysuit, holding knife'),
    'storyteller': (True, '1man, solo, old man, grey hair, topknot, smile, white robe, black inner robe'),
    'taxman': (True, '1man, solo, bald, topknot, goatee, arms crossed, grey changshan, red sash'),
    'ungeom': (True, '1girl, solo, very long black hair, white robe, red hair ornament, small hat, smile, hands together'),
    'wangsam': (True, '1man, solo, fat, mustache, topknot, smile, white robe, rope belt, hands on belly'),
    'wolf': (False, '1man, solo, dark skin, beard, black hair, fur cloak, chains, red cloth, smirk, muscular'),
    'yeomga': (True, '1man, solo, old man, beard, topknot, white robe, holding staff, red tassel'),
}
for k, (good, base) in SINGLES.items():
    CAST.append((k, dict(good=good, steps=800, base=base, v={'char_' + k: V('')})))


def log (msg):
    line = time.strftime('%m-%d %H:%M:%S ') + msg
    print(line, flush=True)
    with io.open(STATUS, 'a', encoding='utf-8') as f: f.write(line + '\n')

def stop_requested ():
    return os.path.exists(os.path.join(LORA_DIR, 'STOP'))

def api (path, body=None):
    data = json.dumps(body).encode('utf-8') if body is not None else None
    req = urllib.request.Request(API + path, data=data, headers={'Content-Type': 'application/json'} if data else {})
    with urllib.request.urlopen(req, timeout=60) as r:
        t = r.read()
    try: return json.loads(t)
    except Exception: return t

def comfy_idle ():
    try:
        q = api('/queue')
        return not q.get('queue_running') and not q.get('queue_pending')
    except Exception:
        return False

def wait_comfy (limit=6 * 3600):
    t0 = time.time()
    while not comfy_idle():
        if time.time() - t0 > limit: log('Comfy 기다림 한도 — 넘어간다'); return False
        time.sleep(20)
    return True

def comfy_free ():
    try: api('/free', {'unload_models': True, 'free_memory': True})
    except Exception as e: log('free 실패 %s' % e)
    time.sleep(8)


# ── 학습 자료 ──
def flatten (im):
    bg = Image.new('RGB', im.size, GRAY)
    bg.paste(im, mask=im.split()[3])
    return bg

def crops (path, face_ok):
    im = Image.open(path).convert('RGBA')
    a = np.array(im.split()[3]) > 32
    ys, xs = np.where(a)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    h = y1 - y0; w = x1 - x0
    flat = flatten(im)
    out = []
    pad = int(0.04 * h)
    out.append(('cowboy shot', flat.crop((max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad)))))
    uy = y0 + int(0.58 * h)
    out.append(('upper body', flat.crop((max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), uy))))
    if face_ok:
        band = a[y0:y0 + int(0.2 * h)]
        cols = np.where(band.any(0))[0]
        cx = int(cols.mean()) if len(cols) else (x0 + x1) // 2
        side = int(0.36 * h)
        fx0 = max(0, min(im.width - side, cx - side // 2)); fy0 = max(0, y0 - int(0.03 * h))
        out.append(('portrait, face focus', flat.crop((fx0, fy0, fx0 + side, min(im.height, fy0 + side)))))
    return out

def build_dataset (key, c):
    trig = 'molak' + key
    imgs = []
    for vid, v in c['v'].items():
        if not v['train']: continue
        p = os.path.join(ART, vid + '.png')
        if not os.path.exists(p): log('없는 그림 ' + vid); continue
        for shot, img in crops(p, v['face']):
            cap = ', '.join(x for x in [trig, c['base'], v['tags'], STYLE, shot] if x)
            imgs.append((vid + '_' + shot.split(',')[0].replace(' ', ''), img, cap))
    n = len(imgs)
    rep = max(1, round(c['steps'] / (n * 8)))
    work = os.path.join(LORA_DIR, key)
    ds = os.path.join(work, '%d_%s' % (rep, trig))
    os.makedirs(ds, exist_ok=True)
    for name, img, cap in imgs:
        img.save(os.path.join(ds, name + '.png'))
        with io.open(os.path.join(ds, name + '.txt'), 'w', encoding='utf-8') as f: f.write(cap)
    log('%s 자료 %d장 × 반복 %d × 8돌림 = %d걸음' % (key, n, rep, n * rep * 8))
    return work


# ── 학습 ──
def train (key, c):
    name = 'molak_%s_v1' % key
    if os.path.exists(os.path.join(LORAS, name + '.safetensors')):
        return name
    work = os.path.join(LORA_DIR, key)
    if os.path.exists(work):
        for d in os.listdir(work):
            if d.endswith('_molak' + key):
                import shutil; shutil.rmtree(os.path.join(work, d))
    work = build_dataset(key, c)
    wait_comfy(); comfy_free()
    args = [PY, 'sdxl_train_network.py', '--pretrained_model_name_or_path', CKPT, '--train_data_dir', work,
            '--output_dir', LORAS, '--output_name', name, '--resolution', '896,896', '--enable_bucket',
            '--min_bucket_reso', '512', '--max_bucket_reso', '1536', '--caption_extension', '.txt', '--max_token_length', '225',
            '--network_module', 'networks.lora', '--network_dim', '16', '--network_alpha', '8', '--network_train_unet_only',
            '--learning_rate', '1e-4', '--unet_lr', '1e-4', '--optimizer_type', 'AdamW8bit', '--lr_scheduler', 'cosine',
            '--lr_warmup_steps', '30', '--max_train_epochs', '8', '--save_every_n_epochs', '4', '--train_batch_size', '1',
            '--mixed_precision', 'fp16', '--save_precision', 'fp16', '--fp8_base', '--gradient_checkpointing', '--sdpa',
            '--cache_latents', '--cache_latents_to_disk', '--cache_text_encoder_outputs', '--min_snr_gamma', '5',
            '--seed', '42', '--save_model_as', 'safetensors']
    env = dict(os.environ, PYTHONIOENCODING='utf-8', PYTHONUTF8='1')
    t0 = time.time()
    log('%s 학습 시작' % key)
    with io.open(os.path.join(work, 'train_log.txt'), 'w', encoding='utf-8', errors='replace') as lf:
        rc = subprocess.call(args, cwd=SD, stdout=lf, stderr=subprocess.STDOUT, env=env)
    ok = os.path.exists(os.path.join(LORAS, name + '.safetensors'))
    log('%s 학습 %s (rc %s, %.0f분)' % (key, '끝' if ok else '실패', rc, (time.time() - t0) / 60))
    return name if ok else None


# ── 그리기 ──
def ref_for (vid):
    os.makedirs(INPUT, exist_ok=True)
    dst = os.path.join(INPUT, 'ref_%s.png' % vid)
    if not os.path.exists(dst):
        flatten(Image.open(os.path.join(ART, vid + '.png')).convert('RGBA')).save(dst)
    return 'molak/ref_%s.png' % vid

def job (lora, lw, pos, neg, ref, seed, prefix):
    return {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': 'animagine-xl-4.0-opt.safetensors'}},
        '2': {'class_type': 'LoraLoader', 'inputs': {'model': ['1', 0], 'clip': ['1', 1], 'lora_name': lora + '.safetensors', 'strength_model': lw, 'strength_clip': lw}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'text': pos, 'clip': ['2', 1]}},
        '4': {'class_type': 'CLIPTextEncode', 'inputs': {'text': neg, 'clip': ['2', 1]}},
        '10': {'class_type': 'LoadImage', 'inputs': {'image': ref}},
        '11': {'class_type': 'Canny', 'inputs': {'image': ['10', 0], 'low_threshold': 0.3, 'high_threshold': 0.7}},
        '12': {'class_type': 'ControlNetLoader', 'inputs': {'control_net_name': 'controlnet-union-sdxl.safetensors'}},
        '13': {'class_type': 'SetUnionControlNetType', 'inputs': {'control_net': ['12', 0], 'type': 'canny/lineart/anime_lineart/mlsd'}},
        '14': {'class_type': 'ControlNetApplyAdvanced', 'inputs': {'positive': ['3', 0], 'negative': ['4', 0], 'control_net': ['13', 0], 'image': ['11', 0], 'strength': 0.55, 'start_percent': 0.0, 'end_percent': 0.6, 'vae': ['1', 2]}},
        '5': {'class_type': 'EmptyLatentImage', 'inputs': {'width': 832, 'height': 1216, 'batch_size': 1}},
        '6': {'class_type': 'KSampler', 'inputs': {'seed': seed, 'steps': 28, 'cfg': 5.0, 'sampler_name': 'euler_ancestral', 'scheduler': 'normal', 'denoise': 1.0, 'model': ['2', 0], 'positive': ['14', 0], 'negative': ['14', 1], 'latent_image': ['5', 0]}},
        '8': {'class_type': 'VAEDecode', 'inputs': {'samples': ['6', 0], 'vae': ['1', 2]}},
        '9': {'class_type': 'SaveImage', 'inputs': {'images': ['8', 0], 'filename_prefix': prefix}},
        '20': {'class_type': 'LoadBackgroundRemovalModel', 'inputs': {'bg_removal_name': 'birefnet.safetensors'}},
        '21': {'class_type': 'RemoveBackground', 'inputs': {'bg_removal_model': ['20', 0], 'image': ['8', 0]}},
        '22': {'class_type': 'InvertMask', 'inputs': {'mask': ['21', 0]}},
        '23': {'class_type': 'JoinImageWithAlpha', 'inputs': {'image': ['8', 0], 'alpha': ['22', 0]}},
        '24': {'class_type': 'SaveImage', 'inputs': {'images': ['23', 0], 'filename_prefix': prefix + '_cut'}},
    }

def generate (key, c, lora):
    trig = 'molak' + key
    n = 0
    for vid, v in c['v'].items():
        ref = ref_for(vid)
        pos = ', '.join(x for x in [trig, c['base'], v['tags'], STYLE, v['shot'], 'masterpiece, best quality'] if x)
        neg = NEG + (', red eyes, red pupils' if c['good'] else '')
        for seed in SEEDS:
            prefix = 'molak/redo/%s/%s_s%d' % (vid, vid, seed)
            if os.path.exists(os.path.join(OUTPUT, 'molak', 'redo', vid, '%s_s%d_cut_00001_.png' % (vid, seed))): continue
            api('/prompt', {'prompt': job(lora, v['lw'] or 0.9, pos, neg, ref, seed, prefix)}); n += 1
    log('%s 그림 %d장 맡김' % (key, n))
    t0 = time.time(); wait_comfy()
    log('%s 그림 끝 (%.0f분)' % (key, (time.time() - t0) / 60))


def main ():
    only = sys.argv[1:]
    log('공장 시작 — 인물 %d' % len(CAST))
    for key, c in CAST:
        if only and key not in only: continue
        if stop_requested(): log('STOP 파일 — 멈춘다'); return
        done = os.path.join(LORA_DIR, key, 'DONE')
        if os.path.exists(done): continue
        try:
            lora = train(key, c)
            if not lora: continue
            generate(key, c, lora)
            os.makedirs(os.path.dirname(done), exist_ok=True)
            io.open(done, 'w').write(time.strftime('%Y-%m-%d %H:%M'))
        except Exception:
            log('%s 오류\n%s' % (key, traceback.format_exc()))
            time.sleep(30)
    log('공장 한 바퀴 끝')


if __name__ == '__main__':
    main()
