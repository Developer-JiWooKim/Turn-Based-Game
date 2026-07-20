# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 따라야 할 작업 규칙입니다.
프로젝트 기획/디자인 내용은 `README.md`를 참고하세요.
코드 구조, 폴더 배치 등은 `/init`으로 스캔한 내용을 우선하고, 이 파일과 충돌하면 이 파일의 규칙을 따릅니다.

## 프로젝트 기본 정보

- 엔진: Unity 6.5
- 장르: 턴제 JRPG 커맨드 배틀 + 로그라이크 무한 타워
- UI: 화면 UI는 UI Toolkit, 월드스페이스 UI(체력바 등)는 uGUI

## 작업 원칙

1. **최적화와 코드 단순성의 균형**
   - 최적화를 신경 쓰되, 그것 때문에 스크립트 개수나 코드 줄 수를 과하게 늘리는 방식은 지양
   - 가독성과 유지보수성을 우선하고, 성능이 실제로 문제가 되는 지점부터 최적화

2. **디자인 패턴 제안**
   - 기획/설계 단계에서 적용 가능한 디자인 패턴이 있으면 먼저 제안하기
   - 예: 턴 진행 → State Machine, 스탯/스킬 데이터 → ScriptableObject, 로직-연출 분리 → 이벤트/옵저버 패턴

3. **로직과 연출(뷰) 분리**
   - 전투 규칙(데미지 계산, 턴 순서, 상태 관리 등)은 순수 C# 클래스로 작성, Unity API에 의존하지 않기
   - MonoBehaviour는 연출(애니메이션, 이펙트, UI 갱신)에만 사용
   - Core(로직)에서 View(연출)로는 이벤트를 통해서만 통신

4. **데이터는 ScriptableObject로**
   - 캐릭터/몬스터 스탯, 로그라이크 선택지, 스폰 패턴 등은 하드코딩하지 않고 SO 기반으로 설계

5. **큰 변경 전에는 계획을 먼저 요약**
   - 여러 파일에 걸친 리팩터링이나 새 시스템 도입 전에는 계획을 먼저 정리해서 확인받고 진행

6. **Unity 6.5 기준**
   - Deprecated된 API나 구버전 방식 사용하지 않기

## 하지 말아야 할 것
- 전투 로직을 View(MonoBehaviour)에 직접 구현하지 않기
- 매직 넘버/하드코딩된 밸런싱 값 추가하지 않기 (SO 데이터로 분리)
- 최적화를 이유로 불필요하게 스크립트를 쪼개거나 코드량을 늘리지 않기
- 사전 논의 없이 큰 구조 변경 진행하지 않기

## 개발 환경

- Unity **6000.5.3f1** (`ProjectSettings/ProjectVersion.txt` 참고), Unity Hub/Editor로 프로젝트를 열어서 작업
- 별도 CLI 빌드/린트/테스트 스크립트나 CI 파이프라인은 구성되어 있지 않음 — 빌드·플레이 테스트는 Unity Editor에서 직접 수행
- `com.unity.test-framework` 패키지는 설치되어 있지만 아직 테스트 어셈블리/테스트 코드가 없음. 테스트 추가 시 Editor의 Window > General > Test Runner 사용
- `Turn-Based-Game.slnx`는 Unity가 자동 생성한 솔루션 파일로, 대부분의 `.csproj`는 Unity 패키지/에디터 어셈블리이며 실제 게임 코드는 `Assembly-CSharp.csproj`(= `Assets/MyAssets/Scripts/`)에 해당

## 현재 코드 상태 (2026-07 기준)

전체 세로 슬라이스(Intro→캐릭터 선택→전투→승리 시 다음 스테이지→전멸 시 리타이어)가 실제 Unity에서 작동 검증됨.

