# -*- coding: utf-8 -*-
# 0.4.33 대사 다듬기: 'A가 아니라, B' 잠언투를 인물의 입말로
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
fixes = {
 'Molak02.nani': [("'서두르지 마라. 익어 가는 것은 네가 아니라, 네 가슴에 심은 열매니라.'", "'서두르지 마라. 익는 것은 네 가슴에 심은 열매니라. 너는 기다리기만 하면 된다.'")],
 'Molak05.nani': [('"앞으로 정심단은 반값이다. 단골값이 아니라, 목숨값이니라."', '"앞으로 정심단은 반값이다. 목숨 하나 건진 값으로 쳐 두마."')],
 'Molak06.nani': [('"……잘 됐다. 익은 놈한테 벤 게 아니라, 아직 덜 익은 놈한테 베였으니."', '"……잘 됐다. 덜 익은 놈한테 베였으니. 익은 놈이었으면 억울할 뻔했다."'),
                  ('"사부의 사부가 남긴 것이었다. 이제 열 사람이 없는 게 아니라, 열 함이 없다."', '"사부의 사부가 남긴 것이었다. 같이 열 사람이 생겼더니, 열 함이 없다."')],
}
n = 0
for f, pairs in fixes.items():
    p = os.path.join(C, f); s = io.open(p, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (f, a); s = s.replace(a, b); n += 1
    io.open(p, 'w', encoding='utf-8').write(s)
print('tone_fix3', n)
