# -*- coding: utf-8 -*-
# 삽화 v6 연결: 새 장면(외나무다리·약방 화재·암향루 지하·전당포 뒷방)과 소아 표정(미소·슬픔·노기)
import io, os, sys
D = os.path.join(os.path.dirname(__file__), '..', 'canon')
files = {}
def F(f):
    if f not in files: files[f] = io.open(os.path.join(D, f), encoding='utf-8').read()
    return files[f]
def rep(f, old, new, count=1):
    s = F(f); assert s.count(old) == count, (f, old[:40], s.count(old)); files[f] = s.replace(old, new)

if 'scene:duelbridge' in F('MolakDuel.nani'):
    print('already'); sys.exit(0)

rep('MolakDuel.nani', '# Sit2\n개울 위 외나무다리.', '# Sit2\n@art scene:duelbridge\n개울 위 외나무다리.')
rep('MolakEvents.nani', '# E_HerbBurn\n@page\n삼경. 약재 거리 쪽 하늘이 붉다.', '# E_HerbBurn\n@page\n@art scene:herbfire\n삼경. 약재 거리 쪽 하늘이 붉다.')
rep('Molak05.nani', '# Amh2FreeGo\n@page\n암향루 지하.', '# Amh2FreeGo\n@page\n@art scene:cellar\n암향루 지하.')
rep('Molak02.nani', '# NLoan\n전당포 뒷방.', '# NLoan\n@art scene:pawn\n전당포 뒷방.')
rep('Molak02.nani', '# NLedger\n', '# NLedger\n@art scene:pawn\n')
# 소아 표정
rep('Molak06.nani', '@art char:soa\n밤 수련을 마친 마당. 소아가 담 위에 앉아 술병을 흔든다.', '@art char:soa_smile\n밤 수련을 마친 마당. 소아가 담 위에 앉아 술병을 흔든다.')
rep('Molak06.nani', '@art char:soa\n남쪽으로 열흘.', '@art char:soa_smile\n남쪽으로 열흘.')
rep('Molak04.nani', '@art char:soa\n그리고, 객잔 지붕 위의 약속을 지켰다.', '@art char:soa_smile\n그리고, 객잔 지붕 위의 약속을 지켰다.')
rep('Molak04.nani', '소아가 술 석 잔을 따른다. 한 잔은 몸에. 한 잔은 검에. 한 잔은 먼저 간 자들에게.\n그리고 넷째 잔을', '@art char:soa_smile\n소아가 술 석 잔을 따른다. 한 잔은 몸에. 한 잔은 검에. 한 잔은 먼저 간 자들에게.\n그리고 넷째 잔을')
rep('Molak06.nani', '@art char:soa\n@vfx kind:rain\n비가 온다. 처마가 운다.', '@art char:soa_grief\n@vfx kind:rain\n비가 온다. 처마가 운다.')
rep('Molak06.nani', '@art char:soa\n밤. 방문을 열자 소아가 탁자에 앉아 있다. 탁자 위에 낡은 장부 한 권.', '@art char:soa_grief\n밤. 방문을 열자 소아가 탁자에 앉아 있다. 탁자 위에 낡은 장부 한 권.')
rep('Molak06.nani', '@art char:soa\n저자에서 돌아오는 소아를, 골목 셋이 막아선다.', '@art char:soa_fierce\n저자에서 돌아오는 소아를, 골목 셋이 막아선다.')
rep('Molak06.nani', '@art char:soa\n@goto #SoaN8Re', '@art char:soa_fierce\n@goto #SoaN8Re')
rep('Molak03.nani', '@art char:soa\n"…."\n싸늘한 기운이 목덜미를 훑는다.', '@art char:soa_fierce\n"…."\n싸늘한 기운이 목덜미를 훑는다.')
for f, t in files.items():
    io.open(os.path.join(D, f), 'w', encoding='utf-8').write(t)
print('patched', sorted(files))
