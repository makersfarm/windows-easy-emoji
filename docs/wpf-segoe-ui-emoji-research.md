# WPF와 Segoe UI Emoji 호환성 리서치

## 목적

Windows Easy Emoji는 .NET WPF 앱이다. 검색 결과 타일에서 이모지가 흑백 텍스트처럼 보이는 문제가 있었고, `Segoe UI Emoji`를 지정하면 충분한지 검토할 필요가 있다.

이 문서는 WPF의 기본 개념, WPF 텍스트 렌더링 구조, `Segoe UI Emoji` 컬러 폰트와의 호환성, 그리고 제품 판단을 정리한다.

## WPF 개요

WPF는 Windows Presentation Foundation의 약자다. .NET 기반 Windows 데스크톱 UI 프레임워크이며, XAML, 컨트롤, 데이터 바인딩, 레이아웃, 2D/3D 그래픽, 애니메이션, 스타일, 템플릿, 문서, 미디어, 텍스트/타이포그래피 기능을 제공한다.

WPF는 Windows 전용이다. .NET이 크로스 플랫폼이어도 WPF 앱은 Windows에서만 실행된다.

WPF의 핵심 모델은 다음과 같다.

- XAML: UI 구조와 스타일을 선언하는 마크업.
- Code-behind: XAML 뒤에서 동작을 구현하는 C# 코드.
- Dependency Property: 바인딩, 스타일, 애니메이션이 작동하는 WPF 속성 시스템.
- Data Binding: UI와 데이터 모델을 연결하는 방식.
- Layout System: `Grid`, `StackPanel`, `WrapPanel` 같은 패널이 자식 요소 크기와 위치를 계산.
- Control Template / Style: 컨트롤의 외형을 교체하거나 재사용 가능한 스타일을 적용.
- Rendering: 벡터 기반, 해상도 독립적인 렌더링 엔진을 사용하며 하드웨어 가속을 활용할 수 있음.

현재 프로젝트가 WPF를 선택한 이유는 트레이 앱, 전역 단축키, 가벼운 오버레이 UI, 빠른 MVP 구현에 적합하기 때문이다. WinUI 3보다 오래된 기술이지만, 데스크톱 유틸리티와 내부 도구에서는 여전히 실용적이다.

## WPF 텍스트 렌더링 개념

WPF의 텍스트 UI는 보통 `TextBlock`, `TextBox`, `Label`, `RichTextBox` 같은 컨트롤을 통해 렌더링된다. 이 컨트롤들은 `FontFamily`, `FontSize`, `FontStyle`, `FontWeight` 같은 속성으로 사용할 폰트를 지정한다.

WPF는 Unicode 기반 텍스트와 font fallback을 지원한다. 즉 지정한 폰트에 특정 문자가 없으면 다른 폰트에서 glyph를 찾아 표시할 수 있다. 이 fallback은 다국어 UI에는 유용하지만, 이모지에서는 문제가 된다. fallback이 `Segoe UI Emoji`의 컬러 glyph가 아니라 흑백 symbol glyph 쪽으로 흐르면 결과가 흑백처럼 보일 수 있다.

WPF 텍스트 렌더링은 Microsoft ClearType 기반이며, glyph run 합성, anti-aliasing, 하드웨어 가속 같은 파이프라인을 거친다. 하지만 WPF의 고수준 `TextBlock`이 DirectWrite의 최신 color glyph 처리 옵션을 모두 제어할 수 있게 노출하는 것은 아니다.

## Segoe UI Emoji란

`Segoe UI Emoji`는 Windows의 기본 이모지 UI 폰트다. Microsoft 문서상 파일명은 `Seguiemj.ttf`이고, Windows 10과 Windows 11이 이 폰트를 제공한다. Windows typography 문서에서도 `Segoe UI Emoji`는 Emoji용 UI font로 분류된다.

즉 Windows 사용자의 기본 이모지 스타일을 따르고 싶다면 `Segoe UI Emoji` 지정은 맞는 방향이다. 다만 이것은 Apple Color Emoji 스타일이 아니라 Windows Segoe 스타일이다.

## 컬러 폰트 렌더링에서 중요한 점

컬러 폰트는 일반 폰트와 다르다. 일반 glyph는 모양만 갖고 있고 앱이 지정한 단색으로 채워진다. 컬러 폰트는 glyph 내부에 색상 정보도 포함한다.

Microsoft DirectWrite/Direct2D는 Windows 10 version 1607 이후 `COLR/CPAL`, `SVG`, `CBDT/CBLC`, `sbix` 같은 여러 컬러 폰트 형식을 지원한다. `Segoe UI Emoji`는 Windows에서 컬러 이모지를 표현하기 위한 대표 폰트다.

하지만 Direct2D의 낮은 수준 텍스트 API는 컬러 폰트를 자동으로 그리지 않는다. color glyph 렌더링을 명시적으로 켜거나, `TranslateColorGlyphRun`으로 base glyph run을 color glyph run으로 변환해 직접 처리해야 한다.

