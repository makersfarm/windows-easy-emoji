# Test Guide for Agents

## 핵심 원칙

이 프로젝트의 UI/E2E 테스트는 로컬 데스크톱에서 직접 실행하지 않는다.

Windows Easy Emoji는 foreground window, `SendInput`, clipboard, tray icon, global keyboard hook, `Win + .` interception을 검증한다. 이 기능들은 현재 로그인 세션, 다른 앱의 focus, RDP/Windows App 상태, 클립보드, 다른 실행 중인 프로세스에 영향을 받는다. 그래서 로컬 PC에서 UI/E2E를 실행하면 개발자 작업 환경을 방해하거나 테스트 결과가 흔들릴 수 있다.

에이전트는 로컬에서 앱을 실행하거나 UI/E2E 테스트를 직접 돌리지 않는다. 실제 desktop UI 검증은 반드시 연결된 self-hosted Windows VM runner의 GitHub Actions workflow로 수행한다.

## 테스트 계층

### Non-UI tests

대상:

- `tests/WindowsEasyEmoji.Core.Tests`
- `tests/WindowsEasyEmoji.Platform.Tests`
- `tests/WindowsEasyEmoji.DataBuilder.Tests`

검증 내용:

- 검색 랭킹
- 한국어 정규화
- emoji repository/data builder
- 사용자 상태 업데이트
- settings 저장
- hotkey parsing
- paste coordinator의 단위 동작
- overlay placement 계산

원칙:

- 코드 변경 검증은 GitHub Actions `UI E2E` workflow 안의 `Run non-UI tests` 단계로 확인한다.
- 에이전트는 사용자가 명시적으로 허용하지 않는 한 로컬에서 `dotnet test`를 실행하지 않는다.

### UI/E2E tests

대상:

- `tests/WindowsEasyEmoji.E2ETests`
- `tests/WindowsEasyEmoji.E2ETarget`

검증 내용:

- `Win + .` replacement
- fallback hotkey
- overlay foreground activation
- search input
- grid result selection
- Enter paste into previously focused target
- copy-only mode
- clipboard restore
- no-results behavior
- favorite toggle
- adaptive overlay placement above/below target

원칙:

- 절대 로컬에서 직접 실행하지 않는다.
- 반드시 GitHub Actions `UI E2E` workflow를 수동 실행한다.
- workflow는 `self-hosted`, `windows`, `ui-e2e` label이 붙은 Windows VM runner에서만 돈다.

## VM Runner 플로우

현재 E2E 경로는 `.github/workflows/ui-e2e.yml`이다.

workflow 순서:

1. Checkout
2. Setup .NET 8
3. Restore
4. Build debug binaries
5. Run non-UI tests
6. Publish installed app
7. Verify interactive desktop before UI E2E
8. Run UI E2E
9. Upload UI E2E artifacts

중요한 점:

- UI E2E는 build output이 아니라 `artifacts/installed-app/WindowsEasyEmoji.App.exe` 설치본을 대상으로 실행한다.
- `Verify interactive desktop before UI E2E` 단계가 먼저 foreground window와 `SendInput` 가능 여부를 검사한다.
- 이 preflight가 실패하면 앱 문제가 아니라 VM desktop session 문제가 원인일 가능성이 크다.

## 실행 명령

에이전트는 로컬 UI 실행 대신 GitHub CLI로 workflow를 실행한다.

```bash
gh workflow run ui-e2e.yml \
  --repo makersfarm/windows-easy-emoji \
  --ref codex/vm-ui-e2e-stability
```

최근 실행 확인:

```bash
gh run list \
  --repo makersfarm/windows-easy-emoji \
  --branch codex/vm-ui-e2e-stability \
  --workflow ui-e2e.yml \
  --limit 5
```

실행 감시:

```bash
gh run watch <run-id> \
  --repo makersfarm/windows-easy-emoji \
  --exit-status
```

결과 상세:

```bash
gh run view <run-id> \
  --repo makersfarm/windows-easy-emoji \
  --json status,conclusion,url,headSha,createdAt,updatedAt
```

## Runner 세션 조건

VM runner는 Windows service로 돌리면 안 된다. 로그인된 사용자 desktop session에서 runner PowerShell 창이 직접 떠 있어야 한다.

필수 조건:

- Cloud PC 또는 Windows VM에 사용자가 로그인되어 있어야 한다.
- 화면이 잠기면 안 된다.
- 절전 상태가 아니어야 한다.
- RDP/Windows App 창이 닫히거나 최소화된 상태면 안 된다.
- runner는 `self-hosted`, `windows`, `ui-e2e` label을 가져야 한다.
- runner 창은 테스트 중 계속 열려 있어야 한다.

자세한 runner 설치/운영 절차는 `docs/cloudpc-ui-e2e-runner.md`를 본다.

## 주요 환경 변수

workflow는 다음 환경 변수를 사용한다.

