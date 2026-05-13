# WinUI 3 마이그레이션 리서치

## 목적

현재 Windows Easy Emoji는 WPF 앱이다. 이 문서는 지금 단계에서 WinUI 3로 마이그레이션할 때의 회귀 위험, 작업 시퀀스, 기술 허들을 조사한 결과다.

결론부터 말하면, 지금 당장 전체 마이그레이션은 추천하지 않는다. WinUI 3는 최신 Windows UI 방향에 더 가깝지만, 우리 앱의 핵심 리스크는 예쁜 화면보다 전역 단축키, 트레이 상주, 오버레이 위치, 원래 입력창 focus 복원, 붙여넣기 안정성이다. 이 영역은 UI 프레임워크 전환 시 회귀 가능성이 크다.

## 공식 문서 기준 요약

Microsoft는 새 Windows-only 앱에는 WinUI를 권장한다. WinUI는 Windows App SDK에 포함된 최신 native UI framework다.

하지만 기존 WPF 앱을 반드시 WinUI로 옮기라는 의미는 아니다. Microsoft FAQ는 WPF가 deprecated가 아니고, mature/stable framework라고 설명한다. 또한 Windows App SDK의 일부 기능은 UI 프레임워크를 바꾸지 않고 WPF 앱에서도 사용할 수 있다.

즉 선택지는 두 가지다.

1. WPF를 유지하고 필요한 Windows App SDK 기능만 점진 도입한다.
2. UI 프레임워크 자체를 WinUI 3로 전환한다.

현재 프로젝트에는 1번이 더 안전하다.

## 현재 프로젝트의 WPF 결합 지점

### App 레이어

- `src/WindowsEasyEmoji.App/App.xaml`
- `src/WindowsEasyEmoji.App/App.xaml.cs`
- `src/WindowsEasyEmoji.App/MainWindow.xaml`
- `src/WindowsEasyEmoji.App/MainWindow.xaml.cs`
- `src/WindowsEasyEmoji.App/SettingsWindow.xaml`
- `src/WindowsEasyEmoji.App/SettingsWindow.xaml.cs`
- `src/WindowsEasyEmoji.App/Tray/TrayAppHost.cs`

이 영역은 WinUI 전환 시 대부분 다시 작성해야 한다.

### WPF에 강하게 묶인 기능

- `System.Windows.Application`
- `System.Windows.Window`
- `System.Windows.Controls.*`
- WPF `ListBox`, `WrapPanel`, `DataTemplate`, `Style`, `ControlTemplate`
- `Topmost`, `WindowStyle=None`, `WindowStartupLocation=Manual`
- `WindowInteropHelper`
- `HwndSource`
- WPF dispatcher와 `Dispatcher.BeginInvoke`
- WPF device-independent pixel 변환

### WPF와 직접 관련은 작지만 회귀 위험이 큰 기능

- `System.Windows.Forms.NotifyIcon` 기반 tray icon
- `WH_KEYBOARD_LL` keyboard hook
- fallback hotkey
- foreground window 기억
- 원래 입력창 focus 복원
- clipboard write/restore
- `Ctrl+V` SendInput
- caret/window/cursor 기반 overlay anchor 계산
- self-hosted Windows VM UI E2E

이 기능들은 WPF UI와 붙어 있기 때문에 App 레이어 전환 시 함께 흔들릴 가능성이 높다.

## 회귀 위험 분석

### 1. 전역 단축키와 Win+. 대체

위험도: 높음.

현재 앱은 WPF message loop 위에서 keyboard hook과 hotkey service를 사용한다. WinUI 3도 Win32 interop가 가능하지만 앱 초기화, window activation, dispatcher 흐름이 달라진다.

회귀 가능성:

- `Win + .`를 intercept하지 못함.
- fallback hotkey가 등록되지 않음.
- hook은 동작하지만 overlay show 호출이 UI thread로 안전하게 전달되지 않음.
- Windows 기본 emoji panel과 우리 overlay가 동시에 뜸.

필수 검증:

- VM에서 `Win + .`로 overlay가 뜨는지.
- fallback hotkey가 뜨는지.
- 설정에서 Win+. 대체를 끄면 hook이 빠지는지.

### 2. 트레이 앱 상주

