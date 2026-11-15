# 0.4.63 '노인' 이름 색 정리 — '노인'은 복면 노인의 색(청록)으로 칠해진다. 하진무·의원·촌장·장꾼 같은 다른 늙은이는 다른 말로.
import io, re
R = {  # (파일, 줄): 바꿀 말 — 그 줄의 '노인'(들·네 앞 제외)을 모두 바꾼다
 ('Molak01',925): '늙은이',
 ('Molak02',600): '영감', ('Molak02',2250): '영감', ('Molak02',2258): '영감', ('Molak02',2267): '영감', ('Molak02',2408): '곁의 늙은이',
 ('Molak03',493): '늙은이',
 ('Molak06',1169): '영감', ('Molak06',1220): '늙은이', ('Molak06',1241): '늙은이',
 ('Molak11',195): '늙은이',
 ('MolakEvents',620): '늙은 의원', ('MolakEvents',622): '늙은 의원', ('MolakEvents',642): '늙은 의원', ('MolakEvents',647): '늙은 의원',
 ('MolakEvents',1134): '늙은이', ('MolakEvents',1142): '늙은이', ('MolakEvents',1147): '늙은이', ('MolakEvents',1150): '늙은이',
 ('MolakEvents',1366): '늙은이', ('MolakEvents',1370): '늙은이', ('MolakEvents',1374): '늙은이', ('MolakEvents',1376): '늙은이',
 ('MolakEvents',1895): '짚신 영감', ('MolakEvents',1904): '영감', ('MolakEvents',1912): '영감', ('MolakEvents',1917): '늙은이', ('MolakEvents',1938): '영감',
 ('MolakEvents',2292): '늙은 표두',
 ('MolakEvents',2463): '늙은 의원', ('MolakEvents',2465): '늙은 의원', ('MolakEvents',2467): '늙은 의원', ('MolakEvents',2488): '늙은 의원', ('MolakEvents',2490): '늙은 의원', ('MolakEvents',2498): '늙은 의원', ('MolakEvents',2502): '늙은 의원',
 ('MolakEvents',3559): '영감', ('MolakEvents',4437): '영감', ('MolakEvents',4471): '영감',
 ('MolakEvents',4581): '영감', ('MolakEvents',4583): '영감', ('MolakEvents',4604): '영감', ('MolakEvents',4614): '하진무', ('MolakEvents',4632): '영감', ('MolakEvents',4636): '영감', ('MolakEvents',4650): '영감', ('MolakEvents',4656): '영감', ('MolakEvents',4661): '영감',
 ('MolakOver',378): '영감', ('MolakOver',501): '영감',
}
files = {}
for (f, n), w in R.items():
    if f not in files: files[f] = io.open(f + '.nani', encoding='utf-8').read().split('\n')
    L = files[f]; old = L[n-1]
    new = re.sub(r'노인(?![들네])', w, old)
    assert new != old, (f, n, old[:60])
    # 조사 맞춤: 받침 없는 '영감'·'늙은이' 등 → '노인이/은/을' 그대로 쓰면 '영감이'(받침 ㅁ) OK, '늙은이이' 방지
    new = new.replace('늙은이이 ', '늙은이가 ').replace('늙은이은 ', '늙은이는 ').replace('늙은이을 ', '늙은이를 ').replace('늙은이의', '늙은이의')
    new = new.replace('늙은이이.', '늙은이다.').replace('늙은이이다', '늙은이다')
    L[n-1] = new
for f, L in files.items(): io.open(f + '.nani', 'w', encoding='utf-8').write('\n'.join(L))
print('ok', len(R))
