# -*- coding: utf-8 -*-
# 0.4.34 이야기의 맛: 모여드는 칼들, 이름을 거는 순간, 회귀자가 먼저 말한다, 청운의 현판
import io, os
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')
def ed(f, pairs):
    p = os.path.join(C, f); s = io.open(p, encoding='utf-8').read()
    for a, b in pairs:
        assert s.count(a) == 1, (f, a[:60]); s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8').write(s)

# 원수의 맏상주: 그 이름을 불러 끝낸 생엔 마지막 계단에 선다
ed('Molak02.nani', [
('''@mark text:"맏상주 앞에서 그 이름을 꺼냈다"
@set grudge=0''', '''@mark text:"맏상주 앞에서 그 이름을 꺼냈다"
@set f_mournerally=1
@set grudge=0'''),
])

ed('Molak04.nani', [
# 1) 모여드는 칼들 — 대전 앞 계단
('''# Ld2
@gosub Molak13#Stairs if:b_stairs<1
@art char:lord
@gosub #LdNamgung''', '''# Ld2
@gosub Molak13#Stairs if:b_stairs<1
@gosub #LdRally
@art char:lord
@gosub #LdNamgung'''),
# 2) 회귀자가 먼저 말한다 — 지난 생에 교주에게 죽거나 진 생
('''# Ld2V0
대전(大殿) 끝, 붉은 단 위에 사내가 앉아 있다. <color=#C03434>공기가 그자를 중심으로 휘어 있다</color>.''', '''# Ld2V0
@goto #Ld2Pro if:g_know_lord>0||g_died_lord>0
대전(大殿) 끝, 붉은 단 위에 사내가 앉아 있다. <color=#C03434>공기가 그자를 중심으로 휘어 있다</color>.'''),
('''# Ld2Again
골짜기 가장 깊은 곳에 검은 전각이 있다. 문은 열려 있다.''', '''# Ld2Pro
대전(大殿) 끝, 붉은 단 위에 사내가 앉아 있다. <color=#C03434>공기가 그자를 중심으로 휘어 있다</color>.
혈마교주, 혈천마. 그가 입을 열기 전에 당신이 먼저 말한다.
"'살아 있는 얼굴이로다, 그 밤의.' 그렇게 말할 참이었지."
교주의 입이 반쯤 열린 채 멎는다.
"그다음은 '일 년을 누워 있다가 여기까지라. 누구의 솜씨냐.'"
대전이 조용하다. 교주가 단 위에서 반쯤 일어선 채로 멈춰 있다.
"……너는 본좌가 할 말을 아는구나."
"당신이 나를 벤 자리도 알지. 왼쪽으로 세 걸음, 기둥 그림자가 끝나는 데. 오늘은 거기 서지 않소."
교주가 마저 일어선다. 지겨워하던 얼굴이 처음 보는 것을 보는 얼굴이 된다.
@set f_lordshaken=1
@goto #Ld2Q
# Ld2Again
골짜기 가장 깊은 곳에 검은 전각이 있다. 문은 열려 있다.'''),
# 3) 이름을 건다
('''@choice "대답 대신 검을 뽑는다" goto:#Ld2C1''', '''@choice "\\"청운검파의 막내. 운검사태의 손제자다.\\" — 이름을 건다" if:f_namedcheongun<1 goto:#Ld2Name
@choice "대답 대신 검을 뽑는다" goto:#Ld2C1'''),
('''; ---- 대사형이 옆문을 막는다 ----''', '''# Ld2Name
교주의 눈썹이 한 번 움직인다.
"청운은 본좌가 지웠다. 산문도, 장부도, 이름도."
"지운 장부에 한 줄이 남았소. 막내라 맨 끝에 적혀 있어서, 불이 거기까진 안 닿았소."
대전의 교도들이 서로를 본다. 그 밤 이후 이 전각에서 아무도 소리 내어 부르지 못하던 이름이다.
교주가 웃는다. 웃음 끝이 짧다.
"좋다. 그 한 줄까지 지워 주마."
@set f_namedcheongun=1
@mark text:"교주 앞에서 청운의 이름을 걸었다"
@goto #Ld2C1

; ---- 0.4.34 모여드는 칼들: 이 생에 살려 보내고, 이름을 불러 주고, 빚을 갚은 자들이 마지막 계단에 선다 ----
# LdRally
@set rallyn=0
@set rallyn=rallyn+1 if:f_rivalfriend>0
@set rallyn=rallyn+1 if:f_yongcrew>0
@set rallyn=rallyn+1 if:rep_gae>=3&&f_gaesold<1
@set rallyn=rallyn+1 if:f_mournerally>0
@set rallyn=rallyn+1 if:f_sparedally>0
@return if:rallyn<1
@page
@art scene:stairs
@art char:none
대전으로 오르는 돌계단은 백여든 칸이다. 그 위에서 집법당 칼잡이 서른이 내려온다. 칼집이 한꺼번에 울린다.
혼자 오른다면 여기서 반을 쓴다. 숨을 고르고 칼을 고쳐 쥔다.
@goto #LdR1 if:f_rivalfriend<1
@vfx kind:heavy
계단 아래에서 창 한 자루가 먼저 날아든다. 맨 앞 칼잡이의 발등이 돌에 꿰매진다.
@art char:gwaksan
곽산이 창대를 어깨에 걸치고 계단을 올라온다. 숨이 차다. 북쪽에서부터 뛰어온 숨이다.
"자네 서열이 내 계단이라 했지. 여기서 끝나면 내 계단도 끝나네. 올라가게."
# LdR1
@goto #LdR2 if:f_yongcrew<1
@art char:yonggae
옆 벼랑에서 돌이 쏟아진다. 지게꾼 다섯이 벼랑 끝에 지게를 엎는다. 맨 앞에 용개가 있다.
"나리! 짐은 저희가 집니다요. 오늘 짐은 돌입죠!"
돌무더기가 계단 허리를 막는다. 칼잡이 열이 그 아래 갇힌다.
# LdR2
@goto #LdR3 if:rep_gae<3||f_gaesold>0
@art char:none
벼랑의 새끼 매듭을 타고 거지 아이들이 쏟아져 내려온다. 꼬지가 맨 앞에서 깡통을 두드린다.
교도들이 소리 나는 쪽을 돌아보는 반 박자. 그 반 박자에 당신은 열 칸을 오른다.
# LdR3
@goto #LdR4 if:f_mournerally<1
@art char:none
계단 중턱에 상복을 입은 사내가 선다. 도길의 맏아들이다. 상복 소매에서 낫이 나온다.
"아버지 이름을 불러 준 값이오. 오늘 하루만이오. 내일 다시 원수 하리다."
# LdR4
@goto #LdR5 if:f_sparedally<1
@art char:none
비무판에서 목을 내밀었다가 손을 받아 일어섰던 칼잡이가 계단 옆에 서 있다. 칼을 뽑아 든다.
"빚이오. 목 하나. 여기서 갚소."
# LdR5
@vfx kind:gold
등 뒤에서 칼 소리가 겹친다. 돌 구르는 소리, 깡통 소리, 창대가 돌을 치는 소리.
혼자 오른 계단이 아니다. 돌아보지 않는다. 돌아보면 저들이 서운해할 것이다.
@mark text:"마지막 계단을 혼자 오르지 않았다"
@set f_rally=1
@return

; ---- 대사형이 옆문을 막는다 ----'''),
# 4) 청운의 현판 — 복수의 끝, 그리고 이름을 건 생
('''@page
그 겨울, 강호는 당신에게 새 이름을 붙인다.''', '''@goto #EpiBoard if:f_namedcheongun>0||f_namedtown>0
@goto #EpiBoardE
# EpiBoard
@page
이듬해 봄, 청운산. 무너진 산문 앞에 사람이 하나둘 모인다. 장꾼, 거지 아이, 지게꾼, 창잡이.
@goto #EpiBoardH if:f_hajindead>0
하진무가 나무판 하나를 지고 왔다. 객잔 장부를 매던 판자를 대패로 밀었다.
"글씨는 객이 쓰시오. 나는 셈만 할 줄 아오."
@goto #EpiBoardW
# EpiBoardH
누군가 객잔 장부를 매던 판자를 대패로 밀어 두고 갔다. 셈을 할 줄 알던 사람의 판자다.
# EpiBoardW
붓을 든다. 사부에게 배운 서체다. 손이 떨려 획이 두 번 끊긴다. 고쳐 쓰지 않는다.
청운(靑雲).
현판이 산문에 걸린다. 비뚤다. 아무도 바로잡지 않는다.
# EpiBoardE
@page
그 겨울, 강호는 당신에게 새 이름을 붙인다.'''),
])

