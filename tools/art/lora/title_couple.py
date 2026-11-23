# -*- coding: utf-8 -*-
"""타이틀 — 히로인과 함께한 뒤의 바탕(2026-11-23, 언메이커: 「좀 더 친근하게, 더 느낌 있게 — 따로 선 그림자는 지금 컨셉에 안 맞는다」).
실루엣을 덧붙이지 않고, 바탕 그림 자체에 검객과 그 사람을 함께 그린다(같은 먹 그림, 같은 수면·달 자리).
히로인 로라 + 지금 바탕(고요한 수면)의 윤곽을 약하게(0.35, 0.55까지) 붙잡아 달·물가 자리만 지킨다.
결과: output\\molak\\title\\with_<인물>_<장면>_s<씨앗>.png
"""
import time, io
import title_drafts as T

SEEDS = [8911, 9022, 9133, 9244]
BASE = ('greyscale, monochrome, spot color, red accents, ink wash painting, ink sketch, traditional media, minimalism, negative space, '
        'scenery, lake, still water, reflection, mist, full moon, night, wide shot, small figures')
SCENES = [
    ('soa', 'molak_soa_v1', 'lean', 'molaksoa, 1boy, 1girl, couple, from behind, swordsman in conical straw hat and a girl with black high ponytail standing shoulder to shoulder, she leans her head on his shoulder, holding hands, sword on her back, red sash, red streak in hair'),
    ('soa', 'molak_soa_v1', 'back', 'molaksoa, 1boy, 1girl, couple, swordsman in conical straw hat and a girl with black high ponytail standing back to back, both holding swords, red sash, red streak in hair, wind'),
    ('amhyang', 'molak_amh_v1', 'lantern', 'molakamh, 1boy, 1girl, couple, from behind, swordsman in conical straw hat walking beside a woman with hair bun and red hair ornament, she holds a red paper lantern, her hand on his sleeve'),
    ('cheongmyo', 'molak_cm_v1', 'rock', 'molakcm, 1boy, 1girl, from behind, swordsman in conical straw hat and a nun in white hood sitting side by side on a rock at the water, shoulder to shoulder, axe resting against the rock, red prayer beads'),
    ('chamsae', 'molak_cham_v1', 'walk', 'molakcham, 1boy, 1girl, from behind, swordsman in conical straw hat walking with a short-haired girl, hands behind her head, she looks up at him laughing, red sash'),
]

def main ():
    while True:
        lines = io.open(T.STATUS, encoding='utf-8').read().rstrip().splitlines()
        if any('STOP 파일 — 멈춘다' in l for l in lines[-3:]) and T.idle(): break
        time.sleep(30)
    n = 0
    for who, lora, tag, tags in SCENES:
        for s in SEEDS:
            T.api('/prompt', {'prompt': T.graph(', '.join([tags, BASE, 'masterpiece, best quality']), T.NEG.replace('crowd, multiple boys, ', 'crowd, '), s,
                                                 'molak/title/with_%s_%s_s%d' % (who, tag, s), lora=lora, lw=0.7,
                                                 ref='molak/ref_title_night.png', cn=0.35)}); n += 1
    T.log('함께한 바탕 %d장 맡김' % n)
    t0 = time.time()
    while not T.idle(): time.sleep(20)
    T.log('함께한 바탕 끝 (%.0f분)' % ((time.time() - t0) / 60))

if __name__ == '__main__':
    main()
