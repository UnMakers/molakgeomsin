# -*- coding: utf-8 -*-
# 0.4.3 날짜 걷어내기: '열하루째 밤'·'예순 날' 같은 날짜 셈을 흐린 시간 표현으로 바꾸고,
#       하루 한 바퀴를 '며칠(사흘)' 한 바퀴로 묶는다(밤 행동 효과 ×3, 방값·사술은 사흘치).
import io, os, sys, re
D = os.path.join(os.path.dirname(__file__), '..', 'canon')
files = {}
def F(f):
    if f not in files: files[f] = io.open(os.path.join(D, f), encoding='utf-8').read()
    return files[f]
def rep(f, old, new, count=None):
    s = F(f); c = s.count(old)
    assert c >= 1 and (count is None or c == count), (f, old[:50], c)
    files[f] = s.replace(old, new)

if '@daytick n:3' in F('Molak02.nani'):
    print('already'); sys.exit(0)

# ---------- 1. 문장 속 날짜 ----------
R = [
('Molak02.nani', '쉰엿새째 아침. 성문 앞 포졸이', '아침. 성문 앞 포졸이'),
('Molak02.nani', '쉰이레째 아침. 이 아침을 안다.', '그물이 좁혀 오는 아침. 이 아침을 안다.'),
('Molak02.nani', '지난 생, 예순 날 아침에 거리가 비었다. 쉰여드레에 성문이 닫히고, 쉰아흐레 밤에 객잔 지붕에 사람이 오른다.', '지난 생, 성문이 먼저 닫혔다. 그 밤 객잔 지붕에 사람이 올랐고, 이튿날 아침 거리가 비었다.'),
('Molak02.nani', '서른여드레가 지나야 나루에 밤배가 다시 뜬다는 것도.', '늑대 울음이 잦아질 무렵에야 나루에 밤배가 다시 뜬다는 것도.'),
('Molak02.nani', '"집법전주가 쉰다섯째 날쯤 운락에 내려온다는 소문이오.', '"집법전주가 그물이 닫히기 전에 운락에 내려온다는 소문이오.'),
('Molak02.nani', '"예순 날이오. 관아 그물이 운락을 다 덮는 데 걸리는 날수. 그 전에 끝을 보든지, 성을 떠나든지."', '"두 달이 채 안 남았소. 관아 그물이 운락을 다 덮는 데 걸리는 시간이. 그 전에 끝을 보든지, 성을 떠나든지."'),
('Molak02.nani', '닷새째 밤. 하진무가 술상을 들고 올라온다.', '밤. 하진무가 술상을 들고 올라온다.'),
('Molak02.nani', '이레째 아침. 게시벽 앞에 탁자 하나가 놓였다.', '아침. 게시벽 앞에 탁자 하나가 놓였다.'),
('Molak02.nani', '서른아흐레째 아침. 객잔 담장 말뚝에', '아침. 객잔 담장 말뚝에'),
('Molak02.nani', '여드레째 아침. 게시벽 앞이 웅성거린다.', '아침. 게시벽 앞이 웅성거린다.'),
('Molak02.nani', '열하루째 밤. ', '밤. '),
('Molak02.nani', '열사흘째. ', '아침. '),
('Molak02.nani', '스무날째. 객잔이 수상하다.', '객잔이 수상하다.'),
('Molak02.nani', '스무날째. ', '아침. '),
('Molak02.nani', '서른이틀째 밤. ', '밤. '),
('Molak03.nani', '"운주 들판. 보름째. 거기서도', '"운주 들판. 보름날. 거기서도'),
('Molak03.nani', '보름째 되는 날, 무인들이 모인다.', '보름날, 무인들이 모인다.'),
('Molak03.nani', '"보름째다. 늦지 마라."', '"보름날이다. 늦지 마라."'),
('Molak03.nani', '운주 들판, 보름째 되는 날 무인들이 모인다.', '운주 들판, 보름날 무인들이 모인다.'),
('Molak03.nani', '운주 들판, 보름째 되는 날. 그 몸으로', '운주 들판, 보름날. 그 몸으로'),
('Molak03.nani', '사흘째 새벽. 마을 어귀에서', '새벽. 마을 어귀에서'),
('Molak03.nani', '보름째 날. 운주 들판.', '보름날. 운주 들판.'),
('Molak06.nani', '서른 날째 강가에서 문양이 목까지 올라왔다.', '한 달쯤 지난 강가에서 문양이 목까지 올라왔다.'),
('Molak06.nani', '마흔이틀이 아니다. 사흘 이르다.', '지난 생보다 사흘 이르다.'),
('Molak06.nani', '"혈랑대 정예는 관도 남쪽 길로 온다. 마흔닷새쯤.', '"혈랑대 정예는 관도 남쪽 길로 온다. 야습이 지나고 며칠 뒤.'),
('Molak06.nani', '마흔닷새째 밤. 관도 북쪽에서', '밤. 관도 북쪽에서'),
('Molak06.nani', '서른 날째 밤. 이름 모를 강가.', '한 달쯤 걸었다. 밤. 이름 모를 강가.'),
('Molak06.nani', '예순 날째 아침.', '그물이 닫히는 아침.'),
('Molak06.nani', '예순 날의 그물이 빈 객잔 위로', '관아의 그물이 빈 객잔 위로'),
('MolakEvents.nani', '"운주 들판, 보름째 날에 무인들이 모이오.', '"운주 들판, 보름날에 무인들이 모이오.'),
('MolakEvents.nani', '그리고 열하루째 밤 뒷문에 서 있던 사람.', '그리고 그 밤 뒷문에 서 있던 사람.'),
('MolakOver.nani', '옆구리의 상처가 열을 낸다. 사흘째 단내가 났다.', '옆구리의 상처가 열을 낸다. 며칠째 단내가 났다.'),
('MolakOver.nani', '몸이 먼저 기억한다 — 사흘째 국밥집의 칼을 한 번 되묻는다', '몸이 먼저 기억한다 — 국밥집의 칼을 한 번 되묻는다'),
('MolakOver.nani', '인연 · 쥐눈이 장 — 열하루째 밤에 팔리지 않는다', '인연 · 쥐눈이 장 — 뒷문의 그 밤에 팔리지 않는다'),
('Molak04.nani', '회귀 {g_runs}회. 마지막 생 {day}일. 그 전부가 이 하루에 닿았다.', '회귀 {g_runs}회. 그 생들의 아침과 밤 전부가 이 하루에 닿았다.'),
]
for f, a, b in R: rep(f, a, b)

