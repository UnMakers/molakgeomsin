# -*- coding: utf-8 -*-
# 0.4.34 활협전의 맛을 빌려: 모욕의 장부와 되갚음, 저잣거리의 협, 끝의 인물록
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
def ed(f, pairs):
    p = os.path.join(C, f); s = io.open(p, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (f, a[:60]); s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8').write(s)

# 1) 모욕을 기억한다
ed('MolakDuel.nani', [
('''# LtE2
"다음엔 검을 들고 오게."
목은 붙어 있다. 치욕도 같이 붙어 있다.
@return
# LtE3
상대가 칼끝으로 턱을 한 번 들었다 놓는다. 베는 것보다 더 큰 모욕이다.
@return''', '''# LtE2
"다음엔 검을 들고 오게."
목은 붙어 있다. 치욕도 같이 붙어 있다.
@goto #LtIns
# LtE3
상대가 칼끝으로 턱을 한 번 들었다 놓는다. 베는 것보다 더 큰 모욕이다.
# LtIns
@return if:f_insulted>0
@set f_insulted=1
@set insultname=foename
@set insultday=day
<color=#9A70C4>{foename}. 그 이름을 잊지 않는다.</color>
@return'''),
])

ROLL = '''
; ---- 0.4.34 그 뒤, 사람들은 — 이 생에 얽힌 이들의 뒷이야기 ----
# EpiRoll
@set rolln=0
@set rolln=rolln+1 if:f_rivalfriend>0
@set rolln=rolln+1 if:f_yongcrew>0
@set rolln=rolln+1 if:f_hajindead<1
@set rolln=rolln+1 if:f_jangfriend>0
@set rolln=rolln+1 if:f_hyeop>0
@return if:rolln<2
@page
@art char:none
그 뒤, 사람들은.
@goto #ERl1 if:f_rivalfriend<1
@goto #ERl0 if:f_gwakwifedied>0
<color=#8A7A66>곽산</color> — 북령 목책에 창을 기대 두고 산다. 아내의 약값은 이제 외상이 아니다. 비무판에서 누가 청운을 비웃으면 창대로 탁자를 한 번 친다.
@goto #ERl1
# ERl0
<color=#8A7A66>곽산</color> — 아내의 무덤 옆에 창을 꽂아 두었다. 비무판에는 다시 오르지 않는다. 대신 아이들에게 창을 가르친다. 독은 가르치지 않는다.
# ERl1
@goto #ERl2 if:f_yongcrew<1
<color=#8A7A66>용개</color> — 지게꾼 다섯이 열이 되었다. 지게마다 청운(靑雲) 두 글자를 새겼다. 글을 몰라 남에게 새겨 달랬다.
# ERl2
@goto #ERl3 if:f_hajindead>0
<color=#8A7A66>하진무</color> — 장부의 빈칸 하나를 끝내 채우지 않는다. 객 방값 칸이다. 셈이 안 맞는 칸은 비워 두는 법이라 했다.
# ERl3
@goto #ERl4 if:f_herbdead>0||f_herbfriend<1
<color=#8A7A66>백초 할멈</color> — 청운 사람에게는 여전히 반값이다. 요즘은 손목부터 뒤집기 전에 "밥은 먹었느냐"를 먼저 묻는다.
# ERl4
@goto #ERl5 if:f_jangfriend<1
<color=#8A7A66>쥐눈이 장</color> — 딸의 이름이 장부에서 지워졌다. 소식은 여전히 판다. 사람은 이제 안 판다.
# ERl5
@goto #ERl6 if:rep_gae<3||f_gaesold>0
<color=#8A7A66>꼬지와 아이들</color> — 벼랑의 새끼 매듭이 길이 되었다. 장꾼들이 그 길로 소금을 나르고, 아이들이 통행세로 주먹밥 하나를 받는다.
# ERl6
@goto #ERl7 if:f_mournerally<1
<color=#8A7A66>도길의 맏아들</color> — 아버지 기일마다 운락객잔 문간에 술 한 병을 두고 간다. 내일 다시 원수 하겠다던 말은 아직 지키는 중이다.
# ERl7
@goto #ERl8 if:f_sparedally<1
<color=#8A7A66>목을 내밀었던 칼잡이</color> — 판에서 이기면 진 자의 목을 치지 않는다. 판주들이 그를 손 내미는 자라 부른다.
# ERl8
@goto #ERl9 if:f_hyeop<1
<color=#8A7A66>저잣거리의 노점 할멈</color> — 좌판 구석에 주먹밥 하나를 늘 남겨 둔다. 임자는 안 와도 된다고 한다. 오면 더 좋고.
# ERl9
@goto #ERlA if:f_insulted<1||f_paidback<1
<color=#8A7A66>{insultname}</color> — 비무판에서 진 상대의 턱을 다시는 들지 않는다. 누가 그 몸짓을 하면 먼저 고개를 돌린다.
# ERlA
@return
'''

ed('Molak04.nani', [
('''@gosub #EpiOchi if:f_ochisaved>0
@goto #EpiBoard if:f_namedcheongun>0||f_namedtown>0''', '''@gosub #EpiOchi if:f_ochisaved>0
@gosub #EpiRoll
@goto #EpiBoard if:f_namedcheongun>0||f_namedtown>0'''),
('''@goto #TrueBoardE if:f_namedcheongun<1&&f_namedtown<1''', '''@gosub #EpiRoll
@goto #TrueBoardE if:f_namedcheongun<1&&f_namedtown<1'''),
('''# LdRally
@set rallyn=0''', ROLL.strip('\n') + '''

# LdRally
@set rallyn=0'''),
])

EV = '''

; ===== 0.4.34 되갚음: 턱을 들어 올리던 칼끝 =====
# E_Payback
@art scene:market
@art char:none
저잣거리 비무판. 판 위에서 낯익은 사내가 쓰러진 젊은 칼잡이를 내려다보고 있다. 칼끝으로 젊은이의 턱을 들었다 놓는다.
{insultname}. 그날 당신의 턱을 그렇게 들었던 칼끝이다.
@goto #E_PaybackN if:byeolho=="none"
구경꾼 하나가 당신을 알아본다. "{byeolho}다." 소리가 판 둘레를 한 바퀴 돈다.
# E_PaybackN
사내가 고개를 든다. 당신을 본다. 칼끝이 반 뼘 내려간다. 사내의 목울대가 한 번 움직인다.
@choice "판에 오른다 — 같은 몸짓을 돌려준다" goto:#E_PaybackA
@choice "판에 오른다 — 쓰러진 젊은이 앞에 선다" goto:#E_PaybackB
@choice "그냥 지나간다" goto:#E_PaybackC
@stop
# E_PaybackA
@vfx kind:heavy
칼을 뽑지 않는다. 칼집 끝으로 사내의 칼등을 친다. 한 번. 칼이 판 밖으로 굴러간다.
칼집 끝으로 사내의 턱을 들었다 놓는다. 천천히.
"다음엔 검을 들고 오게."
판 둘레가 터진다. 사내의 얼굴이 붉었다가 하얘진다. 오래 그 자리에 서 있다.
@set f_paidback=1
@fx sl:1
@mark text:"턱을 들어 올린 칼끝을 그대로 돌려주었다"
@return
# E_PaybackB
젊은이를 일으켜 세우고 그 앞에 선다. 칼은 칼집에 둔 채다.
"다음 판은 내가 받겠소."
사내가 칼을 쥔 채 한참 서 있다. 그러다 칼을 거두고 판에서 내려간다. 칼 한 번 섞지 않고 진 것을 판 둘레가 다 보았다.
젊은이가 허리를 숙인다. "……이름을 여쭤도 되겠습니까."
"이름은 서열록에 있소. 맨 끝 줄쯤."
@set f_paidback=1
@set f_hyeop=f_hyeop+1
@fx curse:-2
@mark text:"칼을 뽑지 않고 그 판을 이겼다"
@return
# E_PaybackC
지나간다. 등 뒤에서 판 둘레가 다시 시끄러워진다.
턱 밑이 잠깐 서늘했다가 만다. 그날의 일은 그날에 두기로 한다.
@fx curse:-1
@return

; ===== 0.4.34 저잣거리의 협(俠) =====
# E_Hyeop
@art scene:market
@art char:none
장날. 징세조 칼잡이 셋이 노점 할멈의 좌판을 걷어찬다. 대추가 흙바닥에 흩어진다.
"자릿세가 석 달 밀렸소, 할멈."
"이 장터에 자릿세가 어디 있소……."
칼집이 할멈의 어깨를 친다. 장꾼들이 고개를 돌린다. 아무도 나서지 않는다. 다들 제 좌판이 있다.
@choice "할멈 앞에 선다" goto:#E_HyeopA
@choice "밀린 자릿세를 대신 낸다 (은자 6)" if:mn>=6 goto:#E_HyeopB
@choice "고개를 돌린다 — 수배된 몸이다" goto:#E_HyeopC
@stop
# E_HyeopA
흩어진 대추를 밟지 않게 걸어 들어가 할멈 앞에 선다.
@goto #E_HyeopF if:byeolho!="none"
"넌 뭐냐."
@goto #E_HyeopW if:sw>=14
칼잡이 셋이 한꺼번에 온다. 하나를 넘기고, 둘을 막고, 셋째의 칼집에 옆구리를 맞는다.
그래도 비키지 않는다. 장꾼 하나가 멜대를 들고 나선다. 그 옆에서 또 하나. 칼잡이들이 침을 뱉고 물러간다.
@fx hp:-3
@goto #E_HyeopE
# E_HyeopW
@vfx kind:heavy
첫째의 손목을 칼집으로 친다. 둘째의 무릎. 셋째는 칼을 뽑다 말고 멈춘다. 뽑으면 어찌 될지 방금 두 번 보았다.
칼잡이들이 동료를 부축해 물러간다.
@goto #E_HyeopE
# E_HyeopF
"{byeolho}다."
장꾼 하나가 중얼거린다. 소리가 번진다. "{byeolho}이 섰다."
칼잡이 셋이 서로를 본다. 맨 앞의 사내가 칼자루에서 손을 뗀다. 비무판 소문은 징세조 막사에도 닿는다.
"……오늘은 장이 시끄럽군."
셋이 돌아선다. 칼 한 번 뽑지 않았다.
# E_HyeopE
할멈이 흙 묻은 대추를 치마에 주워 담는다. 한 줌을 당신 손에 쥐여 준다.
"닦아 먹어. 흙도 약이여."
그날 저녁, 저잣거리 좌판마다 같은 이야기가 돈다. 징세조 앞에 섰다는 사내 이야기다. 이야기는 돌 때마다 칼잡이 수가 늘어난다.
@set f_hyeop=f_hyeop+1
@fx hp:1 curse:-2
@mark text:"아무도 나서지 않는 장터에서 앞에 섰다"
@return
# E_HyeopB
은자 여섯 냥을 칼잡이 손에 쥐여 준다. 칼잡이가 무게를 달아 보고 웃는다.
"할멈, 귀인 만났소."
그들이 다음 좌판으로 간다. 다음 좌판은 아무도 대신 내주지 않는다.
할멈이 대추 한 줌을 쥐여 준다. 고맙다는 말은 하지 않는다. 다음 좌판 쪽을 본다.
@fx mn:-6
@set f_hyeop=f_hyeop+1
@return
# E_HyeopC
고개를 돌린다. 수배 방에 그려진 얼굴이다. 여기서 칼을 뽑으면 내일 객잔에 관아가 온다.
등 뒤에서 대추 밟히는 소리가 난다. 오래 귀에 남는다.
@fx curse:1
@return
'''
p = os.path.join(C, 'MolakEvents.nani'); s = io.open(p, encoding='utf-8').read()
assert '# E_Payback' not in s
io.open(p, 'w', encoding='utf-8').write(s.rstrip('\n') + '\n' + EV)
print('hyeop_patch ok')
