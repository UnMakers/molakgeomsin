# -*- coding: utf-8 -*-
"""0.4.12 경제·물건: 무한히 사던 값싼 약에 하루 한 번 제한, 후반의 비싼 쓸 곳, 쓰이지 않던 물건·인연에 쓰이는 순간."""
import io,re,sys,os
C=sys.argv[1] if len(sys.argv)>1 else 'canon'
P=os.path.join(C,'Molak02.nani')
s=io.open(P,encoding='utf-8').read()
def rep(a,b,cnt=1):
    global s
    if b in s: return
    assert s.count(a)==cnt,(a[:60],s.count(a)); s=s.replace(a,b)
# --- 백초당: 지혈환·청혈산은 하루 한 번(같은 날 몇 번이고 사 먹던 구멍) / 후반의 자하단
rep('@choice "지혈환 (은자 3) — 상처를 다스린다" if:mn>=3&&taint<1 goto:#TownBC0',
    '@choice "지혈환 (은자 3) — 상처를 다스린다" if:mn>=3&&taint<1&&buyhx!=day goto:#TownBC0')
rep('@choice "청혈산 (은자 3) — 피를 맑게 한다" if:mn>=3 goto:#TownBC1',
    '@choice "청혈산 (은자 3) — 피를 맑게 한다" if:mn>=3&&buycx!=day goto:#TownBC1\n'
    '@choice "자하단(紫霞丹) (은자 22) — 사술의 뿌리를 깊이 누른다" if:mn>=22&&day>=36&&f_jaha<1 goto:#TownBC7')
rep('# TownBC0\n@fx mn:-3 hp:4\n@goto #TownB','# TownBC0\n@set buyhx=day\n@fx mn:-3 hp:4\n@goto #TownB')
rep('# TownBC1\n@fx mn:-3 curse:-4\n@goto #TownB','# TownBC1\n@set buycx=day\n@fx mn:-3 curse:-3\n"하루 한 첩이다. 두 첩이면 피가 묽어진다."\n@goto #TownB\n'
    '# TownBC7\n할멈이 약궤 맨 아래 칸에서 보라빛 환약 한 알을 꺼낸다. 손이 잠깐 멈춘다.\n'
    '"마지막 남은 거다. 청운이 망하던 해에 지어 둔 것이지. 그해엔 쓸 사람이 다 죽었다."\n'
    '삼키자 가슴의 문양이 얼음물에 덴 듯 오그라든다. 한참 숨을 쉬지 못한다.\n@vfx kind:curse\n@fx mn:-22 curse:-12\n@set f_jaha=1\n@goto #TownB')
# --- 대장간: 후반의 명검 손질(벼린 칼을 한 번 더)
rep('@choice "철편 덧댄 조끼를 산다 (은자 25) — 체력 상한 +2" if:mn>=25&&f_vest<1 goto:#TownMC5',
    '@choice "철편 덧댄 조끼를 산다 (은자 25) — 체력 상한 +2" if:mn>=25&&f_vest<1 goto:#TownMC5\n'
    '@choice "벼린 날을 다시 세운다 (은자 24) — 이번 생에 한 번, 검술 +2" if:mn>=24&&day>=30&&f_honed>0&&f_reforge<1 goto:#TownMC6')
rep('# TownMC2\n@goto #Town1','# TownMC6\n무고가 처음으로 망치를 내려놓는다. 날을 불빛에 비춰 보고, 오래 본다.\n'
    '"한 번 벼린 날을 또 세우면 칼이 얇아지오. 얇은 칼은 한 번은 무엇이든 벤다. 그다음은 장담 못 하오."\n'
    '이레 밤, 대장간 불이 꺼지지 않는다.\n@vfx kind:clash\n@fx mn:-24 sw:2\n@set f_reforge=1\n@goto #TownM\n# TownMC2\n@goto #Town1')
# --- 아침: 눈 오는 아침엔 솜옷이 몸을 지킨다 / 굶은 아침엔 다리 밑 솥
rep('@set wr=random(1,100)\n@scene id:snow if:day>=40&&wr<=30||day>=50&&wr<=45',
    '@set wr=random(1,100)\n@set wsnow=0\n@set wsnow=1 if:day>=40&&wr<=30||day>=50&&wr<=45\n@scene id:snow if:wsnow>0')
rep('@goto #MTaint if:taint>0\n@goto #MTh if:theme>0&&day>=thday',
    '@goto #MSnow if:wsnow>0&&f_frost<1\n@goto #MSnowF if:wsnow>0&&f_frost>0&&b_frost<1\n# MAfterSnow\n@goto #MGae if:gaefed>0&&gaefedday>0\n# MAfterGae\n'
    '@goto #MTaint if:taint>0\n@goto #MTh if:theme>0&&day>=thday')
rep('; --- 아침 풍경 ---','''; --- 눈·굶주림의 아침(0.4.12) ---
# MSnow
문틈으로 눈발이 든다. 홑옷 사이로 한기가 뼈까지 파고든다.
손가락이 굳어 검집을 두 번 놓친다.
@fx hp:-1
@goto #MAfterSnow
# MSnowF
@set b_frost=1
눈이 온다. 하진무가 말없이 두고 간 솜옷의 깃을 여민다.
한기가 살갗에서 멈춘다. 그 사내는 그때 이미 이 겨울을 알았던 것이다.
@goto #MAfterSnow
# MGae
@set gaefed=0
배를 곯은 아침, 다리 밑 솥에서 거지들이 국 한 그릇을 덜어 준다.
"밥 먹었소? 아직이오. 그럼 앉으시오."
@fx hp:1
@goto #MAfterGae
; --- 아침 풍경 ---''')
# --- 그물의 아침: 하오문의 빚은 귀띔이 되어 돌아온다 / 사공의 약속은 공짜 밤배
rep('@goto #BNetHint if:day>=52&&b_nethint<1&&b_netk<1&&b_capture<1',
    '@goto #BNetHint if:day>=52&&b_nethint<1&&b_netk<1&&b_capture<1||day>=48&&f_haoowe>0&&b_nethint<1&&b_netk<1&&b_capture<1')
rep('@choice "나루의 밤배를 산다 (은자 15)" if:mn>=15 goto:#BNetB',
    '@choice "나루의 밤배를 산다 (은자 15)" if:mn>=15&&f_ferryfree<1 goto:#BNetB\n@choice "사공을 찾아간다 — 빚진 배 한 번" if:f_ferryfree>0 goto:#BNetBF',2)
rep('# BNetB\n@fx mn:-15','# BNetBF\n늙은 사공이 삿대를 고쳐 쥔다. 묻지도 않는다.\n"말씀만 하시라 했잖소. 저 산 쪽 첫 손님이오. 타시오, 등불은 끄고."\n@set f_dojuroute=2\n@goto Molak06#Doju\n# BNetB\n@fx mn:-15')
io.open(P,'w',encoding='utf-8').write(s)
print('ok')
