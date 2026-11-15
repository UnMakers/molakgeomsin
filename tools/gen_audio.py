# -*- coding: utf-8 -*-
# 몰락검신 효과음·배경음 뽑기 — ComfyUI(127.0.0.1:8188)에 작업을 넣고 결과 mp3 를 out 폴더로 옮긴다.
import json, sys, time, urllib.request, os, shutil, random, traceback
HOST = "http://127.0.0.1:8188"
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "audio")
COMFY_OUT = sys.argv[1] if len(sys.argv) > 1 else ""
LOG = os.path.join(HERE, "gen.log")
os.makedirs(OUT, exist_ok=True)
def log(s):
    with open(LOG, "a", encoding="utf-8") as f: f.write(time.strftime("%H:%M:%S ") + s + "\n")
def post(path, data):
    req = urllib.request.Request(HOST + path, data=json.dumps(data).encode("utf-8"), headers={"Content-Type": "application/json"})
    return json.loads(urllib.request.urlopen(req, timeout=60).read().decode("utf-8"))
def get(path):
    return json.loads(urllib.request.urlopen(HOST + path, timeout=60).read().decode("utf-8"))
def wait_server():
    for i in range(240):
        try: get("/system_stats"); return True
        except Exception: time.sleep(2)
    return False

def sfx_graph(prompt, seconds, seed, name, steps=60, cfg=6.0, neg=None):
    return {
      "4": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "stable-audio-open-1.0.safetensors"}},
      "10": {"class_type": "CLIPLoader", "inputs": {"clip_name": "t5-base.safetensors", "type": "stable_audio", "device": "default"}},
      "6": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": ["10", 0]}},
      "7": {"class_type": "CLIPTextEncode", "inputs": {"text": neg or "music, melody, speech, voice, singing, low quality, noise, distortion", "clip": ["10", 0]}},
      "11": {"class_type": "EmptyLatentAudio", "inputs": {"seconds": seconds, "batch_size": 1}},
      "3": {"class_type": "KSampler", "inputs": {"seed": seed, "steps": steps, "cfg": cfg, "sampler_name": "dpmpp_3m_sde_gpu", "scheduler": "exponential", "denoise": 1.0,
             "model": ["4", 0], "positive": ["6", 0], "negative": ["7", 0], "latent_image": ["11", 0]}},
      "12": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["3", 0], "vae": ["4", 2]}},
      "20": {"class_type": "SaveAudioMP3", "inputs": {"audio": ["12", 0], "filename_prefix": "molak/" + name, "quality": "V0"}},
    }

def bgm_graph(tags, seconds, seed, name, bpm, key, ts="4", codes=True):
    return {
      "97": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": "ace_step_1.5_turbo_aio.safetensors"}},
      "94": {"class_type": "TextEncodeAceStepAudio1.5", "inputs": {"clip": ["97", 1], "tags": tags, "lyrics": "[Instrumental]", "seed": seed, "bpm": bpm, "duration": seconds,
              "timesignature": ts, "language": "en", "keyscale": key, "generate_audio_codes": codes, "cfg_scale": 2.0, "temperature": 0.85, "top_p": 0.9, "top_k": 0, "min_p": 0.0}},
      "78": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["97", 0], "shift": 3}},
      "47": {"class_type": "ConditioningZeroOut", "inputs": {"conditioning": ["94", 0]}},
      "98": {"class_type": "EmptyAceStep1.5LatentAudio", "inputs": {"seconds": seconds, "batch_size": 1}},
      "3": {"class_type": "KSampler", "inputs": {"seed": seed, "steps": 8, "cfg": 1.0, "sampler_name": "euler", "scheduler": "simple", "denoise": 1.0,
             "model": ["78", 0], "positive": ["94", 0], "negative": ["47", 0], "latent_image": ["98", 0]}},
      "18": {"class_type": "VAEDecodeAudio", "inputs": {"samples": ["3", 0], "vae": ["97", 2]}},
      "20": {"class_type": "SaveAudioMP3", "inputs": {"audio": ["18", 0], "filename_prefix": "molak/" + name, "quality": "V0"}},
    }

def run(graph, name):
    r = post("/prompt", {"prompt": graph, "client_id": "molak"})
    pid = r.get("prompt_id")
    t0 = time.time()
    while time.time() - t0 < 1800:
        time.sleep(2)
        h = get("/history/" + pid)
        if pid in h:
            st = h[pid].get("status", {})
            if st.get("status_str") == "error":
                log("ERROR " + name + " " + json.dumps(st)[:800]); return False
            outs = h[pid].get("outputs", {})
            for nid, o in outs.items():
                for a in o.get("audio", []):
                    src = os.path.join(COMFY_OUT, a.get("subfolder", ""), a["filename"])
                    dst = os.path.join(OUT, name + ".mp3")
                    shutil.copyfile(src, dst); log("OK " + name + " " + str(round(time.time() - t0)) + "s")
                    return True
            log("NOOUT " + name); return False
    log("TIMEOUT " + name); return False

def main():
    jobs = json.load(open(os.path.join(HERE, "jobs.json"), encoding="utf-8"))
    if not wait_server(): log("server not up"); return
    log("server up, jobs " + str(len(jobs)))
    for j in jobs:
        name = j["name"]
        if os.path.exists(os.path.join(OUT, name + ".mp3")): continue
        try:
            if j["kind"] == "sfx": g = sfx_graph(j["prompt"], j["sec"], j.get("seed", random.randint(1, 10**9)), name, j.get("steps", 60), j.get("cfg", 6.0), j.get("neg"))
            else: g = bgm_graph(j["prompt"], j["sec"], j.get("seed", random.randint(1, 10**9)), name, j.get("bpm", 80), j.get("key", "D minor"), j.get("ts", "4"), j.get("codes", True))
            ok = run(g, name)
            if not ok and j["kind"] == "bgm" and j.get("codes", True):
                g = bgm_graph(j["prompt"], j["sec"], j.get("seed", 7), name, j.get("bpm", 80), j.get("key", "D minor"), j.get("ts", "4"), False)
                run(g, name)
        except Exception as e:
            log("EXC " + name + " " + traceback.format_exc()[-600:])
    try: post("/free", {"unload_models": True, "free_memory": True})
    except Exception: pass
    log("ALLDONE")
main()
