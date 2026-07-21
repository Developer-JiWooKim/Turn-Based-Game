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
로그라이크 루프(선택지 9종·파티 영입/교체·스테이지 스케일링·6스테이지 이후 랜덤 스폰·사망 시 영구 추방)와 전멸 결과 화면까지 돌아가며, 남은 것은 주로 영구 포인트 배분 UI·옵션 메뉴·밸런싱.

### 구현된 것
- **씬**: `Assets/MyAssets/Scenes/`에 `IntroScene`(타이틀+캐릭터 선택), `BattleScene`(전투), `AnimationMakeScene`(작업용) 존재. Build Settings에 IntroScene(0)·BattleScene(1) 등록됨. 씬 흐름: IntroScene에서 Title→CharacterSelect(오버레이) → `LoadScene("BattleScene")`, 전멸 시 BattleScene→`LoadScene("IntroScene")`
- **인프라**: `Scripts/Singleton/`(`Singleton<T>` 베이스, `GameManager` — 씬 전환/페이드 + **`CurrentRun`/`BeginRun`**으로 런 데이터 보관), `Scripts/Utility/FadeScreenEffect`(Unity `Awaitable` 기반 async). ⚠️ Fade Canvas는 GameManager 자식으로 두어야 씬 전환에도 파괴되지 않음
- **영구 저장 (`Scripts/Save/`)**: `SaveData`(런을 넘어 유지되는 값 = 최고 스테이지 + 카테고리별 영구 포인트 투자 내역 + 옵션 설정, `JsonUtility` 직렬화를 위해 public 필드만 사용) + `SaveService`(static, `save.json` 읽기/쓰기). **저장 위치가 에디터/빌드에서 다름** — 에디터는 확인·삭제가 쉽도록 프로젝트 루트의 `SaveData/`(`.gitignore` 등록됨, Assets 바깥이라 `.meta` 안 생김), 빌드는 `Application.persistentDataPath`. 빌드된 게임은 프로젝트 폴더와 무관하고 설치 경로가 읽기 전용일 수 있어 루트 방식을 쓸 수 없다. 최초 접근 시 1회 로드 후 캐싱하고, 파일이 없거나 깨져 있으면 기본값으로 복구한다. `GameManager`가 없는 상태(BattleScene 직접 실행)에서도 동작하도록 static으로 둠. 영구 포인트 **획득량은 저장하지 않고** `BestStage`에서 파생(`GetEarnedPoints`)해 값이 어긋날 여지를 없앰. 필드 추가는 기존 세이브와 호환되며(없는 필드는 초기값 유지), 필드 의미가 바뀔 때만 `Version`을 올려 `SaveService.Normalize`에서 마이그레이션
- **런 데이터 (`Scripts/Run/`)**: `RunData`가 런 전체 상태를 소유하고 씬을 넘어 `GameManager`가 보관(캐릭터 선택 시 `BeginRun`으로 생성). `RunMember`(파티원 1명 = 원본 SO + 성장 누적 `Stats` + `BaseStats` 스냅샷 + 현재 HP + 런 내내 고정인 `UnitId`) 리스트, `PendingModifiers`(다음 스테이지 몬스터 디버프 예약), `CurrentStage`, `NextUnitId()` 발급기를 가짐. 선택지 효과 적용(`ApplyChoice`)·스테이지 자동 성장(`ApplyStageGrowth`)·전투 결과 반영(`SyncFromBattle`)·사망자 영구 추방(`RemoveFallen`)·영입(`Recruit`)·파티가 꽉 찼을 때 교체(`ReplaceMember`)가 전부 여기 있고 View는 결과만 화면에 반영. `PreviewRecruitStats`는 `Recruit`과 같은 소급 성장 계산(`ApplyCatchUp`)을 공유해, 영입 후보 카드에 뜨는 값과 실제 합류 결과가 항상 일치하도록 함
- **UI 흐름 (View)**: `Scripts/UI/` — `GameUIController` + `GameFlowFSM`(순수 C# 상태 머신) + `IGameFlowState`(Title/CharacterSelect), 각 패널은 UI Toolkit(`BasePanelUI` 상속). `CharacterSelectPanelUI`는 `CharacterRosterSO` 6종 순환/선택, `CharacterPreview`가 선택 시 3D 모델(프리뷰 카메라→RenderTexture) 교체, 선택 시 스탯 바 6종(HP/ATK/SPD/DEF/치명타/저항)을 로스터 내 최댓값 대비 비율로 갱신(`RefreshStats`, 밸런싱 값 하드코딩 없이 로스터에서 계산). 전투 시작 시 선택 캐릭터로 `BeginRun`
- **전투 Core (`Scripts/Battle/Core/`, 순수 C#·UnityEngine 비의존)**: `Stats`, `Unit`, `SkillProfile`, `DamageCalculator`(비율감소+크리), `TurnOrder`(SPD 정렬), `BattleState`, `IRandom`/`SystemRandom`, 액션/셀렉터(`IActionSelector`, `PlayerActionSelector`=입력 await, `MonsterAiSelector`=노멀 공격/보스 스킬우선), `BattleEvents`, `BattleSimulation`(async 턴 루프, `BattleOutcome` 반환). **연출 동기화**: 이벤트 인자의 `RegisterPlayback(Task)`로 View 애니메이션 완료를 시뮬레이션이 대기
  - `RoguelikeEffect`: 선택지 1종의 순수 효과. 파티 강화 flat 8종 + 몬스터 디버프 3종 + 영입 플래그를 한 struct에 담음(카테고리별 구현체로 쪼개지 않음). `ApplyTo(Stats)`는 스탯만 더하고 "채워야 할 HP량"을 반환 — 현재 HP 규칙은 호출자(`Unit`/`RunMember`)가 각자 처리
  - `RunModifiers`: 다음 스테이지 몬스터에만 1회 적용되는 디버프 예약함(`Add` 배율 곱연산 중첩 → `ApplyTo` → `Consume`)
  - `StageScaling`: 스테이지별 양측 스탯 성장. 몬스터는 스폰 시 **배율**(복리 옵션), 플레이어는 HP를 이어받으므로 배율 대신 `BaseStats` 기준 **flat 증가를 영구 누적**. `CreatePlayerGrowth(baseStats, step)`은 매 스테이지 개별 반올림 대신 "누적 총량(`기준값×비율×step`)의 차분"을 돌려줘 반올림 오차가 쌓이지 않음(과거엔 스테이지마다 최소 +1 바닥값을 둬서 저스탯 캐릭터가 비정상적으로 급성장하는 문제가 있었음). SPD·치명타·저항은 스케일링 제외(로그라이크 선택지로만 성장)
  - `WeightedPicker`: 가중치 비례 + 중복 없는 추첨. 추후 영구 포인트(카테고리별 가중치 투자) 시스템도 이 위에 얹음
  - 몬스터 1턴 행동불가는 상태이상 레이어 없이 `BattleSimulation`의 `enemySkipFirstTurn` 플래그 1줄 분기로 처리(현재 필요한 게 이 한 종류뿐). 개별 유닛 기절/침묵이 생기면 `Unit`에 상태이상 레이어를 따로 둘 것
- **전투 View (`Scripts/Battle/View/`)**: 역할별로 4개 컴포넌트로 분리되어 있고 전부 BattleDirector 오브젝트에 붙어 있음
  - `BattleDirector`: 스테이지 루프 오케스트레이터. 런 해석 → 웨이브 구성(기준 스탯 → `StageScaling` → `PendingModifiers` 순서) → 시뮬레이션 구동 → 결과를 `RunData`에 반영 → 진급/자동성장/선택지. 파티 `Unit`은 **스테이지마다 `RunMember`에서 새로 생성**(성장 반영 + HP 계승), `UnitId`가 고정이라 View는 재사용. 웨이브 선택(`ResolveWave`/`PickFromPool`)은 `_monsterWaves`(1~N스테이지 수동 설계) 범위를 넘으면 `_randomWavePool`에서 `_bossStageInterval` 배수 여부에 따라 보스/일반 풀을 갈라 가중치 추첨(풀이 비어 있으면 기존 배열 순환으로 안전하게 대체)
  - `UnitViewRegistry`: 슬롯 배치·`Id→UnitView` 조회·스폰/정리·체력바 갱신. 파티 슬롯 점유 현황을 관리해 추방·교체로 빈 자리를 영입 시 재사용
  - `BattlePresenter`: Core 이벤트 구독/해제(`Bind`가 먼저 `Unbind`를 호출해 짝이 어긋날 수 없음) → 공격·피격·사망 연출, HUD 갱신, 카메라 쉐이크
  - `RoguelikeRewardService`: 가중치 추첨 → 선택지 패널 제시 → `RunData.ApplyChoice` 호출. 영입 선택지를 고르면 후보 카드 제시(파티가 꽉 찼으면 "영입 안 함" 카드를 추가) → 실제로 영입을 고르면 현재 파티원 카드로 교체 대상을 이어서 물음 → `RunData.Recruit`/`ReplaceMember` 호출. `RecruitResult`(영입된 멤버 + 교체로 나간 멤버)를 반환해 `BattleDirector`가 View 스폰/제거를 처리
  - `BattleResultPanel`: 전멸 시 도달 스테이지 + 최고 기록을 보여주는 결과 팝업(UI Toolkit, 확인 버튼 대기 후 IntroScene 전환). 신기록 여부는 패널이 직접 비교하지 않고 `SaveService.RecordStage`의 반환값을 받아 표시만 한다(동점을 신기록으로 처리하지 않기 위함). `HandleDefeatAsync`는 저장 **전** 기록을 읽어 "이전 최고 기록"으로 넘긴다
  - 그 외: `UnitView`(`UnitAnimator`/`UnitHealthBar`를 물고 제어), `UnitAnimator`(트리거 재생 + 연출 길이 반환), `UnitHealthBar`(uGUI 월드스페이스 게이지 + TMP 숫자 표기), `TargetingController`(몬스터 클릭→`SubmitTarget`), `BattleHUD`(스테이지/턴 순서/현재 행동 유닛/플레이어 차례 프롬프트), `RoguelikeChoicePanel`(카드 4장, 선택 대기 await), `CameraShake`
- **전투 Data (`Scripts/Battle/Data/`)**: `UnitStatsSO`(베이스) + `CharacterStatsSO`/`MonsterStatsSO`(임시 스탯, 밸런싱 TBD), `CharacterRosterSO`(선택 6종), `SpawnWaveSO`(스테이지별 몬스터 구성 + `Weight`/`IsBossWave`— 후자는 `MonsterStatsSO.Tier`로 계산해 별도 플래그와 어긋날 여지 없음), `RoguelikeChoiceSO`(9종 카테고리 + 효과 수치 + 등장 가중치), `StageScalingSO`(양측 성장률 6종 + 복리 토글)
- **로그라이크 선택지 9종**: 에셋은 `ScriptableObjects/RoguelikeChoice/`에 전부 존재. 승리 시 가중치 추첨으로 3개 제시 → 1개 선택 → 즉시 적용. 영입 선택지는 파티가 꽉 차도 후보에서 빠지지 않고(`_weightPerEmptySlot`는 빈자리가 많을수록 자주 뜨게만 함), 고르면 교체 대상을 플레이어가 선택(위 `RoguelikeRewardService` 참고). 후반 영입/교체 캐릭터에는 스테이지 자동 성장분만 소급 적용(선택지로 얻은 성장은 소급하지 않음). `Heal`은 `_healFlat`(즉시 회복)을 쓰고 `_hpFlat`(최대 HP 영구 증가)은 0 — 과거에 이 둘이 뒤바뀌어 있던 데이터 실수를 수정함
- **스테이지 스폰 확장**: `ScriptableObjects/SpawnWave/`에 수동 설계 웨이브(`WaveStage1~5`) 외에 랜덤 풀용 웨이브 7종(`WavePoolNormal_*` 4개, `WavePoolBoss_*` 3개, 기존 Minion/Mage/Rogue/Warrior 조합) 존재. `BattleDirector`의 `_randomWavePool`/`_bossStageInterval` 인스펙터 필드에 등록해야 실제로 쓰임(비어 있으면 수동 웨이브 순환으로 폴백)
- **애니메이터**: 캐릭터/몬스터 컨트롤러는 `Spawned/Idle/Attack/Hit/Die` 구조(베이스+override). 몬스터 베이스(`Skeleton_Minion.controller`)에 `Skill` 트리거/스테이트 추가됨(현재 Attack과 같은 클립 참조 — 보스 스킬 클립 준비되면 교체). 캐릭터/몬스터 프리팹 + 애니메이션 연결 완료

### 아직 안 된 것
- **영입 후보 카드 표시 방식 변경 예정**: 현재는 이름 + 스탯 6줄(HP/ATK/SPD/DEF/치명타/저항) 텍스트. 캐릭터를 상징하는 **아이콘을 `CharacterStatsSO`에 추가**하고, 카드를 `아이콘 + 이름` 형식으로 바꿀 것. 이때 **이름 텍스트의 폰트 크기를 지금의 절반 정도로 축소**(카드 크기가 아니라 글자 크기). `RoguelikeRewardService.DescribeCandidate`와 `ChoiceCard`(현재 제목+설명 2필드)에 아이콘 필드 확장 필요
- **성향 포인트 배분 팝업**: 저장 레이어(`SaveData.CategoryPoints`·`GetEarnedPoints`/`SetPoints`/`ResetPoints`)는 준비됐지만 **배분 UI와 가중치 반영 로직이 미구현**. 투자한 포인트를 실제 추첨에 반영하려면 `RoguelikeRewardService`가 `RoguelikeChoiceSO.GetWeight` 결과에 `SaveData.GetPoints(category)`를 더해 `WeightedPicker`에 넘기면 됨. "몇 스테이지마다 1점"인지는 밸런싱 값이라 SO로 빼서 `GetEarnedPoints`에 넘길 것(현재 하드코딩 없음)
- **옵션 메뉴**: `SaveData.Options`(볼륨·해상도·전체화면·언어) 필드만 있고 옵션 UI와 실제 적용 로직은 미구현
- **파티 시너지**(동일 캐릭터 2명 이상 시 추가 효과) 미구현
- **타겟팅 피드백**: 유효 대상 하이라이트/커서 피드백 미구현(`TargetingController`에 TODO)
- `Assets/InputSystem_Actions.inputactions`는 Unity 기본 템플릿 그대로(타겟팅은 `Mouse.current` 직접 사용). 커맨드 배틀에 맞는 정식 재정의는 추후
- **밸런싱 전반 TBD**: 캐릭터/몬스터 스탯, 선택지 수치·가중치, `StageScaling` 성장률 모두 임시값. 난이도 조정은 `ScriptableObjects/StageScaling.asset`(특히 `_monsterCompound` 복리 토글)부터 만지는 게 효과가 큼

## 아키텍처 방향 (README.md 기획 기반)

아래는 기획 문서와 위 "작업 원칙"에서 도출된 목표 구조이며, 현재 코드도 이 방향을 따른다. 미구현 부분도 이 방향을 유지한다.

- **씬 흐름**: `TitleScene → SelectWeaponScene(캐릭터 선택 + 성향 포인트 배분) → BattleScene`, 세부 화면은 오버레이/팝업으로 처리(씬 전환 아님)
- **Core / View 분리**: 턴 순서(State Machine), 데미지 계산, 스탯/버프 등 전투 규칙은 Unity API 의존 없는 순수 C# 클래스로; MonoBehaviour는 애니메이션·이펙트·UI 갱신 등 연출 전담이며 Core의 이벤트를 구독하는 방식으로만 갱신
- **데이터**: 캐릭터/몬스터 스탯, 로그라이크 선택지(9종), 스폰 패턴은 하드코딩 대신 ScriptableObject로 관리
- **UI 레이어 분리**: 화면 UI(HUD, 메뉴, 팝업)는 UI Toolkit, 월드스페이스 UI(몬스터 체력바 등)는 uGUI로 별도 구현