Microsoft 문서에는 XAML text element가 color font를 기본 지원한다고 설명되어 있다. 여기서 연결되는 `TextBlock.IsColorFontEnabled`는 WinUI/UWP 계열 XAML 컨트롤의 속성이다. WPF `System.Windows.Controls.TextBlock`에는 동일한 `IsColorFontEnabled` 속성이 없다. 이 차이가 중요하다.

## WPF + Segoe UI Emoji 호환성 판단

WPF에서 `TextBlock FontFamily="Segoe UI Emoji"`를 지정하는 것은 필요조건에 가깝지만 충분조건은 아니다.

기대할 수 있는 것:

- Windows에 설치된 `Segoe UI Emoji` glyph를 우선 사용하도록 유도.
- 일부 환경에서는 컬러 이모지가 정상 표시될 가능성.
- 텍스트 선택, 접근성, 복사/붙여넣기 관점에서는 가장 자연스러운 구조.

보장하기 어려운 것:

- 모든 Windows 버전/WPF 런타임/DPI/렌더링 경로에서 컬러 표시.
- 모든 ZWJ sequence, skin tone, variation selector의 안정적 컬러 렌더링.
- Apple Color Emoji 같은 외형.
- WPF `TextBlock`에서 WinUI처럼 `IsColorFontEnabled`를 켜는 방식.

따라서 `Segoe UI Emoji` 지정만으로 제품 품질의 컬러 이모지 표시를 보장하기는 어렵다. 특히 우리 앱처럼 검색 결과 타일에서 이모지 자체가 핵심 시각 요소인 경우, 흑백 fallback 가능성은 제품 품질 리스크다.

## 가능한 구현 전략

### 1. WPF TextBlock + Segoe UI Emoji

가장 단순하다. 현재 fallback으로 유지할 수 있다.

장점은 구현이 가볍고 텍스트 의미가 유지된다는 점이다. 단점은 컬러 표시가 환경 의존적이고 Apple 스타일이 될 수 없다는 점이다.

### 2. WPF Image + 외부 이모지 리소스

Twemoji, Noto Color Emoji 같은 재배포 가능한 PNG/SVG 리소스를 `Image`로 표시한다.

장점은 WPF color font 이슈를 우회하고 UI에서 항상 컬러 타일을 만들기 쉽다는 점이다. 단점은 텍스트 렌더링이 아니라 이미지 렌더링이므로 캐시, 패키징, 네트워크 실패, 라이선스 고지, fallback이 필요하다.

현재 앱의 검색 결과 타일은 이 방향을 사용한다.

### 3. DirectWrite/Direct2D 컬러 glyph 렌더러

WPF 위에 별도 렌더링 레이어를 두고 DirectWrite color glyph API를 직접 호출한다. `TranslateColorGlyphRun`, `DrawColorBitmapGlyphRun`, `DrawSvgGlyphRun` 같은 경로를 직접 처리하는 방식이다.

장점은 Windows의 실제 컬러 폰트를 더 정확히 쓸 수 있다는 점이다. 단점은 WPF 앱치고 구현 복잡도가 크게 올라가고, 그래도 결과는 Apple 스타일이 아니라 Windows Segoe 스타일이라는 점이다.

### 4. WinUI 3 전환 또는 혼합

WinUI/UWP XAML 계열은 color font 지원 API가 더 명시적이다. 다만 앱 전체를 WinUI 3로 전환하면 트레이 앱, 전역 훅, 오버레이 윈도우, 배포 모델이 다시 복잡해진다.

MVP 단계에서는 WPF 유지가 더 낫다. 렌더링 문제 하나 때문에 앱 프레임워크를 바꾸는 것은 비용 대비 이득이 작다.

## 제품 결정

현재 제품 기준에서는 다음 정책이 적절하다.

1. 결과 그리드의 주요 이모지 표시는 재배포 가능한 컬러 이미지 리소스로 처리한다.
2. 이미지 실패 시 `Segoe UI Emoji` `TextBlock` fallback을 사용한다.
3. 붙여넣기는 이미지와 무관하게 항상 `Record.Emoji` 유니코드 문자열을 사용한다.
4. Windows 네이티브 이모지 스타일이 제품 요구사항이 되면 DirectWrite/Direct2D 렌더러를 별도 spike로 검증한다.
5. Apple 스타일 자체를 목표로 삼지는 않는다. Apple 자산 없이 동일 스타일을 보장할 수 없고, Apple 자산 번들링은 배포/라이선스 리스크가 크다.

## 참고 자료

- WPF overview: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/
- WPF typography: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/typography-in-wpf
- Windows typography font list: https://learn.microsoft.com/windows/apps/design/signature-experiences/typography
- Segoe UI Emoji font family: https://learn.microsoft.com/en-us/typography/font-list/segoe-ui-emoji
- DirectWrite color font support: https://learn.microsoft.com/en-us/windows/win32/directwrite/color-fonts
- WinUI TextBlock IsColorFontEnabled: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.textblock.iscolorfontenabled