```text
WINDOWS_EASY_EMOJI_RUN_UI_E2E=1
WINDOWS_EASY_EMOJI_E2E_ARTIFACT_DIR=<repo>\artifacts\ui-e2e
WINDOWS_EASY_EMOJI_E2E_APP_EXE=<repo>\artifacts\installed-app\WindowsEasyEmoji.App.exe
```

테스트 코드는 `WINDOWS_EASY_EMOJI_RUN_UI_E2E`가 없으면 UI E2E를 실행하지 않는 방향으로 설계되어 있다. UI E2E는 workflow 환경에서만 켜야 한다.

## 아티팩트 확인

workflow는 성공/실패와 관계없이 다음 경로를 artifact로 업로드한다.

```text
artifacts/ui-e2e
artifacts/TestResults
artifacts/installed-app
```

주요 로그:

```text
artifacts/ui-e2e/<session>/driver.log
artifacts/ui-e2e/<session>/app.log
artifacts/ui-e2e/<session>/target.log
artifacts/TestResults/ui-e2e.trx
```

로그 역할:

- `driver.log`: 테스트 드라이버가 어떤 입력을 보냈는지, 어떤 창을 foreground로 봤는지.
- `app.log`: Windows Easy Emoji 앱 내부 이벤트, shortcut dispatch, overlay show, paste result.
- `target.log`: E2E target input window가 받은 텍스트.
- `ui-e2e.trx`: xUnit test result.

## 실패 분석 순서

### 1. Build debug binaries 실패

앱 또는 테스트 코드 컴파일 문제다. XAML namespace, ambiguous type, missing using, project reference를 먼저 확인한다.

### 2. Run non-UI tests 실패

검색 랭킹, 데이터 스키마, platform 단위 로직 문제다. UI를 의심하기 전에 실패한 test name과 assertion을 확인한다.

### 3. Verify interactive desktop before UI E2E 실패

대부분 VM session 문제다.

확인할 것:

- runner가 service가 아니라 로그인된 desktop session에서 실행 중인지.
- VM 화면이 잠기지 않았는지.
- RDP/Windows App 창이 닫히거나 최소화되지 않았는지.
- `SendInput` smoke test가 `ERROR_ACCESS_DENIED(5)`로 실패했는지.

이 단계가 실패하면 앱 코드를 고치기 전에 VM 세션을 복구한다.

### 4. Run UI E2E 실패

이때부터 앱 동작 회귀를 의심한다.

우선순위:

1. `driver.log`에서 foreground window와 보낸 입력 확인.
2. `app.log`에서 shortcut dispatch, overlay show, target handle, paste result 확인.
3. `target.log`에서 실제 붙여넣어진 텍스트 확인.
4. 필요하면 artifact screenshot 또는 추가 로그를 확인.

## 테스트 추가 원칙

새 UI/E2E 테스트는 다음 중 하나를 검증해야 한다.

- 실제 사용자 workflow.
- 과거에 깨진 회귀 지점.
- Windows desktop integration에서 로컬 단위 테스트로 잡기 어려운 behavior.

좋은 UI/E2E 테스트 예:

- `Win + .`로 overlay가 뜬다.
- `하트` 검색 후 Enter가 원래 입력창에 `❤️`를 붙여넣는다.
- 아래쪽 입력창에서는 overlay가 위쪽에 뜬다.
- copy-only mode에서는 target text가 비어 있고 clipboard만 바뀐다.

나쁜 UI/E2E 테스트 예:

- 순수 검색 랭킹만 검증하는 테스트.
- 단위 테스트로 충분한 문자열 정규화 테스트.
- screenshot 없이 색상만 막연히 기대하는 테스트.
- sleep만 길게 걸고 결과를 추측하는 테스트.

검색 품질, 데이터 변환, ranking score는 가능하면 Core/DataBuilder 단위 테스트로 둔다. UI/E2E는 실제 Windows desktop integration만 검증한다.

## 에이전트 금지사항

에이전트는 다음을 하지 않는다.

- 로컬에서 `WindowsEasyEmoji.App.exe` 실행.
- 로컬에서 `tests/WindowsEasyEmoji.E2ETests` 직접 실행.
- 로컬에서 `Win + .`, fallback hotkey, clipboard paste를 수동 검증.
- 사용자의 foreground app, clipboard, keyboard input을 건드리는 실험.
- 실패 원인을 보기 전에 작은 부분만 반복 수정.
- VM session preflight 실패를 앱 회귀로 단정.

허용되는 로컬 작업:

- 파일 읽기.
- 코드/문서 수정.
- `git diff --check`.
- `git status`, `git log`, `git diff`.
- GitHub Actions workflow 실행/조회.

## 커밋 전 확인

테스트 관련 변경을 커밋하기 전 최소 확인:

```bash
git diff --check
git status --short
```

기능 또는 UI/E2E 변경이 있으면 커밋/푸시 후 VM workflow를 실행하고, 통과한 run URL을 최종 보고에 포함한다.
