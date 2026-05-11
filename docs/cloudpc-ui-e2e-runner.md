# Windows 365 Cloud PC UI E2E Runner

이 문서는 Windows Easy Emoji의 실제 Windows 데스크톱 E2E를 Windows 365 Cloud PC에서 돌리기 위한 GitHub self-hosted runner 설정 절차다.

## 원칙

- runner는 GitHub Actions self-hosted runner를 사용한다. runner 소프트웨어 자체는 무료이고, 비용은 Cloud PC와 GitHub artifact/cache 사용량에서만 발생한다.
- UI E2E runner는 Windows service로 설치하지 않는다. `run.cmd`를 로그인된 사용자 데스크톱 세션에서 직접 실행해야 `SendInput`, foreground window, tray UI, clipboard paste가 동작한다.
- Cloud PC 세션은 잠기거나 절전으로 들어가면 안 된다.
- workflow는 `self-hosted`, `windows`, `ui-e2e` label이 있는 runner에서만 수동 실행된다.

## 사용자 준비

1. Windows App 또는 Windows 365 웹에서 Cloud PC에 로그인한다.
2. PowerShell을 "Run as administrator"로 연다.
3. 아래 명령으로 권한을 확인한다.

```powershell
whoami /groups
```

정상적인 초기 설정 PowerShell이면 `BUILTIN\Administrators`가 `Enabled group`이고 integrity level이 `High Mandatory Level`이어야 한다. `Administrators`가 `Group used for deny only`이고 `Medium Mandatory Level`이면 elevated shell이 아니다. 이 경우 관리자 권한 PowerShell로 다시 열거나, Windows 365 admin center에서 해당 Cloud PC 사용자를 Local Administrator로 설정해야 한다.

4. GitHub org 또는 repo에서 runner 등록 token을 만든다.

```text
Repo Settings -> Actions -> Runners -> New self-hosted runner -> Windows x64
```

또는 org-level runner를 쓰는 경우:

```text
Organization Settings -> Actions -> Runners -> New self-hosted runner -> Windows x64
```

GitHub 화면에 `config.cmd --url https://github.com/makersfarm --token ...`처럼 org URL이 나오면 org-level runner token이다. 이 경우 runner group에서 `makersfarm/windows-easy-emoji` repo가 이 runner를 사용할 수 있게 허용되어 있어야 한다.

화면에 나온 token만 복사한다. token은 커밋하거나 로그에 남기지 않는다. token을 채팅이나 로그에 붙여넣었다면 폐기하고 새 token을 만든다.

## Runner 설치

Cloud PC에서 repo를 받은 뒤 이 브랜치를 checkout한다.

```powershell
git clone https://github.com/makersfarm/windows-easy-emoji.git C:\dev\windows-easy-emoji
cd C:\dev\windows-easy-emoji
git fetch origin codex/cloudpc-ui-e2e
git checkout codex/cloudpc-ui-e2e
```

runner를 구성한다.

```powershell
.\scripts\setup-cloudpc-ui-e2e-runner.ps1 -RunnerToken "<github-runner-token>"
```

GitHub가 repo URL이 아니라 org URL을 보여준 경우에도 위 명령 그대로 쓴다. 이 스크립트의 기본 `RepoUrl`은 `https://github.com/makersfarm`이다. repo-level token을 쓰려면 아래처럼 명시한다.

```powershell
.\scripts\setup-cloudpc-ui-e2e-runner.ps1 `
  -RepoUrl "https://github.com/makersfarm/windows-easy-emoji" `
  -RunnerToken "<github-runner-token>"
```

GitHub 화면의 기본 명령을 직접 쓰는 경우에는 우리 workflow가 요구하는 label을 반드시 붙인다.

```powershell
.\config.cmd `
  --url https://github.com/makersfarm `
  --token "<github-runner-token>" `
  --labels "self-hosted,windows,ui-e2e,cloudpc" `
  --work "_work" `
  --replace
```

구성이 끝나면 로그인된 Cloud PC 데스크톱 세션에서 runner를 시작한다.

```powershell
.\scripts\start-cloudpc-ui-e2e-runner.ps1
```

이 PowerShell 창은 UI E2E 실행 중 계속 열어둔다. runner를 Windows service로 설치하지 않는다.

## 실행

GitHub에서 아래 workflow를 수동 실행한다.

```text
Actions -> UI E2E -> Run workflow
```

테스트는 다음 두 시나리오를 실제 Windows 데스크톱에서 검증한다.

- `Win + .` 입력을 앱이 가로채고 overlay를 foreground로 띄운다.
- overlay에서 `Enter`를 누르면 선택된 emoji가 이전 foreground window에 붙여넣어진다.

실패 또는 성공 후 artifact `ui-e2e-<run_id>-<attempt>`에서 다음 로그를 확인한다.

```text
artifacts/ui-e2e/<session>/driver.log
artifacts/ui-e2e/<session>/app.log
artifacts/ui-e2e/<session>/target.log
artifacts/TestResults/ui-e2e.trx
```
