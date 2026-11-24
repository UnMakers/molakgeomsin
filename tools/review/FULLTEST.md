# 몰락검신 — 완전 테스트 절차와 결과

## 절차(칸)
| 칸 | 몰락검신 |
|---|---|
| 엔진, 프로젝트 경로, 인스턴스 | Unity 6000.3, `C:\Users\sjf1t\RuinedSword`, `set_active_instance RuinedSword@51c31f2e` (컴파일·플레이 전환 뒤 다시) |
| 건드리면 안 되는 프로젝트 | 같은 PC의 WhereAreYouGoing(wag) 등 다른 Unity 프로젝트 |
| 저장 파일 | 편집기 `Library/MolakSave/` (global.sav · auto.sav · quick.sav), 기기 `persistentDataPath/MolakSave/`. 시험 전 `Library/MolakSave_fulltest_bak`로 복사 |
| 균형 시험 함수 | `MolakSoakRunner.Matrix(int[] 정책, 생 수, 태그)` (플레이 중, `Engine.Turbo = true`). 결과 `Build/soak_results.csv`, 죽기 직전 12줄 `Build/soak_deaths.txt` |
| 봇이 하는 일 / 안 하는 일 | 모든 선택지·밤 행동·비무 합 카드·이름 짓기 / 손으로 하는 조작감(탭 타이밍, 끌기), 소리 |
| 솜씨 단계와 갈래 | 비주얼 노벨이라 솜씨 대신 정책: 0 균형 · 1 공격 · 2 신중 · 3 무작위 · 4 장사꾼 · 5 사술 · 6 학습(죽음을 부른 선택을 다음 생부터 피함) |
| 보는 숫자 | 사인 분포(비무 패배가 1위면 안 된다), 생존 일수, 첫 생 사망 시점, 말로 도달, 서열, 저주, 멈춤(STALL) |
| 다시 컴파일 없이 바꿀 수치 | MolakRealm 승산·자리 잡는 날, MolakTitle 달·물결, 이야기 모드 Boost |
| 데이터 | `Assets/Scenario/*.nani`(27), `Resources/MolakNani/*.txt`(빌드 때 동기화), `Resources/MolakArt`, `MolakBgm`, `MolakSfx` |
| 정적 점검 | `python3 tools/review/static_check.py`(이동·그림·변수·앎·생 수 말·쪽 넘김·짝·글꼴·기호·사건 길목) |
| 없앤 기능의 낱말 | 나니노벨, 「N일째」(날짜 숨김), 제1부·제2부, 「장부」(소아·하진무·왕삼 말고) |
| 화면 목록 | 타이틀(밤 바탕·짝 바탕 넷), 이름, 본문(극장판), 선택지, 비무, 경지의 벽, 메뉴·설정·말로록·원수록·강호록, 먹 화면, 말로, 회귀, 이야기 모드 |
| 흐름 | 타이틀 → 새 게임 → 서장 → 객잔 → 낮·밤 → 죽음 → 말로 → 회귀 → 저장·불러오기 → 이어하기 |
| 원칙 | ① 이야기가 이끈다(서울2033의 결, 육성은 뒤) ② 무협 정석에 맞는다 ③ 죽음은 비무 패배보다 선택의 결과에서 ④ 첫 생은 숫자로 일찍 죽는다(대본으로 죽이지 않는다) ⑤ 그림이 먼저, 글은 뒤 |