위험도: 중간~높음.

현재는 Windows Forms `NotifyIcon`을 WPF 앱에서 사용한다. WinUI 3에서도 WinForms interop 또는 Win32 tray API를 쓸 수 있지만, 앱 lifetime과 shutdown 모델을 다시 맞춰야 한다.

회귀 가능성:

- 창을 닫으면 앱이 종료됨.
- tray icon이 남거나 dispose되지 않음.
- 설정 메뉴 toggle 상태가 UI와 sync되지 않음.
- tray menu에서 overlay를 열 때 foreground target이 잘못 잡힘.

필수 검증:

- 앱 시작 후 tray icon이 살아 있음.
- tray 메뉴에서 설정과 overlay가 열림.
- 종료 메뉴로 프로세스가 종료됨.

### 3. 오버레이 창 표시와 위치 계산

위험도: 높음.

현재 WPF `Window`는 `Topmost`, borderless, manual 위치, device-independent 좌표 변환을 사용한다. WinUI 3는 `Microsoft.UI.Xaml.Window`와 `AppWindow`를 함께 다뤄야 한다. 공식 문서도 WinUI windowing은 XAML `Window`, Win32 `HWND`, `AppWindow` 조합으로 설명한다.

회귀 가능성:

- overlay가 foreground를 받지 못함.
- topmost가 기대처럼 동작하지 않음.
- 입력창 위/아래 adaptive placement가 DPI 차이로 틀어짐.
- AppWindow physical pixel과 XAML effective pixel 변환 실수.
- multi-monitor에서 work area 계산이 틀어짐.

필수 검증:

- 입력창이 화면 위쪽이면 overlay가 아래쪽에 뜸.
- 입력창이 화면 아래쪽이면 overlay가 위쪽에 뜸.
- overlay가 화면 밖으로 잘리지 않음.
- overlay가 keyboard focus를 받음.

### 4. 원래 입력창 focus 복원과 붙여넣기

위험도: 매우 높음.

이 앱의 제품 핵심은 검색 결과 선택 후 원래 입력 중이던 앱으로 붙여넣는 것이다. WPF에서 WinUI로 바꾸면 overlay activation, hide timing, dispatcher timing이 바뀔 수 있다.

회귀 가능성:

- Enter 후 overlay에 붙여넣기 됨.
- target window가 foreground로 돌아오지 않음.
- clipboard는 바뀌지만 `Ctrl+V`가 실패함.
- clipboard restore timing이 깨짐.
- 일부 앱에서만 붙여넣기가 됨.

필수 검증:

- VM E2E target에 선택 이모지가 붙여넣어짐.
- copy-only mode에서 target text가 비어 있음.
- clipboard restore가 원본으로 돌아감.

### 5. 검색 UI와 키보드 내비게이션

위험도: 중간.

검색 엔진은 `Core`에 있어 안전하다. 그러나 UI control이 WPF `ListBox + WrapPanel`에서 WinUI `GridView` 또는 `ItemsRepeater`로 바뀌면 selection, arrow navigation, scroll, item template 동작이 달라진다.

회귀 가능성:

- Enter가 selected item을 선택하지 않음.
- 방향키 이동 규칙이 달라짐.
- 빈 검색어/결과 없음 UI가 깨짐.
- favorite marker와 이미지 fallback 표시가 깨짐.

필수 검증:

- `하트`, `불`, `ㅋㅋ`, `ㄸㅂ` 검색 후 Enter 동작.
- 방향키 좌우상하 이동.
- 결과 없음 상태.
- favorite toggle.

### 6. 이모지 이미지 렌더링

위험도: 중간.

WinUI 3의 이미지 표시 자체는 가능하지만, 현재 WPF `ImageFailed` fallback과 binding 모델을 WinUI 방식으로 다시 옮겨야 한다.

회귀 가능성:

- Twemoji CDN image가 표시되지 않음.
- 실패 fallback text가 표시되지 않음.
- 이미지 로딩이 느려 UI가 비어 보임.

필수 검증:

- 대표 검색어에서 결과 타일이 컬러 이미지로 표시됨.
- 잘못된 이미지 URI에서 fallback이 표시됨.

### 7. 배포와 설치

위험도: 높음.

