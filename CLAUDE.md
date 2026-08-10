# CLAUDE.md

이 파일은 Claude Code가 이 저장소에서 작업할 때 따라야 할 작업 규칙입니다.
프로젝트 기획/디자인 내용은 `README.md`, 남은 작업은 `TODO.md`,
**적용된 계산식과 밸런싱 기준은 `SystemFormulaBalance.md`**, UI 비주얼 규칙은 `UI_DesignReference.md`를 참고하세요.
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
로그라이크 루프(선택지 9종·파티 영입/교체·파티 시너지·스테이지 스케일링·6스테이지 이후 랜덤 스폰·사망 시 영구 추방)와 전멸 결과 화면, BGM/SFX, 성향 포인트 배분, 배틀 퍼즈(ESC/HUD 버튼·중단)까지 돌아간다.
**전투 연출도 2026-08-06에 일단락됐다** — 애니메이션 이벤트 기반 타격 시퀀스(파티클·피격·데미지 숫자·히트 스톱), 근접 이동/회전, 원거리 투사체, 몬스터 스킬별 연출 분기(메이지=머리 위 낙하 / 워리어=중앙 이동)가 유닛 10종 전부에 배정 완료.
**옵션 메뉴도 2026-08-10에 완료됐다** — 한국어/영어 전환(문구 79종 + 한글 로고 교체)과 화면 모드 2종(창모드 1280×720 / 전체화면 1920×1080).
남은 것은 체력바 감소 애니메이션, 타겟팅 피드백, 밸런싱.

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
  - **키 리바인딩**: `StartRebind(controlIndex, onFinished)`가 `PerformInteractiveRebinding`으로 다음 키 입력을 캡처한다. 방향키·확정은 Battle/Menu 두 맵에 같은 키로 존재하므로 **논리 컨트롤 4종**(이전/다음/확정/일시정지)으로 묶어(`InputControls.Rebindable`) 한 번 재설정하면 양쪽 맵에 함께 적용한다. ⚠️ `RebindControl.Label`은 표시 문자열이 아니라 **문자열 키**(`ui.keybind.*`)이며 번역은 `KeybindListView`가 한다. sibling 액션을 못 찾으면 새 키가 **한쪽 맵에만** 적용되므로 이 경우도 로그를 남긴다. 오버라이드는 `SaveData.Options.InputBindingOverrides`(JSON)에 저장하고 `Awake`에서 `LoadBindingOverridesFromJson`으로 복원. UI는 `OptionPopupUI`가 이 API로 키설정 행을 동적 생성. ⚠️ 두 씬(Intro/Battle)의 InputManager 오브젝트에 `.inputactions` 에셋을 `_actions` 슬롯에 **직접 할당**해야 한다
- **영구 저장 (`Scripts/Progression/Save/`)**: `SaveData`(런을 넘어 유지되는 값 = 최고 스테이지 + 카테고리별 영구 포인트 투자 내역 + 옵션 설정, `JsonUtility` 직렬화를 위해 public 필드만 사용) + `SaveService`(static, `save.json` 읽기/쓰기). **저장 위치가 에디터/빌드에서 다름** — 에디터는 확인·삭제가 쉽도록 프로젝트 루트의 `SaveData/`(`.gitignore` 등록됨, Assets 바깥이라 `.meta` 안 생김), 빌드는 `Application.persistentDataPath`. 빌드된 게임은 프로젝트 폴더와 무관하고 설치 경로가 읽기 전용일 수 있어 루트 방식을 쓸 수 없다. 최초 접근 시 1회 로드 후 캐싱하고, 파일이 없거나 깨져 있으면 기본값으로 복구한다. `GameManager`가 없는 상태(BattleScene 직접 실행)에서도 동작하도록 static으로 둠. 영구 포인트 **획득량은 저장하지 않고** `BestStage`에서 파생(`GetEarnedPoints`)해 값이 어긋날 여지를 없앰. 필드 추가는 기존 세이브와 호환되며(없는 필드는 초기값 유지), 필드 의미가 바뀔 때만 `Version`을 올려 `SaveService.Normalize`에서 마이그레이션
- **런 데이터 (`Scripts/Progression/Run/`)**: `RunData`가 런 전체 상태를 소유하고 씬을 넘어 `GameManager`가 보관(캐릭터 선택 시 `BeginRun`으로 생성). `RunMember`(파티원 1명 = 원본 SO + 성장 누적 `Stats` + `BaseStats` 스냅샷 + **선택지 몫만 따로 센 `ChoiceGrowth`** + 현재 HP + 런 내내 고정인 `UnitId`) 리스트, `PendingModifiers`(다음 스테이지 몬스터 디버프 예약), `CurrentStage`, `NextUnitId()` 발급기를 가짐. 선택지 효과 적용(`ApplyChoice`)·스테이지 자동 성장(`ApplyStageGrowth`)·전투 결과 반영(`SyncFromBattle`)·사망자 영구 추방(`RemoveFallen`)·영입(`Recruit`)·파티가 꽉 찼을 때 교체(`ReplaceMember`)가 전부 여기 있고 View는 결과만 화면에 반영. `PreviewRecruitStats`는 `Recruit`과 같은 소급 성장 계산(`ApplyCatchUp`)을 공유해, 영입 후보 카드에 뜨는 값과 실제 합류 결과가 항상 일치하도록 함
  - ⚠️ **영입자가 기존 파티원보다 약한 건 버그가 아니다**(확정된 규칙, 2026-08 재확인). `ApplyCatchUp`은 **스테이지 자동 성장만** 소급하고 로그라이크 선택지 성장은 소급하지 않는다 — "그건 그 시점 파티가 벌어들인 몫"이라 파티를 살려두는 데 가치를 두는 설계다. 하단 파티 스탯 표기에서 새 영입자만 파란 `(+선택지)` 괄호가 비어 보이는 것이 정상 결과이며, 과거에 이걸 "영입 스탯 불일치"로 두 번 의심한 적이 있다. 바꾸려면 밸런싱 결정이 먼저다
  - **성장 경로가 둘로 나뉜다**: 선택지는 `RunMember.ApplyChoiceGrowth`(→ `ChoiceGrowth`에도 누적), 스테이지 자동 성장·영입 소급은 `ApplyStageGrowth`(집계 안 함). 공용 구현은 private이라 새 성장 경로를 추가할 때 둘 중 하나를 반드시 고르게 된다. 집계는 효과 값을 그대로 더하지 않고 **적용 전후 `Stats`의 차분**을 쓴다 — `RoguelikeEffect.ApplyTo`가 치명타·저항을 1.0으로 클램프하면 넣은 값과 실제 증가분이 달라지기 때문. 하단 파티 스탯 표기가 이 분리에 의존한다
