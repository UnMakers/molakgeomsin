# 정본(canon) — 2026-11-10 윤문(정통무협 문체) 이후
이 폴더(그리고 리포지토리 unity-nani/)의 .nani 가 **정본**이다.
build_nani.js → patch1~11 생성 파이프라인은 윤문 이전 텍스트를 다시 만들어 내므로 **더 이상 돌리지 않는다**.
이후 수정은 정본 파일에 직접 하거나, 정본을 입력으로 하는 새 patchNN 스크립트로 한다.
- MolakEventDefs.cs(사건 조건표)는 evdefs.py 에서 계속 생성 가능(텍스트 무관). 단 patch8 의 다른 단계(본문 재생성)는 돌리지 말 것.
- 문체 기준: review/STYLE.md, 구조 검증: review/validate.py
