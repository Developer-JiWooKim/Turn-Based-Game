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

7. **중괄호는 항상 쓴다** (2026-08 전체 코드베이스에 일괄 적용됨)
   - `if`/`for`/`foreach`/`while`/`using`은 본문이 한 줄이어도 `{ }`로 감싼다 — `if (x == null) return;` 같은 한 줄 축약형을 쓰지 않는다
   - 예외 없음. 새 코드도 기존 코드와 같은 모양이어야 하므로 이 규칙을 먼저 맞춘다
   - 식 본문 멤버(`=> ...`)는 이 규칙과 무관하며 그대로 사용한다

## 하지 말아야 할 것
- 전투 로직을 View(MonoBehaviour)에 직접 구현하지 않기
- 매직 넘버/하드코딩된 밸런싱 값 추가하지 않기 (SO 데이터로 분리)
- 최적화를 이유로 불필요하게 스크립트를 쪼개거나 코드량을 늘리지 않기
- 사전 논의 없이 큰 구조 변경 진행하지 않기
- 중괄호 없는 한 줄 `if`/`for` 작성하지 않기 (작업 원칙 7)

## 개발 환경

- Unity **6000.5.3f1** (`ProjectSettings/ProjectVersion.txt` 참고), Unity Hub/Editor로 프로젝트를 열어서 작업
- 별도 CLI 빌드/린트/테스트 스크립트나 CI 파이프라인은 구성되어 있지 않음 — 빌드·플레이 테스트는 Unity Editor에서 직접 수행
- `com.unity.test-framework` 패키지는 설치되어 있지만 아직 테스트 어셈블리/테스트 코드가 없음. 테스트 추가 시 Editor의 Window > General > Test Runner 사용
- `Turn-Based-Game.slnx`는 Unity가 자동 생성한 솔루션 파일로, 대부분의 `.csproj`는 Unity 패키지/에디터 어셈블리이며 실제 게임 코드는 `Assembly-CSharp.csproj`(= `Assets/MyAssets/Scripts/`)에 해당

## 현재 코드 상태 (2026-08 기준)

전체 세로 슬라이스(Intro→캐릭터 선택→전투→승리 시 성장 선택지→다음 스테이지→전멸 시 리타이어)가 실제 Unity에서 작동 검증됨.
로그라이크 루프(선택지 9종·파티 영입/교체·파티 시너지·스테이지 스케일링·6스테이지 이후 랜덤 스폰·사망 시 영구 추방)와 전멸 결과 화면, BGM/SFX, 성향 포인트 배분, 배틀 퍼즈(ESC/HUD 버튼·중단)까지 돌아간다. 남은 것은 보스 스킬 이펙트, 옵션 메뉴(해상도/언어), 밸런싱.

### 구현된 것
- **씬**: `Assets/MyAssets/Scenes/`에 `IntroScene`(타이틀+캐릭터 선택), `BattleScene`(전투), `AnimationMakeScene`(작업용) 존재. Build Settings에 IntroScene(0)·BattleScene(1) 등록됨. 씬 흐름: IntroScene에서 Title→CharacterSelect(오버레이) → `LoadScene("BattleScene")`, 전멸 시 BattleScene→`LoadScene("IntroScene")`
- **널 체크 양식 (`Systems/NullCheck`)**: "비어 있으면 로그를 남긴다"를 한곳에 모은 공용 헬퍼. 프로젝트의 모든 null 보고가 `[클래스명] 무엇이 어떻다 — 결과(인스펙터 확인).` 형태로 통일된다. 메서드 3종:
  - `LogIfMissing(Object, nameof(…), this, "결과")` — **인스펙터 참조**. 메시지에 "(인스펙터 확인)"이 붙는다
  - `LogIfEmpty(array, …)` — **인스펙터 배열**(슬롯 목록, 선택지 풀). null과 길이 0을 함께 잡는다
  - `LogIfNull(object, …)` — **일반 참조**(생성자 인자, `Q<Slider>()` 같은 UXML 조회 결과). "(인스펙터 확인)"이 붙지 않는다
  - ⚠️ **`UnityEngine.Object`를 `LogIfNull`에 넘기면 컴파일 에러**다(`[Obsolete(error: true)]` 오버로드). 일반 `object`로 받으면 Unity의 `==` 오버로드가 사라져 **파괴된 객체가 null이 아닌 것으로 통과**하기 때문 — `Game.Core`의 `noEngineReferences`처럼 실수를 컴파일러가 막게 해뒀다
  - 반환값을 `hasError |= …`로 누적해 **빠진 것을 한 번에 모두** 보고한다(하나 고치면 또 걸리는 식이 되지 않도록). 접두어는 `owner.GetType().Name`에서 뽑으므로 클래스명을 문자열로 적지 않는다 — 과거에 복붙으로 다른 클래스 이름이 남아 엉뚱한 파일을 뒤진 적이 있다. `owner as Object`로 컨텍스트를 넘겨 MonoBehaviour면 콘솔 클릭 시 해당 오브젝트가 선택된다
  - 호출 시점은 `Awake`(또는 생성자) 1회 + `#if UNITY_EDITOR`의 `OnValidate`(플레이 전에 인스펙터에서 발견). **예외: `UnitView`는 `OnValidate`를 두지 않는다** — 인스펙터 공란이 정상이고(`GetComponentInChildren`으로 자동 탐색) 탐색 전에는 항상 비어 보여 거짓 경고가 난다
  - 적용: `BattleDirector`·`UnitViewRegistry`·`UnitView`·`RoguelikeRewardService`·`GameManager`·`InputManager`·`InputBindingSaver`·`InputRebinder`·`CharacterPreview`·`CharacterSelectPanelUI`·`GameUIController`·`BasePanelUI`·`OptionPopupUI`·`AllocationRowsView`
  - **제외 대상**: 도메인 상태 오류(`BattleDirector`의 "파티가 비어 있어"), 파일 I/O 예외(`SaveService`), 조회 실패(`InputRebinder`의 액션 경로 — 참조가 null인 게 아니라 이름이 어긋난 것이라 자체 메시지가 더 정확하다)
  - ⚠️ **검증 대상은 "없으면 동작 불가능한 참조"뿐이다.** 값 타입(float/int/string)은 null이 될 수 없고, 선택 참조(Tooltip에 "비워두면 ~"이라 적힌 것들 — `_pausePanel`, `_sfx`, `_camera` 등)는 없어도 동작하는 게 설계라 사용처 null 체크가 맞다
  - **누락 시 처리는 두 갈래**: *필수*는 `_isValid` 플래그로 캐싱해 공개 메서드를 막고(`CharacterPreview`, `GameUIController`는 `enabled = false`까지), *연출/선택*은 보고만 하고 사용처가 매번 `!= null`로 건너뛴다(`GameManager._fadeScreenEffect`). **런타임에 파괴될 수 있는 참조는 캐싱하면 안 된다** — Fade Canvas를 GameManager 자식으로 두지 않으면 씬 전환 때 파괴되는데, Awake 시점 스냅샷인 플래그는 그걸 잡지 못한다
    - 플래그 이름은 `_isValid`로 통일한다(`GameUIController`·`CharacterPreview`·`InputRebinder`·`InputBindingSaver`). "준비 완료"가 아니라 **검증 결과**를 담는 값이라 `_isReady`는 쓰지 않는다. `= true` 초기화는 생성자 early-return이 있는 순수 C# 클래스(`InputRebinder`)에만 두고, `Awake`에서 대입하는 MonoBehaviour에는 붙이지 않는다 — 항상 덮어써서 죽은 코드인 데다 "검증 전 기본값은 유효"로 반대로 읽힌다
    - ⚠️ **`enabled = false`로 끄는 경우, 구독 해제 가드는 `OnEnable`·`OnDisable` 양쪽에 대칭으로 둔다.** `Awake`에서 끈 컴포넌트는 한 번도 활성화된 적이 없어 `OnEnable`·`Start`뿐 아니라 **`OnDisable`도 호출되지 않는다** — 가드가 실제로 필요한 건 런타임에 누가 `enabled = true`로 되살리는 경로다. `OnDisable`에만 달면 그때 `OnEnable`이 null 참조로 NRE를 내며 구독 도중에 끊기고, 이어지는 `OnDisable`은 플래그 때문에 return해 **구독이 반쯤 걸린 채 남는다**
