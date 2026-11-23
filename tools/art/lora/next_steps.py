# -*- coding: utf-8 -*-
"""2026-11-23 저녁 순서 바꾸기(언메이커: 「지금 이거까지만 하고, 다시 나올 만한 조연만 판단해서 학습해. 타이틀 시안 진행하고」).
  1) STOP — 지금 학습 중인 인물(철산)을 마치고 공장이 멈춘다
  2) 타이틀 시안(title_drafts.main(now=True)) — 바탕 5종 × 4 · 실루엣 4 × 4
  3) STOP 지우고 '다시 나올 조연'만 공장에 넘겨 다시 띄운다
다시 나올 조연 기준: 대본(unity-nani)에서 입상이 쓰이는 곳이 세 대본 이상이거나 열 번 이상, 또는 결말 대본(Molak21)에 나오는 인물.
"""
import os, io, time, subprocess
import title_drafts as T

LORA_DIR = r'C:\Users\sjf1t\RuinedSword\Build\lora'
STATUS = os.path.join(LORA_DIR, 'factory_status.txt')
KEEP = ['prophet', 'yeomga', 'storyteller', 'panju', 'ggoji', 'cultist', 'taxman', 'wangsam', 'mugo', 'lawlord']

def log (m):
    with io.open(STATUS, 'a', encoding='utf-8') as f: f.write(time.strftime('%m-%d %H:%M:%S ') + m + '\n')

def main ():
    open(os.path.join(LORA_DIR, 'STOP'), 'w').close()
    log('순서 바꿈: 철산까지 하고 타이틀 시안 → 다시 나올 조연 %d(%s)' % (len(KEEP), ' '.join(KEEP)))
    while True:
        lines = io.open(STATUS, encoding='utf-8').read().rstrip().splitlines()
        if any('STOP 파일 — 멈춘다' in l for l in lines[-3:]): break
        time.sleep(30)
    try: T.main(now=True)
    except Exception as e: log('타이틀 시안 오류 %s' % e)
    try: os.remove(os.path.join(LORA_DIR, 'STOP'))
    except Exception: pass
    log('공장 다시 — 다시 나올 조연만')
    subprocess.Popen('cmd /c start "molak_lora_factory" /min "%s" %s' % (os.path.join(LORA_DIR, 'run_factory.bat'), ' '.join(KEEP)), shell=True)

if __name__ == '__main__':
    main()
