# 몰락검신 — 아이폰(iOS) 빌드

맥도, 애플 개발자 계정도 없이 내 아이폰에 설치해 보는 길과, 나중에 정식으로 내는 길을 적어 둔다.

## 1. 윈도우에서 Xcode 프로젝트 뽑기 (준비 완료)
- 유니티 6000.3.22f1 에 **iOS Build Support** 모듈 설치됨(2026-11-13).
- `MolakBuild.IOS("버전", 빌드번호)` → `Build\iOS\v버전\` 에 Xcode 프로젝트, `Build\iOS\몰락검신_v버전_iOS_Xcode.zip` 으로 묶음. 결과는 `Build\ios_build_result.txt`.
- 설정(빌드 때마다 다시 맞춘다): 번들 ID `com.unmakers.molakgeomsin`, 최소 iOS 15.0, 아이폰·아이패드, ARM64, IL2CPP, 세로 고정, 상태바 숨김, 자동 서명(팀 ID는 비워 둠), `ITSAppUsesNonExemptEncryption=false`(제출 때 암호화 질문 면제).

## 2. 맥 없이 .ipa 만들기 — 깃허브 Actions(무료, 공개 저장소)
워크플로: `.github/workflows/ios-ipa.yml`
1. Xcode 프로젝트 zip 을 400MB 조각(`xcode.zip.000`, `.001` …)으로 나눈다.
2. 저장소에 `ios-0.4.41` 같은 태그로 **릴리스**를 만들고 조각을 첨부해 게시한다.
3. 게시되면 깃허브의 macOS 서버가 서명 없이 빌드해 같은 릴리스에 `molakgeomsin-unsigned.ipa` 를 붙인다(20~40분). 실패하면 `xcodebuild.log` 가 붙는다.
- **주의: 저장소가 공개라 릴리스 첨부 파일도 누구나 내려받을 수 있다.** 비공개로 두려면 저장소를 비공개로 돌리거나(그러면 macOS 빌드 분이 유료 한도에서 깎인다), 다른 비공개 저장소를 쓴다.

## 3. 아이폰에 설치 — 윈도우 + 무료 애플 ID
- 윈도우용 **Sideloadly**(또는 AltStore)를 설치하고, 아이폰을 USB로 연결한 뒤 `.ipa` 를 끌어다 놓는다.
- 애플 ID·비밀번호는 언메이커님이 직접 입력한다(무료 계정이면 앱이 **7일** 뒤 만료 → 다시 설치하면 저장은 남는다).
- 아이폰: 설정 → 일반 → VPN 및 기기 관리에서 내 애플 ID 를 '신뢰', iOS 16 이상은 설정 → 개인정보 보호 및 보안 → **개발자 모드** 켜기.

## 4. 정식 출시(나중에)
- **Apple Developer Program(연 99달러)** 가입 → 배포 인증서·프로비저닝 프로파일 → 테스트플라이트·앱스토어.
- 맥이 없으면 Codemagic·Unity Build Automation 같은 클라우드 맥 빌드에 인증서를 등록해 서명까지 맡긴다.
- 앱스토어 심사 대비: 개인정보 처리방침 URL, 연령 등급, 생성형 AI 고지는 안드로이드 출시 준비(RELEASE_PREP.md)와 같다.