- **인프라 (`Scripts/Systems/`)**: `Singleton<T>` 베이스 + 전역 매니저. `GameManager`(씬 전환/페이드 + **`CurrentRun`/`BeginRun`**으로 런 데이터 보관), `AudioManager`(BGM 크로스페이드·SFX·Master/BGM/SFX 3단 볼륨), `FadeScreenEffect`(Unity `Awaitable` 기반 async). ⚠️ Fade Canvas는 GameManager 자식으로 두어야 씬 전환에도 파괴되지 않음
- **입력 허브 (`Scripts/Systems/InputManager`)**: 흩어져 있던 플레이어 입력을 한곳으로 모으는 Singleton(DontDestroyOnLoad). 바인딩은 코드에 하드코딩하지 않고 **`InputSystem_Actions.inputactions` 에셋의 액션맵에서 온다** — `InputManager`가 `[SerializeField] InputActionAsset`을 참조해 `FindAction`으로 조회한다(생성 래퍼 클래스는 제거, `.meta`의 `generateWrapperCode: 0`). View(예: `TargetingController`)는 `Mouse.current` 같은 원시 디바이스를 만지지 않고 이 매니저만 참조한다. 맵 3개: **Battle**(타겟 순환 `CyclePrev/Next`·확정 `Confirm`, 게이트 적용), **Menu**(메뉴 방향키 `NavPrev/Next`·확정 `Submit`·퍼즈 `Pause`, 게이트 무관), **UI**(템플릿 기본 — 마우스 `Point`/`Click`, EventSystem의 InputSystemUIInputModule도 이 맵을 공유). 방향키가 Battle·Menu에 겹치지만 두 맥락은 시간상 겹치지 않아(타겟팅=전투 턴, 메뉴=턴 밖) 충돌하지 않는다. `IsGameplayInputEnabled`는 배틀 입력만 막는 게이트(퍼즈용) — 맵 Enable/Disable로 대체하지 않고 병행한다(퍼즈의 프레임 정밀 복구가 이 게이트에 의존). 노출 API: `PointerPosition`/`PrimaryPressedThisFrame`, `BattleCyclePrev/Next`·`BattleConfirm`, `UiNavigatePrev/Next`·`UiSubmit`, `PauseToggle`(ESC — 게임플레이 게이트에 묶지 않는다. 묶으면 퍼즈 중에 ESC로 못 푼다)
  - ⚠️ **EventSystem의 UI Input Module에서 `m_MoveAction`/`m_SubmitAction`을 끊어 둔다(두 씬 모두 `{fileID: 0}`)**. 방향키/Enter는 우리가 Menu 맵으로 직접 처리하는데, 내장 네비게이션까지 켜져 있으면 방향키가 UI Toolkit 버튼에 포커스를 옮기고 Enter가 그 버튼을 눌러 이중 동작한다(캐릭터 선택에서 방향키→Enter가 prev 버튼을 누르거나, 배틀에서 Enter가 퍼즈 버튼을 누르는 버그였다). 마우스(`m_PointAction`/`m_LeftClickAction`)는 유지되므로 클릭·호버는 정상. 키 조작을 새로 추가할 때 이 둘을 다시 연결하지 말 것
  - ⚠️ **액션 경로 문자열의 유일한 출처는 `Systems/InputControls.cs`**(맵 이름 2개 + 액션 경로 9개 상수 + 논리 컨트롤 표 `Rebindable` + `RebindControl` struct). `InputManager`의 액션 캐싱과 리바인딩 표가 같은 상수를 쓰므로 어긋날 수 없다 — 과거엔 같은 경로가 양쪽에 따로 적혀 있어서 한쪽만 갱신하면 **키 설정 UI만 조용히 망가졌다**(버튼에 `-`가 뜨고 눌러도 무반응). 에셋에서 액션 이름을 바꾸면 이 파일만 고치면 된다. 컴파일러가 에셋과 대조해주지는 않으므로, 캐싱은 `throwIfNotFound: true`로 시작 즉시 터뜨리고 리바인딩 경로(`GetRebindDisplay`·`InputRebinder`)는 `LogError`를 남긴다
  - **키 리바인딩**: `StartRebind(controlIndex, onFinished)`가 `PerformInteractiveRebinding`으로 다음 키 입력을 캡처한다. 방향키·확정은 Battle/Menu 두 맵에 같은 키로 존재하므로 **논리 컨트롤 4종**(이전/다음/확정/퍼즈)으로 묶어(`InputControls.Rebindable`) 한 번 재설정하면 양쪽 맵에 함께 적용한다. sibling 액션을 못 찾으면 새 키가 **한쪽 맵에만** 적용되므로 이 경우도 로그를 남긴다. 오버라이드는 `SaveData.Options.InputBindingOverrides`(JSON)에 저장하고 `Awake`에서 `LoadBindingOverridesFromJson`으로 복원. UI는 `OptionPopupUI`가 이 API로 키설정 행을 동적 생성. ⚠️ 두 씬(Intro/Battle)의 InputManager 오브젝트에 `.inputactions` 에셋을 `_actions` 슬롯에 **직접 할당**해야 한다
- **영구 저장 (`Scripts/Progression/Save/`)**: `SaveData`(런을 넘어 유지되는 값 = 최고 스테이지 + 카테고리별 영구 포인트 투자 내역 + 옵션 설정, `JsonUtility` 직렬화를 위해 public 필드만 사용) + `SaveService`(static, `save.json` 읽기/쓰기). **저장 위치가 에디터/빌드에서 다름** — 에디터는 확인·삭제가 쉽도록 프로젝트 루트의 `SaveData/`(`.gitignore` 등록됨, Assets 바깥이라 `.meta` 안 생김), 빌드는 `Application.persistentDataPath`. 빌드된 게임은 프로젝트 폴더와 무관하고 설치 경로가 읽기 전용일 수 있어 루트 방식을 쓸 수 없다. 최초 접근 시 1회 로드 후 캐싱하고, 파일이 없거나 깨져 있으면 기본값으로 복구한다. `GameManager`가 없는 상태(BattleScene 직접 실행)에서도 동작하도록 static으로 둠. 영구 포인트 **획득량은 저장하지 않고** `BestStage`에서 파생(`GetEarnedPoints`)해 값이 어긋날 여지를 없앰. 필드 추가는 기존 세이브와 호환되며(없는 필드는 초기값 유지), 필드 의미가 바뀔 때만 `Version`을 올려 `SaveService.Normalize`에서 마이그레이션
- **런 데이터 (`Scripts/Progression/Run/`)**: `RunData`가 런 전체 상태를 소유하고 씬을 넘어 `GameManager`가 보관(캐릭터 선택 시 `BeginRun`으로 생성). `RunMember`(파티원 1명 = 원본 SO + 성장 누적 `Stats` + `BaseStats` 스냅샷 + **선택지 몫만 따로 센 `ChoiceGrowth`** + 현재 HP + 런 내내 고정인 `UnitId`) 리스트, `PendingModifiers`(다음 스테이지 몬스터 디버프 예약), `CurrentStage`, `NextUnitId()` 발급기를 가짐. 선택지 효과 적용(`ApplyChoice`)·스테이지 자동 성장(`ApplyStageGrowth`)·전투 결과 반영(`SyncFromBattle`)·사망자 영구 추방(`RemoveFallen`)·영입(`Recruit`)·파티가 꽉 찼을 때 교체(`ReplaceMember`)가 전부 여기 있고 View는 결과만 화면에 반영. `PreviewRecruitStats`는 `Recruit`과 같은 소급 성장 계산(`ApplyCatchUp`)을 공유해, 영입 후보 카드에 뜨는 값과 실제 합류 결과가 항상 일치하도록 함
  - ⚠️ **영입자가 기존 파티원보다 약한 건 버그가 아니다**(확정된 규칙, 2026-08 재확인). `ApplyCatchUp`은 **스테이지 자동 성장만** 소급하고 로그라이크 선택지 성장은 소급하지 않는다 — "그건 그 시점 파티가 벌어들인 몫"이라 파티를 살려두는 데 가치를 두는 설계다. 하단 파티 스탯 표기에서 새 영입자만 파란 `(+선택지)` 괄호가 비어 보이는 것이 정상 결과이며, 과거에 이걸 "영입 스탯 불일치"로 두 번 의심한 적이 있다. 바꾸려면 밸런싱 결정이 먼저다
  - **성장 경로가 둘로 나뉜다**: 선택지는 `RunMember.ApplyChoiceGrowth`(→ `ChoiceGrowth`에도 누적), 스테이지 자동 성장·영입 소급은 `ApplyStageGrowth`(집계 안 함). 공용 구현은 private이라 새 성장 경로를 추가할 때 둘 중 하나를 반드시 고르게 된다. 집계는 효과 값을 그대로 더하지 않고 **적용 전후 `Stats`의 차분**을 쓴다 — `RoguelikeEffect.ApplyTo`가 치명타·저항을 1.0으로 클램프하면 넣은 값과 실제 증가분이 달라지기 때문. 하단 파티 스탯 표기가 이 분리에 의존한다
