# -*- coding: utf-8 -*-
# 0.4.35 무협 장르 다듬기: 경지 사다리를 익숙한 이름으로(삼류~신화경), 서열은 서열록(1만 줄) 기준으로 보인다
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
def ed(f, pairs):
    p = os.path.join(C, f); s = io.open(p, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (f, a[:60]); s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8').write(s)
ed('MolakDuel.nani', [
('— 강호 서열 {foerank}위 · {foetier}.', '— 강호 서열 {foerank} · {foetier}.'),
('강호 서열 <color=#C9A227>{oldrank}위 → {newrank}위</color>. 한 계단을 또 오른다.', '강호 서열 <color=#C9A227>{oldrank} → {newrank}</color>. 한 계단을 또 오른다.'),
('강호 서열은 <color=#C9A227>{newrank}위</color> 그대로다.', '강호 서열은 <color=#C9A227>{newrank}</color> 그대로다.'),
('@goto #TU2 if:tiername=="2경 취위"\n@goto #TU3 if:tiername=="3경 운위"\n@goto #TU4 if:tiername=="4경 관맥"\n@goto #TU5 if:tiername=="5경 화경"\n@goto #TU6 if:tiername=="6경 발경"\n@goto #TU7 if:tiername=="7경 허실"\n@goto #TU8 if:tiername=="8경 천묵"\n@goto #TU9 if:tiername=="9경 멸도"',
 '@goto #TU2 if:tiername=="이류"\n@goto #TU3 if:tiername=="일류"\n@goto #TU4 if:tiername=="절정"\n@goto #TU5 if:tiername=="초절정"\n@goto #TU6 if:tiername=="화경"\n@goto #TU7 if:tiername=="현경"\n@goto #TU8 if:tiername=="생사경"\n@goto #TU9 if:tiername=="신화경"'),
('구름이 흘러가는 쪽을 아는 것. 운위(雲位)라 부르는 까닭을 몸이 먼저 안다.', '삼류는 칼을 휘두르고, 이류는 칼을 쓰고, 일류는 칼이 갈 길을 먼저 안다. 사부가 하던 말을 몸이 먼저 알아듣는다.'),
])
ed('MolakEvents.nani', [
('''노인이 땅에 아홉 칸을 긋는다.
"식위(識位)에서 제 몸을 알고, 취위·운위에서 기를 모아 돌리지. 넷째 관맥(貫脈)에서 기의 길을 꿰어야 무인 소리를 듣고, 화경(化境)부터는 검이 몸을 떠난다네. 그 위 천묵(天默)은 하늘이 입을 다무는 경지일세."
"아홉째 칸은 비어 있소."
"멸도(滅道). 길(道) 자체를 지우는 자리. 사람의 것이 아니지. …지금 세상에 하나 있다고들 하네만."''', '''노인이 땅에 아홉 칸을 긋는다.
"삼류·이류·일류까지는 칼 쓰는 법을 배우는 자리일세. 절정(絶頂)에 들어야 무인 소리를 듣고, 초절정을 넘어 화경(化境)에 이르면 검이 몸을 떠나지. 그 위 현경(玄境)과 생사경(生死境)은 이름만 전하네."
"아홉째 칸은 비어 있소."
"신화경(神化境). 사람의 것이 아니지. …지금 세상에 하나 있다고들 하네만."'''),
('서열록 팔백만 이름 가운데 하나가 지워졌다. 아무도 번호를 기억하지 않는다.', '서열록 맨 끝 줄의 이름 하나가 지워졌다. 아무도 번호를 기억하지 않는다.'),
])
ed('Molak02.nani', [('독아단 위에는 네 번째 경지(관맥)의 고수, 단주 견아(犬牙)가 있다.', '독아단 위에는 절정(絶頂)의 고수, 단주 견아(犬牙)가 있다.')])
ed('Molak04.nani', [
('멸도의 경지, 서열록의 일위.', '신화경(神化境), 서열록의 일위.'),
('멸도의 교주를 벤 당신을,', '신화경의 교주를 벤 당신을,'),
])
ed('Molak01.nani', [('@choice "복수 — 서열록 천 위 안에 이름을 올린다 (살기 +1)" goto:#RbVow1', '@choice "복수 — 서열록 삼백 위 안에 이름을 올린다 (살기 +1)" goto:#RbVow1')])
print('genre_patch ok')
