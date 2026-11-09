# -*- coding: utf-8 -*-
# 사건 정의표 v2 — (id, slot, minDay, maxDay, cond, after, gap, weight)
#  slot : 'd' 낮(Daytime) / 'n' 밤(Evening=밤 늦게) / 'a' 아무 때
#  maxDay: 0 = 제한 없음
#  cond : '&'로 묶은 토큰. name(>0) / !name(<=0) / name>=N / name<N / name>N / name<=N / name==N
#  after: 선행 사건 id (seen_ 필요), gap: 선행 사건 후 최소 경과일
#  weight: 추첨 가중치 (기본 10)
EVENTS = [
    # ---- 서막·초반 단발 ----
    ('E_Yong',        'd', 2, 10,  None,                 None,           0, 30),
    ('E_Thugs',       'd', 3, 20,  None,                 None,           0, 14),
    ('E_Pilgrim',     'd', 6, 30,  None,                 None,           0, 10),
    ('E_Funeral',     'd', 7, 30,  None,                 None,           0,  8),
    ('E_Lostblade',   'd', 7, 30,  None,                 None,           0,  8),
    ('E_Rain',        'a', 3, 20,  None,                 None,           0,  8),
    ('E_Swordgrave',  'd', 10, 28, None,                 None,           0, 10),
    ('E_Cultists',    'd', 12, 26, None,                 None,           0, 12),
    ('E_Bloodtax',    'd', 13, 28, None,                 None,           0, 10),
    # ---- 조건부 단발 (기존) ----
    ('E_Hunter',      'd', 8, 0,   'f_wanted',           None,           0, 14),
    ('E_DokRevenge',  'd', 14, 0,  'f_dokad',            None,           0, 14),
    ('E_Rival',       'd', 16, 0,  'rank>0',             None,           0, 14),
    ('E_Soa2',        'd', 18, 0,  'f_soamet',           None,           0, 14),
    ('E_VillageNews', 'd', 29, 0,  'f_hmsaved',          None,           0, 14),
    ('E_Simma',       'a', 1, 0,   'sl>=7',              None,           0, 12),
    ('E_Dream',       'n', 6, 0,   'curse>=30',          None,           0, 10),
    ('E_Echoes',      'n', 20, 0,  'curse>=50',          None,           0, 10),
    # ---- 백초 할멈 ----
    ('E_Herb',        'd', 1, 12,  None,                 None,           0, 30),
    ('E_Herb2x',      'd', 7, 0,   None,                 'E_Herb',       4, 14),
    ('E_Herb4',       'd', 16, 0,  'curse>=35',          'E_Herb2x',     5, 14),
    ('E_Herb5',       'd', 30, 0,  'curse>=55',          'E_Herb4',      6, 16),
    # ---- 행상 ----
    ('E_Peddler',     'd', 4, 14,  None,                 None,           0, 12),
    ('E_Peddler3',    'd', 12, 0,  None,                 'E_Peddler',    5, 12),
    ('E_Peddler4',    'd', 24, 0,  None,                 'E_Peddler3',   6, 12),
    # ---- 비무꾼 ----
    ('E_Duelist',     'd', 2, 14,  None,                 None,           0, 12),
    ('E_Duelist2',    'd', 10, 0,  None,                 'E_Duelist',    5, 12),
    ('E_Duelist3',    'd', 22, 0,  'rank>0',             'E_Duelist2',   6, 12),
    # ---- 낭인 마충 ----
    ('E_Spar',        'd', 5, 16,  None,                 None,           0, 12),
    ('E_Spar2',       'd', 13, 0,  None,                 'E_Spar',       5, 12),
    ('E_Spar3',       'd', 26, 0,  None,                 'E_Spar2',      7, 14),
    # ---- 표국 ----
    ('E_Guard',       'd', 8, 20,  None,                 None,           0, 12),
    ('E_Guard2',      'd', 18, 0,  None,                 'E_Guard',      5, 12),
    ('E_Guard3',      'd', 30, 0,  None,                 'E_Guard2',     6, 10),
    # ---- 투전판 ----
    ('E_Gamble',      'd', 3, 16,  None,                 None,           0, 10),
    ('E_Gamble3',     'n', 14, 0,  None,                 'E_Gamble',     5, 12),
    ('E_Gamble4',     'd', 26, 0,  None,                 'E_Gamble3',    6, 10),
    # ---- 야시 ----
    ('E_Nightmarket', 'n', 9, 20,  None,                 None,           0, 10),
    ('E_Nightmarket2','n', 16, 0,  None,                 'E_Nightmarket',5, 12),
    ('E_Nightmarket3','n', 28, 0,  None,                 'E_Nightmarket2',6, 10),
    # ---- 금침 노인 ----
    ('E_Acup',        'd', 5, 16,  None,                 None,           0, 12),
    ('E_Acup3',       'd', 18, 0,  'curse>=30',          'E_Acup',       6, 14),
    ('E_Acup4',       'd', 32, 0,  None,                 'E_Acup3',      7, 10),
    # ---- 부적 장수 ----
    ('E_Talisman',    'd', 6, 20,  None,                 None,           0,  8),
    ('E_Talisman2',   'd', 15, 0,  None,                 'E_Talisman',   6,  8),
    # ---- 산사 주지 ----
    ('E_Sutra',       'd', 6, 18,  None,                 None,           0, 10),
    ('E_Sutra2',      'd', 16, 0,  None,                 'E_Sutra',      5, 12),
    ('E_Sutra3',      'd', 30, 0,  None,                 'E_Sutra2',     7, 12),
    # ---- 혈마교도 ----
    ('E_Cultists2',   'd', 20, 0,  None,                 'E_Cultists',   5, 12),
    ('E_Cultists3',   'd', 30, 0,  None,                 'E_Cultists2',  6, 10),
    # ---- 현상금 사냥꾼 ----
    ('E_Hunter2',     'n', 18, 0,  'f_wanted',           'E_Hunter',     6, 12),
    ('E_Hunter3',     'd', 32, 0,  'f_wanted',           'E_Hunter2',    7, 12),
    # ---- 심마 ----
    ('E_Simma2',      'd', 12, 0,  'sl>=9',              'E_Simma',      5, 12),
    ('E_Simma3',      'n', 25, 0,  'sl>=11',             'E_Simma2',     6, 12),
    # ---- 꿈 ----
    ('E_Dream2',      'n', 20, 0,  'curse>=45',          'E_Dream',      6, 12),
    ('E_Dream3',      'n', 34, 0,  'curse>=60',          'E_Dream2',     6, 12),
    # ---- 용개 ----
    ('E_Yong3',       'd', 20, 0,  'f_yongspared&b_yong2', None,         0, 10),
    ('E_YongK2',      'd', 16, 0,  'f_yongkilled',       None,           0, 12),
    # ---- 설서인 ----
    ('E_Storyteller', 'd', 4, 18,  None,                 None,           0, 10),
    ('E_Storyteller2','d', 22, 0,  'rank>0',             'E_Storyteller',8, 12),
    # ---- 거지 중 ----
    ('E_Monk',        'd', 3, 14,  None,                 None,           0, 14),
    ('E_Monk3',       'd', 15, 0,  None,                 'E_Monk',       6, 12),
    ('E_Monk4',       'd', 28, 0,  None,                 'E_Monk3',      7, 10),
    # ---- 거지 ----
    ('E_Beggar',      'd', 2, 14,  None,                 None,           0, 10),
    ('E_Beggar3',     'd', 12, 0,  None,                 'E_Beggar',     5, 10),
    ('E_Beggar4',     'd', 24, 0,  None,                 'E_Beggar3',    6, 10),
    # ---- 세작 ----
    ('E_Spy',         'd', 5, 16,  None,                 None,           0, 10),
    ('E_Spy3',        'n', 18, 0,  None,                 'E_Spy',        6, 12),
    ('E_Spy4',        'd', 32, 0,  None,                 'E_Spy3',       7, 12),
    # ---- 방(榜) ----
    ('E_Notice',      'd', 4, 16,  None,                 None,           0,  8),
    ('E_Notice2',     'd', 18, 0,  'f_wanted',           'E_Notice',     6, 10),
    ('E_Notice3',     'd', 30, 0,  None,                 'E_Notice2',    6, 10),
    # ---- 비 ----
    ('E_Rain2',       'd', 14, 0,  None,                 'E_Rain',       6, 10),
    ('E_Rain3',       'n', 28, 0,  None,                 'E_Rain2',      6, 10),
    # ---- 나루 ----
    ('E_Ferry',       'd', 8, 22,  None,                 None,           0, 10),
    ('E_Ferry2',      'd', 24, 0,  None,                 'E_Ferry',      7, 10),
    ('E_Ferry3',      'd', 40, 0,  None,                 'E_Ferry2',     8, 12),
    # ---- 찻집 노파 ----
    ('E_Teaseller',   'd', 3, 16,  None,                 None,           0,  8),
    ('E_Teaseller2',  'd', 17, 0,  None,                 'E_Teaseller',  7,  8),
    # ---- 검총 ----
    ('E_Swordgrave2', 'd', 30, 0,  None,                 'E_Swordgrave', 8, 10),
    # ---- 아이들 ----
    ('E_Childplay',   'd', 5, 18,  None,                 None,           0, 10),
    ('E_Childplay2',  'd', 18, 0,  None,                 'E_Childplay',  6, 10),
    ('E_Childplay3',  'd', 32, 0,  None,                 'E_Childplay2', 8, 10),
    # ---- 징세조 ----
    ('E_Bloodtax2',   'd', 24, 0,  None,                 'E_Bloodtax',   6, 10),
    # ---- 메아리 ----
    ('E_Echoes2',     'd', 35, 0,  'curse>=65',          'E_Echoes',     6, 12),
    # ---- 냉천 ----
    ('E_Coldspring',  'd', 4, 20,  None,                 None,           0, 10),
    ('E_Coldspring2', 'd', 22, 0,  'curse>=40',          'E_Coldspring', 7, 10),
    # ---- 하진무 ----
    ('E_Hajin1',      'n', 7, 0,   None,                 None,           0, 12),
    ('E_Hajin3',      'n', 22, 0,  None,                 'E_Hajin1',     8, 10),
    # ---- 중·후반 단발 분위기 ----
    ('E_Eclipse',     'n', 20, 0,  None,                 None,           0,  8),
    ('E_Caravan',     'd', 15, 0,  None,                 None,           0,  8),
    ('E_Wine',        'n', 23, 0,  None,                 None,           0,  8),
    ('E_Frost',       'd', 33, 0,  None,                 None,           0,  8),
    ('E_Lantern',     'n', 27, 0,  None,                 None,           0,  8),
    ('E_Wolfhowl',    'n', 38, 0,  'f_wanted',           None,           0, 10),
    # ---- 종반(45일+) 단발 ----
    ('E_Emptytown',   'd', 46, 0,  None,                 None,           0, 12),
    ('E_Lastletter',  'n', 45, 0,  None,                 None,           0, 12),
    ('E_Mirror',      'n', 47, 0,  'curse>=60',          None,           0, 12),
    ('E_Swordsong',   'n', 50, 0,  None,                 None,           0, 12),
    ('E_Lastmarket',  'd', 52, 0,  None,                 None,           0, 12),
    ('E_Vigil',       'd', 55, 0,  None,                 None,           0, 12),
]

