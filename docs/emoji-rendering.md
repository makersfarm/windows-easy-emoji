# 이모지 렌더링 정책

## 결론

Mac 이모지처럼 보이게 하려면 결국 Mac/Apple 스타일의 글리프 리소스가 필요하다. 유니코드 이모지 문자열만으로는 "Apple 스타일로 렌더링해라"를 지정할 수 없다.

이미지 리소스를 전혀 쓰지 않고 Apple 이모지처럼 보이게 하는 것은 Windows 앱에서는 현실적으로 불가능에 가깝다. 가능한 것은 Windows가 제공하는 `Segoe UI Emoji` 스타일로 렌더링하거나, 별도의 컬러 이모지 폰트/이미지/벡터 리소스를 앱이 직접 사용하게 만드는 것이다.

## 기술적 이유

이모지는 텍스트 코드포인트다. 예를 들어 `😀`, `❤️`, `🇰🇷` 같은 값은 "어떤 이모지인가"를 나타내지만, "어떤 그림체로 그릴 것인가"는 OS, 폰트, 렌더링 엔진이 결정한다.

Windows는 기본적으로 `Segoe UI Emoji`를 사용한다. macOS와 iOS는 `Apple Color Emoji`를 사용한다. 같은 유니코드라도 실제 얼굴 모양, 색감, 입체감, 선 두께는 각 플랫폼의 폰트 자산에 들어 있는 글리프가 다르기 때문에 결과가 달라진다.

컬러 이모지 폰트도 내부 형식이 하나로 통일되어 있지 않다. Microsoft DirectWrite는 `COLR/CPAL`, `SVG`, `sbix`, `CBDT/CBLC` 같은 컬러 폰트 형식을 다룰 수 있지만, 앱 프레임워크가 그 기능을 실제로 어떻게 노출하는지는 별도 문제다. WPF `TextBlock`은 특히 컬러 이모지 렌더링을 안정적으로 보장하기 어렵고, fallback이 흑백 글리프처럼 보일 수 있다.

Apple Color Emoji는 Apple 플랫폼용 시스템 폰트다. Windows에 기본 설치되어 있지 않고, 앱에 동봉해서 배포하는 것도 라이선스 관점에서 안전한 선택이 아니다. 즉 오픈소스 Windows 앱이 Apple의 실제 이모지 자산을 포함해 "Mac처럼" 보여주는 방식은 제품 배포 전략으로 잡기 어렵다.

## 선택 가능한 방식

### 1. 시스템 텍스트 렌더링

`TextBlock`에 `Segoe UI Emoji`를 지정해서 Windows 기본 컬러 이모지 렌더링을 기대하는 방식이다.

장점은 가장 가볍고 네이티브 텍스트처럼 동작한다는 점이다. 단점은 WPF에서 색상 렌더링이 항상 기대처럼 나오지 않을 수 있고, 결과가 Apple 스타일이 아니라 Windows 스타일이라는 점이다.

### 2. DirectWrite/Direct2D 직접 렌더링

WPF 텍스트 렌더링을 우회하고 DirectWrite color glyph API를 직접 써서 비트맵으로 그리는 방식이다.

장점은 Windows 컬러 폰트를 더 정확하게 제어할 수 있다는 점이다. 단점은 구현 복잡도가 올라가고, 그래도 Apple 스타일이 되는 것은 아니다. Windows에 있는 폰트 자산을 더 잘 그리는 방식일 뿐이다.

### 3. 외부 컬러 이모지 리소스 사용

Twemoji, Noto Color Emoji 계열처럼 재배포 가능한 이모지 이미지/벡터 리소스를 사용해서 앱 UI에 직접 표시하는 방식이다.

장점은 WPF 색상 폰트 문제를 피하고 항상 컬러로 보이게 만들 수 있다는 점이다. 단점은 텍스트가 아니라 이미지 표시가 되므로 캐시, 네트워크, 패키징, 라이선스 고지, fallback 처리가 필요하다. 현재 앱은 이 방향으로 Twemoji PNG 표시와 텍스트 fallback을 사용한다.

### 4. Apple 스타일 유사 자체 리소스 제작

Apple 자산을 쓰지 않고, Apple의 질감과 색감에 가까운 자체 이모지 세트를 제작하거나 공개 라이선스 리소스를 커스터마이즈하는 방식이다.

장점은 원하는 제품 톤을 만들 수 있다는 점이다. 단점은 전체 유니코드 이모지 세트를 커버하려면 자산 제작/검수 비용이 매우 크다.

## 현재 제품 판단

MVP에서는 검색 품질과 붙여넣기 경험이 핵심이다. 그래서 렌더링은 다음 순서가 현실적이다.

1. 결과 타일은 Twemoji 같은 재배포 가능한 컬러 리소스로 표시한다.
2. 이미지 로딩 실패 시 `Segoe UI Emoji` 텍스트로 fallback한다.
3. 나중에 오프라인 품질이 중요해지면 Twemoji/Noto 리소스를 앱 패키지에 번들링하거나 로컬 캐시한다.
4. Windows 네이티브 스타일이 더 중요해지면 DirectWrite/Direct2D 렌더러를 별도 구현한다.

Apple과 완전히 같은 이모지 스타일을 목표로 잡는 것은 법적/배포/기술 리스크가 크다. 대신 "항상 컬러로 보이고, 검색 결과에서 빠르게 식별 가능하며, 붙여넣기는 표준 유니코드 문자열 그대로 동작한다"를 제품 기준으로 둔다.

## 참고

- Microsoft DirectWrite color font support: https://learn.microsoft.com/en-us/windows/win32/directwrite/color-fonts
- Twemoji repository and license: https://github.com/twitter/twemoji
