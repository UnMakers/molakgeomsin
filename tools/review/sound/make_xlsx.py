from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter
F='Arial'
H=Font(name=F,bold=True,color='FFFFFF'); N=Font(name=F); B=Font(name=F,bold=True)
HF=PatternFill('solid',fgColor='3A2F27'); WARN=PatternFill('solid',fgColor='F6E3C6')
thin=Side(style='thin',color='CFC6B8'); BD=Border(top=thin,bottom=thin,left=thin,right=thin)
wrap=Alignment(wrap_text=True,vertical='top')
bgm=[('title','제목 화면','',3),('prologue','서장의 청운산장 앞마당·산문 (서장에서만)','',3),('day','그 밖의 낮 장면 전부 (기본)','',3),
('night','밤 장면, 지붕 달밤, 밤 장터, 계단, 우물','',3),('rain','비 장면, 홍수, 소아의 비 오는 밤','',3),
('tension','야영지, 고개, 운주 들판(계곡), 지하실, 폐허, 창고','',3),('fire','불타는 산장, 야습, 피 묻은 지붕','불길 효과가 켜진 동안은 장면이 바뀌어도 이 곡',3),
('duel','일반 비무','',4),('boss','교주전·결전 비무, 교주의 대전','대본이 직접 쥐는 자리 2곳(@bgm mood:boss)',3),
('soa','소아 그림이 떠 있는 동안','',3),('heroine','암향·청묘·참새 그림이 떠 있는 동안','',3),
('sorrow','상여, 불탄 사당, 돌함','대본이 쥐는 자리 1곳',3),('end','죽는 말로','',3),
('ending','살아남는 말로 (원행·동행·시천·도주·재건·무향·환속·무명·운락)','',3),
('true','진엔딩 (합벽·면향·운해·명가), 진엔딩 새벽','대본이 쥐는 자리 1곳',3)]
amb=[('amb_rain','비·홍수·비 오는 밤, 비 날씨',''),('amb_fire','불타는 산장·약방 불·야습, 불길 날씨',''),('amb_roof','야습 밤 지붕, 계단',''),
('amb_market','저잣거리·장날·고루·밤 장터·거지굴·마을',''),('amb_night','밤·지붕 달밤·우물·초막·야영지',''),('amb_river','나루·폭포',''),
('amb_temple','산사·사당·청운 앞마당·산문',''),('amb_tavern','객잔 홀·찻집',''),('amb_snow','고개, 눈 날씨',''),('amb_eaves','(쓰이는 곳 없음)','파일만 있음 — 정리 대상')]
sfx=[('조작','tap_brush','글 넘기는 탭',''),('조작','page_turn2','쪽이 넘어갈 때',''),('조작','page_turn','메뉴 확인 소리가 없을 때 대신',''),
('조작','ui_menu','메뉴·설정·안내 단추',''),('조작','ui_title','제목 화면 단추',''),('연출','chapter_pluck','마디 이름(장 제목)이 뜰 때',''),
('연출','bell_rise','마디 소리가 없을 때 대신, 경지·수치 알림',''),('연출','day_rooster','날이 밝을 때',''),('연출','gloss_paper','낱말 풀이 쪽지를 열 때',''),
('연출','autolife_breath','「지난 생의 길을 다시 밟는다」로 건너뛸 때','파일 없음 — 지금은 소리가 안 남'),
('인장','seal_stamp','선택지 인장, 이름 짓기, 날 넘김 도장',''),('인장','seal_good','좋은 결과 인장',''),('인장','seal_bad','나쁜 결과 인장',''),
('인장','seal_fork','갈림길 인장',''),('인장','seal_know','앎(회귀 기억) 인장',''),('인장','seal_skill','기술 인장',''),
('비무','sword_draw','칼 뽑기, 비무 들어갈 때',''),('비무','sword_clash','칼 부딪힘(가벼움)',''),('비무','sword_clash_heavy','칼 부딪힘(무거움)',''),
('비무','hit_drum','맞을 때 (진동 함께)',''),('비무','crowd_cheer','비무장에 들어설 때 구경꾼',''),('비무','card_place','합 카드를 놓을 때',''),
('비무','card_flip','합 카드를 뒤집을 때',''),('비무','momentum_drum','기세가 넘어올 때',''),('비무','win_gong','비무에서 이김',''),('비무','lose_drum','비무에서 짐',''),
('사술·죽음','heartbeat','먹 화면, 광증, 체력 위급',''),('사술·죽음','curse_breath','사술 문양이 숨 쉴 때, 먹 화면 일부',''),
('사술·죽음','frenzy_whisper','광증(사술 폭주 직전)',''),('사술·죽음','death_drum','죽음',''),('사술·죽음','wind_rewind','회귀(시간 되감기), 제목에서 새 생 시작',''),
('사술·죽음','true_bell','진엔딩, 기연','')]
wb=Workbook()
def sheet(ws,headers,rows,widths):
    ws.append(headers)
    for c in ws[1]: c.font=H; c.fill=HF; c.alignment=Alignment(vertical='center'); c.border=BD
    for r in rows: ws.append(list(r))
    for row in ws.iter_rows(min_row=2):
        warn=any(isinstance(c.value,str) and ('정리 대상' in c.value or '파일 없음' in c.value) for c in row)
        for c in row:
            c.font=N; c.alignment=wrap; c.border=BD
            if warn: c.fill=WARN
    for i,w in enumerate(widths,1): ws.column_dimensions[get_column_letter(i)].width=w
    ws.freeze_panes='A2'; ws.row_dimensions[1].height=20
