# -*- coding: utf-8 -*-
# 0.4.33 무협의 맛: 전생의 수로 먼저 끝내기, 한칼, 경지 돌파, 구경꾼과 별호, 몰락검신이라는 이름
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
def ed(f, pairs):
    p = os.path.join(C, f); s = io.open(p, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (f, a[:60]); s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8').write(s)

ed('MolakDuel.nani', [
# 합 앞의 갈림: 전생의 수 / 한칼
('''# RunChoices
; --- 합(合): 상대의 기세를 읽고 응수한다. 공세엔 받아치기, 수비엔 흔들기, 허초엔 정면 ---
@duelx do:init''', '''# RunChoices
@set donecut=0
@goto #ExInit if:memwarn<1&&odds10<8
@goto #ExInit if:memwarn<1&&dlethal>0
@choice "지난 생의 그 수를 먼저 낸다 — 첫 수에 끝낸다" if:memwarn>0 goto:#Pre
@choice "한칼에 끝낸다 — 합을 나눌 상대가 아니다" if:memwarn<1&&odds10>=8&&dlethal<1 goto:#OneCut
@choice "합을 나눈다" goto:#ExInit
@stop
# Pre
@duelx do:pre
{dx_pretext}
{dx_momtext}
@goto #Final
# OneCut
칼을 뽑지 않는다. 손을 칼자루에 얹고 서 있기만 한다.
상대가 먼저 온다. 기합 소리가 크다.
@set donecut=1
@vfx kind:heavy
@wait 0.3
@duel style:onecut
@goto #Result
# ExInit
; --- 합(合): 상대의 기세를 읽고 응수한다. 공세엔 받아치기, 수비엔 흔들기, 허초엔 정면 ---
@duelx do:init'''),
# 진 판: 연승이 끊긴다
('''# LoseTail
''', '''# LoseTail
@set arenawin=0
'''),
# 이긴 판: 한칼의 끝, 별호의 외침
('''# Win
@set wrand=random(1,3)''', '''# Win
@goto #WinOne if:donecut>0
@set wrand=random(1,3)'''),
('''# WinRank
강호 서열''', '''# WinOne
칼이 칼집에서 나왔다가 들어간다. 그 사이에 판이 끝났다.
상대가 두어 걸음 더 걷다가 무릎을 꿇는다. 손에 쥔 칼이 반으로 갈라져 있다.
판 둘레가 조용하다. 누군가 침 삼키는 소리만 크다.
@goto #WinRank
# WinRank
강호 서열'''),
# 경지 돌파
('''# TierUp
<color=#C9A227>─ {tiername} 진입 ─</color>
한 단계 높은 경지에 들어선다. 세상의 소리가 조금 더 또렷해진다.''', '''# TierUp
@vfx kind:tier
<color=#C9A227>─ {tiername} 진입 ─</color>
@goto #TU2 if:tiername=="2경 취위"
@goto #TU3 if:tiername=="3경 운위"
@goto #TU4 if:tiername=="4경 관맥"
@goto #TU5 if:tiername=="5경 화경"
@goto #TU6 if:tiername=="6경 발경"
@goto #TU7 if:tiername=="7경 허실"
@goto #TU8 if:tiername=="8경 천묵"
@goto #TU9 if:tiername=="9경 멸도"
한 단계 높은 경지에 들어선다. 세상의 소리가 조금 더 또렷해진다.
@goto #WinAfter
# TU2
숨이 칼끝까지 내려간다. 칼이 손에 붙는다. 무겁지 않다.
마당에서 천 번 그은 횡소가, 오늘 판 위에서 한 번에 나왔다.
@goto #WinAfter
# TU3
상대의 칼이 오기 전에 바람이 먼저 온다. 그 바람의 방향이 보인다.
구름이 흘러가는 쪽을 아는 것. 운위(雲位)라 부르는 까닭을 몸이 먼저 안다.
@goto #WinAfter
# TU4
단전에서 손끝까지, 막혀 있던 마디가 하나씩 풀린다. 맥이 한 줄로 꿰인다.
칼을 쥔 손이 따뜻하다. 문양의 열과는 다른 열이다. 제 것이다.
@goto #WinAfter
# TU5
칼에 기가 실린다. 베지 않은 자리까지 서늘해진다.
상대의 소매 끝이 칼이 지나간 반 뼘 바깥에서 갈라진다. 칼보다 기가 먼저 닿았다.
@goto #WinAfter
# TU6
힘이 발끝에서 터져 무릎과 허리를 지나 칼끝으로 나간다. 땅이 먼저 울린다.
발밑 흙이 둥글게 패어 있다. 판 둘레의 구경꾼들이 한 걸음씩 물러선다.
@goto #WinAfter
# TU7
허(虛)와 실(實)이 갈라져 보인다. 상대의 칼 셋 중 진짜는 하나다. 나머지 둘은 그림자다.
사부가 평생 말로만 하던 자리다. 이제 말이 필요 없다.
@goto #WinAfter
# TU8
소리가 멎는다. 바람도, 숨소리도, 칼이 움직이는 소리마저.
그 고요 속에서 칼이 혼자 간다. 당신은 따라가기만 한다.
@goto #WinAfter
# TU9
더 오를 자리가 없다. 서열록의 맨 윗줄에서 이름 하나가 지워지고, 그 자리에 당신이 있다.
청운의 막내가, 빗자루를 들던 손으로, 여기까지 왔다.
@goto #WinAfter'''),
# 구경꾼과 별호(목이 붙어 있는 판의 연승)
('''# WinAfter
@goto #WaS1 if:dstake<1''', '''# WinAfter
@set arenawin=arenawin+1 if:dlethal<1
@goto #Fame if:arenawin==3&&byeolho=="none"
@goto #FameCall if:arenawin>=4&&byeolho!="none"&&dlethal<1
@goto #FameE
# Fame
@vfx kind:crowd
판 둘레가 술렁인다. 웅성임이 한 겹 두꺼워진다.
"저 사내, 사흘 판을 내리 이겼다."
@goto #FameS if:curse>=60
@goto #FameK if:sl>=6
"검로가 낯익은데. 청운 같아. 청운은 망했잖아."
"망한 문파의 검이라. 떨어진 칼, 낙검(落劍)이로구먼."
그날 저녁, 저잣거리에 별호 하나가 돈다. 낙검. 떨어진 칼.
@set byeolho="낙검"
@goto #FameE
# FameS
"칼끝에서 검은 김이 나더라. 사람 칼이 아니야."
"사검(邪劍)이다. 저건 사검이야."
그날 저녁, 저잣거리에 별호 하나가 돈다. 사검. 삿된 칼.
@set byeolho="사검"
@goto #FameE
# FameK
"눈 봤나. 상대 목만 보더라."
"혈수(血手)라 불러야겠군. 손에 피 마를 날이 없겠어."
그날 저녁, 저잣거리에 별호 하나가 돈다. 혈수. 피 묻은 손.
@set byeolho="혈수"
@goto #FameE
# FameCall
@vfx kind:crowd
"{byeolho}다!" 판 둘레에서 누군가 외친다. 다음 판 상대가 칼을 고쳐 쥔다. 손바닥이 젖어 있다.
# FameE
@goto #WaS1 if:dstake<1'''),
])

# 몰락검신이라는 이름(복수의 끝, 진엔딩) — 그리고 '제1부'라는 표기를 걷는다
ed('Molak04.nani', [
('''@vfx kind:dawn
복수는 끝났다.<br>이야기는, 끝나지 않았다.
\\[ 제一부 完 \\]''', '''@page
그 겨울, 강호는 당신에게 새 이름을 붙인다.
무너진 문파의 마지막 칼이 서열록 맨 윗줄까지 올라갔다는 이야기가 객잔마다 돈다. 이야기는 돌 때마다 부풀고, 이름은 돌 때마다 짧아진다.
몰락검신(沒落劍神).
@set byeolho="몰락검신"
@vfx kind:dawn
복수는 끝났다.<br>이야기는, 끝나지 않았다.
\\[ 終 \\] — 몰락검신'''),
('''\\[ 眞 · 雲劍合璧 \\]''', '''강호는 그해 겨울 당신을 몰락검신(沒落劍神)이라 부른다. 소아는 그 이름을 한 번도 부르지 않는다. 다른 이름으로 부른다.
@set byeolho="몰락검신"
\\[ 眞 · 雲劍合璧 \\]'''),
])
print('hype_patch ok')
