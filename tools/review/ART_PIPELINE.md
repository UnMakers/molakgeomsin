# 몰락검신 — 그림·소리 파이프라인 (2026-11-20 기준)

예전 판(2026-11-06~10, 패치 파이프라인·옛 HUD 시절)은 git 이력에 있다. 지금 쓰는 방식만 적는다.

## 1. 기계
- 언메이커 PC: RTX 2060 6GB, RAM 32GB. ComfyUI(Comfy Desktop) 출력 `C:\Users\sjf1t\AppData\Local\Comfy-Desktop\ComfyUI-Shared\output`.
- 설치된 모델: animagine-xl-4.0-opt(그림), ace_step_1.5_turbo_aio(배경음), stable-audio-open-1.0(효과음·장면 소리). 새 모델은 내려받기 전에 허락을 받는다.
- ComfyUI는 끄지 않는다. 그래픽카드는 한 번에 한 가지 일만(배경음을 뽑는 동안 그림은 기다린다).
- 유니티에서 `execute_code` 로 ComfyUI HTTP(`/prompt` `/queue` `/history/<id>` `/system_stats`)를 부를 수 있다. 플레이 모드에 들어가거나 다시 컴파일하면 에디터 안의 대기열 러너가 끊기니 다시 건다.

## 2. 인물 입상
- 832×1216, 배경 투명, 흑백 먹그림에 붉은 포인트, 애니풍 얼굴, 허벅지 위까지. **해상도를 낮추지 않는다.**
- 생성: `generate_image`(투명 배경) → 원본 `VNArt/molak/vNN/char_*.png`.
- 후처리: `tools/post_art_v16.py 원본 결과`(흰 얼굴 비침·구멍 고침, 붉은색 밖의 색 빼기). 위 여백이 40px보다 적으면 60px로 채운다. 할멈처럼 붉은 눈만 남길 때는 `tools/art/face_desat.py`.
- 표정만 바꿀 때: 얼굴 둘레(400~420px)만 잘라 ControlNet 0.4로 붙잡고 62%만 다시 그린 뒤 둥근 가림판으로 붙인다(`VNArt/mcp-workflows/_edit_expression.json`, `tools/art/face_paste.py`). 몸·옷·머리는 원본 그대로라 같은 사람이 나온다.
- **자세가 바뀌는 상황 그림**(무릎·멱살·주저앉음·팔을 든 노여움 등): 원래 입상 프롬프트에 자세를 더해 새로 뽑는다. 이름은 `char_<사람>_<상황>`. 표정 그림처럼 이름 인장·자리 그림은 사람 이름(앞부분)에서 찾는다.
- 장마다 최대 다섯 번, 안 되면 다음으로. 장 사이 1분 30초 쉼. 1.5배 고해상 다시 그리기(edit) 방식은 쓰지 않는다.
- 프롬프트 함정: aura 같은 말은 물결처럼 깨지고, wings는 날개가 돋는다. 산적·광부 등은 왜색(카타나·오비) 배제어를 붙인다.
- 원본 보관: `unity-art-v16/`(v16 후처리분), `unity-art-v17/`(표정), `unity-art-v18/`(조연 13장).

## 3. 장면·마디 대표 그림
- 장면 1344×768, 먹 담채, 세피아 종이, 붉은 한 점.
- 대표 그림 `key_*`(12장, `unity-art-key/`): 섞여 나온 가짜 글씨는 지우고(낙관은 남김) `scene_innhall.png.meta`를 본떠 넣는다. 초점은 `MolakArt.KeyFocus`(가로)·`KeyFocusY`(세로), 양옆 종이빛은 `KeyEdge`. 폰·폴드·PC 세 비율로 잘림을 확인한다.

## 4. 유니티에 넣기
- 저장소에서 고친 뒤 새 `outputs/sNNN/` 폴더에 모아 `device_commit_files`로 보낸다: 코드 `Assets/Scripts/Molak/`, 대본 `Assets/Scenario/`, 그림 `Assets/Resources/MolakArt/`, 셰이더 `Assets/Shaders/`.
- **복사와 보내기를 한 번에 나란히 돌리지 않는다**(옛 파일이 남았다).
- 그림 .meta 는 기존 것을 본떠 guid 만 새로. 새 그림 이름은 `MolakArt/_index.txt`에 더한다(미리 읽기 목록).
- 보낸 뒤 `refresh_unity`(컴파일) → 콘솔 오류 확인.

## 5. 화면에서 그림이 하는 일(MolakArt)
- 극장판: 장면은 80%로 눌러 깔고 좌우를 먹으로 누른다. 인물은 창보다 조금 크게, 살짝 오른쪽. 정수리는 창 위 28단위 아래, 아랫변은 창 밖.
- 말하지 않는 인물: 흐림 없이 밝기 약 45%, 빛깔 절반 뺌(셰이더 `Molak/SpriteCine`의 `_Quiet`). 말하면 0.25초에 또렷해진다.
- 새 사람은 오른쪽에서 0.45초에 들어온다. 같은 사람의 표정만 바뀌면 제자리에서.
- 장면이 바뀌면 앞 인물을 바로 내린다. `@page`로 장면이 바뀌면 먹으로 0.2초 덮고 0.6초에 걷는다.
- 몸짓(입상 밀고 당기기)은 껐다(`GestureOn = false`). 상황은 그림으로.

## 6. 소리
- 효과음 31·장면 소리 10: Stable Audio Open 1.0(0.4.70).
- 배경음: ACE-Step 1.5 turbo, 묶음마다 세 곡. 작업 파일 `C:\Users\sjf1t\RuinedSword\Build\bgm2jobs\g_<묶음>_c{0,1,2}.json`, 결과 `Build\bgm2\`, 기록 `Build\bgm2\log.txt`. 곡당 약 5분, 사이 90초 쉼. 태그는 무협 게임 OST(관현악 + 동양 악기, 노래 없음)에 묶음별 분위기.
- 넣기: `tools/audio_post.py bgm`(RMS −18, 앞뒤 페이드, ogg q4) → `Resources/MolakBgm/<묶음>_NN.ogg`. 묶음이 없으면 가까운 묶음으로 물러난다(MolakBgm). 시험용 `bgm_*`는 새 곡이 들어가면 뺀다.

## 7. 점검 도구
- `MolakProbe.Run(대본, 라벨, 변수, 선택)` / `RunMany("대본,라벨[,변수[,선택]]§…")` — 플레이 모드에서. `Status`·`Log`(=== 작업, ERR 오류, → 넘어감).
- 화면 찍기: `ScreenCapture.CaptureScreenshot("Build/x.png")` 뒤 내려받아 본다.
- 빌드 전: 유니티 파일과 저장소를 MD5로 맞대 본다(MolakAutoLife는 BOM만 다르다).
