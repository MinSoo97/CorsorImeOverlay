# ImeOverlay

마우스 커서 옆에 현재 한/영 IME 상태를 표시하는 트레이 앱입니다.

## 빌드 방법

### 방법 1 — dotnet CLI (권장)

```bash
cd ImeOverlay
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

빌드 결과물: `bin\Release\net8.0-windows\win-x64\publish\ImeOverlay.exe`

### 방법 2 — Visual Studio

1. `ImeOverlay.csproj` 열기
2. 빌드 → 게시 → 폴더 → 단일 파일 체크 후 게시

## 파일 구조

```
ImeOverlay/
├── ImeOverlay.csproj   프로젝트 파일
├── Program.cs          진입점
├── Win32.cs            P/Invoke 선언
├── ImeMonitor.cs       키보드 훅 + IME 상태 읽기
├── OverlayForm.cs      투명 오버레이 폼
└── ImeOverlayApp.cs    트레이 아이콘 + 생명주기
```

## 사용법

- **실행**: ImeOverlay.exe 실행 → 트레이 아이콘 생성
- **오버레이**: 마우스 커서 우측 하단에 `한` / `영` 배지 표시
- **트레이 좌클릭**: 오버레이 표시/숨기기 토글
- **트레이 우클릭**: 일시 정지 / 종료

## 기술 사항

| 항목 | 내용 |
|------|------|
| 훅 방식 | `SetWindowsHookEx(WH_KEYBOARD_LL)` 전역 키보드 훅 |
| IME 감지 | `ImmGetConversionStatus` → `IME_CMODE_NATIVE(0x1)` 비트 확인 |
| 오버레이 | `WS_EX_TRANSPARENT` (클릭 통과) + `WS_EX_TOOLWINDOW` (Alt+Tab 제외) |
| 위치 추적 | 16ms 타이머 + `SetWindowPos(HWND_TOPMOST, SWP_NOACTIVATE)` |

## 자동 시작 등록 (선택)

```
레지스트리: HKCU\Software\Microsoft\Windows\CurrentVersion\Run
값 이름: ImeOverlay
값 데이터: "C:\경로\ImeOverlay.exe"
```