# 5) 저잣거리에서 사문을 댄다 — 비무판 연승 뒤 서열록
DEF = '''

; ===== 0.4.34 사문을 대다: 비무판 서열록에 이름을 올리는 날 =====
# E_Cheongun
@art scene:market
비무판 앞. 연승 소문을 들은 구경꾼이 저잣거리 끝까지 늘어섰다.
판주가 서열록을 펼치고 붓을 든다.
"별호 말고, 본명과 사문을 적어야 서열록에 오르오. 어디 사람이오?"
저잣거리 담벼락에 수배 방(榜)이 아직 붙어 있다. 가슴에 문양을 그린 조악한 그림.
@choice "\\"청운검파.\\" — 사문을 댄다" goto:#E_CheongunA
@choice "\\"떠돌이요.\\" — 이름을 감춘다" goto:#E_CheongunB
@stop
# E_CheongunA
판주의 붓이 멎는다. 구경꾼 사이로 웅성임이 번진다.
"청운? 혈마교가 하룻밤에 지웠다던 데 아니야?"
"막내가 하나 살아남았다더니."
판주가 붓에 먹을 다시 찍는다. 이번엔 천천히 쓴다. 청운(靑雲).
맨 앞줄의 늙은 장꾼 하나가 갓을 벗는다. 그 뒤로 몇이 따라 벗는다. 무너진 문파의 이름 앞에서 강호 사람들이 하는 일이다.
@vfx kind:gold
그날 밤, 수배 방 위에 누군가 숯으로 한 줄을 덧쓴다. 「청운은 살아 있다.」
@set f_namedtown=1
@set f_wanted=1
@mark text:"서열록에 청운의 이름을 다시 올렸다"
@return
# E_CheongunB
판주가 '떠돌이'라고 적는다. 구경꾼들이 흩어진다. 아무도 그 이름을 오래 기억하지 않는다.
안전한 길이다. 가슴 어딘가가 조금 식는다.
@return
'''
p = os.path.join(C, 'MolakEvents.nani'); s = io.open(p, encoding='utf-8').read()
assert '# E_Cheongun' not in s
io.open(p, 'w', encoding='utf-8').write(s.rstrip('\n') + '\n' + DEF)
print('saga_patch ok')