WPF는 현재 `dotnet publish` 기반 exe 배포가 단순하다. WinUI 3는 Windows App SDK deployment model을 결정해야 한다. 공식 문서 기준으로 framework-dependent, self-contained, packaged, unpackaged 선택지가 있으며 각각 장단점과 런타임 의존성이 있다.

회귀 가능성:

- 개발 PC에서는 실행되지만 사용자 PC에서 `Microsoft.ui.xaml.dll` 또는 Windows App SDK runtime 오류 발생.
- unpackaged 실행 시 bootstrapper 초기화 누락.
- self-contained 배포 크기가 커짐.
- MSIX/package identity 선택 때문에 설치/업데이트 방식이 바뀜.
- 기존 GitHub Actions publish/install workflow가 깨짐.

필수 검증:

- clean Windows VM에서 설치본 실행.
- self-hosted VM UI E2E가 설치본 대상으로 통과.
- GitHub release artifact 구조가 명확함.

## 작업 시퀀스

### Phase 0. 마이그레이션하지 않고 준비

목표: WPF를 유지하면서 나중에 UI 교체가 가능하도록 App 결합을 줄인다.

작업:

1. `MainWindow` code-behind에서 검색/선택/붙여넣기 orchestration을 view model 또는 presenter로 분리.
2. `TrayAppHost`에서 WPF 타입 의존을 줄이고 `IOverlayWindow` 인터페이스 추출.
3. overlay placement, target remembering, paste flow는 Platform/Core 테스트로 더 이동.
4. E2E 테스트에 시각/위치/포커스 검증을 계속 추가.

판단: 지금 당장 가장 추천하는 단계.

### Phase 1. WinUI 3 spike 프로젝트 생성

목표: 제품 코드를 옮기기 전에 개발/배포/VM 실행 가능성을 검증한다.

작업:

1. 별도 브랜치 또는 별도 `src/WindowsEasyEmoji.WinUI` 프로젝트 생성.
2. packaged/unpackaged 중 하나를 명시적으로 선택.
3. 빈 WinUI 창이 VM에서 빌드/실행되는지 확인.
4. tray icon과 app lifetime만 먼저 검증.
5. overlay topmost/manual positioning만 검증.

통과 기준:

- VM에서 설치/실행 가능.
- top-level window가 실제로 뜸.
- tray icon lifecycle이 안정적.

### Phase 2. Shell 이식

목표: 검색 기능 없이 앱 shell만 WinUI로 만든다.

작업:

1. App startup/lifetime 구현.
2. tray menu 구현.
3. shortcut event에서 overlay show까지 연결.
4. AppWindow/HWND interop로 topmost/manual placement 구현.
5. Settings window 또는 settings page 구현.

통과 기준:

- Win+. 또는 fallback hotkey로 overlay가 뜸.
- tray menu에서 overlay/settings/exit 동작.
- 위/아래 adaptive placement VM E2E 통과.

### Phase 3. 검색 UI 이식

목표: WPF 결과 그리드를 WinUI control로 교체한다.

작업:

1. `TextBox` 검색 입력.
2. `GridView` 또는 `ItemsRepeater` 결과 그리드.
3. selection, Enter, Escape, 방향키 내비게이션.
4. Twemoji image + text fallback.
5. no-results 상태.

통과 기준:

- 기존 검색 golden tests는 그대로 통과.
- UI E2E에서 대표 쿼리 paste 테스트 통과.

### Phase 4. 붙여넣기/foreground 복원 이식

목표: 제품 핵심 flow를 WinUI shell에서 검증한다.

작업:

1. target foreground window 저장.
2. overlay hide timing 조정.
3. target foreground 복원.
4. clipboard write/restore.
5. `Ctrl+V` SendInput.

통과 기준:

- VM UI E2E 전체 통과.
- copy-only, restore-clipboard, no-results, Escape flow 통과.

### Phase 5. 배포 경로 확정

목표: 사용자 배포 가능성을 확인한다.

작업:

1. framework-dependent vs self-contained 결정.
2. packaged/MSIX vs unpackaged 결정.
3. GitHub Actions publish artifact 업데이트.
4. clean Windows VM smoke test.
5. README 설치 가이드 업데이트.

통과 기준:

