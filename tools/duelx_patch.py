# -*- coding: utf-8 -*-
# 0.4.30 비무의 합(合): 마지막 한 칼 앞에 상대의 기세를 읽고 주고받는 합을 넣는다
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
p = os.path.join(C, 'MolakDuel.nani')
s = io.open(p, encoding='utf-8').read()
old = '''# RunChoices
@choice "정면으로 벤다" goto:#StFront
@choice "살기를 실어 선공한다" if:cansl>0 goto:#StKiller
@choice "수를 아끼며 받아친다 — 안전하게" if:cansense<1 goto:#StGuard
@choice "칼날의 푸른 기를 먼저 읽는다" if:cansense>0 goto:#StSense
@choice "사술을 끌어낸다 — 저주 +5" if:cancurse>0&&curse<75 goto:#StCurse
@choice "사술이 먼저 나선다 — 막지 못한다. 저주 +8" if:cancurse>0&&curse>=75 goto:#StCurse2
@choice "물러선다" if:canflee>0 goto:#Flee
@stop'''
new = '''# RunChoices
; --- 합(合): 상대의 기세를 읽고 응수한다. 공세엔 받아치기, 수비엔 흔들기, 허초엔 정면 ---
@duelx do:init
# ExLoop
@duelx do:tell
{dx_telltext}
@choice "받아친다 — 들어오는 칼을 흘려 되친다" goto:#ExA2
@choice "칼끝을 흔든다 — 허초로 빈틈을 끌어낸다" goto:#ExA3
@choice "정면으로 든다 — 곧장 벤다" goto:#ExA1
@choice "물러선다" if:canflee>0&&dx_i<1 goto:#Flee
@stop
# ExA1
@vfx kind:clash
@duelx act:1
@goto #ExRes
# ExA2
@vfx kind:clash
@duelx act:2
@goto #ExRes
# ExA3
@vfx kind:clash
@duelx act:3
# ExRes
{dx_restext}
@goto #ExLoop if:dx_i<dx_n
{dx_momtext}
# Final
@choice "끝을 낸다 — 정면으로 벤다" goto:#StFront
@choice "끝을 낸다 — 살기를 실어 선공한다" if:cansl>0 goto:#StKiller
@choice "버틴다 — 수를 아끼며 받아친다" if:cansense<1 goto:#StGuard
@choice "칼날의 푸른 기를 먼저 읽는다" if:cansense>0 goto:#StSense
@choice "사술을 끌어낸다 — 저주 +5" if:cancurse>0&&curse<75 goto:#StCurse
@choice "사술이 먼저 나선다 — 막지 못한다. 저주 +8" if:cancurse>0&&curse>=75 goto:#StCurse2
@choice "물러선다" if:canflee>0 goto:#Flee
@stop'''
assert s.count(old) == 1
s = s.replace(old, new)
io.open(p, 'w', encoding='utf-8').write(s)
print('duelx ok')
