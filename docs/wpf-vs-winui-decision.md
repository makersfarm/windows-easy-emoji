# WPF와 WinUI 3 비교 및 프로젝트 선택 이유

## 목적

Windows Easy Emoji는 Windows에서 항상 떠 있는 트레이 유틸리티다. 전역 단축키로 검색 오버레이를 열고, 사용자가 고른 이모지를 원래 입력창에 붙여넣는 것이 핵심이다.

이 문서는 Windows 데스크톱 UI 프레임워크를 잘 모르는 사람을 위해 `WPF`와 `WinUI 3`의 개념, 차이, 사례, 그리고 이 프로젝트가 왜 WPF를 선택했는지 정리한다.

## 먼저 용어 정리

`.NET`은 런타임과 개발 플랫폼이다. C#으로 앱을 만들고 실행하게 해주는 기반이다.

`WPF`와 `WinUI 3`는 UI 프레임워크다. 즉 창, 버튼, 텍스트박스, 리스트, 레이아웃, 스타일, 애니메이션 같은 화면 계층을 만드는 도구다.

이 프로젝트의 질문은 ".NET을 쓸 것인가?"가 아니라 ".NET으로 Windows 데스크톱 앱을 만들 때 UI 프레임워크를 WPF로 할 것인가, WinUI 3로 할 것인가?"에 가깝다.

## WPF 개요

WPF는 Windows Presentation Foundation의 약자다. 오래된 Windows 데스크톱 UI 프레임워크이며, XAML로 UI를 선언하고 C# code-behind 또는 MVVM 구조로 동작을 붙인다.

Microsoft 문서상 WPF는 해상도 독립적이고 벡터 기반 렌더링 엔진을 사용하는 Windows 전용 UI 프레임워크다. XAML, 컨트롤, 데이터 바인딩, 레이아웃, 2D/3D 그래픽, 애니메이션, 스타일, 템플릿, 문서, 미디어, 텍스트/타이포그래피 기능을 제공한다.

WPF의 성격은 "성숙한 Windows 데스크톱 앱 프레임워크"다. 최신 Windows 11 디자인 언어를 가장 자연스럽게 얻는 도구는 아니지만, 오래된 만큼 자료가 많고, Win32/Windows Forms/트레이/전역 훅 같은 데스크톱 API와 섞어 쓰기 쉽다.

### WPF가 어울리는 사례

- 트레이 앱, 런처, 클립보드 도구, 단축키 유틸리티.
- 업무용 내부 도구, 관리자 도구, 데이터 입력 도구.
- Windows API와 깊게 붙는 데스크톱 앱.
- 빠르게 MVP를 만들고 안정적으로 배포해야 하는 앱.
- 오래 유지될 가능성이 높고, 화려한 최신 Windows 디자인보다 동작 안정성이 중요한 앱.

## WinUI 3 개요

WinUI 3는 Windows App SDK에 포함된 Microsoft의 현대적인 Windows 네이티브 UI 프레임워크다. Microsoft는 WinUI 3를 Windows 데스크톱 앱을 만들기 위한 modern native UI framework로 설명한다.

WinUI 3도 XAML 기반이다. 하지만 WPF의 `System.Windows.*` 계열이 아니라 `Microsoft.UI.Xaml.*` 계열을 사용한다. Windows 11 스타일, 최신 컨트롤, 현대적인 XAML 플랫폼과 더 가까운 것이 장점이다.

WinUI 3의 성격은 "새 Windows 앱을 현대적인 Windows UI로 만들기 위한 프레임워크"다. 장기적으로 Microsoft가 더 밀고 있는 방향에 가깝지만, WPF보다 생태계와 실전 샘플, 문제 해결 자료가 적고, 트레이 앱/전역 훅/오버레이 같은 전통적인 데스크톱 유틸리티 패턴에서는 추가 작업이 생기기 쉽다.

### WinUI 3가 어울리는 사례

- Windows 11 스타일을 적극적으로 따르는 새 데스크톱 앱.
- Microsoft Store 배포와 Windows App SDK 기능을 중심에 두는 앱.
- 최신 Windows UI 컨트롤과 Fluent Design이 중요한 앱.
- 일반적인 창 기반 생산성 앱, 미디어 앱, 소비자용 앱.
- 장기적으로 Windows App SDK 기능을 많이 쓸 계획이 있는 앱.

## 비교 분석

| 항목 | WPF | WinUI 3 |
| --- | --- | --- |
| 성격 | 성숙한 Windows 데스크톱 UI 프레임워크 | 최신 Windows App SDK 기반 UI 프레임워크 |
| UI 기술 | XAML + `System.Windows` | XAML + `Microsoft.UI.Xaml` |
| 운영체제 | Windows 전용 | Windows 10/11 대상, Windows App SDK 기반 |
| 안정성 | 오래 쓰였고 자료가 많음 | 최신 방향이지만 상대적으로 변화가 있음 |
| 디자인 | 기본 스타일은 오래된 느낌이 날 수 있음 | Windows 11/Fluent Design에 더 가까움 |
| 트레이 앱 | 구현 자료와 패턴이 많음 | 가능하지만 전통적 Win32 interop가 더 필요할 수 있음 |
| 전역 단축키/훅 | Win32 interop와 섞기 쉬움 | 가능하지만 앱 모델/윈도우 모델을 더 신경 써야 함 |
| 오버레이 창 | borderless/topmost window 구현이 단순 | 가능하지만 AppWindow/Window interop 고려 필요 |
| 배포 | exe/publish/MSIX 등 선택 폭이 넓고 단순 | Windows App SDK runtime, packaged/unpackaged 판단 필요 |
| 학습/자료 | 오래된 자료와 예제가 많음 | 최신 문서는 좋지만 실전 이슈 검색량은 WPF보다 적음 |
| 장기 방향성 | 레거시 성격이 있지만 유지됨 | Microsoft의 현대 Windows UI 방향에 더 가까움 |