- **파티 시너지 (`Progression/Run/PartySynergyTracker`)**: 같은 캐릭터가 `CharacterStatsSO.SynergyThreshold` 이상 모이면 그 캐릭터들에게만 스탯 보너스(`CreateSynergy()`가 `RoguelikeEffect`를 재사용, HP 증분 0이라 회복 부작용 없음). 런 데이터에 누적하지 않고 전투 중에만 트래커가 붙였다 떼는 방식이라, 대상이 죽어 인원이 임계 밑으로 떨어지면 `OnAllyDied`가 즉시 되돌린다(README 규칙). 조회는 `PartySynergyTracker.GetSynergies()` 하나뿐이며(스테이지 시작·아군 사망 양쪽이 같은 판정을 쓴다) `BattlePresenter.SetSynergies` → `BattleHUD.ShowSynergies` → `SynergyPanelView`로 흐른다
  - **`GetSynergies()`는 미발동 시너지도 함께 돌려준다** — `_aliveCountBySource`가 애초에 "파티의 시너지 보유 캐릭터 전부"라 그대로 순회하면 된다. 발동 여부는 저장하지 않고 `PartySynergy.IsActive`(= 인원 ≥ 임계치)로 파생시킨다
  - ⚠️ **패널의 `×N`은 요구 인원(`SynergyThreshold`)이지 현재 모인 인원이 아니다** — 파티 구성이 바뀌어도 변하지 않는 값이다. 과거엔 이 자리에 현재 인원(`Count`)을 찍어 숫자가 흔들렸다. 현재 인원은 화면에 숫자로 내보내지 않고 **발동/미발동 농담(濃淡)으로만** 나타낸다