# 기존(이미 본문이 있는) 사건 — 본문을 새로 쓰지 않음
EXISTING = {'E_Yong','E_Herb','E_Thugs','E_Beggar','E_Monk','E_Notice','E_Duelist','E_Rain','E_Spy','E_Peddler',
            'E_Gamble','E_Spar','E_Storyteller','E_Funeral','E_Guard','E_Coldspring','E_Acup','E_Talisman','E_Sutra',
            'E_Cultists','E_Hunter','E_DokRevenge','E_Simma','E_Dream','E_Soa2','E_VillageNews','E_Pilgrim',
            'E_Swordgrave','E_Teaseller','E_Nightmarket','E_Childplay','E_Bloodtax','E_Lostblade','E_Ferry','E_Rival','E_Echoes'}

# 아침 풍경 — (minDay, maxDay(0=무제한), cond, text)
MORNING = [
    (1, 9, None,        u'운락객잔의 아침. 장작 냄새와 어제 비 냄새가 마당에 남아 있다.'),
    (1, 12, None,       u'국밥 끓는 냄새에 눈을 뜬다. 허기가—당신이 살아 있다는 증거처럼 군다.'),
    (1, 0, None,        u'하진무가 주판알을 튕기다 말고, 당신의 안색부터 살핀다. 일 년을 간호한 사람의 버릇이다.'),
    (1, 0, None,        u'멀리 북쪽 하늘이 유난히 검다. 혈마곡이 있는 방향이다.'),
    (3, 0, None,        u'우물가의 아낙들이 목소리를 낮춘다. 또 누가 서열전에서 죽었다는 이야기일 것이다.'),
    (8, 0, 'rank>0',    u'점소이가 마당을 쓸다 말고 당신에게 꾸벅, 고개를 숙인다. 어제보다 깊이. 소문이 도는 모양이다.'),
    (2, 0, None,        u'밤새 비가 다녀갔다. 처마 끝에서 떨어지는 물방울이 셈을 세듯 고르다.'),
    (4, 0, None,        u'관도 쪽에서 말발굽 소리. 붉은 끈의 전령이 북쪽으로 달려간다. 교의 소식은 늘 사람보다 빠르다.'),
    (1, 10, None,       u'창호지에 빛이 번진다. 일 년을 누워 보던 그 천장이 오늘은 조금 낮아 보인다. 몸이 자란 것이 아니라—마음이 일어선 것이다.'),
    (2, 14, None,       u'마당에서 점소이가 닭 모가지를 비튼다. 국밥집의 아침은 늘 작은 죽음으로 시작한다. 당신은 그것을 끝까지 본다.'),
    (5, 0, None,        u'객잔 앞 느티나무에 까치가 운다. 좋은 소식이 올 거라고 점소이가 말하지만—이 현에서 좋은 소식은 대개 남의 것이다.'),
    (6, 0, None,        u'아침 저자로 나가는 장사치들의 행렬. 그 틈에 붉은 끈 하나가 섞여 걷는다. 세는 사람이, 세어지고 있다.'),
    (9, 0, None,        u'가슴의 문양이 아침 햇빛 아래서는 희미하다. 낮에는 잠든 척하는 것이다. 밤이 되면 다시 제 할 일을 할 것이다.'),
    (10, 0, None,       u'거울 대신 세숫물에 얼굴을 비춘다. 일 년 전의 얼굴은 거기 없다. 수배서의 그림이 틀린 것이 아니라—당신이 바뀐 것이다.'),
    (12, 0, 'f_wanted', u'아침부터 객잔 앞을 서성이는 낯선 그림자. 하진무가 빗자루로 마당을 쓸며 슬쩍 그자를 밀어낸다. 빗자루질에도 솜씨가 있다.'),
    (12, 0, None,       u'시장통 개 짖는 소리가 평소보다 사납다. 밤새 누가 지나갔다는 뜻이다. 사람인지, 사람이 아닌 것인지는 모른다.'),
    (15, 0, None,       u'아침 안개가 짙다. 열 걸음 앞이 보이지 않는 날—검을 쥔 손이 먼저 깨어 있다.'),
    (15, 0, 'f_soamet', u'마당의 담 위에 쌍검 자국 같은 흠집이 새로 나 있다. 소아가 다녀갔거나—그녀를 흉내 내는 바람이 지나갔거나.'),
    (18, 0, None,       u'점소이가 조심스럽게 묻는다. "객, 북쪽에 정말 가실 겁니까." 대답 대신 국밥을 비운다. 그릇 바닥이 답이다.'),
    (20, 0, None,       u'아침부터 가슴이 미지근하다. 문양이 자라는 날은 몸이 먼저 안다. 당신은 그 미지근함을 세어 두는 버릇이 생겼다.'),
    (22, 0, 'rank>0',   u'낯선 무인 하나가 마당 건너편에서 당신의 서열 패를 흘끔거린다. 올라갈수록 등 뒤의 눈이 늘어난다. 그것이 서열의 값이다.'),
    (25, 0, None,       u'하진무가 방값 장부를 넘기다 당신 이름 칸에서 손을 멈춘다. 셈이 틀려서가 아니다. 날짜가—너무 많이 쌓여서다.'),
    (28, 0, None,       u'아침 바람이 차다. 계절이 바뀌고 있다. 당신에게 남은 계절이 몇 개나 되는지—세어보지 않기로 한다.'),
    (30, 0, None,       u'가슴의 문양이 쇄골 쪽으로 손가락 한 마디쯤 기어올랐다. 옷깃을 여민다. 숨기는 것이 아니라—보지 않으려는 것이다.'),
    (33, 0, None,       u'마당의 느티나무 잎이 절반쯤 졌다. 떨어진 잎을 세듯 남은 날을 센다. 세는 것을 그만둔 날이 진짜 끝일 것이다.'),
    (36, 0, None,       u'새벽에 누가 창에 돌을 던졌다. 창호지에 구멍 하나. 내다보니 아무도 없다. 교의 인사인지, 다른 누구의 것인지.'),
    (40, 0, None,       u'눈을 뜨자 손이 검자루를 쥐고 있다. 잠결에 쥔 것이다. 몸이 먼저 전쟁을 시작했다.'),
    (45, 0, None,       u'하진무가 국밥에 고기를 한 점 더 얹는다. 말은 없다. 셈 밝은 영감이 셈을 틀리는 날은—무슨 뜻인지, 서로 안다.'),
]

