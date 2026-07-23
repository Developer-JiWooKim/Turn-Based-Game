# 리팩터링 계획 — 모듈화 / 기능 분리 / 최적화

## Context

현재 코드는 60개 파일 5,598줄이고 가장 큰 파일이 `BattleDirector.cs`(304줄)다. **god object 문제는 없다.**
CLAUDE.md가 정한 Core(순수 C#) / Data(SO) / View(MonoBehaviour) 분리도 실제로 잘 지켜지고 있다 —
검증 결과 `Battle/Core/`는 UnityEngine을 **주석에서만** 언급하고 실제 참조가 0건이다.

그래서 이 리팩터링의 목적은 "망가진 것을 고치는 것"이 아니라 세 가지다.

1. **규약을 컴파일러가 강제하게 만들기** — 지금 Core/View 분리는 사람의 규율로만 유지된다.
   누군가 Core에 `using UnityEngine`을 한 줄 넣으면 아무도 못 막는다.
2. **실측 가능한 성능 문제 제거** — 무한 타워라 매 프레임·매 턴 비용이 런 전체에 누적된다.
3. **중복과 버그 유발 구조 제거** — 같은 패턴이 4곳에 복사돼 있고, 최근 실제로 버그를 만든 구조가 남아 있다.

사용자 결정: **asmdef 도입**, **범위는 구조 재편까지**.

### 사전 조사에서 드러난 사실 (계획을 바꾼 부분)

- ✅ `Battle/Core/`는 이미 100% 순수 C#이다 → `noEngineReferences: true` 어셈블리로 **수정 없이** 분리 가능하다.
- ❌ **정정**: 질문 단계에서 "`RoguelikeCategory`를 옮겨야 한다"고 했으나, 확인해보니 불필요하다.
  `Progression → Battle.Data`는 정당한 계층 간선이고(`RunData`/`RunMember`가 `CharacterStatsSO`를 쓴다),
  `SaveData`의 `RoguelikeCategory` 사용도 같은 간선에 올라탄다. **타입 이동 없음.**
- ⚠️ **새로 발견한 진짜 걸림돌**: `Systems ↔ UI` 순환 참조.
  `Systems/GameManager.cs:3`이 `UI.FadeScreenEffect`를 참조하고, `UI/{GameUIController, OptionPopupUI, CharacterSelectPanelUI}`가 `Systems`를 참조한다.
  어셈블리를 나누려면 이 고리를 끊어야 한다.

---

## 1. 모듈화 — 어셈블리 정의 도입

### 1-1. `FadeScreenEffect` 이동 (순환 참조 해소, 선행 작업)

`Assets/MyAssets/Scripts/UI/FadeScreenEffect.cs` → `Assets/MyAssets/Scripts/Systems/FadeScreenEffect.cs`
네임스페이스 `...Scripts.UI` → `...Scripts.Systems`, `GameManager.cs`의 `using` 제거.

근거: 이 컴포넌트는 `CanvasGroup` 알파만 만지는 자기완결 코드이고(다른 UI 타입 의존 0),
CLAUDE.md에 "Fade Canvas는 GameManager 자식으로 두어야 한다"고 적힌 대로 **씬 전환 인프라**지 UI 패널이 아니다.

> `.cs`와 `.cs.meta`를 함께 옮기면 GUID가 유지되어 씬/프리팹의 컴포넌트 연결이 끊기지 않는다.

### 1-2. asmdef 5개 배치

| 어셈블리 | 포함 폴더 | 참조 |
|---|---|---|
| `Game.Core` | `Battle/Core/` | **없음** (`noEngineReferences: true`) |
| `Game.Data` | `Battle/Data/`, `Audio/Data/` | Core |
| `Game.Progression` | `Progression/` | Core, Data |
| `Game.Systems` | `Systems/` (+FadeScreenEffect) | Core, Data, Progression |
| `Game.View` | `Battle/View/`, `UI/`, `Audio/View/` | 위 전부 |

**얻는 것**: Core에 Unity 참조가 들어오면 즉시 컴파일 에러가 난다. 스크립트 수정 시 해당 어셈블리만 재컴파일된다.
TODO의 "Core 유닛 테스트 도입"은 테스트 어셈블리가 참조할 대상이 필요하므로 이 작업이 전제조건이다.

---

## 2. 기능 분리 (구조 재편)

### 2-1. `BattleDirector`에서 런 경계 분리

`BattleDirector`(304줄)가 스테이지 루프와 **런의 시작/끝 정책**을 함께 들고 있다.
후자 때문에 전투 오케스트레이터가 `SaveService`, `GameManager`, 씬 이름 상수까지 알고 있다.

`Battle/View/BattleRunFlow.cs`(신규)로 이동:
- `ResolveRun()` — `GameManager.CurrentRun` 조회 + 테스트 파티 폴백
- `HandleDefeatAsync()` — `SaveService.RecordStage` → 결과 팝업 → `GameManager.LoadScene`
- `IntroSceneName` 상수, `_testParty`, `_resultPanel` 필드

결과: Director는 "스테이지 루프 + 시뮬레이션 구동 + 퍼즈/중단"만 남아 ~220줄.

### 2-2. 배틀 패널 3종을 `BasePanelUI`로 통합

`RoguelikeChoicePanel`, `BattleResultPanel`, `BattlePausePanel`이 각자
`_document.rootVisualElement.Q<VisualElement>(name)` + `style.display` 토글을 재구현하고 있다.
`UI/BasePanelUI.cs`가 정확히 그 일을 하는데 상속하지 않는다. 셋 다 `BasePanelUI` 상속으로 전환하고
`Battle/View/Panels/`로 모은다.

> 인스펙터에서 `_rootElementName`(각각 `roguelike-panel` / `result-panel` / `pause-panel`)을 채워야 한다.

### 2-3. 시너지 조회 중복 제거

`RunData.GetActiveSynergies()`(멤버 기준)와 `PartySynergyTracker.GetActiveSynergies()`(살아있는 유닛 기준)가
같은 개념을 두 벌로 구현하고 있어 규칙이 갈라질 위험이 있다. `PartySynergyTracker` 쪽으로 일원화한다.

---

## 3. 재사용 — 중복 패턴 추출

### 3-1. `TaskCompletionSource` + `ct.Register` 패턴 (4곳)

`Battle/Core/PlayerActionSelector.cs:25-35`, `RoguelikeChoicePanel.PresentAsync`,
`BattleResultPanel.PresentAsync`, `BattlePausePanel.WaitWhilePausedAsync`가
"TCS 생성 → `RunContinuationsAsynchronously` → `ct.Register(TrySetCanceled)` → await → 정리"를 똑같이 반복한다.

`Battle/Core/PendingSignal.cs`(신규, 순수 C#)로 추출해 Core·View 양쪽에서 재사용한다.

---

## 4. 최적화 (실측 근거 있는 것만, 효과 큰 순서)

### 4-1. `UnitHealthBar.LateUpdate`의 `Camera.main` — **최우선**

```csharp
private void LateUpdate() {
    if (_billboardRoot == null) return;
    Camera camera = Camera.main;   // ← 유닛마다, 매 프레임
```
전장에 8유닛이면 **초당 480회** 호출된다. 정적 캐시로 1회 조회하고 씬 로드 시 무효화한다.

### 4-2. `UnitView.Initialize`의 렌더러 재수집

`GetComponentsInChildren<Renderer>(true)` + `new int[]`를 **스폰마다** 실행한다.
풀링 도입으로 인스턴스 계층은 고정이므로 **인스턴스당 1회**만 캐시하면 된다.

부수 효과가 더 중요하다 — 현재 CLAUDE.md에 "`ResetOutlineLayer`가 `Initialize`보다 먼저여야 한다"는
순서 함정이 명시돼 있는데, 원래 레이어를 한 번만 캐싱하면 **그 함정 자체가 사라진다.**

### 4-3. `BattleState`의 조회 할당

`AliveEnemiesOf()`가 호출마다 `List`를 새로 만든다 — **모든 유닛의 모든 행동마다** 호출된다.
`IsBattleOver`도 LINQ `Any` 2회를 턴 루프 조건과 유닛마다 평가한다. 재사용 버퍼 + 직접 루프로 교체.

### 4-4. `TurnOrder.Build`의 매 턴 LINQ 체인

`Where().OrderByDescending().ThenBy().ToList()` — 턴마다 정렬 결과 리스트와 중간 열거자를 새로 만든다.
재사용 버퍼에 담아 in-place 정렬로 교체.

### 4-5. 상태이상 `Ticked` 이벤트 과다 발생

`BattleSimulation.ResolveStatusesAsync`가 **남은 상태 개수만큼** `StatusChanged`를 발생시키고,
그때마다 `UnitHealthBar`가 라벨 문자열을 통째로 다시 만든다. 유닛당 1회 갱신으로 합친다.

---

## 실행 순서

| 단계 | 내용 | 검증 시점 |
|---|---|---|
| 1 | `FadeScreenEffect` 이동 (1-1) | 씬 전환 페이드 동작 |
| 2 | asmdef 5개 배치 (1-2) | **컴파일 통과 = 계층 검증 완료** |
| 3 | 최적화 4-1 ~ 4-5 | 전투 1회 플레이 |
| 4 | 중복 추출 3-1 | 타겟팅·선택지·결과·퍼즈 await 동작 |
| 5 | 구조 재편 2-1 ~ 2-3 | 전체 세로 슬라이스 |

각 단계는 독립적으로 커밋 가능하다. 2단계에서 컴파일이 깨지면 그 자체가 계층 위반 발견이므로,
이후 단계로 넘어가기 전에 반드시 통과시킨다.

---

## 검증 방법

CLI 빌드·테스트 파이프라인이 없으므로 **Unity Editor에서 직접 확인**한다.

**단계별 필수 확인**
1. **컴파일** — Console에 에러 0. 특히 `Game.Core`가 Unity 참조 없이 빌드되는지.
2. **인스펙터 연결** — asmdef 도입과 파일 이동 후 씬/프리팹의 컴포넌트 참조가 살아 있는지
   (`BattleDirector`, `GameManager`의 `_fadeScreenEffect`, 패널 3종의 `_rootElementName`).
3. **세로 슬라이스 1회** — IntroScene → 캐릭터 선택 → 전투 → 승리 선택지 → 다음 스테이지 → 전멸 → 결과 → 타이틀.

**리팩터링 대상별 회귀 확인**
- 4-1/4-2: 몬스터 처치 후 **같은 프리팹이 다음 웨이브에 재등장**할 때 체력바·아웃라인·애니메이션 정상 (풀 재사용 경로)
- 4-3/4-4: 턴 순서가 SPD대로 나오는지, 전투 종료 판정이 정확한지
- 4-5: 독·방어감소 중첩 시 남은 턴 수가 매 차례 1씩 감소하는지
- 3-1: 타겟 확정, 선택지 카드 선택, 결과 확인 버튼, 퍼즈 재개가 모두 await에서 정상 복귀하는지
- 2-1: 배틀 중단 → 결과 화면 → 타이틀 복귀, BattleScene 단독 실행 시 테스트 파티 폴백

**성능 확인(선택)**
Profiler로 전투 중 GC Alloc을 리팩터링 전후 비교한다. 4-1~4-4는 프레임당/턴당 할당을 줄이는 작업이라
Profiler의 GC Alloc 컬럼에서 차이가 드러나야 한다.

---

## 하지 않을 것

- **입력 폴링 이벤트화** — `CharacterSelectPanelUI`, `RoguelikeChoicePanel`, `BattlePausePanel`,
  `TargetingController` 4곳이 `Update()`에서 `InputManager`를 폴링한다. 이벤트로 바꿀 수 있지만
  현재 코드가 짧고 읽기 쉬우며 성능 문제도 아니다. 리바인딩(TODO 4-2) 작업 때 함께 다루는 편이 낫다.
- **`RunData`/`AudioManager` 분해** — 각각 186줄/196줄이지만 응집도가 높고 나눌 자연스러운 경계가 없다.
- **LINQ 전면 제거** — 스테이지당 1회 수준(`RoguelikeRewardService.PickChoices` 등)은 가독성이 더 중요하다.