- **UI 계층 규약 (`Scripts/UI/`)**: 화면 스크립트를 세 종류로 나눈다 — 섞이기 시작하면 분리한다
  1. **`*PanelUI`/`*PopupUI`** (MonoBehaviour, `BasePanelUI` 상속) = 화면의 주인. **UXML 배선 + Show/Hide + 이벤트 발행만** 하고, "어떤 요소가 무엇에 대응하는가"까지만 안다. 적용 규칙도 도메인 계산도 갖지 않는다(`TitlePanelUI` 19줄이 기준형)
  2. **`*View`** (순수 C#, MonoBehaviour 아님) = 화면 일부의 표현 전담. 컨테이너 `VisualElement`를 `Build`로 넘겨받아 그 안에서만 동작하므로 패널이 필드로 들고 쓴다 — `KeybindListView`(키 설정 목록), `CharacterStatBarsView`(스탯 바 6종), `AllocationRowsView`(포인트 배분 행). 씬 오브젝트를 소유해야 하면 예외적으로 MonoBehaviour(`CharacterPreview`의 프리뷰 리그·모델 캐시)
  3. **규칙의 주인은 UI 밖** — 값의 적용·저장은 `Systems/GameSettings`, 도메인 판정은 데이터 소유자에게 둔다(`SaveData.TryAdjustPoints`가 잔여 포인트 규칙을, `CharacterRosterSO.CreateStatCeiling`이 로스터 최댓값을 소유). 밸런싱 값이 UI 인스펙터에 남아 있는 곳이 아직 있다(`PointAllocationPopupUI._stagesPerPoint`, `RoguelikeRewardService._weightPerPoint`)
  - ⚠️ 입력 폴링(`Update`에서 `InputManager` 조회)은 `CharacterSelectPanelUI`·`RoguelikeChoicePanel`·`BattlePausePanel`·`TargetingController` 4곳에 의도적으로 남겨둔 것이다. 짧고 성능 문제도 아니라서 이벤트화하지 않기로 했다 — 바꾼다면 4곳을 한 번에
- **UI 흐름 (View)**: `Scripts/UI/` — `GameUIController` + `GameFlowFSM`(순수 C# 상태 머신) + `IGameFlowState`(Title/CharacterSelect), 각 패널은 UI Toolkit(`BasePanelUI` 상속). `CharacterSelectPanelUI`는 `CharacterRosterSO` 6종 순환/선택(prev/next 버튼 + **좌/우 방향키**가 동일하게 `Cycle`, `InputManager.UiNavigate*` 사용, 방향키 순환 시 버튼과 같은 클릭음), `CharacterPreview`가 3D 프리뷰 일체(리그·RenderTexture·모델 인스턴스 캐시)를 소유하고 패널은 `Show(character)`/`SetVisible(bool)`로 지시만 한다(**단방향 참조** — 과거엔 프리뷰가 패널의 `OnSelectionChanged`를 구독해 서로를 참조했다). 모델은 파괴/재생성 대신 캐릭터별 1회 생성 후 캐시하며, 앵커가 리그 자식이라 리그를 끄면 모델도 함께 멈춘다. 스탯 바 6종(HP/ATK/SPD/DEF/치명타/저항)은 `CharacterStatBarsView`가 그리고, 바 길이의 기준인 로스터 내 최댓값은 `CharacterRosterSO.CreateStatCeiling()`이 계산한다(밸런싱 값 하드코딩 없이 데이터에서 도출). 전투 시작 시 선택 캐릭터로 `BeginRun`
- **전투 Core (`Scripts/Battle/Core/`, 순수 C#·UnityEngine 비의존)**: `Stats`, `Unit`, `SkillProfile`, `DamageCalculator`(비율감소+크리), `TurnOrder`(SPD 정렬), `BattleState`, `IRandom`/`SystemRandom`, 액션/셀렉터(`IActionSelector`, `PlayerActionSelector`=입력 await, `MonsterAiSelector`=노멀 공격/보스 스킬우선), `BattleEvents`, `BattleSimulation`(async 턴 루프, `BattleOutcome` 반환). **연출 동기화**: 이벤트 인자의 `RegisterPlayback(Task)`로 View 애니메이션 완료를 시뮬레이션이 대기
  - `RoguelikeEffect`: 선택지 1종의 순수 효과. 파티 강화 flat 8종 + 몬스터 디버프 3종 + 영입 플래그를 한 struct에 담음(카테고리별 구현체로 쪼개지 않음). `ApplyTo(Stats)`는 스탯만 더하고 "채워야 할 HP량"을 반환 — 현재 HP 규칙은 호출자(`Unit`/`RunMember`)가 각자 처리
  - `RunModifiers`: 다음 스테이지 몬스터에만 1회 적용되는 디버프 예약함(`Add` 배율 곱연산 중첩 → `ApplyTo` → `Consume`)
  - `StageScaling`: 스테이지별 양측 스탯 성장. 몬스터는 스폰 시 **배율**(복리 옵션), 플레이어는 HP를 이어받으므로 배율 대신 `BaseStats` 기준 **flat 증가를 영구 누적**. `CreatePlayerGrowth(baseStats, step)`은 매 스테이지 개별 반올림 대신 "누적 총량(`기준값×비율×step`)의 차분"을 돌려줘 반올림 오차가 쌓이지 않음(과거엔 스테이지마다 최소 +1 바닥값을 둬서 저스탯 캐릭터가 비정상적으로 급성장하는 문제가 있었음). SPD·치명타·저항은 스케일링 제외(로그라이크 선택지로만 성장)
  - `WeightedPicker`: 가중치 비례 + 중복 없는 추첨. 추후 영구 포인트(카테고리별 가중치 투자) 시스템도 이 위에 얹음
  - `PendingSignal<T>`: "결과가 들어올 때까지 기다리는 한 번짜리 신호". 플레이어 타겟 입력·선택지 카드·결과 확인이 전부 같은 TCS 패턴이라 여기로 모았다(`RunContinuationsAsynchronously`와 `ct.Register` 해제를 빠뜨릴 여지를 없앤다). **`BattlePausePanel`은 일부러 쓰지 않는다** — 그쪽은 신호를 여는 주체(퍼즈)와 기다리는 주체(시뮬레이션)가 달라 소유 모델이 반대다
  - `BattleState`/`TurnOrder`는 조회 결과를 **재사용 버퍼**로 돌려준다(모든 유닛의 모든 행동마다 불리므로). 호출자는 즉시 소비해야 하며, await 너머로 들고 있으면 안 된다 — `PlayerActionSelector`와 `MonsterAiSelector`(라인 스킬)는 그래서 자체 리스트로 복사한다
  - `IPauseGate`: 전투를 멈추는 순수 계약. `BattleSimulation`이 **각 유닛 행동 직전**에만 await하므로 진행 중인 연출이 잘리지 않는다(턴제에 자연스러운 경계). 생성자 인자가 선택적이라 null이면 기존과 동일하게 동작. 구현은 View의 `BattlePausePanel`. **배틀 중단(취소)도 매 유닛 행동 직전에 `ThrowIfCancellationRequested`로 확인한다** — 턴 시작 지점에서만 보면 플레이어 차례 중 중단해도 이번 턴의 남은 몬스터 행동이 다 끝난 뒤에야 결과 화면으로 넘어간다(실제로 겪은 버그)
  - `StatusEffect.cs`: 상태이상 시스템(한 파일에 `StatusKind` 5종 + 부여 정의 `StatusEffect` + 진행 중 상태 `ActiveStatus` + `StatusChangeReason`). `RoguelikeEffect` 선례대로 종류별 구현체로 쪼개지 않는다. **`Stats.Res`가 여기서 소비된다** — 최종 부여 확률 = `ApplyChance × (1 − 대상 RES)`이므로 RES 1.0은 완전 면역(`Unit.TryApplyStatus`)
    - 스탯 감소형(`AtkDown`/`DefDown`/`SpdDown`)은 **`Stats`를 직접 고치지 않고** `Unit.EffectiveAtk/EffectiveDef/EffectiveSpd`로 읽는 시점에 반영한다. 파티 시너지가 이미 `Stats`를 스냅샷/복원 방식으로 조작 중이라, 양쪽이 같은 필드를 쓰면 서로를 덮어쓰기 때문. `DamageCalculator`와 `TurnOrder`가 이 유효 스탯을 쓴다
    - 처리 순서는 `BattleSimulation.ResolveStatusesAsync`에 모여 있다: 자기 차례 시작 시 **도트 피해 → 지속 턴 감소 → 기절 판정**. 기절 여부를 감소 *전*에 읽어야 1턴짜리 기절이 실제로 한 번의 행동을 막는다. 기절한 유닛은 `ActorTurnStarted`를 발생시키지 않고 넘어간다(플레이어 유닛이 기절했을 때 "당신의 차례" 프롬프트가 잘못 뜨지 않도록)
    - 부여는 `ResolveAction`에서 데미지 적용 직후, **살아남은 대상에게만** 시도한다
    - `StatusChanged`는 부여/저항/만료뿐 아니라 **지속 턴이 줄 때마다(`Ticked`)도 발생**시켜야 한다. 빼먹으면 남은 턴 수 표기가 부여 시점 값에서 멈춰 "디버프가 안 걸린 것처럼" 보인다(실제로 겪은 버그)
    - **상태이상은 전투(스테이지) 단위다**(확정된 규칙) — 스테이지를 클리어하면 사라지고 다음 전투는 깨끗한 상태로 시작한다. 파티 `Unit`이 스테이지마다 새로 생성되므로 로직은 자동으로 초기화된다(유지하고 싶다면 `RunMember`에 저장해야 함)
    - 반면 **파티 View는 런 내내 재사용**되므로 표기는 코드가 직접 지워야 한다. 두 지점 모두 필요하다 — 전투 종료 직후 `UnitViewRegistry.ClearStatuses()`(선택지 화면에 남지 않도록)와 스테이지 시작 시 `RefreshStatuses(players)`(실제 상태를 다시 밀어넣음). 빼먹으면 효과는 사라졌는데 글자만 남는다(실제로 겪은 버그)
  - **체력바 상태이상 표기는 아이콘**(2026-08, 과거엔 `STUN`/`PSN`/`ATK-` ASCII 약어였다). 태그 조립은 `UnitHealthBar.IconTag(spriteName)` 한 곳에 모여 있고 상태이상(`Label`)과 스폰 디버프(`MonsterSpawner.DescribeDebuff`)가 같이 쓴다 — 크기(`IconSize`)·기준선(`IconVOffset`)이 두 표기에서 어긋나지 않게 하기 위함
    - 에디터 세팅(**완료됨**): Debuff 아이콘 6종(`Textures/Icons/Debuff/`)으로 TMP Sprite Asset을 **개별 생성**하고(생성 메뉴가 텍스처 1개당 1에셋이라 한 번에 합쳐지지 않는다), 그중 하나를 `HealthBar.prefab`의 `_statusText`에 할당한 뒤 **그 에셋의 Fallback 목록에 나머지 전부**를 넣는다. TMP는 할당된 에셋 → 그 에셋의 Fallback 순으로만 찾으므로, Fallback을 체인의 시작점이 아닌 다른 에셋에 걸면 아무 효과가 없다
      - 현재 체인의 **시작점은 `Debuff_AttackDown.asset`**(`Textures/Icons/Debuff/SpriteAssets/`)이고 나머지 5종이 그 Fallback에 들어 있다. Debuff 아이콘을 추가하면 새 Sprite Asset을 만들어 **이 에셋의 Fallback에** 등록할 것 — 다른 곳에 걸면 조용히 무시된다
    - ⚠️ **`<sprite>`에 없는 속성을 넣으면 태그가 통째로 글자로 출력된다.** 인식하는 건 name/index/anim/color/tint뿐이라 크기를 `scale=`로 주려다 5종 전부가 `<sprite name="...">` 문자열로 찍힌 적이 있다(실제로 겪은 버그 — 이름·Fallback이 멀쩡한데 증상이 "이름 못 찾음"과 똑같아 엉뚱한 데를 오래 뒤졌다). **크기는 바깥의 `<size=%>`로 준다.** 글자로 보이면 ①잘못된 속성 ②스프라이트 이름 불일치 순으로 의심할 것
    - ⚠️ **월드스페이스 체력바의 일반 텍스트는 여전히 ASCII만**: TMP는 폰트 에셋의 글리프 아틀라스에 있는 문자만 그리는데 기본 폰트에 한글·화살표가 없어 네모로 깨진다. 한글을 쓰려면 한글 글리프를 포함한 TMP Font Asset을 만들어 지정할 것(화면 UI는 UI Toolkit이라 이 제약이 없다)
  - **로그라이크 몬스터 디버프 3종의 처리 방식이 서로 다르다**(성격이 달라서 억지로 통일하지 않았다)
    - *행동불가* → 스폰 시 `Stun` 1턴을 **확정 부여**(`Unit.ApplyStatus`, 저항 판정 없음 — 플레이어가 선택지를 소모해 얻은 효과라 몬스터 RES로 무효화되면 안 된다). 과거의 `BattleSimulation.enemySkipFirstTurn` 특수 분기는 제거됨
    - *체력감소·공격력감소* → 상태이상이 아니라 **스폰 시점에 스탯에 녹아드는 배율**(`RunModifiers.ApplyTo`). 지속 턴 개념이 없고 최대 HP는 상태이상으로 표현할 수단이 없어서 그대로 뒀다. 대신 `MonsterSpawner.DescribeDebuff`가 `아이콘 -30%` 형태의 라벨을 만들어 `UnitView.SetSpawnDebuff` → 체력바에 고정 표기한다(상태이상 목록과 별도 필드라 턴마다 갱신되는 아래 줄과 섞이지 않는다). 아이콘 태그는 상태이상과 같은 `UnitHealthBar.IconTag`를 쓴다
- **어셈블리 분리 (asmdef)**: `Game.Core`(`Battle/Core/`) → `Game.Data`(`Battle/Data/` + `Audio/Data/`) → `Game.Progression` → `Game.Systems` → `Game.View`(`Battle/View/` + `UI/` + `Audio/View/`) 순의 단방향 의존. **`Game.Core`는 `noEngineReferences: true`** 라 UnityEngine을 참조하면 즉시 컴파일 에러가 난다(Core/View 분리를 규약이 아니라 컴파일러가 강제). 폴더가 떨어져 있는 `Audio/Data`·`UI`·`Audio/View`는 `.asmref`로 각 어셈블리에 합류시킨다
  - ⚠️ `InputSystem_Actions.cs`(생성 코드)와 `.inputactions` 에셋은 **`Scripts/Systems/`에 있어야 한다**. `Assets/` 루트에 두면 Assembly-CSharp에 속하는데, asmdef 어셈블리는 Assembly-CSharp를 참조할 수 없어 `InputManager`가 컴파일되지 않는다. 에셋 옆에 코드가 생성되므로(`wrapperCodePath` 비어 있음) 에셋만 제자리에 두면 재생성도 같은 폴더로 간다
  - ⚠️ `FadeScreenEffect`는 UI가 아니라 **`Systems/`**에 있다. `GameManager`가 참조하므로 UI에 두면 `Systems↔UI` 순환 참조가 된다
- **전투 View (`Scripts/Battle/View/`)**: 역할별 컴포넌트로 분리되어 있고 전부 BattleDirector 오브젝트에 붙어 있음
  - `BattleDirector`: 스테이지 루프 오케스트레이터. 시뮬레이션 구동 → 결과를 `RunData`에 반영 → 진급/자동성장/선택지. 파티 `Unit`은 **스테이지마다 `RunMember`에서 새로 생성**(성장 반영 + HP 계승), `UnitId`가 고정이라 View는 재사용. 몬스터 구성은 `MonsterSpawner`에, 런의 시작·종료는 `BattleRunFlow`에 위임하고 Director는 흐름만 담당한다. `_stageScaling`은 몬스터뿐 아니라 파티 자동 성장·영입 소급 성장에도 쓰여 Director가 계속 소유
  - `BattleRunFlow`: 런의 경계 전담. `ResolveRun`(런 데이터 조회 + 테스트 파티 폴백)과 `EndRunAsync`(기록 저장 → 결과 팝업 → IntroScene 전환). Director가 세이브·씬 전환·씬 이름까지 알 필요가 없어 떼어냈다
  - `MonsterSpawner`: "이번 스테이지에 어떤 몬스터가 나오는가" 전담. 웨이브 선택(`ResolveWave`/`PickFromPool`)은 `_monsterWaves`(1~N스테이지 수동 설계) 범위를 넘으면 `_randomWavePool`에서 `_bossStageInterval` 배수 여부에 따라 보스/일반 풀을 갈라 가중치 추첨(풀이 비어 있으면 기존 배열 순환으로 안전하게 대체). `SpawnWave`가 몬스터 `Unit` 생성(기준 스탯 → `StageScaling` → `PendingModifiers` 순서)까지 처리. 레지스트리는 `TargetingController`와 같이 `Initialize(registry)` 주입 방식이라 인스펙터에 중복 연결하지 않는다
  - **화면 배치 인덱스 (`A1`/`E2`)**: 체력바 왼쪽에 `[A1]`로, 상단 턴 순서 칩에는 이름 대신 `A1`로 표기해 **칩과 화면 위 유닛을 1:1로 맞출 수 있게** 한다(같은 캐릭터를 2명 영입해도 구분된다). 접두어 A=아군/E=적군, 번호는 1부터. 문자열은 `UnitViewRegistry.CreateSlotLabel` **한 곳에서만** 만들어 `UnitView.SlotLabel`에 실리고, 체력바(`UnitHealthBar.SetSlotLabel`)와 칩(`BattlePresenter` → `TurnChipInfo` → `BattleHUD.ShowTurnOrder`)이 같은 값을 쓴다 — 두 표기가 어긋날 수 없다. 칩 색은 기존 `turn-chip-player`/`turn-chip-enemy` USS 클래스가 담당
    - `_indexText`는 **선택 참조**라 프리팹에 연결하지 않으면 체력바 표기만 조용히 빠지고 칩은 정상 동작한다
  - `UnitViewRegistry`: 슬롯 배치·`Id→UnitView` 조회·스폰/정리·체력바 갱신. 파티 슬롯 점유 현황을 관리해 추방·교체로 빈 자리를 영입 시 재사용. **화면 순서가 필요한 UI는 `GetPartySlots()`**(슬롯 순서대로 `PartySlot` = 멤버 + 배치 라벨)를 쓴다 — 멤버와 라벨을 한 struct로 묶어 돌려주는 이유는 따로 조회하면 둘이 어긋날 여지가 생기기 때문이다. **오브젝트 풀링**: 무한 타워라 인스턴스를 파괴하지 않고 `UnityEngine.Pool.ObjectPool<UnitView>`에 프리팹 단위로 반납·재사용한다(`_pools` + 반납할 풀을 찾는 `_sourcePrefab`). 재사용 인스턴스는 이전 전투 상태를 그대로 들고 오므로 스폰 시 `UnitView.ResetForSpawn()`이 아웃라인 레이어를 원복하고 `UnitAnimator.ResetToSpawn()`(트리거 소거 + `Rebind` + `Update(0)`)으로 사망 포즈를 지운다. 렌더러의 "원래 레이어"는 **인스턴스당 1회만** 캐싱하므로(`UnitView.CacheRenderers`) `Initialize`와의 호출 순서는 상관없다 — 과거엔 스폰마다 다시 캐싱해서 겨냥 레이어가 원본으로 굳는 함정이 있었다
  - `BattlePresenter`: Core 이벤트 구독/해제(`Bind`가 먼저 `Unbind`를 호출해 짝이 어긋날 수 없음) → 공격·피격·사망 연출, HUD 갱신(시너지 표시 포함), 카메라 쉐이크 + 크리티컬 SFX. 레지스트리는 `MonsterSpawner`·`TargetingController`와 같이 `Initialize(ct, registry)` 주입 방식(인스펙터 슬롯 없음)
  - ⚠️ **`UnitViewRegistry`는 `BattleDirector`만 인스펙터로 들고, 나머지 넷(`BattlePresenter`/`MonsterSpawner`/`TargetingController`/`RoguelikeRewardService`)은 `Initialize` 주입으로 받는다.** 슬롯을 중복으로 두면 서로 다른 오브젝트를 가리켜도 알아챌 방법이 없다 — Director가 이미 `_registry`를 검증하므로 주입받는 쪽은 null 검사가 필요 없다
    - 단 **주입하는 쪽에는 가드가 필요할 수 있다** — `_rewards`·`_pausePanel`은 Director의 *선택* 참조(없으면 선택지/퍼즈 없이 진행)라 `ValidateReferences`에 없다. 그래서 `Initialize` 호출도 `!= null` 안에 둔다. 필수 참조(`_presenter`·`_spawner`)는 그대로 호출한다
  - `RoguelikeRewardService`: 가중치 추첨 → 선택지 패널 제시 → `RunData.ApplyChoice` 호출. 영입 선택지를 고르면 후보 카드 제시(파티가 꽉 찼으면 "영입 안 함" 카드를 추가) → 실제로 영입을 고르면 현재 파티원 카드로 교체 대상을 이어서 물음 → `RunData.Recruit`/`ReplaceMember` 호출. `RecruitResult`(영입된 멤버 + 교체로 나간 멤버)를 반환해 `BattleDirector`가 View 스폰/제거를 처리
    - ⚠️ **교체 대상 카드는 `RunData.Members` 순서로 만들면 안 된다** — 그건 영입 순서이고 화면 순서는 슬롯 순서다. `SpawnMember`가 앞쪽 빈자리를 재사용하는 반면 영입은 리스트 끝에 붙어서, 추방으로 중간 슬롯이 빈 뒤 영입하면 두 순서가 갈라진다(1번 자리에 선 캐릭터와 1번 카드가 달라지던 실제 버그). `UnitViewRegistry.GetPartySlots()`가 돌려주는 **슬롯 순서**를 쓰고, 카드 제목에 배치 라벨을 같이 싣는다(`A1 바바리안`). 레지스트리는 `Initialize(registry)` 주입으로 받는다
    - 카드 스탯 표기(`DescribeStats`)는 **7종**이며 항목·순서·% 표기를 `PartyStatusBarView`와 맞춰 뒀다 — 같은 캐릭터를 두 화면이 다른 항목 수로 보여주면 비교가 되지 않기 때문. 영입 후보와 교체 대상이 같은 함수를 쓰므로 한 곳만 고치면 양쪽에 반영된다
  - `BattlePausePanel`: 배틀 퍼즈 오버레이 겸 `IPauseGate` 구현. ESC(`InputManager.PauseTogglePressed`)와 HUD 우상단 버튼(`BattleHUD.PauseClicked`) 두 경로로 토글. 퍼즈 화면은 PAUSE/현재 스테이지/이전 최고 기록 + '계속하기' + '배틀 중단'. **`Time.timeScale`을 쓰지 않는다** — 연출 대기(`Awaitable.WaitForSecondsAsync`)가 timeScale에 영향받는지 보장되지 않고, 씬 전환 페이드도 같은 Awaitable 기반이라 함께 멈출 수 있다. 몬스터 차례는 게이트 대기로, 플레이어 차례는 `IsGameplayInputEnabled=false`로 멈춘다. 퍼즈 해제 시 배틀 입력 복구를 **한 프레임 미룬다**(`_enableInputAtFrame`) — 오버레이는 3D 레이캐스트를 막지 않아서, 즉시 복구하면 '계속하기' 클릭이 같은 프레임에 `TargetingController`의 타겟팅 클릭으로 새어 들어간다. 퍼즈는 전투 구간에서만 허용(`SetBattleActive`) — 로그라이크 선택지 패널과 방향키/Enter가 겹치기 때문
    - **'배틀 중단'은 연출 재생 중에는 잠긴다**(`BattlePresenter.IsPlayingBack`, `Initialize(presenter)` 주입). 시뮬레이션의 await 중 **취소 토큰을 받지 않는 것은 `WhenPlaybackComplete()`뿐**이라, 적의 공격 모션이 재생되는 동안 중단을 눌러도 그 행동이 끝난 뒤에야 반영돼 **버튼이 먹지 않는 것처럼 보였다**(실제로 겪은 버그). 반대로 플레이어 차례(타겟 입력 대기)와 게이트 대기는 토큰을 받아 즉시 중단되므로 **잠그면 안 된다** — 조건이 "퍼즈 중"이 아니라 "연출 재생 중"인 이유다. 퍼즈 중에도 연출은 계속 재생되므로 `Update`에서 매 프레임 확인해 연출이 끝나는 순간 잠금이 풀린다
    - `IsPlayingBack`은 `BattlePresenter`가 등록 경로 셋(행동/사망/도트)을 `RegisterPlayback(args, task)` 한 곳으로 모아 세며, `finally`에서 감소하므로 취소·예외로 끝나도 카운트가 새지 않는다
    - 잠금 표시는 `.pause-button:disabled { opacity: 0.4 }`(`BattlePause.uss`). `Common.uss`의 `.btn:disabled`는 `opacity: 1`이라(성향 배분 +/- 버튼용) 여기서 다시 낮춘다 — 특정도가 같아 나중에 로드되는 쪽이 이긴다
  - `BattleResultPanel`: 전멸 시 도달 스테이지 + 최고 기록을 보여주는 결과 팝업(UI Toolkit, 확인 버튼 대기 후 IntroScene 전환). 신기록 여부는 패널이 직접 비교하지 않고 `SaveService.RecordStage`의 반환값을 받아 표시만 한다(동점을 신기록으로 처리하지 않기 위함). `BattleRunFlow.EndRunAsync`가 저장 **전** 기록을 읽어 "이전 최고 기록"으로 넘긴다
  - 배틀 패널 3종(`RoguelikeChoicePanel`/`BattleResultPanel`/`BattlePausePanel`)은 화면 UI 패널과 같은 `BasePanelUI`를 상속한다. 루트 엘리먼트 이름은 인스펙터가 아니라 `RootElementName` 오버라이드로 **코드에 고정**한다(UXML과 1:1이라 인스펙터에 빈칸을 남길 이유가 없다)
  - `MainCameraCache`: `Camera.main` 조회 결과를 담아두는 static 캐시. `Camera.main`은 "MainCamera" 태그 검색이라 매 프레임 쓰면 비용이 쌓이는데(체력바 빌보드는 유닛마다 `LateUpdate`에서 부른다), 캐시된 카메라가 파괴되거나 비활성화되면 게터가 알아서 다시 조회하므로 **수동 무효화가 필요 없다**. 씬 전환으로 카메라가 바뀌어도 안전하다. 사용처는 `UnitHealthBar`(빌보드)·`PartyStatusBarView`(`WorldToScreenPoint`)·`TargetingController`(레이캐스트·좌→우 정렬)·`DamagePopup`. 도메인 리로드를 끈 경우를 대비해 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`으로 static 필드를 비운다
  - 그 외: `UnitView`(`UnitAnimator`/`UnitHealthBar`를 물고 제어), `UnitAnimator`(트리거 재생 + 연출 길이 반환), `UnitHealthBar`(uGUI 월드스페이스 게이지 + TMP 숫자 표기 + 상태이상/스폰 디버프 표기. 사망 시 `PlayDieAsync`가 `SetVisible(false)`로 숨기고 스폰 시 `Initialize`가 되살린다 — 풀에서 재사용되므로 복구를 빼먹으면 그 인스턴스는 영영 체력바가 안 보인다), `TargetingController`, `BattleHUD`(스테이지/턴 순서/현재 행동 유닛/플레이어 차례 프롬프트), `RoguelikeChoicePanel`, `CameraShake`, `DamagePopup`/`DamagePopupSpawner`, `PartyStatusBarView`, `SynergyPanelView`
    - `PartyStatusBarView`: 화면 하단 파티 스탯 표기(순수 C# View — `BattleHUD`가 필드로 들고 `BattleHUD.uxml`의 `party-status` 컨테이너에 `Build`). 패널을 파티 최대 인원만큼 코드로 만든다. **막대가 아니라 텍스트**이며 한 행이 `현재값 (+선택지) (+자동성장) (+시너지) (-디버프)` 형태다 — 앞 숫자가 실제 적용 중인 값이고 괄호는 그 값이 어디서 왔는지의 내역이라, 디버프가 걸리면 앞 숫자는 이미 깎여 있다. 0인 항목은 괄호를 생략한다
      - 네 갈래 분해: 선택지 = `RunMember.ChoiceGrowth` / 스테이지 자동 성장 = `(Stats − BaseStats) − ChoiceGrowth` / 시너지 = `Unit.Stats − RunMember.Stats`(트래커가 전투 중에만 얹는다) / 디버프 = `Unit.Stats − 유효 스탯`. 그래서 `PartyMemberSlot`이 `Unit`과 `RunMember`를 **둘 다** 들고 다닌다
      - 괄호 색은 리치 텍스트 태그라 USS 변수를 못 쓴다 — `PartyStatusBarView`의 색 상수가 유일한 출처다
      - **정렬**: 각 패널을 담당 캐릭터의 화면 X에 맞춘다(`WorldToScreenPoint` → Y 뒤집기 → `RuntimePanelUtils.ScreenToPanel` → `style.left`, 가운데 맞춤은 USS `translate: -50% 0`이라 레이아웃 전 폭에 의존하지 않는다). **매 프레임 추적하지 않는다** — `CameraShake`가 카메라를 흔들어 하단 바까지 떨리기 때문이며, 슬롯·카메라가 고정이라 스테이지 시작과 `GeometryChangedEvent`(첫 레이아웃·해상도 변경)만으로 충분하다
      - **좌표는 `RunData.Members` 순서가 아니라 View에서 얻는다** — 추방·영입으로 슬롯이 재사용돼 멤버 순서와 슬롯 순서가 어긋나므로, `UnitId`로 `UnitViewRegistry.TryGet` 한 View의 실제 위치를 쓴다
      - **앞 숫자는 실시간 유효 스탯**(`Unit.EffectiveAtk/EffectiveDef/EffectiveSpd`)이라 상태이상 감소가 즉시 보인다. 갱신은 `BattlePresenter`의 `OnStatusChanged`·`SetSynergies`·`MarkDead` 세 곳뿐이다 — **피격 때는 갱신하지 않는다**(현재 HP를 표시하지 않으므로 피해로 바뀌는 값이 없다)
      - **행은 7종**(HP/ATK/SPD/DEF/치명타/치명피해/저항). HP 행은 **최대 HP**다 — 현재 HP는 캐릭터 위 체력바가 담당하고, 여기서는 성장 대상인 최대치만 다룬다(시너지는 `HpFlat`을 항상 0으로 두므로 HP엔 시너지 괄호가 뜨지 않는다). 치명타·치명피해·저항은 배율이라 100을 곱해 정수 %로 표기한다(치명피해 1.5 → `150%`)
      - 선택 화면의 `CharacterStatBarsView`(막대)와는 표현 방식이 달라 **공유하지 않는다** — 한때 재사용했지만 막대를 걷어내면서 되돌렸다
    - `DamagePopup`/`DamagePopupSpawner`: 피격 위치에 뜨는 피해량 숫자(일반=흰색 / 크리티컬=빨강+확대 / 도트=별도 색, `DamageKind` 3종). 스포너가 `ObjectPool`을 소유하고 `BattlePresenter`가 히트 루프와 `OnStatusTicked` 두 곳에서 스폰한다. 위치는 `UnitView.PopupOrigin`(`_popupHeight` 값 하나 — 앵커 오브젝트를 두지 않아 유닛 프리팹 10종을 건드리지 않는다). 연출은 `CameraShake`와 같은 `async Awaitable` 프레임 루프이며 취소는 `destroyCancellationToken`
      - ⚠️ **팝업은 스포너의 자식으로 만든다** — 유닛 View는 풀 반납 시 비활성화되므로 유닛 밑에 두면 재생 중인 팝업이 함께 꺼진다
      - **`RegisterPlayback`에 넣지 않는다**(의도) — 장식이라 시뮬레이션을 기다리게 할 이유가 없다. 숫자가 뜨는 타이밍은 피격 연출과 같은 `_impactDelay` 뒤다
      - **표시값은 클램프 전 피해량이다**(확정된 규칙) — `HitResult.Damage`·`StatusTickedEventArgs.Damage`가 `Unit.ApplyDamage`의 반환값(실제 감소량)이 아니라 `DamageCalculator`가 계산한 원본을 담는다. HP 20 남은 적을 크리 300으로 잡으면 **20이 아니라 300**이 떠야 캐릭터의 실제 화력이 읽히기 때문. 체력 게이지는 `Unit.CurrentHp`를 따로 보므로 표기와 어긋나지 않는다
    - `TargetingController`: 몬스터를 겨냥→확정해 `SubmitTarget`. **마우스**는 2단계 클릭(1차 겨냥=빨강 아웃라인, 같은 대상 재클릭=확정), **키보드**는 좌/우 방향키로 유효 대상을 순환 겨냥하고 Enter/Space로 확정. 입력은 `InputManager`(마우스 `PointerPosition`/`PrimaryPressedThisFrame`, 방향키 `BattleCycle*`, 확정 `BattleConfirm`)를 통해서만 받고 원시 디바이스는 만지지 않는다. 방향키 순환 순서는 리스트 순서가 아니라 **화면 좌→우**(각 대상 View를 `Camera.WorldToScreenPoint`로 정렬, `BattleDirector`가 `Initialize`에 `UnitViewRegistry` 주입해 `Id→View` 조회). 확정 시 `AudioManager.Confirm()`(마우스·키보드 공통)
    - `RoguelikeChoicePanel`: 카드 4장, 선택 대기 await. 마우스 클릭 외에 **방향키로 카드 겨냥**(마우스 `:hover`와 같은 강조를 `.choice-card--active` 클래스로 재현, 이동 시 `AudioManager.UiNavigate()`)하고 Enter/Space로 선택(선택 시 `AudioManager.UiClick()` — 마우스 클릭은 `UiClickSfx`가 `ClickEvent`로 내므로 키보드 분기에서만 재생해 중복 방지). 키보드 입력은 `InputManager.UiNavigate*`/`UiSubmit` 사용
- **전투 Data (`Scripts/Battle/Data/`)**: `UnitStatsSO`(베이스) + `CharacterStatsSO`/`MonsterStatsSO`(임시 스탯, 밸런싱 TBD), `SkillSO`(스킬 1종 = 쿨타임·범위·배율·상태이상. **유닛 종류에 묶이지 않은 별도 에셋**이라 `MonsterStatsSO`가 참조만 하고, 캐릭터 스킬이 추가되면 `CharacterStatsSO`가 코드 변경 없이 같은 타입을 재사용한다. 여러 유닛이 같은 스킬 에셋을 공유해도 됨), `CharacterRosterSO`(선택 6종), `SpawnWaveSO`(스테이지별 몬스터 구성 + `Weight`/`IsBossWave`— 후자는 `MonsterStatsSO.Tier`로 계산해 별도 플래그와 어긋날 여지 없음), `RoguelikeChoiceSO`(9종 카테고리 + 효과 수치 + 등장 가중치), `StageScalingSO`(양측 성장률 6종 + 복리 토글)
- **로그라이크 선택지 9종**: 에셋은 `ScriptableObjects/RoguelikeChoice/`에 전부 존재. 승리 시 가중치 추첨으로 3개 제시 → 1개 선택 → 즉시 적용. 영입 선택지는 파티가 꽉 차도 후보에서 빠지지 않고(`_weightPerEmptySlot`는 빈자리가 많을수록 자주 뜨게만 함), 고르면 교체 대상을 플레이어가 선택(위 `RoguelikeRewardService` 참고). 후반 영입/교체 캐릭터에는 스테이지 자동 성장분만 소급 적용(선택지로 얻은 성장은 소급하지 않음). `Heal`은 `_healFlat`(즉시 회복)을 쓰고 `_hpFlat`(최대 HP 영구 증가)은 0 — 과거에 이 둘이 뒤바뀌어 있던 데이터 실수를 수정함
- **스테이지 스폰 확장**: 수동 설계 웨이브(`WaveStage1~5`)는 `ScriptableObjects/SpawnWave/TutorialWaveStage/`에, 랜덤 풀용 웨이브 7종(`WavePoolNormal_*` 4개, `WavePoolBoss_*` 3개)은 `ScriptableObjects/SpawnWave/` 바로 아래에 있다. 인스펙터 등록 대상은 `BattleDirector`가 아니라 **`MonsterSpawner`**의 `_monsterWaves`/`_randomWavePool`/`_bossStageInterval`(비어 있으면 수동 웨이브 순환으로 폴백)
- **몬스터 데이터 세팅** (`ScriptableObjects/Monster/`): `MinionSO`=Normal·스킬 없음, `MageSO`/`RogueSO`=Elite·**단일 대상 스킬**, `WarriorSO`=Boss·**전체(라인) 스킬**. 스킬 내용은 각 `SkillSO` 에셋에 있고 몬스터 SO는 참조만 한다. 등급 차이는 `Tier`가 아니라 연결된 스킬의 유무·범위로 표현(`MonsterAiSelector`/`CreateSkill`은 `Tier`를 보지 않음) — 다만 `SpawnWaveSO.IsBossWave`(보스 BGM·보스 웨이브 강제 판정)는 `Tier==Boss`만 보므로, Elite만으로 구성된 웨이브는 스킬을 써도 "보스 웨이브" 취급되지 않는다(의도된 동작)
- **애니메이터**: 캐릭터/몬스터 컨트롤러는 `Spawned/Idle/Attack/Hit/Die` 구조(베이스+override). 몬스터 베이스(`Skeleton_Minion.controller`)에 `Skill` 트리거/스테이트가 있고, **스킬 전용 클립이 실제로 연결됨** — `Skill_SkeletonMage`/`Skill_SkeletonRogue`/`Skill_SkeletonWarrior`가 각 override 컨트롤러에 물려 있다(더 이상 Attack 클립을 공유하지 않음). 캐릭터/몬스터 프리팹 + 애니메이션 연결 완료
- **사운드 (`Scripts/Audio/{Data,View}` + `Scripts/Systems/AudioManager`)**: `AudioManager`(Singleton, BGM 소스 2개로 크로스페이드 — 같은 클립이면 무시해 스테이지가 넘어갈 때 처음부터 다시 재생되지 않음 — SFX는 단일 소스 `PlayOneShot`, Master/BGM/SFX 3단 볼륨을 `SaveData.Options`에서 초기화), `AudioLibrarySO`(씬별 BGM + 전투/보스 BGM + UI클릭/**UI이동/확정**/승리/패배/크리티컬 SFX — `UiNavigate`/`Confirm`은 전용 클립 미할당 시 `UiClick`으로 폴백), `UnitSfxSO`(유닛별 등장/공격/스킬/피격/사망, `UnitView`에 연결). `UiClickSfx`는 UIDocument 루트에 `ClickEvent` 버블링을 걸어 **마우스** 버튼 클릭음을 재생(기존 UI 코드 무수정). **키보드** 조작음은 `AudioManager`의 정적 헬퍼(`UiClick`/`UiNavigate`/`Confirm`)를 각 패널의 키보드 분기에서 직접 호출 — 마우스 경로(ClickEvent)와 겹치지 않게 키보드에서만 울림. 소리 매핑: 방향키 카드 이동=`UiNavigate`, 로그라이크/캐릭터선택 확정=`UiClick`, 배틀 타겟 확정(마우스·키보드 공통)=`Confirm`. IntroScene·BattleScene 양쪽에 `AudioManager`를 배치해 BattleScene 단독 실행 테스트도 지원(중복 인스턴스는 고친 `Singleton`이 자동 파괴). 클립을 하나도 연결하지 않아도 모든 재생 경로가 조용히 넘어감(null 가드)
- **옵션 설정 (`Systems/GameSettings`)**: 옵션 값의 단일 진입점(static, `SaveService` 선례). "저장값 갱신 + 실제 적용"을 한곳에 모아 UI가 적용 규칙을 모르게 한다 — `GameSettings.MasterVolume = v` 한 줄이 세이브 메모리 값과 `AudioManager`를 함께 갱신한다. **`AudioManager`는 세이브를 직접 읽지 않는다** — 자기 `Awake` 끝에서 `GameSettings.ApplyAudio()`를 호출해 저장값을 받는다(자기 자신에게 적용하므로 두 싱글턴의 초기화 순서에 의존하지 않고, "저장값 → 적용" 매핑이 변경 경로와 갈라지지 않는다). 파일 저장은 `Flush()`로 팝업 닫을 때 1회(드래그 중 매 프레임 디스크 쓰기 방지). 해상도/전체화면을 붙일 때도 같은 자리에 `ApplyDisplay()`를 두고 `GameManager.Awake`에서 부르면 된다
  - `OptionPopup.uxml` + `OptionPopupUI`는 화면만 담당 — 볼륨은 `GameSettings`, 키 재설정은 `InputManager`에 위임하고 자신은 요소 조회와 키설정 행 조립만 한다
- **성향 포인트 배분**: `PointAllocationPopupUI`(`OptionPopupUI`와 같은 오버레이 팝업 패턴). 캐릭터 선택 화면 `select-footer`의 '성향' 버튼(`CharacterSelectPanelUI.OnAllocationClicked` → `GameUIController`가 `Show`)으로 열림. 카테고리 행(9종)은 `AllocationRowsView`가 인스펙터에 할당된 `RoguelikeChoiceSO[]`를 기반으로 동적 생성(이름은 `.Title`, 매핑 키는 `.Category` 재사용, 하드코딩 없음). `[+]`/`[-]`는 `SaveData.TryAdjustPoints`를 호출하고 — **잔여 포인트가 없으면 못 늘리고 0이면 못 줄이는 규칙은 세이브 쪽이 판정한다** — 팝업은 반환값이 true일 때 헤더(`보유/총`)와 행을 다시 그리기만 한다. 초기화 버튼은 `ResetPoints()`. 몇 스테이지마다 1점인지는 `_stagesPerPoint`(인스펙터, 밸런싱 값)로 `GetEarnedPoints`에 넘김. 닫을 때(`Hide` 오버라이드) 1회만 `SaveService.Save()`(옵션 팝업과 동일 패턴). `RoguelikeRewardService.PickChoices`가 가중치 계산에 `SaveService.Current.GetPoints(category) * _weightPerPoint`(인스펙터, 기본 1)를 더해 추첨에 실제로 반영

### 아직 안 된 것
- **영입 후보 카드 표시 방식 — 아이콘은 완료, 나머지 남음**(2026-08): 아이콘 필드는 `UnitStatsSO._icon`(캐릭터 6종)과 `RoguelikeChoiceSO._icon`(선택지 9종)에 있고, `ChoiceCard.Icon` → `RoguelikeChoice.uxml`의 `card-icon` 슬롯으로 그린다(아이콘 없는 카드는 자동 숨김). 남은 것 둘: **이름 텍스트 폰트 크기를 절반 정도로 축소**(카드 크기가 아니라 글자 크기, `RoguelikeChoice.uss`의 `.card-title` — 현재 22px 그대로), 그리고 스탯 표기 정리 — **같은 캐릭터를 세 화면이 서로 다른 항목 수로 보여준다**: 영입 카드 4줄(HP/ATK/SPD/DEF, `RoguelikeRewardService.DescribeStats`) / 캐릭터 선택 6종(치명타·저항까지, `CharacterStatBarsView`) / 전투 하단 파티 표기 7종(치명피해까지, `PartyStatusBarView`). 항목 수를 맞출지, 화면별 목적에 맞게 다른 게 맞는지부터 정할 것
- **옵션 메뉴(해상도/언어)**: 볼륨(마스터/BGM/SFX)은 구현 완료. 해상도·전체화면·언어는 `SaveData.Options` 필드만 있고 UI·적용 로직 미구현
- **Elite/Boss 스킬 연출**: 스킬 애니메이션 클립·`MonsterStatsSO` 세팅(Elite=단일, Boss=전체)은 끝났고, 남은 것은 스킬 사용 시 이펙트(파티클). `BattlePresenter.PlayActionAsync`에 스폰 훅을 넣을 자리가 있다
- **타겟팅 피드백**: 겨냥된 단일 대상 아웃라인(빨강)은 있으나, 플레이어 차례에 **유효 대상 전체 하이라이트/커서 피드백**은 미구현(`TargetingController` TODO). 방향키 순환·확정과 마우스 2단계 클릭은 구현 완료
- **밸런싱 전반 TBD**: 캐릭터/몬스터 스탯, 선택지 수치·가중치, `StageScaling` 성장률 모두 임시값. 난이도 조정은 `ScriptableObjects/StageScaling.asset`(특히 `_monsterCompound` 복리 토글)부터 만지는 게 효과가 큼

## 아키텍처 방향 (README.md 기획 기반)

아래는 기획 문서와 위 "작업 원칙"에서 도출된 목표 구조이며, 현재 코드도 이 방향을 따른다. 미구현 부분도 이 방향을 유지한다.

- **씬 흐름**: 기획상 `타이틀 → 캐릭터 선택 + 성향 포인트 배분 → 전투`이며, 실제로는 앞의 두 단계를 **`IntroScene` 하나**에 오버레이로 합쳐 `IntroScene → BattleScene` 2씬으로 구현했다(기획 문서의 SettingScene/SelectWeaponScene은 같은 화면을 가리키는 옛 이름). 세부 화면은 씬 전환이 아니라 오버레이/팝업으로 처리
- **Core / View 분리**: 턴 순서(State Machine), 데미지 계산, 스탯/버프 등 전투 규칙은 Unity API 의존 없는 순수 C# 클래스로; MonoBehaviour는 애니메이션·이펙트·UI 갱신 등 연출 전담이며 Core의 이벤트를 구독하는 방식으로만 갱신
- **데이터**: 캐릭터/몬스터 스탯, 로그라이크 선택지(9종), 스폰 패턴은 하드코딩 대신 ScriptableObject로 관리
- **UI 레이어 분리**: 화면 UI(HUD, 메뉴, 팝업)는 UI Toolkit, 월드스페이스 UI(몬스터 체력바 등)는 uGUI로 별도 구현
