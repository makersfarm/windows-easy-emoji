# Windows Easy Emoji 계획 4

## 목적

검색 데이터 스키마와 .NET 앱 아키텍처를 확정한다. 목표는 가벼운 Windows 네이티브 앱으로 `Win + .` 대체, 로컬 이모지 검색, 자동 붙여넣기를 구현할 수 있는 구조를 정하는 것이다.

## 결정내용

- UI 프레임워크는 WPF로 한다.
- 앱은 `.NET` 기반 Windows 전용 데스크톱 앱으로 만든다.
- 트레이 아이콘은 Windows Forms `NotifyIcon` interop을 사용한다.
- `Win + .` 대체는 `SetWindowsHookEx` + `WH_KEYBOARD_LL` 저수준 키보드 훅으로 구현한다.
- fallback 단축키는 `RegisterHotKey`로 등록한다.
- 기본 fallback 단축키는 `Ctrl + Win + Space`다.
- 붙여넣기는 `Clipboard.SetText(emoji)` 후 `SendInput(Ctrl + V)`로 처리한다.
- `SendInput`이 실패하거나 대상 앱에 입력할 수 없으면 클립보드 복사 완료 상태로 fallback한다.
- 검색 데이터는 로컬 JSON 파일로 배포한다.
- 검색 인덱스는 앱 시작 시 메모리에 생성한다.
- 검색은 AI나 서버 API 없이 룰 기반 로컬 검색으로 처리한다.
- 사용자 상태는 앱 번들 데이터와 별도 JSON으로 저장한다.

## 검색 데이터 스키마

이모지 기본 데이터는 canonical id를 중심으로 둔다.

```json
{
  "id": "red_heart",
  "emoji": "❤️",
  "baseEmoji": "❤",
  "unicode": ["2764", "FE0F"],
  "version": "0.6",
  "category": "Smileys & Emotion",
  "group": "heart",
  "order": 120,
  "name": {
    "en": "red heart",
    "ko": "빨간 하트"
  },
  "keywords": {
    "en": ["heart", "love", "red"],
    "ko": ["하트", "사랑", "빨강", "마음"]
  },
  "aliases": [":heart:", "heart", "red_heart"],
  "koAliases": ["하트", "빨간하트", "사랑", "좋아함"],
  "chosung": ["ㅎㅌ", "ㅃㄱㅎㅌ", "ㅅㄹ"],
  "searchText": "red heart heart love red 빨간 하트 하트 사랑 빨강 마음 :heart: ㅎㅌ",
  "variant": {
    "type": "emoji_presentation",
    "parentId": null,
    "skinTone": null,
    "default": true
  },
  "flags": {
    "supportsSkinTone": false,
    "isVariant": false,
    "hiddenByDefault": false
  }
}
```

사용자 데이터는 별도 파일로 둔다.

```json
{
  "emojiId": "red_heart",
  "lastUsedAt": "2026-05-11T13:20:00+09:00",
  "useCount": 17,
  "favorite": true,
  "customAliases": ["내하트"]
}
```

## 검색 랭킹 정책

- 한국어 별칭 정확 일치: +1000
- shortcode/alias 정확 일치: +900
- 한국어 CLDR 이름/키워드 정확 일치: +800
- 초성 정확 일치: +760
- 영어 이름/키워드 정확 일치: +700
- prefix match: +500
- contains match: +300
- fuzzy match: 최대 +100
- 즐겨찾기 boost: +80
- 최근 사용 boost: 최대 +60
- 사용 횟수 boost: 최대 +50
- 숨김 variant penalty: -300

## 앱 아키텍처

```text
WindowsEasyEmoji.App
  App.xaml
  Bootstrapper
  TrayAppHost
  OverlayWindow
  SettingsWindow

WindowsEasyEmoji.Core
  EmojiRecord
  EmojiRepository
  SearchIndex
  SearchService
  RankingService
  KoreanTextNormalizer
  ChosungMatcher

WindowsEasyEmoji.Platform
  KeyboardHookService
  HotkeyService
  ClipboardService
  PasteService
  ForegroundWindowService
  StartupService
  SettingsStore

WindowsEasyEmoji.Data
  emoji.json
  ko-aliases.json
```

## 실행 흐름

1. 앱 시작
2. 설정 로드
3. 트레이 아이콘 생성
4. 검색 데이터 로드
5. 메모리 검색 인덱스 생성
6. `WH_KEYBOARD_LL` 훅 설치
7. fallback 단축키 등록
8. `Win + .` 감지
9. 기본 이모지 패널 이벤트 suppress
10. 검색 오버레이 표시
11. 이모지 선택
12. 클립보드에 이모지 저장
13. `SendInput`으로 `Ctrl + V` 입력
14. 사용 기록 저장
15. 오버레이 닫기

## 결정근거

- WPF는 Windows 전용 .NET 데스크톱 앱에 안정적이고, XAML, 데이터 바인딩, 오버레이 창 구현이 단순하다.
- WinUI 3는 현대적인 Fluent UI에 강하지만 Windows App SDK 의존성이 추가되고, MVP의 핵심인 훅/클립보드/트레이 구현에는 WPF가 더 단순하다.
- `RegisterHotKey`는 시스템 전역 핫키 등록에는 적합하지만 이미 Windows가 사용하는 `Win + .` 대체에는 한계가 있다.
- `WH_KEYBOARD_LL`은 `Win + .`을 공격적으로 가로채는 MVP 목표에 더 적합하다.
- `Clipboard.SetText` 후 `SendInput(Ctrl + V)`는 대부분의 텍스트 입력 앱에서 가장 현실적인 붙여넣기 방식이다.
- `SendInput`은 보안 경계나 높은 권한 앱에서 실패할 수 있으므로 복사 완료 fallback이 필요하다.
- 로컬 JSON과 메모리 인덱스는 빠르고 오프라인 동작이 가능하며, 가벼운 앱이라는 목표와 맞다.

## 선택하지 않은 대안과 이유

- WinUI 3: 장기적으로 매력적이지만 MVP에는 Windows App SDK 의존성과 배포 복잡도가 늘어난다.
- Electron/Tauri: UI 구현은 편하지만 가벼운 네이티브 Windows 유틸리티 목표와 덜 맞는다.
- `RegisterHotKey`만 사용: fallback 단축키에는 적합하지만 `Win + .` 대체 경험을 만들기 어렵다.
- UI Automation 기반 직접 입력: 대상 앱 호환성과 구현 복잡도가 높다.
- `SendKeys` 기반 입력: 유니코드 이모지와 포커스 처리 안정성이 낮을 수 있다.
- 서버 검색 API: 오프라인 사용성과 가벼운 배포 목표에 맞지 않는다.
- AI 검색: 의미 검색은 장기적으로 고려할 수 있지만 MVP에는 비용, 속도, 배포 부담이 크다.

## 다음에 정할 것

- 실제 CLDR/Emojibase 계열 데이터 소스와 라이선스
- `emoji.json`과 `ko-aliases.json`의 초기 생성 스크립트
- `WH_KEYBOARD_LL` 훅 실패 감지 기준
- 클립보드 원본 복원 여부
- 설정 저장 경로와 포맷
- WPF 프로젝트 구조와 첫 구현 범위
