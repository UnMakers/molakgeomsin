# -*- coding: utf-8 -*-
"""타이틀 — 히로인과 함께한 뒤의 바탕, 둘째 판(2026-11-23).
첫 판(title_couple.py)은 바탕 윤곽을 0.35 로 붙잡았더니 스무 장 가운데 열넷이 검객 혼자였다(소아 기대기 s9244 · 청묘 바위 s9022/s9244 만 둘).
둘째 판: 혼자 나온 장면(소아 등 맞대기 · 암향 등불 · 참새 걷기)을 윤곽을 거의 놓고(0.15) / 아예 없이 다시 뽑는다. 두 사람을 먼저 적는다.
결과: output\\molak\\title\\with2_<인물>_<장면>_<c|f>_s<씨앗>.png  (c = 윤곽 0.15, f = 윤곽 없음)
"""
import time
import title_drafts as T
import title_couple as C

SEEDS = [8911, 9022, 9133, 9244]
DUO = '2people, duo, two figures standing close together, '
PICK = ('back', 'lantern', 'walk')

def main ():
    n = 0
    for who, lora, tag, tags in C.SCENES:
        if tag not in PICK: continue
        for i, s in enumerate(SEEDS):
            free = i >= 2
            T.api('/prompt', {'prompt': T.graph(', '.join([DUO + tags, C.BASE, 'masterpiece, best quality']), T.NEG.replace('crowd, multiple boys, ', 'crowd, ') + ', solo, 1other',
                                                 s, 'molak/title/with2_%s_%s_%s_s%d' % (who, tag, 'f' if free else 'c', s), lora=lora, lw=0.7,
                                                 ref=None if free else 'molak/ref_title_night.png', cn=0.15)}); n += 1
    T.log('함께한 바탕 둘째 판 %d장 맡김' % n)
    t0 = time.time()
    while not T.idle(): time.sleep(20)
    T.log('함께한 바탕 둘째 판 끝 (%.0f분)' % ((time.time() - t0) / 60))

if __name__ == '__main__':
    main()
