# 0.4.41: 돈이 없어도 승부를 볼 수 있게 — 은자 판돈을 요구하는 내기 네 곳에 '다른 판돈' 선택지
import sys
C='canon/'
def rep(f, old, new, count=1):
    p=C+f; s=open(p,encoding='utf-8').read()
    assert old in s, (f, old[:50]); s=s.replace(old,new,count); open(p,'w',encoding='utf-8').write(s)

# 1) 투전판 — 외상(왕삼 어른 장부)
rep('Molak01.nani','@choice "투전판에 앉는다 (은자 5)" if:mn>=5&&mkgamble<1 goto:#MkGamble\n',
'@choice "투전판에 앉는다 (은자 5)" if:mn>=5&&mkgamble<1 goto:#MkGamble\n@choice "외상 장부에 이름을 올리고 투전판에 앉는다 — 지면 빚 다섯 냥" if:mn<5&&mkgamble<1 goto:#MkGambleD\n')
rep('Molak01.nani','''# MkGambleA
@set tr=random(1,100)
@goto #MkGambleA2 if:tr<=35
마지막 패가 넘어가고, 맞은편 사내가 판돈을 쓸어 간다. 다섯 냥짜리 구경이었다.
@goto #MkTavern
# MkGambleA2
판주가 숨긴 패를 꺼낼 틈을 놓친다. 판돈 열다섯 냥이 당신 앞으로 온다.
"운 좋은 양반이로군."
@fx mn:15
@goto #MkTavern
# MkGambleB
판주의 손목에 손바닥을 얹는다. 소매 속 패가 툭 떨어진다.
판주가 이를 드러냈다가 당신 눈을 보고 다문다.
"……판돈은 돌려드리리다. 그냥 가시오."
@fx mn:10 sl:1
@set f_gambleshamed=1
@goto #MkTavern
# MkGambleC
패를 엎고 일어선다.
"보는 눈은 있는데 겁이 많군."
겁이 아니다. 오늘은 칼부림할 날이 아니다.
@goto #MkTavern''','''# MkGambleD
@set mkgamble=1
@set gdebt=1
판주가 빈 전낭을 힐끗 보고 붓을 든다.
"외상이오? 이 판은 왕삼 어른 돈으로 돌아가오. 지면 그 장부에 이름이 올라가는 거요."
붓끝이 장부에 당신 이름을 적는다.
판주가 투전목을 돌린다. 패를 쥐는 순간, 판주의 엄지가 패 하나를 소매로 밀어 넣는 게 보인다.
@choice "모른 척 끝까지 친다" goto:#MkGambleA
@choice "판주의 손목을 내리누른다" goto:#MkGambleB
@choice "패를 엎고 일어선다" goto:#MkGambleC
@stop
# MkGambleA
@set tr=random(1,100)
@goto #MkGambleA2 if:tr<=35
@goto #MkGambleAD if:gdebt>0
마지막 패가 넘어가고, 맞은편 사내가 판돈을 쓸어 간다. 다섯 냥짜리 구경이었다.
@goto #MkTavern
# MkGambleAD
마지막 패가 넘어가고, 판주가 장부의 당신 이름 옆에 '五' 자를 적는다. 먹이 마르기도 전에 장부가 덮인다.
"닷새 뒤에 왕삼 어른 사람이 찾아갈 거요. 객잔이 어디인지는 다 아오."
@set debtday=day+5 if:debt<1
@set debt=debt+5
@set gdebt=0
@goto #MkTavern
# MkGambleA2
판주가 숨긴 패를 꺼낼 틈을 놓친다. 판돈 열다섯 냥이 당신 앞으로 온다.
"운 좋은 양반이로군."
@goto #MkGambleA2D if:gdebt>0
@fx mn:15
@goto #MkTavern
# MkGambleA2D
판주가 그중 다섯 냥을 떼어 내고 장부의 이름에 줄을 긋는다.
@fx mn:10
@set gdebt=0
@goto #MkTavern
# MkGambleB
판주의 손목에 손바닥을 얹는다. 소매 속 패가 툭 떨어진다.
판주가 이를 드러냈다가 당신 눈을 보고 다문다.
@goto #MkGambleBD if:gdebt>0
"……판돈은 돌려드리리다. 그냥 가시오."
@fx mn:10 sl:1
@set f_gambleshamed=1
@goto #MkTavern
# MkGambleBD
"……장부에서 이름은 지우리다. 이건 입막음 값이오. 그냥 가시오."
판주가 은자 다섯 냥을 탁자 너머로 민다.
@fx mn:5 sl:1
@set f_gambleshamed=1
@set gdebt=0
@goto #MkTavern
# MkGambleC
패를 엎고 일어선다.
"보는 눈은 있는데 겁이 많군."
겁이 아니다. 오늘은 칼부림할 날이 아니다.
@goto #MkGambleCD if:gdebt>0
@goto #MkTavern
# MkGambleCD
판주가 혀를 차며 장부의 이름에 줄을 긋는다.
"앉았다 일어난 값은 안 받겠소. 다음엔 은자를 들고 오시오."
@set gdebt=0
@goto #MkTavern''')