## 이모지 앱 관점의 핵심 비교

이 프로젝트는 일반적인 "창 하나 띄우는 앱"이 아니다. 핵심은 OS 주변 기능에 붙는 유틸리티다.

필요한 기능은 다음과 같다.

- 시스템 트레이에 상주.
- `Win + .` 대체 또는 fallback hotkey 처리.
- 현재 foreground window 기억.
- 검색 오버레이를 topmost borderless window로 표시.
- 선택 시 원래 입력창으로 focus 복원.
- 클립보드에 이모지를 넣고 `Ctrl+V` 전송.
- 설정과 사용자 상태를 로컬 파일로 저장.
- 나중에 exe 또는 Microsoft Store 배포 가능성 유지.

이 요구사항에서는 최신 UI보다 Win32/데스크톱 interop 안정성이 더 중요하다. WPF는 이 지점에서 유리하다.

## 왜 이 프로젝트는 WPF를 쓰는가

### 1. MVP 속도가 중요하다

이 앱의 핵심 가치는 예쁜 창이 아니라 "한국어 이모지 검색이 잘 되고, 바로 붙여넣어진다"는 경험이다. WPF는 기본 창, 검색창, 리스트/그리드, 설정창, 트레이 메뉴를 빠르게 만들 수 있다.

WinUI 3로도 가능하지만, 프로젝트 초기에 Windows App SDK 앱 모델, 패키징, 트레이/오버레이 interop를 동시에 해결해야 한다. MVP에는 불필요한 리스크다.

### 2. 전역 훅과 트레이 앱 구조에 잘 맞는다

현재 앱은 `WH_KEYBOARD_LL`, fallback hotkey, foreground window, clipboard, SendInput 같은 Win32 계층을 많이 쓴다. WPF는 이런 전통적인 데스크톱 API와 섞어 쓰는 사례가 많고, 단순한 exe 형태로도 운용하기 쉽다.

### 3. 오버레이 창 구현이 단순하다

이 앱은 일반 앱 창보다 런처/검색 팔레트에 가깝다. `Topmost`, `WindowStyle=None`, `ResizeMode=NoResize`, foreground 복원 같은 동작이 중요하다. WPF는 이런 borderless overlay 구현을 빠르게 만들 수 있다.

### 4. 배포와 디버깅이 단순하다

초기 오픈소스 배포에서는 사용자가 zip/exe/publish 결과물을 받아 실행할 수 있어야 한다. WPF는 이 경로가 단순하다. WinUI 3는 packaged/unpackaged, Windows App SDK runtime, MSIX 여부를 초기에 더 많이 결정해야 한다.

### 5. UI보다 검색 품질이 제품 성공을 좌우한다

우리 제품의 가장 큰 차별점은 한국어 검색 데이터, 랭킹, 초성/별칭/최근 사용 boost다. UI 프레임워크를 최신으로 바꾸는 것보다 검색 품질과 붙여넣기 안정성을 올리는 것이 더 중요하다.

### 6. 필요하면 나중에 전환 가능하다

현재 구조는 Core, Platform, App으로 나누고 있다. 검색 데이터, 검색 랭킹, 사용자 상태, 붙여넣기 정책 같은 핵심 로직은 WPF에 강하게 묶이지 않도록 유지할 수 있다.

따라서 나중에 WinUI 3가 필요해지면 App 레이어를 교체하는 전략을 택할 수 있다. 지금부터 WinUI 3로 시작해 모든 제품 리스크를 한 번에 떠안는 것보다 현실적이다.

## WPF 선택의 단점도 인정한다

WPF가 완벽한 선택은 아니다.

- 기본 UI 스타일은 Windows 11 앱처럼 보이지 않는다.
- 컬러 이모지 폰트 렌더링은 WinUI/UWP 쪽보다 덜 명시적이다.
- 최신 Windows App SDK 기능을 쓰려면 interop가 필요할 수 있다.
- 장기적으로 소비자용 polished 앱을 목표로 하면 WinUI 3 검토가 다시 필요하다.

그래서 현재 제품 정책은 WPF를 유지하되, 이모지 표시는 WPF `TextBlock`에만 의존하지 않고 Twemoji/Noto 같은 재배포 가능한 컬러 리소스를 사용한다.

## 최종 판단

Windows Easy Emoji의 현재 단계에서는 WPF가 맞다.

이유는 명확하다. 이 앱은 최신 Windows UI 쇼케이스가 아니라, OS 입력 흐름에 붙는 빠르고 안정적인 생산성 유틸리티다. 전역 단축키, 트레이 상주, foreground window 복원, 클립보드/붙여넣기, 작은 오버레이 창이 핵심이다. WPF는 이 요구사항을 가장 적은 프레임워크 리스크로 구현하게 해준다.

WinUI 3는 장기적으로 검토할 수 있다. 특히 Microsoft Store 중심 배포, Windows 11 디자인 완성도, 현대적인 컨트롤 경험이 제품의 주요 경쟁력이 되는 시점이 오면 다시 비교해야 한다. 하지만 지금은 검색 품질과 붙여넣기 안정성을 먼저 완성하는 것이 맞다.

## 참고 자료

- WPF overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- WinUI 3 overview: https://learn.microsoft.com/windows/apps/winui/
- Windows app framework options: https://learn.microsoft.com/windows/apps/get-started/
- Visual Studio WPF overview: https://visualstudio.microsoft.com/features/wpf-vs.aspx