# ---------- 2. 한 바퀴 = 며칠(사흘) ----------
s = F('Molak02.nani')
# 하루 마감 → 사흘 마감, 날짜 마디는 '지났으면'으로
rep('Molak02.nani', '# NightEnd\n@daytick\n', '# NightEnd\n@set nmul=0\n@daytick n:3\n', 1)
for d in ['10', '18', '25', '35', '50']:
    rep('Molak02.nani', '@goto #D%s if:day==%s\n' % (d, d), '@goto #D%s if:day>=%s&&ms_d%s<1\n' % (d, d, d), 1)
    rep('Molak02.nani', '# D%s\n' % d, '# D%s\n@set ms_d%s=1\n' % (d, d), 1)
rep('Molak02.nani', '하진무가 방값 패를 바꿔 단다. "교의 징세가 또 올랐소, 객. 내일부터 두 냥이오."', '하진무가 방값 패를 바꿔 단다. "교의 징세가 또 올랐소, 객. 이제부터 하룻밤 두 냥이오."', 1)
# 밤 행동(검·운기·날품·쉼): 며칠 밤치 효과
for L in ['NSw', 'NQi', 'NWork', 'NRest']:
    rep('Molak02.nani', '\n# %s\n' % L, '\n# %s\n@set nmul=3\n' % L, 1)
# 밤 풍경: 반시진 → 며칠 밤
NV = [
('해가 저문다. 온전히 당신의 것인 반시진이 남았다.', '해가 저문다. 며칠 밤이 온전히 당신의 것이다.'),
('반시진이 남았다. 전보다 한결 짧은 반시진이.', '며칠 밤이 남았다. 전보다 한결 짧은 밤들이.'),
('하루의 셈이 끝났다. 이제 당신의 반시진이다.', '셈이 끝났다. 이제 며칠 밤은 당신의 것이다.'),
('남은 반시진을 손에 쥔다.', '남은 며칠 밤을 손에 쥔다.'),
('아래층에서 술꾼들의 노랫소리가 올라온다. 반시진이 남았다.', '아래층에서 술꾼들의 노랫소리가 올라온다. 며칠 밤이 남았다.'),
('당신의 하루는 아직 반시진 남았다.', '당신의 밤은 며칠 더 남았다.'),
('반시진이 전보다 무겁다.', '밤들이 전보다 무겁다.'),
]
for a, b in NV: rep('Molak02.nani', a, b)
s = F('Molak02.nani')
files['Molak02.nani'] = re.sub(r'반시진', '며칠 밤', s.replace('; --- 밤: 반시진의 선택 ---', '; --- 밤: 며칠 밤의 선택 ---'))
# 낮: 며칠치 낮이니 사건을 두 번까지
rep('Molak02.nani', '# Daytime\n@pickevent chance:65 slot:day\n@gosub #RunEvent if:nextevent!="none"\n',
    '# Daytime\n@pickevent chance:85 slot:day\n@gosub #RunEvent if:nextevent!="none"\n@pickevent chance:40 slot:day\n@gosub #RunEvent if:nextevent!="none"\n', 1)
rep('Molak02.nani', '# Evening\n@art char:none\n@pickevent chance:40 slot:night\n', '# Evening\n@art char:none\n@pickevent chance:55 slot:night\n', 1)

for f, t in files.items():
    io.open(os.path.join(D, f), 'w', encoding='utf-8').write(t)
print('patched', sorted(files))