# 밤 서두 — (minDay, maxDay, cond, text)
NIGHTFLAVOR = [
    (1, 0, None,        u'해가 저문다. 온전히 당신의 것인 반시진이 남았다.'),
    (1, 0, None,        u'등불이 하나둘 켜진다. 하루의 셈이 끝나고—이제 당신의 반시진이다.'),
    (1, 0, None,        u'객잔의 소음이 잦아든다. 몸은 피곤을 말하고, 가슴의 문양은 조용히 저 할 일을 한다. 반시진—어디에 쓸 것인가.'),
    (1, 0, None,        u'창밖에 달이 올랐다. 그날 밤의 달보다 희다. 남은 반시진을 손에 쥔다.'),
    (3, 0, None,        u'아래층에서 술꾼들의 노랫소리. 강호의 밤은 늘 누군가의 잔치고 누군가의 초상이다. 당신의 반시진이 남았다.'),
    (6, 0, None,        u'점소이가 등잔 기름을 채워 간다. "객, 오늘도 늦으십니까." 늦는다. 매일 늦는다. 반시진은 그러라고 있는 것이다.'),
    (10, 0, None,       u'밤바람에 문풍지가 운다. 가슴의 문양도 같은 박자로 운다. 둘 다 못 들은 척하고—반시진을 어디에 쓸지 정한다.'),
    (14, 0, None,       u'하진무가 셈을 끝내고 주판을 덮는 소리. 하루가 닫히는 소리다. 당신의 하루는 아직 반시진 남았다.'),
    (18, 0, 'f_wanted', u'골목 어귀에 등불 하나가 오래 서 있다. 누군가 객잔을 지켜보는 밤. 그래도 반시진은 당신의 것이다.'),
    (20, 0, None,       u'밤이 길어졌다. 계절 탓인지, 당신 탓인지. 반시진이 전보다 무겁다.'),
    (26, 0, None,       u'달이 붉다. 그날 밤과 같은 색. 당신은 창을 등지고 앉는다. 반시진—등 뒤의 달은 잊기로 한다.'),
    (32, 0, None,       u'문양이 밤이면 뜨겁다. 손바닥을 얹으면 심장이 둘인 것 같다. 둘 중 하나의 것인 반시진을, 당신 것으로 쓴다.'),
    (38, 0, None,       u'객잔 아래층이 조용하다. 손님이 줄었다. 당신 때문이라는 걸 하진무는 말하지 않는다. 반시진이 남았다.'),
    (45, 0, None,       u'밤마다 가슴의 글자를 센다. 오늘도 하나 늘었다. 늘어난 만큼 검을 쥔다. 반시진.'),
]
