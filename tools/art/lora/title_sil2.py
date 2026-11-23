# -*- coding: utf-8 -*-
"""타이틀 히로인 실루엣 둘째 판(2026-11-23, 언메이커: 「소아와 함께한 뒤를 예로 들면 좀 더 친근하게 — 너무 따로 있는 느낌.
소아뿐 아니라 다른 히로인들도 마찬가지」).
히로인은 검객 바로 곁에 선다 — 같은 뒷모습으로, 검객 쪽으로 고개를 돌린 자세. 소아·암향·청묘·참새 넷 × 씨앗 넷.
  검객 오른쪽에 서는 사람(소아·청묘)은 왼쪽으로, 왼쪽에 서는 사람(암향·참새)은 오른쪽으로 고개를 돌린다.
로라 공장이 STOP 으로 멈춘 뒤(교도까지) 돈다. 결과: output\\molak\\title\\sil2_<인물>_s<씨앗>_cut.png
"""
import os, io, time
import title_drafts as T

SEEDS = [8505, 8606, 8707, 8808]
STYLE = 'greyscale, monochrome, ink sketch, white background, simple background, full body, from behind, standing close, gentle'
SILS = [
    ('soa', 'molak_soa_v1', 'molaksoa, 1girl, solo, black hair, high ponytail, red streak in hair, black robe, red sash, sword on back, head turned to the left, looking to the side, hands behind back'),
    ('amhyang', 'molak_amh_v1', 'molakamh, 1girl, solo, black hair, hair bun, red hair ornament, grey robe, red sash, long sleeves, head turned to the right, looking to the side'),
    ('cheongmyo', 'molak_cm_v1', 'molakcm, 1girl, solo, white hood, nun, white robe, red prayer beads, axe on shoulder, head turned to the left, looking to the side'),
    ('chamsae', 'molak_cham_v1', 'molakcham, 1girl, solo, black hair, short hair, grey robe, red sash, hands behind head, head turned to the right, looking to the side'),
]

def main ():
    while True:
        lines = io.open(T.STATUS, encoding='utf-8').read().rstrip().splitlines()
        if any('STOP 파일 — 멈춘다' in l for l in lines[-3:]) and T.idle(): break
        time.sleep(30)
    n = 0
    for name, lora, tags in SILS:
        for s in SEEDS:
            T.api('/prompt', {'prompt': T.graph(', '.join([tags, STYLE, 'masterpiece, best quality']), T.NEG + ', scenery, multiple girls', s,
                                                 'molak/title/sil2_%s_s%d' % (name, s), lora=lora, cut=True)}); n += 1
    T.log('히로인 실루엣 둘째 판 %d장 맡김' % n)
    t0 = time.time()
    while not T.idle(): time.sleep(20)
    T.log('히로인 실루엣 둘째 판 끝 (%.0f분)' % ((time.time() - t0) / 60))

if __name__ == '__main__':
    main()