- **파티 시너지 (`Progression/Run/PartySynergyTracker`)**: 같은 캐릭터가 `CharacterStatsSO.SynergyThreshold` 이상 모이면 그 캐릭터들에게만 스탯 보너스(`CreateSynergy()` → `SynergyBonus`, HP는 대상 제외라 회복 부작용 없음).
  - **혼합 모델**(2026-08): 정수 스탯(ATK/SPD/DEF)은 **비율**, 비율 스탯(치명타/치명피해/저항)은 **%p 가산**이다. 무한 타워라 고정 증분은 후반에 무의미해지므로 비율이 맞지만, 치명타·저항은 이미 0~1 비율값이라 거기에 다시 비율을 곱하면(15%에 +10% → 16.5%) 사실상 아무 일도 안 일어난다. HUD 표기도 `%`와 `%p`로 구분해 적는다 — 같은 "+10%"로 보이면 플레이어가 어느 쪽인지 알 수 없다
  - **`RoguelikeEffect`를 재사용하지 않는다**(과거엔 재사용했다) — 그쪽은 런 내내 누적되는 고정 증분이라 곱셈이라는 개념이 없고, 몬스터 디버프·영입 플래그까지 함께 들고 있어 성격이 다르다. 비율 시너지는 덤으로 **스탯 리스케일의 영향을 받지 않는다**(마이그레이션 불필요)
  - `SynergyBonus.Scale`에 "0이면 최소 1" 바닥값을 두지 않는다 — 과거 `StageScaling`의 같은 바닥이 저스탯 캐릭터 급성장을 일으킨 전례가 있고, 스탯 규모상 필요하지도 않다
  - ⚠️ **수치를 조정할 때 표시된 %를 스탯 간에 비교하면 안 된다.** ATK는 피해량에 선형으로 들어가지만 DEF는 `K/(K+DEF)` 감쇠라 수익이 급감하고(DEF 150→300으로 +100% 해도 받는 피해는 −11.5%), SPD는 모든 유닛이 턴당 1회 행동하므로 순서만 바꾼다. 2026-08 조정 전에는 이 차이를 못 보고 Knight(DEF +53%)·Ranger(SPD +50%)를 과하다고 판단했지만 실제로 과했던 건 Barbarian(ATK +30% = 피해 +30%)이었다 — 환산표는 `TODO.md` 밸런싱 항목에 있다 런 데이터에 누적하지 않고 전투 중에만 트래커가 붙였다 떼는 방식이라, 대상이 죽어 인원이 임계 밑으로 떨어지면 `OnAllyDied`가 즉시 되돌린다(README 규칙). 조회는 `PartySynergyTracker.GetSynergies()` 하나뿐이며(스테이지 시작·아군 사망 양쪽이 같은 판정을 쓴다) `BattlePresenter.SetSynergies` → `BattleHUD.ShowSynergies` → `SynergyPanelView`로 흐른다
  - **`GetSynergies()`는 미발동 시너지도 함께 돌려준다** — `_aliveCountBySource`가 애초에 "파티의 시너지 보유 캐릭터 전부"라 그대로 순회하면 된다. 발동 여부는 저장하지 않고 `PartySynergy.IsActive`(= 인원 ≥ 임계치)로 파생시킨다
  - ⚠️ **패널의 `×N`은 요구 인원(`SynergyThreshold`)이지 현재 모인 인원이 아니다** — 파티 구성이 바뀌어도 변하지 않는 값이다. 과거엔 이 자리에 현재 인원(`Count`)을 찍어 숫자가 흔들렸다. 현재 인원은 화면에 숫자로 내보내지 않고 **발동/미발동 농담(濃淡)으로만** 나타낸다
