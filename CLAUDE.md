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

전체 세로 슬라이스(Intro→캐릭터 선택→전투→승리 시 성장 선택지→다음 스테이지→전멸 시 리타이어)가 실제 Unity에서 작동 검증됨.
로그라이크 루프(선택지 9종·파티 영입/교체·파티 시너지·스테이지 스케일링·6스테이지 이후 랜덤 스폰·사망 시 영구 추방)와 전멸 결과 화면, BGM/SFX, 성향 포인트 배분, 배틀 퍼즈(ESC/HUD 버튼·중단)까지 돌아간다. 남은 것은 보스 스킬 이펙트, 옵션 메뉴(해상도/언어), 밸런싱.

### 구현된 것
- **씬**: `Assets/MyAssets/Scenes/`에 `IntroScene`(타이틀+캐릭터 선택), `BattleScene`(전투), `AnimationMakeScene`(작업용) 존재. Build Settings에 IntroScene(0)·BattleScene(1) 등록됨. 씬 흐름: IntroScene에서 Title→CharacterSelect(오버레이) → `LoadScene("BattleScene")`, 전멸 시 BattleScene→`LoadScene("IntroScene")`
- **인프라 (`Scripts/Systems/`)**: `Singleton<T>` 베이스 + 전역 매니저. `GameManager`(씬 전환/페이드 + **`CurrentRun`/`BeginRun`**으로 런 데이터 보관), `AudioManager`(BGM 크로스페이드·SFX·Master/BGM/SFX 3단 볼륨), `FadeScreenEffect`(Unity `Awaitable` 기반 async). ⚠️ Fade Canvas는 GameManager 자식으로 두어야 씬 전환에도 파괴되지 않음
- **입력 허브 (`Scripts/Systems/InputManager`)**: 흩어져 있던 플레이어 입력을 한곳으로 모으는 Singleton(DontDestroyOnLoad). 생성된 `InputSystem_Actions` 래퍼(전역 클래스, 프로젝트 asset에서 자동 생성)를 직접 소유하고, View(예: `TargetingController`)는 `Mouse.current` 같은 원시 디바이스를 만지지 않고 이 매니저만 참조한다. 입력을 **두 갈래로 분리**: ①UI 입력(포인터/클릭은 래퍼의 UI 맵, 메뉴 조작은 EventSystem), ②배틀·UI 방향키/확정은 코드로 만든 별도 `InputAction` 그룹(방향키가 UI 네비게이션과 충돌하지 않게 배틀용/UI용을 각각 잡음). `IsGameplayInputEnabled`는 배틀 입력만 막는 게이트(추후 Pause용) — UI 입력은 게이트와 무관해 퍼즈 중에도 메뉴 조작 가능. 노출 API: `PointerPosition`/`PrimaryPressedThisFrame`(마우스), `BattleCyclePrev/Next`·`BattleConfirm`(배틀 타겟 방향키/Enter·Space), `UiNavigatePrev/Next`·`UiSubmit`(UI 방향키/Enter·Space), `PauseToggle`(ESC — 게임플레이 게이트에 묶지 않는다. 묶으면 퍼즈 중에 ESC로 못 푼다). 과거의 `PlayerInput` 컴포넌트 + `PlayerInputAction` 스크립트 방식은 제거됨(씬의 잔여 컴포넌트는 에디터에서 정리 필요). `InputSystem_Actions.inputactions`의 Player 맵은 여전히 비어 있고, 정식 액션맵 재정의는 추후(TODO 4-2)
- **영구 저장 (`Scripts/Progression/Save/`)**: `SaveData`(런을 넘어 유지되는 값 = 최고 스테이지 + 카테고리별 영구 포인트 투자 내역 + 옵션 설정, `JsonUtility` 직렬화를 위해 public 필드만 사용) + `SaveService`(static, `save.json` 읽기/쓰기). **저장 위치가 에디터/빌드에서 다름** — 에디터는 확인·삭제가 쉽도록 프로젝트 루트의 `SaveData/`(`.gitignore` 등록됨, Assets 바깥이라 `.meta` 안 생김), 빌드는 `Application.persistentDataPath`. 빌드된 게임은 프로젝트 폴더와 무관하고 설치 경로가 읽기 전용일 수 있어 루트 방식을 쓸 수 없다. 최초 접근 시 1회 로드 후 캐싱하고, 파일이 없거나 깨져 있으면 기본값으로 복구한다. `GameManager`가 없는 상태(BattleScene 직접 실행)에서도 동작하도록 static으로 둠. 영구 포인트 **획득량은 저장하지 않고** `BestStage`에서 파생(`GetEarnedPoints`)해 값이 어긋날 여지를 없앰. 필드 추가는 기존 세이브와 호환되며(없는 필드는 초기값 유지), 필드 의미가 바뀔 때만 `Version`을 올려 `SaveService.Normalize`에서 마이그레이션
- **런 데이터 (`Scripts/Progression/Run/`)**: `RunData`가 런 전체 상태를 소유하고 씬을 넘어 `GameManager`가 보관(캐릭터 선택 시 `BeginRun`으로 생성). `RunMember`(파티원 1명 = 원본 SO + 성장 누적 `Stats` + `BaseStats` 스냅샷 + 현재 HP + 런 내내 고정인 `UnitId`) 리스트, `PendingModifiers`(다음 스테이지 몬스터 디버프 예약), `CurrentStage`, `NextUnitId()` 발급기를 가짐. 선택지 효과 적용(`ApplyChoice`)·스테이지 자동 성장(`ApplyStageGrowth`)·전투 결과 반영(`SyncFromBattle`)·사망자 영구 추방(`RemoveFallen`)·영입(`Recruit`)·파티가 꽉 찼을 때 교체(`ReplaceMember`)가 전부 여기 있고 View는 결과만 화면에 반영. `PreviewRecruitStats`는 `Recruit`과 같은 소급 성장 계산(`ApplyCatchUp`)을 공유해, 영입 후보 카드에 뜨는 값과 실제 합류 결과가 항상 일치하도록 함
- **파티 시너지 (`Progression/Run/PartySynergyTracker`)**: 같은 캐릭터가 `CharacterStatsSO.SynergyThreshold` 이상 모이면 그 캐릭터들에게만 스탯 보너스(`CreateSynergy()`가 `RoguelikeEffect`를 재사용, HP 증분 0이라 회복 부작용 없음). 런 데이터에 누적하지 않고 전투 중에만 트래커가 붙였다 떼는 방식이라, 대상이 죽어 인원이 임계 밑으로 떨어지면 `OnAllyDied`가 즉시 되돌린다(README 규칙). `RunData.GetActiveSynergies()`(전투 시작 전 초기 표시)와 트래커의 실시간 갱신 결과 모두 `BattlePresenter.SetSynergies` → `BattleHUD.ShowSynergies`로 HUD에 반영
- **UI 흐름 (View)**: `Scripts/UI/` — `GameUIController` + `GameFlowFSM`(순수 C# 상태 머신) + `IGameFlowState`(Title/CharacterSelect), 각 패널은 UI Toolkit(`BasePanelUI` 상속). `CharacterSelectPanelUI`는 `CharacterRosterSO` 6종 순환/선택(prev/next 버튼 + **좌/우 방향키**가 동일하게 `Cycle`, `InputManager.UiNavigate*` 사용, 방향키 순환 시 버튼과 같은 클릭음), `CharacterPreview`가 선택 시 3D 모델(프리뷰 카메라→RenderTexture) 교체, 선택 시 스탯 바 6종(HP/ATK/SPD/DEF/치명타/저항)을 로스터 내 최댓값 대비 비율로 갱신(`RefreshStats`, 밸런싱 값 하드코딩 없이 로스터에서 계산). 전투 시작 시 선택 캐릭터로 `BeginRun`
- **전투 Core (`Scripts/Battle/Core/`, 순수 C#·UnityEngine 비의존)**: `Stats`, `Unit`, `SkillProfile`, `DamageCalculator`(비율감소+크리), `TurnOrder`(SPD 정렬), `BattleState`, `IRandom`/`SystemRandom`, 액션/셀렉터(`IActionSelector`, `PlayerActionSelector`=입력 await, `MonsterAiSelector`=노멀 공격/보스 스킬우선), `BattleEvents`, `BattleSimulation`(async 턴 루프, `BattleOutcome` 반환). **연출 동기화**: 이벤트 인자의 `RegisterPlayback(Task)`로 View 애니메이션 완료를 시뮬레이션이 대기
  - `RoguelikeEffect`: 선택지 1종의 순수 효과. 파티 강화 flat 8종 + 몬스터 디버프 3종 + 영입 플래그를 한 struct에 담음(카테고리별 구현체로 쪼개지 않음). `ApplyTo(Stats)`는 스탯만 더하고 "채워야 할 HP량"을 반환 — 현재 HP 규칙은 호출자(`Unit`/`RunMember`)가 각자 처리
  - `RunModifiers`: 다음 스테이지 몬스터에만 1회 적용되는 디버프 예약함(`Add` 배율 곱연산 중첩 → `ApplyTo` → `Consume`)
  - `StageScaling`: 스테이지별 양측 스탯 성장. 몬스터는 스폰 시 **배율**(복리 옵션), 플레이어는 HP를 이어받으므로 배율 대신 `BaseStats` 기준 **flat 증가를 영구 누적**. `CreatePlayerGrowth(baseStats, step)`은 매 스테이지 개별 반올림 대신 "누적 총량(`기준값×비율×step`)의 차분"을 돌려줘 반올림 오차가 쌓이지 않음(과거엔 스테이지마다 최소 +1 바닥값을 둬서 저스탯 캐릭터가 비정상적으로 급성장하는 문제가 있었음). SPD·치명타·저항은 스케일링 제외(로그라이크 선택지로만 성장)
  - `WeightedPicker`: 가중치 비례 + 중복 없는 추첨. 추후 영구 포인트(카테고리별 가중치 투자) 시스템도 이 위에 얹음
  - `IPauseGate`: 전투를 멈추는 순수 계약. `BattleSimulation`이 **각 유닛 행동 직전**에만 await하므로 진행 중인 연출이 잘리지 않는다(턴제에 자연스러운 경계). 생성자 인자가 선택적이라 null이면 기존과 동일하게 동작. 구현은 View의 `BattlePausePanel`
  - `StatusEffect.cs`: 상태이상 시스템(한 파일에 `StatusKind` 5종 + 부여 정의 `StatusEffect` + 진행 중 상태 `ActiveStatus` + `StatusChangeReason`). `RoguelikeEffect` 선례대로 종류별 구현체로 쪼개지 않는다. **`Stats.Res`가 여기서 소비된다** — 최종 부여 확률 = `ApplyChance × (1 − 대상 RES)`이므로 RES 1.0은 완전 면역(`Unit.TryApplyStatus`)
    - 스탯 감소형(`AtkDown`/`DefDown`/`SpdDown`)은 **`Stats`를 직접 고치지 않고** `Unit.EffectiveAtk/EffectiveDef/EffectiveSpd`로 읽는 시점에 반영한다. 파티 시너지가 이미 `Stats`를 스냅샷/복원 방식으로 조작 중이라, 양쪽이 같은 필드를 쓰면 서로를 덮어쓰기 때문. `DamageCalculator`와 `TurnOrder`가 이 유효 스탯을 쓴다
    - 처리 순서는 `BattleSimulation.ResolveStatusesAsync`에 모여 있다: 자기 차례 시작 시 **도트 피해 → 지속 턴 감소 → 기절 판정**. 기절 여부를 감소 *전*에 읽어야 1턴짜리 기절이 실제로 한 번의 행동을 막는다. 기절한 유닛은 `ActorTurnStarted`를 발생시키지 않고 넘어간다(플레이어 유닛이 기절했을 때 "당신의 차례" 프롬프트가 잘못 뜨지 않도록)
    - 부여는 `ResolveAction`에서 데미지 적용 직후, **살아남은 대상에게만** 시도한다
    - `StatusChanged`는 부여/저항/만료뿐 아니라 **지속 턴이 줄 때마다(`Ticked`)도 발생**시켜야 한다. 빼먹으면 남은 턴 수 표기가 부여 시점 값에서 멈춰 "디버프가 안 걸린 것처럼" 보인다(실제로 겪은 버그)
    - **상태이상은 전투(스테이지) 단위다**(확정된 규칙) — 스테이지를 클리어하면 사라지고 다음 전투는 깨끗한 상태로 시작한다. 파티 `Unit`이 스테이지마다 새로 생성되므로 로직은 자동으로 초기화된다(유지하고 싶다면 `RunMember`에 저장해야 함)
    - 반면 **파티 View는 런 내내 재사용**되므로 표기는 코드가 직접 지워야 한다. 두 지점 모두 필요하다 — 전투 종료 직후 `UnitViewRegistry.ClearStatuses()`(선택지 화면에 남지 않도록)와 스테이지 시작 시 `RefreshStatuses(players)`(실제 상태를 다시 밀어넣음). 빼먹으면 효과는 사라졌는데 글자만 남는다(실제로 겪은 버그)
  - ⚠️ **월드스페이스 체력바(uGUI/TMP) 텍스트는 ASCII만 사용**: TMP는 폰트 에셋의 글리프 아틀라스에 있는 문자만 그리는데 기본 폰트에 한글·화살표가 없어 네모로 깨진다. 그래서 상태이상 약어가 `STUN`/`PSN`/`ATK-` 형태다. 한글을 쓰려면 한글 글리프를 포함한 TMP Font Asset을 만들어 지정할 것(화면 UI는 UI Toolkit이라 이 제약이 없다)
  - **로그라이크 몬스터 디버프 3종의 처리 방식이 서로 다르다**(성격이 달라서 억지로 통일하지 않았다)
    - *행동불가* → 스폰 시 `Stun` 1턴을 **확정 부여**(`Unit.ApplyStatus`, 저항 판정 없음 — 플레이어가 선택지를 소모해 얻은 효과라 몬스터 RES로 무효화되면 안 된다). 과거의 `BattleSimulation.enemySkipFirstTurn` 특수 분기는 제거됨
    - *체력감소·공격력감소* → 상태이상이 아니라 **스폰 시점에 스탯에 녹아드는 배율**(`RunModifiers.ApplyTo`). 지속 턴 개념이 없고 최대 HP는 상태이상으로 표현할 수단이 없어서 그대로 뒀다. 대신 `MonsterSpawner.DescribeDebuff`가 `HP -30%` 같은 문자열을 만들어 `UnitView.SetSpawnDebuff` → 체력바에 고정 표기한다(상태이상 목록과 별도 필드라 턴마다 갱신되는 아래 줄과 섞이지 않는다)
- **전투 View (`Scripts/Battle/View/`)**: 역할별로 4개 컴포넌트로 분리되어 있고 전부 BattleDirector 오브젝트에 붙어 있음
  - `BattleDirector`: 스테이지 루프 오케스트레이터. 런 해석 → 시뮬레이션 구동 → 결과를 `RunData`에 반영 → 진급/자동성장/선택지. 파티 `Unit`은 **스테이지마다 `RunMember`에서 새로 생성**(성장 반영 + HP 계승), `UnitId`가 고정이라 View는 재사용. 몬스터 구성은 `MonsterSpawner`에 위임하고 Director는 흐름만 담당한다. `_stageScaling`은 몬스터뿐 아니라 파티 자동 성장·영입 소급 성장에도 쓰여 Director가 계속 소유
  - `MonsterSpawner`: "이번 스테이지에 어떤 몬스터가 나오는가" 전담. 웨이브 선택(`ResolveWave`/`PickFromPool`)은 `_monsterWaves`(1~N스테이지 수동 설계) 범위를 넘으면 `_randomWavePool`에서 `_bossStageInterval` 배수 여부에 따라 보스/일반 풀을 갈라 가중치 추첨(풀이 비어 있으면 기존 배열 순환으로 안전하게 대체). `SpawnWave`가 몬스터 `Unit` 생성(기준 스탯 → `StageScaling` → `PendingModifiers` 순서)까지 처리. 레지스트리는 `TargetingController`와 같이 `Initialize(registry)` 주입 방식이라 인스펙터에 중복 연결하지 않는다
  - `UnitViewRegistry`: 슬롯 배치·`Id→UnitView` 조회·스폰/정리·체력바 갱신. 파티 슬롯 점유 현황을 관리해 추방·교체로 빈 자리를 영입 시 재사용. **오브젝트 풀링**: 무한 타워라 인스턴스를 파괴하지 않고 `UnityEngine.Pool.ObjectPool<UnitView>`에 프리팹 단위로 반납·재사용한다(`_pools` + 반납할 풀을 찾는 `_sourcePrefab`). 재사용 인스턴스는 이전 전투 상태를 그대로 들고 오므로 스폰 시 `UnitView.ResetForSpawn()`이 **아웃라인 레이어 원복 → `UnitAnimator.ResetToSpawn()`(트리거 소거 + `Rebind` + `Update(0)`)** 순으로 초기화한다. 아웃라인 원복이 `Initialize`보다 먼저여야 한다 — `Initialize`가 렌더러의 "원래 레이어"를 다시 캐싱하기 때문에 순서가 뒤집히면 겨냥 레이어가 원본으로 굳는다
  - `BattlePresenter`: Core 이벤트 구독/해제(`Bind`가 먼저 `Unbind`를 호출해 짝이 어긋날 수 없음) → 공격·피격·사망 연출, HUD 갱신(시너지 표시 포함), 카메라 쉐이크 + 크리티컬 SFX
  - `RoguelikeRewardService`: 가중치 추첨 → 선택지 패널 제시 → `RunData.ApplyChoice` 호출. 영입 선택지를 고르면 후보 카드 제시(파티가 꽉 찼으면 "영입 안 함" 카드를 추가) → 실제로 영입을 고르면 현재 파티원 카드로 교체 대상을 이어서 물음 → `RunData.Recruit`/`ReplaceMember` 호출. `RecruitResult`(영입된 멤버 + 교체로 나간 멤버)를 반환해 `BattleDirector`가 View 스폰/제거를 처리
  - `BattlePausePanel`: 배틀 퍼즈 오버레이 겸 `IPauseGate` 구현. ESC(`InputManager.PauseTogglePressed`)와 HUD 우상단 버튼(`BattleHUD.PauseClicked`) 두 경로로 토글. 퍼즈 화면은 PAUSE/현재 스테이지/이전 최고 기록 + '계속하기' + '배틀 중단'. **`Time.timeScale`을 쓰지 않는다** — 연출 대기(`Awaitable.WaitForSecondsAsync`)가 timeScale에 영향받는지 보장되지 않고, 씬 전환 페이드도 같은 Awaitable 기반이라 함께 멈출 수 있다. 몬스터 차례는 게이트 대기로, 플레이어 차례는 `IsGameplayInputEnabled=false`로 멈춘다. 퍼즈 해제 시 배틀 입력 복구를 **한 프레임 미룬다**(`_enableInputAtFrame`) — 오버레이는 3D 레이캐스트를 막지 않아서, 즉시 복구하면 '계속하기' 클릭이 같은 프레임에 `TargetingController`의 타겟팅 클릭으로 새어 들어간다. 퍼즈는 전투 구간에서만 허용(`SetBattleActive`) — 로그라이크 선택지 패널과 방향키/Enter가 겹치기 때문
  - `BattleResultPanel`: 전멸 시 도달 스테이지 + 최고 기록을 보여주는 결과 팝업(UI Toolkit, 확인 버튼 대기 후 IntroScene 전환). 신기록 여부는 패널이 직접 비교하지 않고 `SaveService.RecordStage`의 반환값을 받아 표시만 한다(동점을 신기록으로 처리하지 않기 위함). `HandleDefeatAsync`는 저장 **전** 기록을 읽어 "이전 최고 기록"으로 넘긴다
  - 그 외: `UnitView`(`UnitAnimator`/`UnitHealthBar`를 물고 제어), `UnitAnimator`(트리거 재생 + 연출 길이 반환), `UnitHealthBar`(uGUI 월드스페이스 게이지 + TMP 숫자 표기 + 상태이상/스폰 디버프 표기. 사망 시 `PlayDieAsync`가 `SetVisible(false)`로 숨기고 스폰 시 `Initialize`가 되살린다 — 풀에서 재사용되므로 복구를 빼먹으면 그 인스턴스는 영영 체력바가 안 보인다), `TargetingController`, `BattleHUD`(스테이지/턴 순서/현재 행동 유닛/플레이어 차례 프롬프트), `RoguelikeChoicePanel`, `CameraShake`
    - `TargetingController`: 몬스터를 겨냥→확정해 `SubmitTarget`. **마우스**는 2단계 클릭(1차 겨냥=빨강 아웃라인, 같은 대상 재클릭=확정), **키보드**는 좌/우 방향키로 유효 대상을 순환 겨냥하고 Enter/Space로 확정. 입력은 `InputManager`(마우스 `PointerPosition`/`PrimaryPressedThisFrame`, 방향키 `BattleCycle*`, 확정 `BattleConfirm`)를 통해서만 받고 원시 디바이스는 만지지 않는다. 방향키 순환 순서는 리스트 순서가 아니라 **화면 좌→우**(각 대상 View를 `Camera.WorldToScreenPoint`로 정렬, `BattleDirector`가 `Initialize`에 `UnitViewRegistry` 주입해 `Id→View` 조회). 확정 시 `AudioManager.Confirm()`(마우스·키보드 공통)
    - `RoguelikeChoicePanel`: 카드 4장, 선택 대기 await. 마우스 클릭 외에 **방향키로 카드 겨냥**(마우스 `:hover`와 같은 강조를 `.choice-card--active` 클래스로 재현, 이동 시 `AudioManager.UiNavigate()`)하고 Enter/Space로 선택(선택 시 `AudioManager.UiClick()` — 마우스 클릭은 `UiClickSfx`가 `ClickEvent`로 내므로 키보드 분기에서만 재생해 중복 방지). 키보드 입력은 `InputManager.UiNavigate*`/`UiSubmit` 사용
- **전투 Data (`Scripts/Battle/Data/`)**: `UnitStatsSO`(베이스) + `CharacterStatsSO`/`MonsterStatsSO`(임시 스탯, 밸런싱 TBD), `SkillSO`(스킬 1종 = 쿨타임·범위·배율·상태이상. **유닛 종류에 묶이지 않은 별도 에셋**이라 `MonsterStatsSO`가 참조만 하고, 캐릭터 스킬이 추가되면 `CharacterStatsSO`가 코드 변경 없이 같은 타입을 재사용한다. 여러 유닛이 같은 스킬 에셋을 공유해도 됨), `CharacterRosterSO`(선택 6종), `SpawnWaveSO`(스테이지별 몬스터 구성 + `Weight`/`IsBossWave`— 후자는 `MonsterStatsSO.Tier`로 계산해 별도 플래그와 어긋날 여지 없음), `RoguelikeChoiceSO`(9종 카테고리 + 효과 수치 + 등장 가중치), `StageScalingSO`(양측 성장률 6종 + 복리 토글)
- **로그라이크 선택지 9종**: 에셋은 `ScriptableObjects/RoguelikeChoice/`에 전부 존재. 승리 시 가중치 추첨으로 3개 제시 → 1개 선택 → 즉시 적용. 영입 선택지는 파티가 꽉 차도 후보에서 빠지지 않고(`_weightPerEmptySlot`는 빈자리가 많을수록 자주 뜨게만 함), 고르면 교체 대상을 플레이어가 선택(위 `RoguelikeRewardService` 참고). 후반 영입/교체 캐릭터에는 스테이지 자동 성장분만 소급 적용(선택지로 얻은 성장은 소급하지 않음). `Heal`은 `_healFlat`(즉시 회복)을 쓰고 `_hpFlat`(최대 HP 영구 증가)은 0 — 과거에 이 둘이 뒤바뀌어 있던 데이터 실수를 수정함
- **스테이지 스폰 확장**: 수동 설계 웨이브(`WaveStage1~5`)는 `ScriptableObjects/SpawnWave/TutorialWaveStage/`에, 랜덤 풀용 웨이브 7종(`WavePoolNormal_*` 4개, `WavePoolBoss_*` 3개)은 `ScriptableObjects/SpawnWave/` 바로 아래에 있다. 인스펙터 등록 대상은 `BattleDirector`가 아니라 **`MonsterSpawner`**의 `_monsterWaves`/`_randomWavePool`/`_bossStageInterval`(비어 있으면 수동 웨이브 순환으로 폴백)
- **몬스터 데이터 세팅** (`ScriptableObjects/Monster/`): `MinionSO`=Normal·스킬 없음, `MageSO`/`RogueSO`=Elite·**단일 대상 스킬**, `WarriorSO`=Boss·**전체(라인) 스킬**. 스킬 내용은 각 `SkillSO` 에셋에 있고 몬스터 SO는 참조만 한다. 등급 차이는 `Tier`가 아니라 연결된 스킬의 유무·범위로 표현(`MonsterAiSelector`/`CreateSkill`은 `Tier`를 보지 않음) — 다만 `SpawnWaveSO.IsBossWave`(보스 BGM·보스 웨이브 강제 판정)는 `Tier==Boss`만 보므로, Elite만으로 구성된 웨이브는 스킬을 써도 "보스 웨이브" 취급되지 않는다(의도된 동작)
- **애니메이터**: 캐릭터/몬스터 컨트롤러는 `Spawned/Idle/Attack/Hit/Die` 구조(베이스+override). 몬스터 베이스(`Skeleton_Minion.controller`)에 `Skill` 트리거/스테이트가 있고, **스킬 전용 클립이 실제로 연결됨** — `Skill_SkeletonMage`/`Skill_SkeletonRogue`/`Skill_SkeletonWarrior`가 각 override 컨트롤러에 물려 있다(더 이상 Attack 클립을 공유하지 않음). 캐릭터/몬스터 프리팹 + 애니메이션 연결 완료
- **사운드 (`Scripts/Audio/{Data,View}` + `Scripts/Systems/AudioManager`)**: `AudioManager`(Singleton, BGM 소스 2개로 크로스페이드 — 같은 클립이면 무시해 스테이지가 넘어갈 때 처음부터 다시 재생되지 않음 — SFX는 단일 소스 `PlayOneShot`, Master/BGM/SFX 3단 볼륨을 `SaveData.Options`에서 초기화), `AudioLibrarySO`(씬별 BGM + 전투/보스 BGM + UI클릭/**UI이동/확정**/승리/패배/크리티컬 SFX — `UiNavigate`/`Confirm`은 전용 클립 미할당 시 `UiClick`으로 폴백), `UnitSfxSO`(유닛별 등장/공격/스킬/피격/사망, `UnitView`에 연결). `UiClickSfx`는 UIDocument 루트에 `ClickEvent` 버블링을 걸어 **마우스** 버튼 클릭음을 재생(기존 UI 코드 무수정). **키보드** 조작음은 `AudioManager`의 정적 헬퍼(`UiClick`/`UiNavigate`/`Confirm`)를 각 패널의 키보드 분기에서 직접 호출 — 마우스 경로(ClickEvent)와 겹치지 않게 키보드에서만 울림. 소리 매핑: 방향키 카드 이동=`UiNavigate`, 로그라이크/캐릭터선택 확정=`UiClick`, 배틀 타겟 확정(마우스·키보드 공통)=`Confirm`. IntroScene·BattleScene 양쪽에 `AudioManager`를 배치해 BattleScene 단독 실행 테스트도 지원(중복 인스턴스는 고친 `Singleton`이 자동 파괴). 클립을 하나도 연결하지 않아도 모든 재생 경로가 조용히 넘어감(null 가드)
- **옵션 볼륨**: `OptionPopup.uxml`에 마스터/BGM/SFX 슬라이더 3개(`OptionPopupUI`). 드래그 중 `AudioManager`에 즉시 반영, 닫을 때(`Hide` 오버라이드) 1회만 `SaveService.Save()`
- **성향 포인트 배분**: `PointAllocationPopupUI`(`OptionPopupUI`와 같은 오버레이 팝업 패턴). 캐릭터 선택 화면 `select-footer`의 '성향' 버튼(`CharacterSelectPanelUI.OnAllocationClicked` → `GameUIController`가 `Show`)으로 열림. 카테고리 행(9종)은 인스펙터에 할당된 `RoguelikeChoiceSO[]`를 기반으로 C#에서 동적 생성(이름은 `.Title`, 매핑 키는 `.Category` 재사용, 하드코딩 없음). `[+]`/`[-]`가 `SaveData.SetPoints`를 즉시 갱신하고 헤더(`보유/총`)·버튼 활성 상태를 다시 그림, 초기화 버튼은 `ResetPoints()`. 몇 스테이지마다 1점인지는 `_stagesPerPoint`(인스펙터, 밸런싱 값)로 `GetEarnedPoints`에 넘김. 닫을 때(`Hide` 오버라이드) 1회만 `SaveService.Save()`(옵션 팝업과 동일 패턴). `RoguelikeRewardService.PickChoices`가 가중치 계산에 `SaveService.Current.GetPoints(category) * _weightPerPoint`(인스펙터, 기본 1)를 더해 추첨에 실제로 반영

### 아직 안 된 것
- **영입 후보 카드 표시 방식 변경 예정**: 현재는 이름 + 스탯 6줄(HP/ATK/SPD/DEF/치명타/저항) 텍스트. 캐릭터를 상징하는 **아이콘을 `CharacterStatsSO`에 추가**하고, 카드를 `아이콘 + 이름` 형식으로 바꿀 것. 이때 **이름 텍스트의 폰트 크기를 지금의 절반 정도로 축소**(카드 크기가 아니라 글자 크기). `RoguelikeRewardService.DescribeCandidate`와 `ChoiceCard`(현재 제목+설명 2필드)에 아이콘 필드 확장 필요
- **옵션 메뉴(해상도/언어)**: 볼륨(마스터/BGM/SFX)은 구현 완료. 해상도·전체화면·언어는 `SaveData.Options` 필드만 있고 UI·적용 로직 미구현
- **Elite/Boss 스킬 연출**: 스킬 애니메이션 클립·`MonsterStatsSO` 세팅(Elite=단일, Boss=전체)은 끝났고, 남은 것은 스킬 사용 시 이펙트(파티클). `BattlePresenter.PlayActionAsync`에 스폰 훅을 넣을 자리가 있다
- **타겟팅 피드백**: 겨냥된 단일 대상 아웃라인(빨강)은 있으나, 플레이어 차례에 **유효 대상 전체 하이라이트/커서 피드백**은 미구현(`TargetingController` TODO). 방향키 순환·확정과 마우스 2단계 클릭은 구현 완료
- `Assets/InputSystem_Actions.inputactions`의 **Player 맵은 여전히 비어 있음**. 입력은 `InputManager`로 모았고 타겟팅은 더 이상 `Mouse.current`를 직접 쓰지 않지만, 배틀 키(방향키/확정)는 아직 asset의 정식 액션맵이 아니라 코드로 만든 `InputAction`이다. 커맨드 배틀에 맞는 asset 액션맵 재정의는 추후(TODO 4-2)
- **밸런싱 전반 TBD**: 캐릭터/몬스터 스탯, 선택지 수치·가중치, `StageScaling` 성장률 모두 임시값. 난이도 조정은 `ScriptableObjects/StageScaling.asset`(특히 `_monsterCompound` 복리 토글)부터 만지는 게 효과가 큼

## 아키텍처 방향 (README.md 기획 기반)

아래는 기획 문서와 위 "작업 원칙"에서 도출된 목표 구조이며, 현재 코드도 이 방향을 따른다. 미구현 부분도 이 방향을 유지한다.

- **씬 흐름**: 기획상 `타이틀 → 캐릭터 선택 + 성향 포인트 배분 → 전투`이며, 실제로는 앞의 두 단계를 **`IntroScene` 하나**에 오버레이로 합쳐 `IntroScene → BattleScene` 2씬으로 구현했다(기획 문서의 SettingScene/SelectWeaponScene은 같은 화면을 가리키는 옛 이름). 세부 화면은 씬 전환이 아니라 오버레이/팝업으로 처리
- **Core / View 분리**: 턴 순서(State Machine), 데미지 계산, 스탯/버프 등 전투 규칙은 Unity API 의존 없는 순수 C# 클래스로; MonoBehaviour는 애니메이션·이펙트·UI 갱신 등 연출 전담이며 Core의 이벤트를 구독하는 방식으로만 갱신
- **데이터**: 캐릭터/몬스터 스탯, 로그라이크 선택지(9종), 스폰 패턴은 하드코딩 대신 ScriptableObject로 관리
- **UI 레이어 분리**: 화면 UI(HUD, 메뉴, 팝업)는 UI Toolkit, 월드스페이스 UI(몬스터 체력바 등)는 uGUI로 별도 구현