ws=wb.active; ws.title='요약'
s1=wb.create_sheet('배경음'); sheet(s1,['묶음','곡 수','파일','나오는 상황','비고'],
  [(n,c,', '.join(f'{n}_{i:02d}.ogg' for i in range(c)),w,note) for n,w,note,c in bgm],[12,8,40,48,40])
s2=wb.create_sheet('앰비언스'); sheet(s2,['파일','깔리는 장면','비고'],[(f+'.ogg',w,n) for f,w,n in amb],[16,48,28])
s3=wb.create_sheet('효과음'); sheet(s3,['분류','파일','나오는 상황','비고'],sfx,[12,22,48,34])
ws['A1']='몰락검신 사운드 목록'; ws['A1'].font=Font(name=F,bold=True,size=14)
ws['A2']='PC 프로젝트(Assets/Resources/MolakBgm·MolakSfx) 기준, 2026-11-24'; ws['A2'].font=Font(name=F,color='6D665C')
rows=[('배경음 묶음 수',"=COUNTA('배경음'!A2:A100)"),('배경음 곡 수',"=SUM('배경음'!B2:B100)"),('앰비언스',"=COUNTA('앰비언스'!A2:A100)"),('효과음',"=COUNTA('효과음'!B2:B100)")]
for i,(k,f) in enumerate(rows,4):
    ws.cell(i,1,k).font=B; c=ws.cell(i,2,f); c.font=N
ws['A9']='주황색 칸: 정리가 필요한 항목(파일만 있고 안 쓰임 / 코드는 부르는데 파일 없음)'; ws['A9'].font=Font(name=F,color='8A5A12')
ws['A10']='배경음은 장면의 그림·인물을 보고 묶음을 고르고, 묶음 안 곡은 차례로 돈다. 묶음이 없으면 가까운 묶음으로 넘어간다(fire→tension, boss→duel, heroine→soa, rain·sorrow→night).'; ws['A10'].font=N; ws['A10'].alignment=wrap
ws.merge_cells('A10:E10'); ws.row_dimensions[10].height=45
ws.column_dimensions['A'].width=22; ws.column_dimensions['B'].width=12
wb.save('몰락검신_사운드목록.xlsx')