- **UI 계층 규약 (`Scripts/UI/`)**: 화면 스크립트를 세 종류로 나눈다 — 섞이기 시작하면 분리한다
  1. **`*PanelUI`/`*PopupUI`** (MonoBehaviour, `BasePanelUI` 상속) = 화면의 주인. **UXML 배선 + Show/Hide + 이벤트 발행만** 하고, "어떤 요소가 무엇에 대응하는가"까지만 안다. 적용 규칙도 도메인 계산도 갖지 않는다(`TitlePanelUI` 19줄이 기준형)
  2. **`*View`** (순수 C#, MonoBehaviour 아님) = 화면 일부의 표현 전담. 컨테이너 `VisualElement`를 `Build`로 넘겨받아 그 안에서만 동작하므로 패널이 필드로 들고 쓴다 — `KeybindListView`(키 설정 목록), `CharacterStatBarsView`(스탯 바 6종), `AllocationRowsView`(포인트 배분 행). 씬 오브젝트를 소유해야 하면 예외적으로 MonoBehaviour(`CharacterPreview`의 프리뷰 리그·모델 캐시)
  3. **규칙의 주인은 UI 밖** — 값의 적용·저장은 `Systems/GameSettings`, 도메인 판정은 데이터 소유자에게 둔다(`SaveData.TryAdjustPoints`가 잔여 포인트 규칙을, `CharacterRosterSO.CreateStatCeiling`이 로스터 최댓값을 소유). 밸런싱 값이 UI 인스펙터에 남아 있는 곳이 아직 있다(`PointAllocationPopupUI._stagesPerPoint`, `RoguelikeRewardService._weightPerPoint`)
  - ⚠️ 입력 폴링(`Update`에서 `InputManager` 조회)은 `CharacterSelectPanelUI`·`RoguelikeChoicePanel`·`BattlePausePanel`·`TargetingController` 4곳에 의도적으로 남겨둔 것이다. 짧고 성능 문제도 아니라서 이벤트화하지 않기로 했다 — 바꾼다면 4곳을 한 번에
- **UI 흐름 (View)**: `Scripts/UI/` — `GameUIController` + `GameFlowFSM`(순수 C# 상태 머신) + `IGameFlowState`(Title/CharacterSelect), 각 패널은 UI Toolkit(`BasePanelUI` 상속). `CharacterSelectPanelUI`는 `CharacterRosterSO` 6종 순환/선택(prev/next 버튼 + **좌/우 방향키**가 동일하게 `Cycle`, `InputManager.UiNavigate*` 사용, 방향키 순환 시 버튼과 같은 클릭음), `CharacterPreview`가 3D 프리뷰 일체(리그·RenderTexture·모델 인스턴스 캐시)를 소유하고 패널은 `Show(character)`/`SetVisible(bool)`로 지시만 한다(**단방향 참조** — 과거엔 프리뷰가 패널의 `OnSelectionChanged`를 구독해 서로를 참조했다). 모델은 파괴/재생성 대신 캐릭터별 1회 생성 후 캐시하며, 앵커가 리그 자식이라 리그를 끄면 모델도 함께 멈춘다. 스탯 바 6종(HP/ATK/SPD/DEF/치명타/저항)은 `CharacterStatBarsView`가 그리고, 바 길이의 기준인 로스터 내 최댓값은 `CharacterRosterSO.CreateStatCeiling()`이 계산한다(밸런싱 값 하드코딩 없이 데이터에서 도출). 전투 시작 시 선택 캐릭터로 `BeginRun`
- **스탯 스케일 (2026-08 전체 10배 리스케일)**: 정수 스탯(HP/ATK/SPD/DEF)은 10배 규모다(캐릭터 HP 800~1200, ATK 180~350). 목적은 "숫자가 커 보이게"가 아니라 **반올림 해상도** — ATK가 12이던 시절엔 표현 가능한 최소 증가폭이 +1 = +8%라 성장이 뭉텅뭉텅 뛰었다. 비율값(치명타/치명피해/저항/성장률/스킬 배율/상태이상 크기)은 **스케일 대상이 아니다**
  - ⚠️ **`DamageCalculator.DefenseConstant`는 스탯 스케일과 함께 움직여야 한다.** K는 "피해가 절반으로 줄어드는 DEF 값"이라, 스탯만 10배 하고 K를 두면 방어력이 10배 세져 전투가 늘어진다(DEF 20에서 통과율 83% → DEF 200에서 33%). 10배 리스케일에 맞춰 100 → 1000으로 올렸다. 다시 스케일을 바꿀 일이 생기면 **이 상수를 가장 먼저** 확인할 것
  - `MinimumDamage`(=1)는 같이 올리지 않는다 — 스케일이 커질수록 이 바닥이 계산에 개입하는 빈도가 줄어드는 것이 정상이고, 같이 올리면 초저데미지 구간을 다시 왜곡한다
  - 스케일 대상 에셋: `CharacterStatsSO` 6종·`MonsterStatsSO` 4종의 HP/ATK/SPD/DEF, `RoguelikeChoiceSO`의 `_hpFlat`/`_atkFlat`/`_spdFlat`/`_defFlat`/`_healFlat`. `StageScalingSO`(전부 비율)·`SkillSO`(배율·상태이상 크기 전부 비율)는 손대지 않는다
- **전투 Core (`Scripts/Battle/Core/`, 순수 C#·UnityEngine 비의존)**: `Stats`, `Unit`, `SkillProfile`, `DamageCalculator`(비율감소+크리), `TurnOrder`(SPD 정렬), `BattleState`, `IRandom`/`SystemRandom`, 액션/셀렉터(`IActionSelector`, `PlayerActionSelector`=입력 await, `MonsterAiSelector`=노멀 공격/보스 스킬우선), `BattleEvents`, `BattleSimulation`(async 턴 루프, `BattleOutcome` 반환). **연출 동기화**: 이벤트 인자의 `RegisterPlayback(Task)`로 View 애니메이션 완료를 시뮬레이션이 대기
  - `RoguelikeEffect`: 선택지 1종의 순수 효과. 파티 강화 flat 8종 + 몬스터 디버프 3종 + 영입 플래그를 한 struct에 담음(카테고리별 구현체로 쪼개지 않음). `ApplyTo(Stats)`는 스탯만 더하고 "채워야 할 HP량"을 반환 — 현재 HP 규칙은 호출자(`Unit`/`RunMember`)가 각자 처리
  - `RunModifiers`: 다음 스테이지 몬스터에만 1회 적용되는 디버프 예약함(`Add` 배율 곱연산 중첩 → `ApplyTo` → `Consume`)
  - `StageScaling`: 스테이지별 양측 스탯 성장. 몬스터는 스폰 시 **배율**(복리 옵션), 플레이어는 HP를 이어받으므로 배율 대신 `BaseStats` 기준 **flat 증가를 영구 누적**. `CreatePlayerGrowth(baseStats, step)`은 매 스테이지 개별 반올림 대신 "누적 총량(`기준값×비율×step`)의 차분"을 돌려줘 반올림 오차가 쌓이지 않음(과거엔 스테이지마다 최소 +1 바닥값을 둬서 저스탯 캐릭터가 비정상적으로 급성장하는 문제가 있었음). 치명타·저항은 스케일링 제외(로그라이크 선택지로만 성장)
    - **난이도 가속**(2026-08-10): `_accelStartStage`(30)까지는 기본 성장률, 그 뒤는 `_accelMultiplier`(1.5)를 곱한 성장률로 전환한다. 두 구간을 이어 붙이므로 경계 이전 누적값이 보존된다. **성장률만 통째로 올리면 초반까지 함께 어려워지므로** 구간을 끊는다
    - **SPD 스케일링은 2026-08-10에 방침이 뒤집혔다.** 원래 "몬스터만 올리면 선공을 계속 뺏겨 체감이 나쁘다"고 제외했는데, 그 결과 **속도 강화 선택지가 빈 선택지**가 됐다. 지금은 `_spdStartStage`(5) 이후 몬스터 SPD를 `_monsterSpdRate`(3%)씩 올려 선공 압박을 만들고, 보스 처치마다 플레이어에게 `_bossSpdRate`(기준 SPD의 8%)를 돌려준다
      - ⚠️ **보스 보상은 일부러 몬스터 증가분보다 작다** — 같은 속도로 주면 압박이 사라져 "SPD를 올릴 이유가 없다"는 원래 문제로 되돌아간다
      - ⚠️ **SPD에는 난이도 가속을 적용하지 않는다** — 가속 구간에서 속도까지 튀면 선공이 한순간에 뒤집혀 대응할 여지가 없다
      - ⚠️ **보스 보상은 이벤트가 아니라 스테이지 번호에서 파생시킨다.** `CreatePlayerGrowth`가 `step % 보스간격 == 0`일 때만 SPD를 채우므로 영입자 소급(`ApplyCatchUp`)이 같은 step들을 되짚기만 해도 값이 정확히 일치한다 — "보스를 잡았다"는 이벤트로 만들면 소급 경로가 그 사실을 알 수 없어 어긋난다
      - 보스 간격의 **단일 출처는 `MonsterSpawner._bossStageInterval`**이고, `BattleDirector`가 `StageScalingSO.Create(bossStageInterval)`로 넘긴다. SO에 같은 값을 또 두면 두 곳이 어긋나도 알아챌 방법이 없다
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
  - **체력바 상태이상 표기는 아이콘**(2026-08, 과거엔 `STUN`/`PSN`/`ATK-` ASCII 약어였다). 태그 조립은 `UnitHealthBar.IconTag(spriteName)` 한 곳에 모여 있고 상태이상(`Label`)과 스폰 디버프(`MonsterSpawner.DescribeDebuff`)가 같이 쓴다 — 크기(`IconSize`)·기준선(`IconVOffset`)·항목 간격(`EntrySeparator`)이 두 표기에서 어긋나지 않게 하기 위함
    - ⚠️ **항목은 줄을 나누지 않고 한 줄에 나열한다.** 줄이 늘면 글자 블록이 아래로 자라 **체력바를 덮는다**(디버프 2개부터 실제로 겪은 버그). 항목마다 줄을 나누던 건 ASCII 약어 시절의 잔재이고, 아이콘은 훨씬 좁아 종류 5개가 전부 걸려도 가로 폭(3 월드 단위)에 들어간다
    - 같은 문제의 나머지 절반은 프리팹에 있다 — `HealthBar.prefab`의 `StatusText`는 **Vertical Alignment = Bottom**이어야 한다. Middle이면 글자 블록이 중심에서 위아래로 함께 자라 아래쪽이 체력바를 침범한다. Bottom이면 아래 모서리가 고정돼 **위로만** 자란다(줄이 넘쳐 접혀도 안전)
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
  - 그 외: `UnitView`(`UnitAnimator`/`UnitHealthBar`를 물고 제어), `UnitAnimator`(트리거 재생 + 연출 길이 반환), `UnitHealthBar`(uGUI 월드스페이스 게이지 + TMP 숫자 표기 + 상태이상/스폰 디버프 표기. 사망 시 `PlayDieAsync`가 `SetVisible(false)`로 숨기고 스폰 시 `Initialize`가 되살린다 — 풀에서 재사용되므로 복구를 빼먹으면 그 인스턴스는 영영 체력바가 안 보인다), `TargetingController`, `BattleHUD`(스테이지/턴 순서/현재 행동 유닛/플레이어 차례 프롬프트), `RoguelikeChoicePanel`, `CameraShake`, `DamagePopup`/`DamagePopupSpawner`, `HitEffectSpawner`, `HitStop`, `PartyStatusBarView`, `SynergyPanelView`
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
    - **타격 순간 연출 시퀀스**(2026-08): 공격 시작 → **타격 시점 대기** → 피격 애니메이션 + 파티클 + 숫자 팝업 + 히트 스톱을 한 지점에서 터뜨린다(`BattlePresenter.PlayActionAsync`)
      - 타격 시점은 `UnitAnimator.WaitForImpactAsync(fallback, ct)`가 정한다. 유닛별 토글 `_useImpactEvent`가 켜져 있으면 클립의 `OnImpactFrame` **애니메이션 이벤트**를, 꺼져 있으면 `BattlePresenter._impactDelay` 고정 지연을 쓴다 — 클립 작업을 유닛 하나씩 옮길 수 있게 한 장치다
        - **2026-08 현재 유닛 10종 전부 `_useImpactEvent` ON**이고 실제 재생되는 Attack/Skill 클립에 이벤트가 심겨 있다. `_impactDelay`(0.35)는 사실상 폴백으로만 남아 있다. 예외는 `Skill_SkeletonMinion.anim`(이벤트 없음)뿐인데 Minion은 `Tier=Normal`이라 스킬이 없어 재생되지 않는다
      - ⚠️ **애니메이션 이벤트는 Animator와 같은 GameObject의 컴포넌트만 호출한다.** `UnitAnimator`가 `[RequireComponent(typeof(Animator))]`로 그 자리에 묶여 있어 성립한다. 클립이 메서드를 **이름 문자열로** 참조하므로 `OnImpactFrame`을 rename하면 이벤트가 조용히 끊긴다
      - ⚠️ 이벤트를 켜 뒀는데 클립에 없으면 전투가 멈추므로 연출 길이만큼의 **안전 타임아웃 + 경고 로그**를 둔다(조용히 넘어가지 않게)
      - `HitStop`: **`Time.timeScale`을 쓰지 않는다** — 연출 대기·씬 전환 페이드가 모두 `Awaitable` 기반이라 함께 묶일 수 있다(`BattlePausePanel`과 같은 이유). 대신 관여한 유닛(때린 쪽 + 맞은 쪽)의 `Animator.speed`만 낮춘다. 늦춘 만큼 애니메이션이 뒤로 밀리므로 길게 잡으면 `UnitAnimator`의 연출 시간도 늘려야 한다. `finally`와 `ResetToSpawn` 양쪽에서 속도를 1로 되돌린다 — 취소·풀 반납 중에 느려진 채로 굳지 않도록
      - **공격 이펙트 3종은 유닛별로 `UnitView`에 꽂는다**(`_projectile`/`_muzzleFlash`/`_hitEffect`). 전부 선택 사항이라 비워두면 그 연출만 빠진다. `MasterStylizedProjectiles`처럼 한 폴더에 `Bullet`/`Muzzle`/`Hit`가 세트로 오는 에셋을 그대로 배정하기 위한 구조다(시전 이펙트는 검토 후 뺐다)
        - 재생 순서: 발사 섬광 → (`_muzzleLeadSeconds` 뒤) 투사체 발사 → 도착 시 명중 이펙트
        - **일반 공격과 스킬은 이펙트가 갈린다** — `_skill*` 3종을 채우면 스킬에만 쓰이고, 비워두면 일반 공격 것을 그대로 쓴다(`UnitView.ResolveEffects(isSkill)`가 한 번에 골라 `AttackEffects`로 돌려준다)
        - **`_skillFallsOnTarget`을 켜면 투사체가 시전자가 아니라 대상 머리 위에서 떨어진다**(스켈레톤 메이지의 번개). 바뀌는 것은 프리팹이 아니라 **발사 원점**(`ResolveLaunchOrigin`)뿐이라 비행·명중 처리는 그대로 쓴다. 발사 섬광이 그 시작점에 뜨므로 "머리 위에 기운이 모였다가 내리꽂힌다"가 자연스럽게 나온다
        - ⚠️ 낙하 연출의 발사 원점은 **대상마다 다르다** — 라인 스킬이면 대상 수만큼 각각 계산해야 한다(`FlyProjectilesAsync`가 대상별 `(from, to)` 구간을 먼저 모으는 이유). 총구 발사는 전부 같은 지점이라 그 경우에도 문제없이 동작한다
        - ⚠️ **명중 이펙트는 맞은 쪽이 아니라 때린 쪽(`actorView.HitEffect`)의 것을 쓴다** — 공격의 성질(화살/마법)을 나타내는 연출이기 때문. 비워두면 `HitEffectSpawner`의 전역 기본값으로 떨어진다
        - 유닛별 이펙트에는 크리티컬 변형을 따로 두지 않고 크기만 키운다(슬롯을 유닛마다 둘씩 늘리지 않기 위함)
      - ⚠️ **풀링한 `ParticleSystem`은 `Play()`와 같은 프레임에 `Stop()`하면 입자가 하나도 방출되지 않는다** — 이펙트가 통째로 안 보인다(총구 섬광이 안 보이던 실제 버그). `ProjectileSpawner.Retire`의 `stopEmitting`은 **이미 여러 프레임 재생된 뒤에만** 켠다(투사체는 도착 후라 OK, 총구 섬광은 스폰 직후라 NG)
      - `HitEffectSpawner`: `DamagePopupSpawner`와 같은 풀 구조지만 프리팹이 여러 종일 수 있어 `UnitViewRegistry`처럼 **프리팹별** 풀. 프리팹은 스크립트 없는 순수 `ParticleSystem`이면 되고 크기·오프셋·수명·크리티컬 배율이 인스펙터 값이다(수명 0 = 파티클 설정에서 자동 계산). 기준 위치는 `UnitView.HitEffectOrigin`(`_hitEffectHeight` 하나 — 팝업과 같이 앵커 오브젝트를 두지 않는다)
        - ⚠️ **파티클 스케일링 모드를 `Hierarchy`로 덮어쓴다.** 기본값 `Shape`는 transform 스케일을 **방출 모양에만** 적용해 입자 크기·속도는 그대로라, 인스펙터 크기 배율을 아무리 올려도 이펙트가 커지지 않는다(실제로 겪은 증상). **자식 파티클 시스템까지 전부** 바꾼다 — 스파클류는 여러 시스템이 겹쳐 하나의 이펙트를 이루는 경우가 많아 루트만 바꾸면 자식들만 원래 크기로 남는다. 같은 이유로 자동 수명 계산도 자식 중 **가장 오래 사는 것**에 맞춘다(루트만 보면 긴 자식이 재생 도중 반납된다)
        - ⚠️ **위치 보정은 고정 방향이 아니라 카메라 쪽으로 당긴다**(`_cameraOffset`). 타격 지점이 몸 한가운데라 그대로 두면 메시에 가려지는데, 아군은 화면 아래·적군은 위에 서 있어서 고정 방향으로 밀면 한쪽 진영에서는 오히려 몸 안으로 들어간다. `MainCameraCache`를 써서 양쪽 모두 항상 캐릭터 앞으로 나오게 한다
      - **이펙트·팝업은 `RegisterPlayback`에 넣지 않고**(장식) **히트 스톱은 기다린다** — 늦추는 동안 다음 연출이 겹쳐 들어오면 효과가 사라지기 때문
      - `_hitEffects`·`_hitStop`·`_projectiles`는 **선택 참조**라 비워두면 해당 연출만 빠지고 나머지는 그대로 동작한다
      - `ProjectileSpawner`: 원거리 유닛의 투사체. **타격 시점의 의미가 원거리에서는 "명중"이 아니라 "발사"**이며, 발사 → 비행 → 도착 순으로 진행하고 **도착을 기다린 뒤에** 피격 연출·숫자·이펙트가 나간다(화살이 닿기 전에 피해 숫자가 뜨면 순서가 거꾸로 보인다). 비행 시간은 거리÷속도이고 상한이 있다
        - 프리팹은 **유닛별**(`UnitView._projectile`/`_muzzleFlash`)이라 캐릭터마다 다른 투사체를 쓴다. 비워두면 투사체 없이 즉시 명중하므로 근접 유닛은 그대로 둔다. 에셋은 `MasterStylizedProjectiles`의 `*Bullet`/`*Muzzle`이 그대로 맞는다(루트에 `ParticleSystem`이 있고 이동 스크립트가 없다 — 비행은 스포너가 직접 옮긴다)
        - 발사 지점은 **프리팹 안의 앵커**(`UnitView._muzzlePoint`, 프리팹의 `MuzzlePoint`)다. 팝업·타격 이펙트가 높이 값(float)만 쓰는 것과 달리 여기만 앵커를 두는 이유 — 무기·손 본 아래에 두면 **공격 모션을 따라 움직여** 실제 무기 위치에서 발사된다(고정 오프셋으로는 불가능). 발사 시점에 좌표를 읽으므로 그 순간의 자세가 반영된다
        - 앵커가 없으면 몸통 높이(`HitEffectOrigin`)로 대신하고, `_projectile`이 있는데 앵커가 없으면 `ValidateReferences`가 **조건부로** 보고한다(근접 유닛은 비어 있는 게 정상이므로 무조건 검사하지 않는다)
        - 도착 지점은 `targetView.HitEffectOrigin` — 화살이 닿은 자리에서 타격 이펙트가 터지도록 같은 좌표를 쓴다
    - **공격 시 대상 바라보기 + 근접 이동 연출**(2026-08): 공격 전 대상을 향하고, 공격 후 제자리 배치로 되돌아온다. **근접·원거리의 차이는 `UnitView`가 흡수하므로 `BattlePresenter`는 둘을 구분하지 않는다**(`FaceTargetAsync` → 공격 → `RestorePoseAsync`)
      - 근접(`_approachTarget` ON — 바바리안·기사·도적(대거)·스켈레톤 미니언): 대상 앞까지 점프해 이동 + 회전
      - 원거리(OFF — 나머지 6종): 제자리에서 회전만(`_turnDuration`)
      - **공격하는 동안 자기 체력바를 숨긴다**(근접·원거리 공통) — 게이지가 공격 연출 위로 겹치면 타격이 잘 보이지 않고, 근접은 대상 위로 옮겨가 누구 것인지도 알 수 없다. 끄는 곳은 `FaceTargetAsync` 하나, 켜는 곳은 `RestorePoseAsync`·`SnapHome` 둘뿐이라 짝이 어긋날 여지가 없다
      - 몬스터 프리팹에도 같은 토글이 있어 켜면 그대로 동작한다
      - **각도 차가 5도 미만이면 회전을 건너뛴다**(`TurnAsync`) — 슬롯이 애초에 상대 진영을 향하고 있어 정면 대상은 몇 도뿐인데, 매 공격마다 왕복으로 기다리면 보이지도 않는 연출에 페이싱만 쓰게 된다
      - **점프 클립을 쓰지 않는다** — 애니메이터가 `Spawned/Idle/Attack/Hit/Die`(+`Skill`) 구조라 점프 상태가 없다. 대신 트랜스폼을 `sin(t·π)` 포물선으로 움직인다(`UnitView.MoveAsync`). 나중에 점프 클립이 생기면 같은 자리에서 트리거만 함께 재생하면 된다
      - 서는 자리는 대상에서 **자기 제자리 쪽으로** 물러난 지점이다 — 히트 이펙트의 카메라 오프셋과 같은 이유로, 고정 방향으로 계산하면 한쪽 진영이 대상 뒤로 넘어간다
      - **회전은 이동과 같은 구간에 걸쳐 `Slerp`로 함께 돌린다**(갈 때는 대상 쪽, 올 때는 원래 회전으로). 착지 후에 방향을 맞추면 그 순간 튀어 보이므로 스냅을 쓰지 않는다. 수평 성분만 보므로(`LookRotation`이 `y`를 0으로) 위아래로 기울지 않는다
      - **이동 중에는 체력바를 숨긴다** — 대상 위로 겹쳐 떠다니면 누구 것인지 알 수 없다. `PlayDieAsync`와 같은 `SetVisible` 경로를 쓰지만 공격하는 쪽은 자기 차례에 죽지 않으므로 둘이 겹치지 않는다
      - **전체 공격은 전장 한가운데서 시전할 수 있다**(`_skillMovesToCenter` — 스켈레톤 워리어). 목적지는 `UnitViewRegistry.GetBattlefieldCenter()`, 바라보는 방향은 **맞는 대상들의 평균 위치**다(첫 대상만 보면 끝자리를 향해 비스듬히 선다). 근접 토글(`_approachTarget`)과 무관하게 동작한다
        - ⚠️ **중심은 살아 있는 유닛이 아니라 `_playerSlots`/`_enemySlots`의 첫 칸·마지막 칸 중간점으로 잡는다.** 유닛 위치의 평균을 쓰면 몬스터 4마리 대 파티 1명일 때 중심이 몬스터 쪽으로 쏠리고 누가 죽을 때마다 자리가 달라진다. 슬롯 기준이면 웨이브 구성·생존자 수와 무관하게 늘 같은 자리다. 전체 평균이 아니라 양 끝을 쓰는 이유는 슬롯 간격이 고르지 않아도 줄의 한가운데가 나오게 하기 위함
      - ⚠️ **복귀 판정은 근접 토글이 아니라 `_isDisplaced`(실제로 자리를 떠났는지)로 한다.** 근접이 아닌 유닛도 전체 공격 연출로 중앙까지 나가므로, 토글로 판단하면 그 유닛이 돌아오지 않는다. 풀 재사용 대비로 `ResetForSpawn`에서도 초기화한다
      - ⚠️ **복귀는 `finally`에서 `SnapHome()`으로 보장한다.** 취소(씬 종료·배틀 중단)로 왕복이 끊기면 그 유닛만 엉뚱한 자리에 **체력바도 없이** 선 채 다음 턴이 진행된다. `SnapHome`이 위치·회전·체력바를 한 번에 되돌리는 이유다. 제자리 값은 `Initialize` 시점의 스냅샷(`_homePosition`/`_homeRotation`)이라 View가 슬롯 배치 규칙을 알 필요가 없다
      - 라인 스킬처럼 대상이 여럿이면 **첫 대상** 앞으로 간다(`TryGetApproachTarget`)
      - 왕복이 `RegisterPlayback` 안에 들어가므로 시뮬레이션이 복귀까지 기다린다 — 유닛이 자리를 비운 채 다음 유닛이 행동하지 않는다. 대신 **공격 1회당 왕복 시간(기본 0.25×2초)이 그대로 페이싱에 더해진다**
      - 하단 파티 스탯 표기는 매 프레임 추적하지 않는데도 어긋나지 않는다 — 유닛이 항상 제자리로 돌아오기 때문이다. 복귀를 없애려면 그쪽 정렬도 같이 손봐야 한다
    - `TargetingController`: 몬스터를 겨냥→확정해 `SubmitTarget`. **마우스**는 2단계 클릭(1차 겨냥=빨강 아웃라인, 같은 대상 재클릭=확정), **키보드**는 좌/우 방향키로 유효 대상을 순환 겨냥하고 Enter/Space로 확정. 입력은 `InputManager`(마우스 `PointerPosition`/`PrimaryPressedThisFrame`, 방향키 `BattleCycle*`, 확정 `BattleConfirm`)를 통해서만 받고 원시 디바이스는 만지지 않는다. 방향키 순환 순서는 리스트 순서가 아니라 **화면 좌→우**(각 대상 View를 `Camera.WorldToScreenPoint`로 정렬, `BattleDirector`가 `Initialize`에 `UnitViewRegistry` 주입해 `Id→View` 조회). 확정 시 `AudioManager.Confirm()`(마우스·키보드 공통)
    - `RoguelikeChoicePanel`: 카드 4장, 선택 대기 await. 마우스 클릭 외에 **방향키로 카드 겨냥**(마우스 `:hover`와 같은 강조를 `.choice-card--active` 클래스로 재현, 이동 시 `AudioManager.UiNavigate()`)하고 Enter/Space로 선택(선택 시 `AudioManager.UiClick()` — 마우스 클릭은 `UiClickSfx`가 `ClickEvent`로 내므로 키보드 분기에서만 재생해 중복 방지). 키보드 입력은 `InputManager.UiNavigate*`/`UiSubmit` 사용
- **전투 Data (`Scripts/Battle/Data/`)**: `UnitStatsSO`(베이스) + `CharacterStatsSO`/`MonsterStatsSO`(임시 스탯, 밸런싱 TBD), `SkillSO`(스킬 1종 = 쿨타임·범위·배율·상태이상. **유닛 종류에 묶이지 않은 별도 에셋**이라 `MonsterStatsSO`가 참조만 하고, 캐릭터 스킬이 추가되면 `CharacterStatsSO`가 코드 변경 없이 같은 타입을 재사용한다. 여러 유닛이 같은 스킬 에셋을 공유해도 됨), `CharacterRosterSO`(선택 6종), `SpawnWaveSO`(스테이지별 몬스터 구성 + `Weight`/`IsBossWave`— 후자는 `MonsterStatsSO.Tier`로 계산해 별도 플래그와 어긋날 여지 없음), `RoguelikeChoiceSO`(9종 카테고리 + 효과 수치 + 등장 가중치), `StageScalingSO`(양측 성장률 6종 + 복리 토글)
- **로그라이크 선택지 9종**: 에셋은 `ScriptableObjects/RoguelikeChoice/`에 전부 존재. 승리 시 가중치 추첨으로 3개 제시 → 1개 선택 → 즉시 적용. 영입 선택지는 파티가 꽉 차도 후보에서 빠지지 않고(`_weightPerEmptySlot`는 빈자리가 많을수록 자주 뜨게만 함), 고르면 교체 대상을 플레이어가 선택(위 `RoguelikeRewardService` 참고). 후반 영입/교체 캐릭터에는 스테이지 자동 성장분만 소급 적용(선택지로 얻은 성장은 소급하지 않음). `Heal`은 `_healFlat`(즉시 회복)을 쓰고 `_hpFlat`(최대 HP 영구 증가)은 0 — 과거에 이 둘이 뒤바뀌어 있던 데이터 실수를 수정함
- **스테이지 스폰 확장**: 수동 설계 웨이브(`WaveStage1~5`)는 `ScriptableObjects/SpawnWave/TutorialWaveStage/`에, 랜덤 풀용 웨이브 7종(`WavePoolNormal_*` 4개, `WavePoolBoss_*` 3개)은 `ScriptableObjects/SpawnWave/` 바로 아래에 있다. 인스펙터 등록 대상은 `BattleDirector`가 아니라 **`MonsterSpawner`**의 `_monsterWaves`/`_randomWavePool`/`_bossStageInterval`(비어 있으면 수동 웨이브 순환으로 폴백)
- **몬스터 데이터 세팅** (`ScriptableObjects/Monster/`): `MinionSO`=Normal·스킬 없음, `MageSO`/`RogueSO`=Elite·**단일 대상 스킬**, `WarriorSO`=Boss·**전체(라인) 스킬**. 스킬 내용은 각 `SkillSO` 에셋에 있고 몬스터 SO는 참조만 한다. 등급 차이는 `Tier`가 아니라 연결된 스킬의 유무·범위로 표현(`MonsterAiSelector`/`CreateSkill`은 `Tier`를 보지 않음) — 다만 `SpawnWaveSO.IsBossWave`(보스 BGM·보스 웨이브 강제 판정)는 `Tier==Boss`만 보므로, Elite만으로 구성된 웨이브는 스킬을 써도 "보스 웨이브" 취급되지 않는다(의도된 동작)
- **애니메이터**: 캐릭터/몬스터 컨트롤러는 `Spawned/Idle/Attack/Hit/Die` 구조(베이스+override). 몬스터 베이스(`Skeleton_Minion.controller`)에 `Skill` 트리거/스테이트가 있고, **스킬 전용 클립이 실제로 연결됨** — `Skill_SkeletonMage`/`Skill_SkeletonRogue`/`Skill_SkeletonWarrior`가 각 override 컨트롤러에 물려 있다(더 이상 Attack 클립을 공유하지 않음). 캐릭터/몬스터 프리팹 + 애니메이션 연결 완료
- **사운드 (`Scripts/Audio/{Data,View}` + `Scripts/Systems/AudioManager`)**: `AudioManager`(Singleton, BGM 소스 2개로 크로스페이드 — 같은 클립이면 무시해 스테이지가 넘어갈 때 처음부터 다시 재생되지 않음 — SFX는 단일 소스 `PlayOneShot`, Master/BGM/SFX 3단 볼륨을 `SaveData.Options`에서 초기화), `AudioLibrarySO`(씬별 BGM + 전투/보스 BGM + UI클릭/**UI이동/확정**/승리/패배/크리티컬 SFX — `UiNavigate`/`Confirm`은 전용 클립 미할당 시 `UiClick`으로 폴백), `UnitSfxSO`(유닛별 등장/공격/스킬/피격/사망, `UnitView`에 연결). `UiClickSfx`는 UIDocument 루트에 `ClickEvent` 버블링을 걸어 **마우스** 버튼 클릭음을 재생(기존 UI 코드 무수정). **키보드** 조작음은 `AudioManager`의 정적 헬퍼(`UiClick`/`UiNavigate`/`Confirm`)를 각 패널의 키보드 분기에서 직접 호출 — 마우스 경로(ClickEvent)와 겹치지 않게 키보드에서만 울림. 소리 매핑: 방향키 카드 이동=`UiNavigate`, 로그라이크/캐릭터선택 확정=`UiClick`, 배틀 타겟 확정(마우스·키보드 공통)=`Confirm`. IntroScene·BattleScene 양쪽에 `AudioManager`를 배치해 BattleScene 단독 실행 테스트도 지원(중복 인스턴스는 고친 `Singleton`이 자동 파괴). 클립을 하나도 연결하지 않아도 모든 재생 경로가 조용히 넘어감(null 가드)
- **로컬라이제이션 (`Scripts/Localization/`, `.asmref`로 `Game.Data` 합류)**: 한국어/영어 2종. 조회 진입점은 static `Loc`(`SaveService`·`GameSettings` 선례 — 표를 주입할 `GameManager`가 없어도 안전해야 하므로), 데이터는 `LocalizationTableSO`(키 + Ko + En 배열, Dictionary 1회 캐시). 표 에셋은 `ScriptableObjects/Localization/UiStringTable.asset`이고 **`GameManager._stringTable`에 할당**해야 한다(비어 있으면 UI에 키가 그대로 보인다)
  - ⚠️ **`Game.Data`에 두는 이유**: `UnitStatsSO`·`RoguelikeChoiceSO`가 표시 이름을 조회해야 하는데 의존 방향이 `Data → Progression → Systems → View`라, Systems에 두면 역방향 참조가 된다
  - ⚠️ **클래스명이 `Localization`이 아니라 `Loc`인 이유**: 네임스페이스가 `…Scripts.Localization`이라 같은 이름의 타입을 두면 `…Scripts` 아래 모든 코드에서 단순 이름이 **타입이 아니라 네임스페이스로** 해석돼 컴파일 에러가 난다(네임스페이스 멤버가 using 임포트보다 먼저 잡힌다)
  - **키 규약 3종** — 섞인 게 아니라 출처가 다르다. 어느 쪽이든 표에 행이 없으면 **원문/키가 그대로 나오고 게임은 멈추지 않는다**
    1. `ui.…` — UI 문구. UXML의 `text=`/`label=`과 코드에 직접 적는다. `BasePanelUI`가 트리를 훑어 치환할 때 **이 접두어로 "번역할 문자열"과 "코드가 나중에 채우는 값"(캐릭터 이름·스탯 숫자)을 구분**하므로, 접두어를 빼면 조용히 번역되지 않는다
    2. `choice.<카테고리>.title/desc` — 로그라이크 선택지. 본문이 여러 줄이라 원문을 키로 쓰면 공백 하나에 깨지므로 `RoguelikeCategory` enum에서 키를 만든다
    3. 유닛 표시 이름(`UnitStatsSO.DisplayName`) — **에셋에 적힌 원문 자체가 키**다. 한 줄짜리 짧은 이름이라 안전하고, 에셋을 고치지 않고 번역을 얹을 수 있다
  - **`BasePanelUI`가 UXML 치환을 담당한다**: `Start()`에서 트리를 1회 걷어 `(엘리먼트, 키)` 쌍을 캐싱하고, `Loc.LanguageChanged`마다 다시 그린다. 번역을 덮어쓰면 키가 사라지므로 **캐싱이 필수**다
    - ⚠️ **`Start()`를 오버라이드하면 `base.Start()`를 반드시 부를 것** — 빠뜨리면 그 패널만 키가 그대로 보인다
    - ⚠️ `OnEnable`/`OnDisable`이 **`protected virtual`**인 이유: Unity는 같은 이름의 메시지가 파생 클래스에도 있으면 파생 쪽만 호출해 베이스의 private 구독이 조용히 실행되지 않는다. virtual이면 파생이 같은 이름을 선언할 때 컴파일러가 hides 경고를 낸다(`BattlePausePanel`이 실제로 그 경우라 `base.OnEnable()`을 부른다)
    - 코드가 채우는 문구가 있는 패널은 `OnLanguageChanged`를 오버라이드해 함께 갱신한다(`CharacterSelectPanelUI`=캐릭터 이름, `PointAllocationPopupUI`=카테고리 이름·헤더, `TitlePanelUI`=로고, `OptionPopupUI`=화면 모드·키설정 이름)
  - **한글 폰트 작업은 필요 없다** — 화면 UI가 전부 UI Toolkit이고, ASCII만 되는 월드스페이스 체력바(TMP)에는 슬롯 라벨·숫자·아이콘만 찍힌다
- **화면 모드 (`GameSettings.DisplayPresets`)**: `창모드 1280×720` / `전체화면 1920×1080` 2종. 해상도와 전체화면 여부를 **한 프리셋으로 묶어** 어긋난 조합이 저장되지 않게 했고(그래서 `Options.Fullscreen` bool은 없앴다), 세이브에는 인덱스만 남으므로 **프리셋은 배열 뒤에만 추가**해야 한다. 적용은 `ApplyDisplay()` → `GameManager.Awake` 1회이며, 고른 적이 없으면(-1) 아무것도 하지 않아 플레이어가 정한 해상도를 빼앗지 않는다. ⚠️ 에디터 Game 뷰는 `Screen.SetResolution`을 따르지 않아 **확인은 빌드에서** 해야 한다
- **옵션 설정 (`Systems/GameSettings`)**: 옵션 값의 단일 진입점(static, `SaveService` 선례). "저장값 갱신 + 실제 적용"을 한곳에 모아 UI가 적용 규칙을 모르게 한다 — `GameSettings.MasterVolume = v` 한 줄이 세이브 메모리 값과 `AudioManager`를 함께 갱신한다. **`AudioManager`는 세이브를 직접 읽지 않는다** — 자기 `Awake` 끝에서 `GameSettings.ApplyAudio()`를 호출해 저장값을 받는다(자기 자신에게 적용하므로 두 싱글턴의 초기화 순서에 의존하지 않고, "저장값 → 적용" 매핑이 변경 경로와 갈라지지 않는다). 파일 저장은 `Flush()`로 팝업 닫을 때 1회(드래그 중 매 프레임 디스크 쓰기 방지). 해상도/전체화면을 붙일 때도 같은 자리에 `ApplyDisplay()`를 두고 `GameManager.Awake`에서 부르면 된다
  - `OptionPopup.uxml` + `OptionPopupUI`는 화면만 담당 — 볼륨·화면 모드는 `GameSettings`, 언어는 `GameSettings.Language`, 키 재설정은 `InputManager`에 위임하고 자신은 요소 조회와 행/버튼 조립만 한다
  - **레이아웃은 2열**(왼쪽 = 사운드·화면·언어 / 오른쪽 = 키 설정)이며 너비·여백은 `.option-frame`(1120px, padding 40)이 담당한다. **`.popup-frame`(480px)을 직접 키우면 성향 포인트 팝업까지 넓어진다** — 그래서 옵션 전용 클래스를 덧붙였고, 특정도가 같아 `Common.uss`에서 `.popup-frame`보다 **아래**에 있어야 이긴다. USS 값은 PanelSettings 기준 해상도(1920×1080) 기준이다
  - 화면 모드·언어 버튼은 UXML에 없고 코드가 만든다(`GameSettings.DisplayPresets` / `OptionPopupUI.LanguageOrder`가 목록의 유일한 주인). 선택 표시는 `.option-toggle--active`이며, `.btn-secondary:hover`와 특정도가 같아 `--active:hover` 규칙을 **뒤에** 둬야 호버 중에 선택 색이 풀리지 않는다
  - ⚠️ **flex 버튼에는 `min-width`를 반드시 준다** — UI Toolkit의 Yoga는 CSS의 `min-width: auto`(내용보다 좁아지지 않음)를 적용하지 않아, `flex-basis: 0`인 버튼이 글자보다 좁게 줄어들면 **텍스트가 버튼 밖으로 삐져나온다**(영어로 바꿨을 때 "Windowed 1280x720"이 한국어보다 길어 실제로 겪은 증상). `.option-toggle`·`.keybind-key`가 그래서 `min-width`를 갖는다. 보험으로 `.option-toggle-row`에 `flex-wrap: wrap`을 둬서 그래도 모자라면 아래로 접히게 했다
- **성향 포인트 배분**: `PointAllocationPopupUI`(`OptionPopupUI`와 같은 오버레이 팝업 패턴). 캐릭터 선택 화면 `select-footer`의 '성향' 버튼(`CharacterSelectPanelUI.OnAllocationClicked` → `GameUIController`가 `Show`)으로 열림. 카테고리 행(9종)은 `AllocationRowsView`가 인스펙터에 할당된 `RoguelikeChoiceSO[]`를 기반으로 동적 생성(이름은 `.Title`, 매핑 키는 `.Category` 재사용, 하드코딩 없음). `[+]`/`[-]`는 `SaveData.TryAdjustPoints`를 호출하고 — **잔여 포인트가 없으면 못 늘리고 0이면 못 줄이는 규칙은 세이브 쪽이 판정한다** — 팝업은 반환값이 true일 때 헤더(`보유/총`)와 행을 다시 그리기만 한다. 초기화 버튼은 `ResetPoints()`. 몇 스테이지마다 1점인지는 `_stagesPerPoint`(인스펙터, 밸런싱 값)로 `GetEarnedPoints`에 넘김. 닫을 때(`Hide` 오버라이드) 1회만 `SaveService.Save()`(옵션 팝업과 동일 패턴). `RoguelikeRewardService.PickChoices`가 가중치 계산에 `SaveService.Current.GetPoints(category) * _weightPerPoint`(인스펙터, 기본 1)를 더해 추첨에 실제로 반영

### 아직 안 된 것
- **옵션 메뉴 에디터 세팅**: 해상도·언어는 2026-08-10에 코드 완료. 남은 것은 두 씬(Intro/Battle)의 `GameManager` 오브젝트 `_stringTable` 슬롯에 `UiStringTable.asset`을 할당하는 것뿐이다(비어 있으면 UI에 `ui.…` 키가 그대로 보인다)
- **타겟팅 피드백**: 겨냥된 단일 대상 아웃라인(빨강)은 있으나, 플레이어 차례에 **유효 대상 전체 하이라이트/커서 피드백**은 미구현(`TargetingController` TODO). 방향키 순환·확정과 마우스 2단계 클릭은 구현 완료
- **체력바 감소 애니메이션**: `UnitHealthBar`가 `_fill.fillAmount`를 즉시 대입해 게이지가 뚝 끊긴다. 히트 스톱으로 느려진 순간에 게이지가 줄어드는 그림이 타격감의 핵심이라 남은 연출 중 우선순위가 높다(TODO 4-1-b)
- **밸런싱 전반 TBD**: 캐릭터/몬스터 스탯, 선택지 수치·가중치, `StageScaling` 성장률 모두 임시값. **적용된 계산식 14종과 조정 시 함께 움직여야 하는 값은 `SystemFormulaBalance.md`에 정리돼 있다** — 밸런싱 전에 그 문서의 "밸런싱 시 함께 움직여야 하는 값들" 표를 먼저 볼 것

## 아키텍처 방향 (README.md 기획 기반)

아래는 기획 문서와 위 "작업 원칙"에서 도출된 목표 구조이며, 현재 코드도 이 방향을 따른다. 미구현 부분도 이 방향을 유지한다.

- **씬 흐름**: 기획상 `타이틀 → 캐릭터 선택 + 성향 포인트 배분 → 전투`이며, 실제로는 앞의 두 단계를 **`IntroScene` 하나**에 오버레이로 합쳐 `IntroScene → BattleScene` 2씬으로 구현했다(기획 문서의 SettingScene/SelectWeaponScene은 같은 화면을 가리키는 옛 이름). 세부 화면은 씬 전환이 아니라 오버레이/팝업으로 처리
- **Core / View 분리**: 턴 순서(State Machine), 데미지 계산, 스탯/버프 등 전투 규칙은 Unity API 의존 없는 순수 C# 클래스로; MonoBehaviour는 애니메이션·이펙트·UI 갱신 등 연출 전담이며 Core의 이벤트를 구독하는 방식으로만 갱신
- **데이터**: 캐릭터/몬스터 스탯, 로그라이크 선택지(9종), 스폰 패턴은 하드코딩 대신 ScriptableObject로 관리
- **UI 레이어 분리**: 화면 UI(HUD, 메뉴, 팝업)는 UI Toolkit, 월드스페이스 UI(몬스터 체력바 등)는 uGUI로 별도 구현
