# ImeOverlay

마우스 커서 옆에 현재 한/영 IME 상태(및 CapsLock 상태)를 표시하는 Windows 트레이 앱입니다.

## 빌드 방법

```bash
cd ImeOverlay
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

빌드 결과물: `bin\Release\net8.0-windows\win-x64\publish\ImeOverlay.exe` (단일 실행 파일)

실행 중인 `ImeOverlay.exe`가 있으면 파일이 잠겨 빌드가 실패합니다. 트레이 아이콘에서 종료 후 다시 빌드하세요.

## 파일 구조

```
ImeOverlay/
├── ImeOverlay.csproj   프로젝트 파일 (버전, 매니페스트 설정 포함)
├── app.manifest        관리자 권한 자동 요청 매니페스트
├── app.ico             트레이/실행파일 아이콘
├── Program.cs          진입점
├── Win32.cs            공용 P/Invoke 선언
├── ImeMonitor.cs       IME 상태 폴링 + 한영/CapsLock 키 훅
├── OverlayForm.cs      마우스 추적 오버레이 배지 (페이드 인/아웃)
├── Settings.cs         설정 모델 + 레지스트리 저장/로드
├── SettingsForm.cs     다크 테마 환경설정 창 (사이드바 네비게이션)
├── ImeOverlayApp.cs    트레이 아이콘 + 전체 생명주기 관리
└── Updater.cs          GitHub Releases 기반 자동 업데이트 체크
```

## 핵심 동작

- 100~200ms 주기로 포그라운드 창의 IME 변환 상태(`WM_IME_CONTROL`)를 읽어 한글/영문 여부 판정
- `VK_CAPITAL` 키 상태로 캡스락 on/off 판정
- 한/영 키, CapsLock 키, 마우스 클릭(좌/우/중간) 중 하나가 발생하면 오버레이를 표시
- 표시 모드가 "클릭 시 표시"인 경우 설정된 시간 후 서서히 페이드아웃, "항상 표시"인 경우 상시 노출
- 오버레이는 `WS_EX_TRANSPARENT`(클릭 통과) + `WS_EX_TOOLWINDOW`(Alt+Tab 제외) + `WS_EX_NOACTIVATE`로 동작해 다른 프로그램 포커스에 영향 없음
- 관리자 권한으로 실행되어(`app.manifest`) Visual Studio 등 관리자 권한 프로그램 위에서도 오버레이가 동작

## 트레이 메뉴

- **환경설정**: 표시 설정 / 스타일 설정 (아래 참고)
- **업데이트 확인**: GitHub Releases에서 최신 버전 확인 후 다운로드
- **일시 정지 / 재개**: 오버레이 표시를 끄고 켬
- **종료**

좌클릭으로도 일시정지/재개 토글이 가능합니다.

## 환경설정 — 표시 설정

- **항상 표시** / **클릭·한영·CapsLock 시 표시 후 사라짐** 중 선택
- 클릭 모드 선택 시 표시 유지 시간(1~30초) 입력
- Windows 시작 시 자동 실행 (레지스트리 `Run` 키 등록/해제)

## 환경설정 — 스타일

- 최대 투명도 (10~100%, 슬라이더 + 직접 입력)
- 글자 크기 (6.0~30.0pt, 슬라이더 + 직접 입력)
- 한글 모드: 배경색 / 글자색 / 표시 글자(최대 5자, 기본 "한")
- 영문 모드: 배경색 / 글자색 / 소문자 표시 글자(기본 "a") / 대문자 표시 글자(기본 "A")
- 모든 변경 사항은 오른쪽 미리보기 패널에 실시간 반영
- **초기화** 버튼으로 모든 설정을 최초 기본값으로 복원 가능

설정은 레지스트리(`HKCU\Software\ImeOverlay`)에 저장되어 재실행해도 유지됩니다.

## 자동 업데이트

`Updater.cs`의 `REPO` 상수를 본인 GitHub 저장소(`사용자명/저장소명`)로 지정하면, 앱 실행 3초 후 백그라운드에서 최신 릴리스를 확인합니다.

GitHub Releases에 올릴 때:
1. 태그를 `v1.0.0` 형식으로 지정
2. Assets에 빌드된 `ImeOverlay.exe` 첨부
3. `.csproj`의 `<Version>` 값을 릴리스와 맞춰 갱신 후 재빌드

새 버전이 감지되면 다운로드 확인 창이 뜨고, 동의 시 새 exe로 자동 교체 후 재시작됩니다.

## 자동 시작 등록 (직접 등록 시)

```
레지스트리: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
값 이름: ImeOverlay
값 데이터: "C:\경로\ImeOverlay.exe"
```

환경설정의 "Windows 시작 시 자동 실행" 체크박스로도 동일하게 설정/해제됩니다.

## 배포 참고

- 코드 서명 인증서가 없으면 실행 시 Windows SmartScreen 경고가 나타날 수 있습니다. 개인/사내 배포에서는 무시해도 무방합니다.
- `app.manifest`에 의해 항상 관리자 권한으로 실행되므로, 실행 시 UAC 동의가 필요합니다.
