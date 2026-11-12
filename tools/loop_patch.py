# -*- coding: utf-8 -*-
# 0.4.30 ① 일과를 장면으로 ② 원수의 습격 변주
import io, os, sys
C = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'canon')

def ed(name, pairs):
    p = os.path.join(C, name)
    s = io.open(p, encoding='utf-8').read()
    for o, n in pairs:
        if s.count(o) != 1:
            sys.exit('FAIL %s %d :: %s' % (name, s.count(o), o[:60]))
        s = s.replace(o, n)
    io.open(p, 'w', encoding='utf-8').write(s)

# ---------- 회차 시작 때 새 변수 초기화 ----------
ed('Molak01.nani', [
('''@set grudgewarned=0''', '''@set grudgewarned=0
@set grudgevisit=0
@set grudgename=0
@set c_sw=0
@set c_qi=0
@set c_rest=0
@set c_work=0
@set lv_sw=0
@set lv_qi=0
@set lv_rest=0
@set lv_work=0'''),
])

# ---------- ② 원수의 습격: 올 때마다 다른 모양으로 ----------
ed('Molak02.nani', [
('''@set grudge=grudge-1
@set grudgewarn=0
@goto #KGrA if:f_alms>0||f_thiefkid>0||f_beggarnet>0''',
'''@set grudge=grudge-1
@set grudgewarn=0
@set grudgevisit=grudgevisit+1
@goto #KGrV4 if:grudgevisit>=4
@goto #KGrV3 if:grudgevisit==3
@goto #KGrV2 if:grudgevisit==2
@goto #KGrA if:f_alms>0||f_thiefkid>0||f_beggarnet>0'''),
('''# KGrWarnA
띠를 품에 넣는다. 목을 칠 때는 몰랐던 이름이다.''',
'''# KGrWarnA
@set grudgename=1
띠를 품에 넣는다. 목을 칠 때는 몰랐던 이름이다.'''),
('''# KGrWake
@goto #KGrWakeD if:f_hajinhurt>0''',
'''# KGrWake
@goto #KGrWakeX if:f_hajindead>0
@goto #KGrWakeD if:f_hajinhurt>0'''),
('''# KGrAlly
담 위에서 사내가 뛰어내린다.''',
'''# KGrWakeX
"불이야!"
아무도 나오지 않는다. 점소이 방에서 빗장 거는 소리가 난다.
하진무가 없는 객잔은 이제 남의 집이다. 소리친 만큼 상복들이 웃는다.
"부를 사람도 없는 놈이로군."
@goto #KGrFight
# KGrV2
@scene id:day
@art scene:well
새벽. 우물에 물을 길으러 나간다. 두레박 줄이 반쯤 내려갔을 때, 물에 비친 얼굴이 넷이다.
돌아보지 않아도 안다. 상복이다. 이번엔 지붕이 아니라 땅으로 왔다. 담 넘는 소리도 내지 않았다.

"지난번엔 셋이라 못 끝냈다. 그래서 넷을 데려왔다."

부엌 문이 열렸다가 소리 없이 닫힌다. 점소이다.
@choice "두레박 줄을 놓고 검을 뽑는다" goto:#KGrFight
@choice "우물을 가운데 두고 돈다 — 넷이 한꺼번에 들어올 수 없는 자리다" if:sw>=10 goto:#KGrWell
@choice "\\"이름을 대라. 이번엔 듣겠다.\\"" goto:#KGrName
@choice "소리쳐 객잔 사람들을 깨운다" goto:#KGrWake
@stop
# KGrWell
우물을 사이에 두고 돈다. 하나가 돌아 나오는 순간마다 그 하나만 상대한다.
넷이 셋이 되고, 셋이 다시 넷이 된다. 숨이 차오르는 쪽은 저들이다.
@set f_mournambush=1
@goto #KGrFight
# KGrName
칼끝을 내리지 않은 채 묻는다.
맨 앞의 상복이 잠깐 멈춘다.

"……도 자, 길 자. 도길이다. 마흔하나였다. 딸이 둘이다."

이름을 입에 올리자 가슴의 문양이 식는다. 칼은 거두어지지 않는다. 그래도 넷 가운데 하나가 칼끝을 내린다.
@set grudgename=1
@set grudgewarn=1
@fx curse:-1
@goto #KGrFight
# KGrV3
@scene id:day
@art scene:market
대낮, 국밥집 앞. 상복 넷이 길을 막는다. 저잣거리 사람들이 둥글게 물러선다.
이번엔 숨지 않았다. 저잣거리 한복판에서 끝을 보겠다는 뜻이다.
맨 뒤에 늙은 여자가 서 있다. 상가에서 곡하던 그 어머니다. 아무 말도 하지 않는다.

"세 번 왔다. 세 번 다 네가 살았다. 오늘은 어머니가 보신다."
@choice "검을 뽑는다 — 사람들 앞에서 끝을 본다" goto:#KGrV3F
@choice "검을 땅에 내려놓고 무릎을 꿇는다" goto:#KGrKneel
@choice "어머니 앞으로 가서 그 이름을 부른다" if:grudgename>0 goto:#KGrMother
@stop
# KGrV3F
@set dsitforce=3
@goto #KGrFight
# KGrKneel
검을 내려놓는다. 저잣거리가 숨을 죽인다.
무릎이 흙에 닿는다. 상복들이 칼을 든 채 서로를 본다.
늙은 여자가 앞으로 나온다. 당신 앞에 서서 오래 내려다본다.

"……일어나시오. 내 아들은 무릎 꿇은 사람을 벤 적이 없소."

그녀가 상복들을 데리고 돌아선다. 그날 저녁부터 저잣거리에 말이 돈다. 비무판의 칼이 상주 앞에 무릎을 꿇었다고.
@mark text:"상주 앞에 무릎을 꿇었다"
@set grudge=0
@set grudgevisit=0
@set rank=rank+rank/5 if:rank>0
@fx sl:-2 curse:-2
@goto #NightEnd
# KGrMother
그 이름을 부른다. 띠 끝에서 외운, 우물가에서 들은 그 이름이다.
늙은 여자의 어깨가 내려앉는다. 장례 뒤로 남의 입에서 아들 이름을 들은 적이 없는 얼굴이다.

"……그 아이를 아시오."

"모르오. 판에서 한 번 보았을 뿐이오. 그래서 이름이라도 외웠소."

늙은 여자가 오래 말이 없다. 이윽고 상복들 쪽으로 손을 젓는다.
원한이 다 풀린 건 아니다. 칼만 거두어졌다.
@mark text:"상주에게 그 이름을 불렀다"
@set grudge=0
@set grudgevisit=0
@fx curse:-3
@goto #NightEnd
# KGrV4
@scene id:night
@art scene:mourners
밤. 마당에 상복이 하나뿐이다. 맏상주다. 아우들은 데려오지 않았다.

"아우 둘이 네 칼에 다쳤다. 더는 못 데려온다. 나 하나로 끝내자."

그가 칼끝으로 마당에 금을 긋는다. 생사결이다. 이기는 쪽이 원한을 가져간다.
@choice "금 안으로 든다" goto:#KGrV4F
@choice "검을 내려놓고 무릎을 꿇는다" goto:#KGrKneel
@choice "\\"도길. 마흔하나, 딸이 둘.\\" — 그 이름을 먼저 꺼낸다" if:grudgename>0 goto:#KGrElder
@stop
# KGrV4F
@set dkind="grudge"
@gosub MolakDuel#Run
@goto #NightEnd if:duelwin<1
맏상주가 금 안에 눕는다. 마지막 숨이 길다.

"……끝났다. 원한은, 네가 가져가라."

마당에 금 하나와 사람 하나가 남는다. 새벽에 하진무가 아무 말 없이 금을 쓸어 낸다.
@mark text:"맏상주를 벴다"
@set grudge=0
@set grudgevisit=0
@fx sl:2 curse:3
@goto #NightEnd
# KGrElder
맏상주의 칼끝이 금 위에서 멈춘다.
"……아우가 너한테 그것까지 말했더냐."
그가 칼을 칼집에 꽂는다. 금은 그대로 두고 간다. 넘어오지 않겠다는 뜻이다.
@mark text:"맏상주 앞에서 그 이름을 꺼냈다"
@set grudge=0
@set grudgevisit=0
@fx curse:-2
@goto #NightEnd
# KGrAlly
담 위에서 사내가 뛰어내린다.'''),
])