# 2) 운주 들판 내기 — 칼집을 건다
rep('Molak03.nani','@choice "허세꾼의 맞상대에게 판돈을 건다 (은자 5)" if:mn>=5 goto:#Unju2Y\n',
'@choice "허세꾼의 맞상대에게 판돈을 건다 (은자 5)" if:mn>=5 goto:#Unju2Y\n@choice "\\"은자는 없소. 칼집을 걸겠소.\\" — 허세꾼의 맞상대에게" if:mn<5 goto:#Unju2W\n')
rep('Molak03.nani','''# Unju2Z
대도가 크게 휘두르고''','''# Unju2W
내기꾼이 칼집을 받아 손바닥에 무게를 단다. 낡았지만 마구리에 구름무늬가 남아 있다.
"…좋소. 지면 이건 내 거요. 이기면 열 냥 얹어 돌려주지."
다들 목소리 큰 대도(大刀) 사내에게 건다. 당신은 말없이 칼을 닦는 마른 사내 쪽에 칼집을 놓는다.
@set tr=random(1,100)
@goto #Unju2WZ if:tr<=55
대도가 크게 휘두르고, 마른 사내가 그 밑으로 파고든다.
대도의 왼손에서 비수가 튀어나온다. 허세 속에 진짜가 숨어 있었다.
내기꾼이 칼집을 제 허리에 찬다. 맨 칼날을 헝겊으로 감아 등에 멘다. 며칠은 칼이 손에 붙지 않을 것이다.
@fx sw:-1
@goto Molak02#AfterChapter
# Unju2WZ
대도가 크게 휘두르고, 마른 사내가 파고들어 칼등으로 손목을 친다.
내기꾼이 혀를 차며 칼집과 은자 열 냥을 함께 내민다.
"형씨, 눈이 무섭구려. 그 구름무늬는 못 본 걸로 하리다."
@fx mn:10
@goto Molak02#AfterChapter
# Unju2Z
대도가 크게 휘두르고''')

# 3) 곽산과의 내기 — 몸으로 건다(창대를 깎아 준다)
rep('Molak05.nani','@choice "\\"판돈을 걸자. 지면 은자 다섯이다.\\" (은자 5)" if:mn>=5 goto:#Rival2X\n',
'@choice "\\"판돈을 걸자. 지면 은자 다섯이다.\\" (은자 5)" if:mn>=5 goto:#Rival2X\n@choice "\\"은자는 없다. 지면 사흘, 네 창대를 깎아 주마.\\"" if:mn<5 goto:#Rival2W\n')
rep('Molak05.nani','''# Rival2Y
곽산이 흙을 털고''','''# Rival2W
"내가 지면 사흘, 네 창대를 깎고 날을 세워 주마. 네가 지면 북쪽 길 하나."
곽산이 쪼개진 창대를 내려다본다.
"……칼잡이가 창을 깎는다라. 그것도 구경거리군. 좋다."
@set dkind="rival"
@gosub MolakDuel#Run
@goto #Rival2Y if:duelwin>0
곽산이 창대를 당신 발치에 던진다.
"사흘이다. 대패는 내가 빌려 오마."
사흘 동안 나무를 깎는다. 손바닥에 물집이 잡히는 사이, 창이 어디서 힘을 받고 어디서 꺾이는지가 손에 들어온다.
@fx day:2 sw:1
@set f_rivalfriend=1
@goto Molak02#AfterChapter
# Rival2Y
곽산이 흙을 털고''')

# 4) 백초 할멈 감별 내기 — 약연을 돌린다
rep('MolakEvents.nani','@choice "감별 내기를 받는다" if:mn>=2 goto:#E_HerbC1\n',
'@choice "감별 내기를 받는다" if:mn>=2 goto:#E_HerbC1\n@choice "\\"은자는 없소. 틀리면 반나절 약연을 돌리겠소.\\"" if:mn<2 goto:#E_HerbC4\n')
rep('MolakEvents.nani','''# E_HerbC2
@goto #E_Herb3''','''# E_HerbC2
@goto #E_Herb3
# E_HerbC4
할멈이 약연 손잡이를 툭 친다.
"반나절이면 허리가 끊어질 게다. 좋다."
@set herbbody=1
@gosub #H_Herbbet
@set herbbody=0
@return''')
rep('MolakEvents.nani','''"틀렸다. 박주가리 뿌리니라. 그걸로 여럿 속여 먹었지."
할멈이 손바닥을 내민다. 내기는 내기다.
@fx mn:-2
@return''','''"틀렸다. 박주가리 뿌리니라. 그걸로 여럿 속여 먹었지."
@goto #HerbbetBody if:herbbody>0
할멈이 손바닥을 내민다. 내기는 내기다.
@fx mn:-2
@return
# HerbbetBody
"반나절이다. 손잡이를 잡거라."
해가 기울 때까지 약연을 돌린다. 어깨가 빠질 것 같은데, 할멈이 그사이 약재 이름 열두 개를 귀에 박아 넣는다.
@fx hp:-1 ng:1
@return''')
print('ok')