### 구현된 것
- **씬**: `Assets/MyAssets/Scenes/`에 `IntroScene`(타이틀+캐릭터 선택), `BattleScene`(전투), `AnimationMakeScene`(작업용) 존재. Build Settings에 IntroScene(0)·BattleScene(1) 등록됨. 씬 흐름: IntroScene에서 Title→CharacterSelect(오버레이) → `LoadScene("BattleScene")`, 전멸 시 BattleScene→`LoadScene("IntroScene")`
- **인프라**: `Scripts/Singleton/`(`Singleton<T>` 베이스, `GameManager` — 씬 전환/페이드 + **`CurrentRun`/`BeginRun`**으로 런 데이터 보관), `Scripts/Utility/FadeScreenEffect`(Unity `Awaitable` 기반 async). ⚠️ Fade Canvas는 GameManager 자식으로 두어야 씬 전환에도 파괴되지 않음
- **런 데이터**: `Scripts/Run/RunData`(파티 리스트 + `CurrentStage`). 씬을 넘어 `GameManager`가 소유. 캐릭터 선택 시 `BeginRun`으로 생성
- **UI 흐름 (View)**: `Scripts/UI/` — `GameUIController` + `GameFlowFSM`(순수 C# 상태 머신) + `IGameFlowState`(Title/CharacterSelect), 각 패널은 UI Toolkit(`BasePanelUI` 상속). `CharacterSelectPanelUI`는 `CharacterRosterSO` 6종 순환/선택, `CharacterPreview`가 선택 시 3D 모델(프리뷰 카메라→RenderTexture) 교체. 전투 시작 시 선택 캐릭터로 `BeginRun`
- **전투 Core (`Scripts/Battle/Core/`, 순수 C#·UnityEngine 비의존)**: `Stats`, `Unit`, `SkillProfile`, `DamageCalculator`(비율감소+크리), `TurnOrder`(SPD 정렬), `BattleState`, `IRandom`/`SystemRandom`, 액션/셀렉터(`IActionSelector`, `PlayerActionSelector`=입력 await, `MonsterAiSelector`=노멀 공격/보스 스킬우선), `BattleEvents`, `BattleSimulation`(async 턴 루프, `BattleOutcome` 반환). **연출 동기화**: 이벤트 인자의 `RegisterPlayback(Task)`로 View 애니메이션 완료를 시뮬레이션이 대기
- **전투 View (`Scripts/Battle/View/`)**: `BattleDirector`(파티 1회 스폰 + 웨이브별 몬스터 스폰/전투/정리 반복 = 스테이지 루프, 승리→`CurrentStage++`, 패배→IntroScene 복귀), `UnitView`(Unit.Id↔프리팹, Spawn 대기 + Attack/Skill/Hit/Die 트리거 재생), `UnitHealthBar`(uGUI 월드스페이스), `TargetingController`(몬스터 클릭→`SubmitTarget`), `CameraShake`
- **전투 Data (`Scripts/Battle/Data/`)**: `UnitStatsSO`(베이스) + `CharacterStatsSO`/`MonsterStatsSO`(임시 스탯, 밸런싱 TBD), `CharacterRosterSO`(선택 6종), `SpawnWaveSO`(스테이지별 몬스터 구성)
- **애니메이터**: 캐릭터/몬스터 컨트롤러는 `Spawned/Idle/Attack/Hit/Die` 구조(베이스+override). 몬스터 베이스(`Skeleton_Minion.controller`)에 `Skill` 트리거/스테이트 추가됨(현재 Attack과 같은 클립 참조 — 보스 스킬 클립 준비되면 교체). 캐릭터/몬스터 프리팹 + 애니메이션 연결 완료

### 아직 안 된 것
- **로그라이크 선택지(9종)**: 승리 시 3개 중 1개 선택 팝업(UI Toolkit) + 효과 적용 미구현 — 현재는 승리 즉시 다음 웨이브로 직행
- **스테이지 스폰**: `SpawnWaveSO` 배열을 순서대로 순환하는 수준. README의 6스테이지+ 랜덤 스폰 풀 / 5의 배수 보스 패턴 미구현
- **결과 화면**: 승리/패배 팝업 없이 로그·씬전환만. 패배 시 결과 팝업 후 복귀로 개선 예정
- **BattleHUD**(턴 순서 표시, 현재 행동 유닛/타겟 하이라이트 등) 미구현
- **성향 포인트 배분 팝업 / 영구 포인트·최고 기록 저장**, 파티 시너지, 파티원 영입(4명 확장) 미구현
- `Assets/InputSystem_Actions.inputactions`는 Unity 기본 템플릿 그대로(타겟팅은 `Mouse.current` 직접 사용). 커맨드 배틀에 맞는 정식 재정의는 추후

## 아키텍처 방향 (README.md 기획 기반)

아래는 기획 문서와 위 "작업 원칙"에서 도출된 목표 구조이며, 현재 코드도 이 방향을 따른다. 미구현 부분도 이 방향을 유지한다.

- **씬 흐름**: `TitleScene → SelectWeaponScene(캐릭터 선택 + 성향 포인트 배분) → BattleScene`, 세부 화면은 오버레이/팝업으로 처리(씬 전환 아님)
- **Core / View 분리**: 턴 순서(State Machine), 데미지 계산, 스탯/버프 등 전투 규칙은 Unity API 의존 없는 순수 C# 클래스로; MonoBehaviour는 애니메이션·이펙트·UI 갱신 등 연출 전담이며 Core의 이벤트를 구독하는 방식으로만 갱신
- **데이터**: 캐릭터/몬스터 스탯, 로그라이크 선택지(9종), 스폰 패턴은 하드코딩 대신 ScriptableObject로 관리
- **UI 레이어 분리**: 화면 UI(HUD, 메뉴, 팝업)는 UI Toolkit, 월드스페이스 UI(몬스터 체력바 등)는 uGUI로 별도 구현