- fresh VM에서 설치/실행 가능.
- 기존 WPF release 방식 대비 사용자가 더 복잡해지지 않음.

## 주요 허들

### 허들 1. UI만 교체하는 migration이 아니다

WinUI 3는 XAML 문법이 비슷하지만 WPF와 타입/namespace/control behavior가 다르다. `System.Windows.*`가 `Microsoft.UI.Xaml.*`로 바뀌고, windowing도 WPF `Window`가 아니라 WinUI `Window + AppWindow + HWND interop` 조합이 된다.

### 허들 2. 배포 모델 결정이 필요하다

WinUI 3는 Windows App SDK runtime과 연결된다. framework-dependent는 배포 크기가 작지만 런타임 설치/servicing 이슈가 있고, self-contained는 xcopy 배포가 가능하지만 크기와 메모리 비용이 늘 수 있다. packaged/unpackaged 선택도 초기부터 결정해야 한다.

### 허들 3. AppWindow 좌표와 XAML 좌표 차이

공식 문서 기준 AppWindow는 physical device pixels를 사용하고, XAML은 effective pixels를 사용한다. 현재 overlay placement는 WPF device-independent 좌표 변환에 의존한다. WinUI 전환 시 DPI/multi-monitor 회귀 가능성이 크다.

### 허들 4. 트레이와 앱 lifetime

WinUI 3 자체는 일반 창 앱에 초점이 있다. 트레이 상주 유틸리티로 만들려면 Win32/WinForms interop 또는 별도 tray 구현이 필요하다. 창을 닫아도 앱이 남아 있어야 하는 lifetime 정책을 다시 설계해야 한다.

### 허들 5. VM E2E 재작성 비용

현재 VM E2E는 window title, foreground handle, visible window 탐색, 로그 기반 대기 조건을 사용한다. WinUI 전환 시 창 class/title/focus timing/log path가 달라져 테스트도 함께 수정해야 한다.

## 회귀 방지 체크리스트

WinUI 3 마이그레이션을 실제 시작한다면 다음 테스트가 전부 통과해야 한다.

- Build debug binaries.
- Run non-UI tests.
- Publish installed app.
- Verify interactive desktop before UI E2E.
- `WinPeriodShortcut_shows_search_overlay`.
- `Fallback_hotkey_shows_search_overlay`.
- `Enter_after_win_period_pastes_selected_emoji_into_previously_focused_window`.
- `Korean_alias_query_pastes_matching_emoji`.
- `Chosung_query_pastes_matching_emoji`.
- `Copy_only_mode_copies_selected_emoji_without_pasting_target`.
- `Restore_clipboard_after_paste_restores_original_clipboard`.
- `Overlay_opens_above_target_when_input_is_near_bottom`.
- `Overlay_opens_below_target_when_input_is_near_top`.
- `No_results_enter_does_not_paste_or_copy`.

## 지금 단계의 추천

지금은 WinUI 3 전체 마이그레이션을 시작하지 않는다.

대신 다음을 한다.

1. WPF 앱 유지.
2. Core/Platform/App 분리 강화.
3. `IOverlayWindow`, `ITrayHost`, `IAppDispatcher` 같은 경계 추출.
4. VM E2E를 더 강하게 만든다.
5. 이후 별도 branch에서 WinUI 3 spike를 1~2일 타임박스로 실행한다.

이렇게 하면 WinUI 3가 실제로 필요한지, 필요한 경우 어느 부분이 제일 위험한지 확인할 수 있다. 지금 바로 migration을 시작하면 검색 품질과 붙여넣기 안정성 개선 속도를 늦출 가능성이 크다.

## 참고 자료

- Migrate WPF app patterns to WinUI 3: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/wpf-patterns-winui3
- Windows developer FAQ: https://learn.microsoft.com/en-us/windows/apps/get-started/windows-developer-faq
- Modernize your desktop apps for Windows: https://learn.microsoft.com/windows/apps/desktop/modernize/
- Windowing overview for WinUI and Windows App SDK: https://learn.microsoft.com/en-us/windows/apps/develop/ui/windowing-overview
- Manage app windows: https://learn.microsoft.com/en-us/windows/apps/develop/ui/manage-app-windows
- Windows App SDK deployment overview: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/deploy-overview
