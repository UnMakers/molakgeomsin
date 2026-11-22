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
- **잘림 막기(언메이커: 좌우 팔·위 머리가 잘리지 않게, 2026-11-21)** — 뽑을 때마다 지킨다.
  - 프롬프트에 붙인다: `full upper body in frame, head fully visible with space above, both arms and sleeves fully inside the frame, centered, margin around the figure, narrow pose`. 팔을 크게 벌리는 자세(노여움·멱살·창 들기)는 `arms close to body` 또는 `arms raised but inside frame`으로 좁힌다.
  - 배제어에 붙인다: `cropped, cut off, out of frame, head out of frame, cropped arms, cropped hands, cropped weapon, close-up, wide pose`.
  - 뽑은 뒤 `python3 tools/art/edge_check.py 그림.png`: 인물이 위 24px 안(머리), 좌우 16px 안(팔·소매·무기)에 닿으면 「잘림」. 아래는 허벅지에서 끊는 구도라 봐주지 않는다. 잘림이면 쓰지 않고 다시 뽑는다(다섯 번까지). 다섯 번 다 잘리면 그 그림은 넘기고 목록에 적는다.
  - 2026-11-21 v19 상황 그림 11장을 검사하니 일곱 장이 잘렸다(곽산 숙임·하진무 무릎·호치 무릎·쥐눈이 장 겁·진백 손 뻗음·교주 노여움·소아 피투성이).
- **이미 쓰는 그림이 잘렸을 때는 새로 뽑지 않고 바깥을 그린다(0.4.93)**: 그림을 판 안에 82%쯤으로 앉히고, 잘린 가장자리(좌·우·위 90px 띠 안에 인물이 닿은 자리)와 그 바깥만 가림판으로 열어 다시 그린다(`VNArt/mcp-workflows/_edit_outpaint.json` — VAEEncode → SetLatentNoiseMask → 변화 0.9). 얼굴·몸은 그대로라 같은 사람이 남는다. 잠깐 edit.json 자리에 끼우고, 끝나면 표정 작업 흐름(_edit_expression.json)으로 되돌린다. 표정만 다른 짝 그림은 새 바탕에 원래 얼굴(달라진 화소)을 다시 앉힌다. 뒤에 post_v21(회색+붉은 점) → fit_portrait → edge_check. 검은 머리칼 사이 흰 틈이 남으면 투명 바탕에서 흰 화소로만 번져 지운다.
  - **바깥을 그린 뒤 크기를 되돌린다(언메이커: 참새·소아가 너무 내려갔다)**: 판 안에 82%로 앉혀 그렸으니 그대로 두면 인물이 작아지고 아래로 처진다(게임은 그림 높이로 키를 맞춘다). 기준: 정수리 여백을 다른 입상과 같은 40px로 — 인물을 잘라 키운다. 좌우 여백 24px은 극장판에 보이는 위 70%에서만 지킨다(허벅지 쪽은 창 밖). 도끼·흩날린 머리칼처럼 위쪽이 넓은 그림(청묘·진백 멱살·노인 맨얼굴)은 좌우에 걸려 110~130px에서 멈춘다.
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
- **그림 로라(2026-11-23, 소아 시험)**: RTX 2060 6GB 에서 된다(wag 로라와 같은 도구). `C:\Users\sjf1t\wag-train\sd-scripts` + `venv`, `sdxl_train_network.py`, 바탕 animagine-xl-4.0-opt. 아끼는 설정: `--fp8_base --gradient_checkpointing --sdpa --cache_latents(_to_disk) --cache_text_encoder_outputs --network_train_unet_only --optimizer_type AdamW8bit --train_batch_size 1 --mixed_precision fp16`, 896 해상도·버킷.
  - 실행 파일 `Build\lora\soa\train.bat` — 맨 앞에 `set PYTHONIOENCODING=utf-8` / `set PYTHONUTF8=1` 이 있어야 한다(없으면 진행 줄의 일본어 한자에서 cp949 오류로 멈춘다). 처음 3~4분은 모델을 읽고 fp8로 줄이느라 CPU만 바쁘다.
  - 소아: 초상 5장 × 전신·상반신·얼굴 = 15장(좌우 뒤집기 금지 — 안대), 부름말 `molaksoa`, dim 16 / alpha 8, lr 1e-4, 반복 10 × 8회 = 1200걸음, 걸음당 2.5~3.3초(약 55분). 결과 `ComfyUI-Shared\models\loras\molak_soa_v1(-00000N).safetensors`. 쓸 때 LoraLoader 세기 0.9.
  - 시험(밤 지붕·술잔): 로라를 끼우면 안대 쪽·얼굴형·붉은 머리끈이 초상과 맞게 고정되고, 없으면 얼굴과 머리 모양이 그림마다 흔들린다(`Build\lora\soa\test`).