# ---------- ① 일과를 장면으로 ----------
ed('Molak02.nani', [
# 선택지: 열흘째부터는 효과 설명을 떼고 행동만. 몸이 바닥이면 쉬라는 말이 붙는다
('''@choice "마당에서 검을 수련한다 — 검술이 오른다" goto:#NSw
@choice "앉아서 기를 돌린다(운기조식) — 내공이 오르고 사술이 가라앉는다" goto:#NQi
@choice "저자로 나간다 — 품을 팔아 은자를 벌거나 물건을 산다" goto:#NightTown
@choice "비무판을 찾아간다 — 이기면 서열이 오른다" if:day>=4&&f_ringclosed<1 goto:#NDuel''',
'''@choice "마당에서 검을 수련한다 — 검술이 오른다" if:day<10 goto:#NSw
@choice "마당에서 검을 긋는다" if:day>=10 goto:#NSw
@choice "앉아서 기를 돌린다(운기조식) — 내공이 오른다. 문양도 제 몫을 떼어 간다" if:day<10 goto:#NQi
@choice "앉아서 기를 돌린다" if:day>=10 goto:#NQi
@choice "저자로 나간다 — 품을 팔아 은자를 벌거나 물건을 산다" if:day<10 goto:#NightTown
@choice "저자로 나간다" if:day>=10 goto:#NightTown
@choice "비무판을 찾아간다 — 이기면 서열이 오른다" if:day>=4&&day<10&&f_ringclosed<1 goto:#NDuel
@choice "비무판을 찾아간다" if:day>=10&&f_ringclosed<1 goto:#NDuel'''),
('''@choice "몸을 쉰다 — 체력을 되찾는다" goto:#NRest''',
'''@choice "몸을 쉰다 — 체력을 되찾는다" if:day<10 goto:#NRest
@choice "몸을 쉰다 — 더 버티면 쓰러진다" if:day>=10&&hp<=hpmax/4 goto:#NRest
@choice "몸을 쉰다" if:day>=10&&hp>hpmax/4 goto:#NRest'''),
# 밤의 첫머리: 그날의 형편을 먼저 읽는다
('''@pickflavor pool:night
@goto #Nv2 if:nrand==2''',
'''@set tr=random(1,100)
@goto #NvHj if:f_hajindead>0&&tn_nvhj<1
@goto #NvGr if:grudge>0&&grudgewarn>0&&tr<=60
@goto #NvSoa if:f_soamet>0&&f_soadead<1&&aff>=4&&tr<=30
@goto #NvSoaD if:f_soadead>0&&tr<=25
@goto #NvLow if:hp<=hpmax/4&&tr<=70
@goto #NvBroke if:mn<=1&&tr<=60
@pickflavor pool:night
@goto #Nv2 if:nrand==2'''),
('''# NvOld
@set tn_oldvoice=1''',
'''# NvHj
@set tn_nvhj=1
하진무가 없는 첫 밤이다. 주판 소리 대신 처마 물소리가 셈을 한다.
점소이가 등잔 기름을 채우다 말고 빈 계산대를 한참 본다.
@goto #NightChoice
# NvGr
기와 하나가 운다. 바람이다. 그걸 알면서도 손이 먼저 검자루로 간다.
상복은 오늘 밤 오지 않는다. 언제 올지 모르는 게 오는 것보다 길다.
@goto #NightChoice
# NvSoa
지붕 위에 인기척이 있다. 소아다. 내려오지 않는다. 술병 내려놓는 소리만 한 번 난다.
그 소리를 들으면 이상하게 잠이 온다.
@goto #NightChoice
# NvSoaD
해가 저문다. 지붕 위에 아무도 없다. 그걸 확인하려고 고개를 드는 버릇은 아직 안 고쳐졌다.
@goto #NightChoice
# NvLow
등잔불이 두 개로 보인다. 계단을 오르는 데 난간을 두 번 붙잡았다.
오늘 밤까지 몸을 굴리면 내일 아침에 못 일어날지도 모른다.
@goto #NightChoice
# NvBroke
전낭을 뒤집어 턴다. 먼지만 떨어진다.
방값 셈은 하진무가 하지 않는다. 그래서 더 눈치가 보인다.
@goto #NightChoice
# NvOld
@set tn_oldvoice=1'''),
# 검 수련: 그 밤의 형편 한 줄 + 쌓인 횟수 + 같은 문장 연달아 피하기
('''# NSw
@art scene:yard
@set nmul=3''',
'''# NSw
@art scene:yard
@set nmul=3
@set c_sw=c_sw+1
@gosub #NSwCtx'''),
('''@set sv=random(1,4)
@goto #NSw2 if:sv==2
@goto #NSw3 if:sv==3
@goto #NSw4 if:sv==4
달빛 아래 옆으로 크게 베는 횡소(橫掃)를 천 번 긋는다.''',
'''@set sv=random(1,4)
@set sv=sv+1 if:sv==lv_sw
@set sv=1 if:sv>4
@set lv_sw=sv
@goto #NSw2 if:sv==2
@goto #NSw3 if:sv==3
@goto #NSw4 if:sv==4
달빛 아래 옆으로 크게 베는 횡소(橫掃)를 천 번 긋는다.'''),
('''# NSwT1
@set tn_sw1=1''',
'''# NSwCtx
@goto #NSwC1 if:c_sw==1
@goto #NSwC5 if:c_sw==5
@goto #NSwC10 if:c_sw==10
@goto #NSwC20 if:c_sw==20
@set tr=random(1,100)
@goto #NSwCa if:hp<=3&&tr<=80
@goto #NSwCb if:injury>0&&tr<=70
@goto #NSwCc if:curse>=60&&tr<=45
@goto #NSwCd if:f_hajindead>0&&tr<=35
@goto #NSwCe if:theme==1&&tr<=40
@goto #NSwCf if:grudge>0&&grudgewarn>0&&tr<=40
@goto #NSwCg if:killed>=2&&tr<=25
@return
# NSwC1
이번 생에 처음 마당에 선다. 손이 기억하는 것보다 검이 가볍다. 몸이 아직 그 생을 못 따라왔다.
@return
# NSwC5
다섯째 밤. 마당 흙에 발자국 둘이 깊게 팼다. 늘 같은 자리다.
@return
# NSwC10
열째 밤. 느티나무 껍질에 칼자국이 허리 높이로 줄을 섰다. 점소이가 그 나무에 빨래를 널지 않는다.
@return
# NSwC20
스무째 밤. 횡소를 세지 않는다. 몸이 센다. 사형들도 이쯤에서 세는 걸 그만두었을 것이다.
@return
# NSwCa
팔이 어깨 위로 올라가지 않는다. 그래도 든다. 백 번째에 무릎이 꺾인다.
@return
# NSwCb
옆구리가 당겨 횡소가 반쯤에서 끊긴다. 끊긴 자리부터 다시 긋는다.
@return
# NSwCc
검끝이 자꾸 문양 쪽으로 끌려간다. 베고 싶은 것은 허공이 아니다.
@return
# NSwCd
주판 소리가 없는 마당은 너무 조용하다. 검 소리가 담에 부딪혀 돌아온다.
@return
# NSwCe
빗속에서 긋는다. 젖은 소매가 칼보다 무겁다. 진창에 디딘 발이 미끄러질 때마다 사형의 호통이 들린다.
@return
# NSwCf
기와가 울 때마다 검이 멎는다. 바람이다. 다시 긋는다. 또 멎는다.
@return
# NSwCg
허공을 베는데 손끝에 그 감촉이 남아 있다. 사람을 벤 손은 나무를 벨 때도 그걸 기억한다.
@return
# NSwT1
@set tn_sw1=1'''),
# 운기
('''# NQi
@set nmul=3''',
'''# NQi
@set nmul=3
@set c_qi=c_qi+1
@gosub #NQiCtx'''),
('''@set sv=random(1,4)
@goto #NQi2 if:sv==2
@goto #NQi3 if:sv==3
@goto #NQi4 if:sv==4''',
'''@set sv=random(1,4)
@set sv=sv+1 if:sv==lv_qi
@set sv=1 if:sv>4
@set lv_qi=sv
@goto #NQi2 if:sv==2
@goto #NQi3 if:sv==3
@goto #NQi4 if:sv==4'''),
('''# NQiT1
@set tn_qi1=1''',
'''# NQiCtx
@goto #NQiC1 if:c_qi==1
@goto #NQiC6 if:c_qi==6
@goto #NQiC12 if:c_qi==12
@set tr=random(1,100)
@goto #NQiCa if:curse>=70&&tr<=60
@goto #NQiCb if:f_soamet>0&&f_soadead<1&&aff>=3&&tr<=30
@goto #NQiCc if:hp<=3&&tr<=70
@goto #NQiCd if:f_hajindead>0&&tr<=30
@goto #NQiCe if:f_soadead>0&&tr<=25
@goto #NQiCf if:killed>=3&&tr<=30
@return
# NQiC1
가부좌를 튼다. 이번 생의 몸은 기가 다니는 길을 아직 모른다. 길을 처음부터 다시 낸다.
@return
# NQiC6
여섯째 밤. 앉자마자 숨이 저절로 길어진다. 몸이 이 자리를 기억한다.
@return
# NQiC12
열두째 밤. 단전이 미지근하게 데워져 있다. 앉기 전부터다.
@return
# NQiCa
숨을 들이쉬면 문양도 같이 들이쉰다. 내쉬면 문양은 내쉬지 않는다.
@return
# NQiCb
지붕 위에서 술병 내려놓는 소리. 숨이 그 박자에 맞춰 고르게 된다.
@return
# NQiCc
기를 돌리기도 전에 어지럽다. 첫 바퀴는 숨만 고른다.
@return
# NQiCd
아래층에서 주판 소리가 나지 않는다. 그 소리에 맞춰 숨을 세던 버릇이 헛돈다.
@return
# NQiCe
눈을 감으면 지붕 끝에 술잔 하나가 보인다. 숨이 거기서 걸린다.
@return
# NQiCf
눈을 감으면 벤 얼굴들이 차례로 지나간다. 기가 그 얼굴들 사이를 돌아서 내려간다.
@return
# NQiT1
@set tn_qi1=1'''),
# 쉼
('''# NRest
@set nmul=3''',
'''# NRest
@set nmul=3
@set c_rest=c_rest+1
@gosub #NRestCtx'''),
('''@set sv=random(1,4)
@goto #NRest2 if:sv==2
@goto #NRest3 if:sv==3
@goto #NRest4 if:sv==4''',
'''@set sv=random(1,4)
@set sv=sv+1 if:sv==lv_rest
@set sv=1 if:sv>4
@set lv_rest=sv
@goto #NRest2 if:sv==2
@goto #NRest3 if:sv==3
@goto #NRest4 if:sv==4'''),
('''# NRestT1
@set tn_rest1=1''',
'''# NRestCtx
@set tr=random(1,100)
@goto #NRestCa if:f_hajindead>0&&tr<=40
@goto #NRestCb if:mn<=1&&tr<=60
@goto #NRestCc if:f_soamet>0&&f_soadead<1&&aff>=3&&tr<=25
@goto #NRestCd if:grudge>0&&tr<=40
@goto #NRestCe if:curse>=70&&tr<=45
@goto #NRestCf if:theme==1&&tr<=35
@goto #NRestCg if:c_rest>=6&&tr<=20
@return
# NRestCa
국밥을 들고 올 사람이 없다. 식은 밥을 혼자 먹고 눕는다.
@return
# NRestCb
배가 고프다. 잠으로 배고픔을 속인다. 속는 건 반쯤이다.
@return
# NRestCc
창틀에 소아가 잠깐 앉았다 간다. 아무 말도 없다. 문을 닫고 가는 소리가 평소보다 조용하다.
@return
# NRestCd
검을 베개 옆에 두고 눕는다. 기와가 울 때마다 눈이 떠진다. 잠은 반만 잔다.
@return
# NRestCe
꿈에서 가슴의 문양이 입을 연다. 무슨 말인지 깨면 기억나지 않는다. 땀만 남는다.
@return
# NRestCf
비가 지붕을 두드린다. 이 소리에는 이상하게 잠이 잘 온다. 청운파 산문에도 이런 비가 왔다.
@return
# NRestCg
요즘 잠이 많다. 몸이 쉬는 게 아니라 무언가를 미루고 있다는 걸 안다.
@return
# NRestT1
@set tn_rest1=1'''),
# 날품
('''# NWork
@set nmul=3''',
'''# NWork
@set nmul=3
@set c_work=c_work+1
@gosub #NWorkCtx'''),
('''@set sv=random(1,3)
@goto #NWork2 if:sv==2
@goto #NWork3 if:sv==3''',
'''@set sv=random(1,3)
@set sv=sv+1 if:sv==lv_work
@set sv=1 if:sv>3
@set lv_work=sv
@goto #NWork2 if:sv==2
@goto #NWork3 if:sv==3'''),
('''# NWorkT1
@set tn_wk1=1''',
'''# NWorkCtx
@goto #NWorkC5 if:c_work==5
@goto #NWorkC10 if:c_work==10
@set tr=random(1,100)
@goto #NWorkCa if:mn<=1&&tr<=60
@goto #NWorkCb if:rank>0&&rank<=100000&&tr<=40
@goto #NWorkCc if:injury>0&&tr<=60
@goto #NWorkCd if:theme==1&&tr<=40
@return
# NWorkC5
십장이 이름을 부른다. 이제는 청운이 아니라 '물지게'라고 부른다. 그게 이름보다 편하다.
@return
# NWorkC10
열 번째 날품. 같이 지게를 지던 늙은이가 오늘은 안 나왔다. 아무도 이유를 묻지 않는다.
@return
# NWorkCa
전낭이 비었다. 오늘 삯을 못 받으면 내일 국밥이 없다. 어깨가 그걸 안다.
@return
# NWorkCb
비무판 서열에 이름이 오른 사람이 물지게를 진다. 십장이 삯을 셈하다 말고 눈치를 본다.
@return
# NWorkCc
상처가 당겨 지게가 한쪽으로 기운다. 반대쪽 어깨로 옮겨 진다.
@return
# NWorkCd
빗물이 물지게 통에 같이 담긴다. 물 긷는 날에 비가 오면 일은 두 배, 삯은 그대로다.
@return
# NWorkT1
@set tn_wk1=1'''),
])
print('loop ok')