- **배경음 기준 결(2026-11-22 언메이커: 「결전후보4 피아노와현은 확실히 괜찮다, 앞으로도 이 느낌을 참고」)**: 지금 결전_1(`boss_00`). 피아노가 선율을 끌고 옥타브 베이스가 몰아치며 현악이 받쳐 부풀고 북은 낮게, 동양 악기는 한 줌. 악기를 많이 쌓으면 「AI 같고 거칠다」(옛 boss_00·duel_00 — 합창·이중 킥·찌그러진 기타·브라스·바이올린·태고·얼후를 한꺼번에).
  - 태그(그대로 출발점): `piano led battle theme, fast heroic piano melody with driving octave bass, light string section swells, subtle low drums, urgent and determined, dramatic, a hint of guzheng plucks, clear arrangement, clean studio recording, instrumental, no vocals` · bpm 150~160 · D/E minor · 105초 · temperature 0.8 · seed 4301(결전_1).
  - 다음 후보는 이 곡을 본으로 한 변주(소리→소리)부터 시도한다. 고르는 잣대: 8kHz 위 비·스펙트럼 평탄도(잡음기)·5초 단위 소리 크기(중간에 끊기는 곡 거름) — 망한 곡(옛 duel_00)이 평탄도 0.65로 가장 높았다.
  - 서장 불길(`fire_00`, seed 6301, bpm 84 C minor): `tragic piano and strings, a burning home at night, powerless and helpless, watching everyone die and unable to do anything, fragile trembling piano at first, grief swelling into rage, furious pounding piano octaves, violent string tremolo, crying cello lament, heavy drums like a racing heartbeat, anguish, fury, despair, a hint of erhu crying, …` — 장면의 감정(무력함·분노)을 태그에 그대로 적은 것이 먹혔다. 원본에 깨짐이 조금 있다(0.3%).
  - 그나마 쓸 만했던 관현악 록: 비무_1(`duel_00`, seed 3201, `fast martial arts duel theme, orchestral rock, tight live drums, driving cello and violin ostinato, sharp brass stabs, crisp electric guitar riff, short dizi bamboo flute accents, …`).
- 넣기: `tools/audio_post.py bgm`(RMS −18, 앞뒤 페이드, ogg q4) → `Resources/MolakBgm/<묶음>_NN.ogg`. 묶음이 없으면 가까운 묶음으로 물러난다(MolakBgm). 시험용 `bgm_*`는 새 곡이 들어가면 뺀다.

## 7. 점검 도구
- `MolakProbe.Run(대본, 라벨, 변수, 선택)` / `RunMany("대본,라벨[,변수[,선택]]§…")` — 플레이 모드에서. `Status`·`Log`(=== 작업, ERR 오류, → 넘어감).
- 화면 찍기: `ScreenCapture.CaptureScreenshot("Build/x.png")` 뒤 내려받아 본다.
- 빌드 전: 유니티 파일과 저장소를 MD5로 맞대 본다(MolakAutoLife는 BOM만 다르다).
