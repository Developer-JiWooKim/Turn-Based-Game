# 입력 구조 — 현재 상태와 개선 방향

이 문서는 플레이어 입력 처리의 **현재 구조**, **지금 구조가 못 푸는 문제**, 그리고 **손대게 될 때의 방향**을 남긴다.
작업 규칙은 `CLAUDE.md`, 할 일 목록은 `TODO.md`를 참고.

> 결론부터: **지금 당장 바꿀 필요는 없다.** 동작하고 있고 근거도 남아 있다.
> 다만 아래 "한계" 두 가지를 땜빵으로 막아둔 상태라, 입력 관련 버그가 또 나오면 이 문서의 방향으로 정리한다.

---

## 1. 현재 구조

```
.inputactions 에셋          ← 진실의 출처: "어떤 키가 어떤 액션인가"
        ↓ FindAction(경로)
   InputManager             ← 허브: 액션 9개를 캐싱하고 bool 프로퍼티로 노출
        ↓ 프로퍼티 조회
소비자 4곳 (각자 Update 폴링)
```

핵심 원칙은 **"View는 원시 디바이스를 만지지 않는다"** 다.
과거엔 `TargetingController`가 `Mouse.current`를 직접 읽었으나, 지금은 어떤 View도
`UnityEngine.InputSystem`을 참조하지 않고 `InputManager`만 본다.

### 1-1. 액션 맵 3개

| 맵 | 액션 | 용도 | 게이트(`IsGameplayInputEnabled`) |
|---|---|---|---|
| **Battle** | `CyclePrev` / `CycleNext` / `Confirm` | 전투 타겟팅 | **적용** |
| **Menu** | `NavPrev` / `NavNext` / `Submit` / `Pause` | 메뉴 조작·퍼즈 | 무관 |
| **UI** | `Point` / `Click` | 마우스(EventSystem과 에셋 공유) | `Click`만 적용 |

**방향키가 Battle과 Menu에 중복으로 존재한다** — 의도된 설계다.
타겟팅은 전투 턴 중에만, 메뉴 조작은 턴 밖에서만 일어나 **시간상 겹치지 않는다.**
대신 게이트 적용 여부가 달라야 해서(퍼즈 중 타겟팅은 막고 메뉴는 살려야 함) 맵을 나눴다.

### 1-2. 문자열의 유일한 출처

액션 경로 문자열은 `Systems/InputControls.cs` **한 파일에만** 있다.
`InputManager`의 액션 캐싱과 리바인딩 표(`Rebindable`)가 같은 상수를 참조하므로 서로 어긋날 수 없다.

> 과거엔 같은 경로가 `CacheActions()`와 리바인딩 테이블 양쪽에 따로 적혀 있었다.
> 한쪽만 갱신하면 **게임은 멀쩡한데 키 설정 UI만 조용히 망가졌다**(버튼에 `-`가 뜨고 눌러도 무반응).

컴파일러가 에셋과 대조해주지는 않으므로, 어긋났을 때 **시끄럽게** 실패하도록 해뒀다:
- 액션 캐싱 → `throwIfNotFound: true`로 **시작 즉시 예외**
- 리바인딩 경로(`GetRebindDisplay` / `InputRebinder`) → `Debug.LogError`

### 1-3. 소비자 4곳 — 전부 자기 활성 조건을 스스로 안다

| 컴포넌트 | 읽는 입력 | 자기 게이트 |
|---|---|---|
| `TargetingController` | 마우스 + `BattleCycle*` + `BattleConfirm` | `if (!_awaitingInput) return;` |
| `CharacterSelectPanelUI` | `UiNavigate*` | `if (!_visible) return;` |
| `RoguelikeChoicePanel` | `UiNavigate*` + `UiSubmit` | `if (!_pending.IsWaiting \|\| _cardCount == 0) return;` |
| `BattlePausePanel` | `PauseToggle`(ESC) | 없음 — 항상 받아야 함 |

**이벤트가 아니라 폴링인 것은 검토 후 내린 결정이다**(`misty-hugging-lighthouse.md`의 "하지 않을 것").
근거: 각 폴링 코드가 짧고 읽기 쉬우며, 성능 문제가 아니다.

### 1-4. 게이트: `IsGameplayInputEnabled`

퍼즈의 핵심 장치. **맵을 Disable하지 않고 bool 하나로 막는다.**

```csharp
// BattlePausePanel.Pause()
InputManager.Instance.IsGameplayInputEnabled = false;   // 배틀 입력만 차단

// Update() — 복구는 다음 프레임에
if (_enableInputAtFrame >= 0 && Time.frameCount >= _enableInputAtFrame)
{
    _enableInputAtFrame = -1;
    input.IsGameplayInputEnabled = true;
}
```

`PauseTogglePressed`(ESC)는 **게이트에 묶지 않는다** — 묶으면 퍼즈 중에 ESC로 풀 수 없다.

### 1-5. 리바인딩: 3개 클래스로 분담

```
InputManager        창구 (UI가 부르는 API — 인덱스 기반)
   ├ InputRebinder      키 캡처 (PerformInteractiveRebinding)
   └ InputBindingSaver  영속 (SaveData.Options.InputBindingOverrides에 JSON)
```

**논리 컨트롤 4종**(이전/다음/확정/퍼즈)으로 묶는 것이 특징이다.
방향키·확정은 Battle·Menu 두 맵에 같은 키로 존재하므로, 한 번 재설정하면 양쪽에 함께 적용해야 한다.
`InputRebinder`가 대표 액션(배열의 첫 번째)으로 키를 캡처한 뒤 나머지(sibling)에 같은 값을 복사한다.

UI(`KeybindListView`)는 `RebindControlCount`만큼 행을 만들므로 **컨트롤을 추가해도 UI 코드는 그대로**다.

---

## 2. 지금 구조가 못 푸는 것 — 땜빵 2개

두 버그 모두 **"위에 있는 것이 입력을 먹으면 아래로 안 내려간다"** 는 개념이 없어서 생겼다.

### 2-1. 퍼즈 해제 시 클릭이 타겟팅으로 샌다

퍼즈 오버레이는 UI Toolkit이라 **3D 레이캐스트를 막지 않는다.**
'계속하기'를 눌러 입력을 즉시 복구하면, 같은 클릭이 같은 프레임에
`TargetingController`의 타겟팅 클릭으로도 읽혀 **엉뚱한 몬스터를 공격**한다.

**현재 땜빵**: `BattlePausePanel._enableInputAtFrame`으로 배틀 입력 복구를 한 프레임 미룬다.

### 2-2. 방향키가 UI 버튼에 포커스를 옮겨 Enter가 이중 동작

방향키·Enter를 우리가 Menu 맵으로 직접 처리하는데, EventSystem의 내장 네비게이션까지 켜져 있으면
방향키가 UI Toolkit 버튼에 포커스를 옮기고 Enter가 그 버튼을 누른다.
(캐릭터 선택에서 방향키→Enter가 prev 버튼을 누르거나, 배틀에서 Enter가 퍼즈 버튼을 누르는 버그였다.)

**현재 땜빵**: 두 씬 모두 EventSystem의 `m_MoveAction`/`m_SubmitAction`을 `{fileID: 0}`으로 끊어둠.
마우스(`m_PointAction`/`m_LeftClickAction`)는 유지되므로 클릭·호버는 정상.

> ⚠️ 키 조작을 새로 추가할 때 이 둘을 다시 연결하지 말 것.

---

## 3. 개선 방향 — 입력 우선순위 / 소비(consume)

위 두 문제의 뿌리는 같다. **입력이 "누가 먼저 먹는가"라는 개념 없이 모두에게 동시에 보인다.**

목표 형태:

```
[퍼즈 오버레이]   ← 열려 있으면 여기서 소비하고 아래로 안 내려감
[선택지 패널]
[타겟팅]
```

얻는 것:
- 2-1의 프레임 지연 해킹(`_enableInputAtFrame`)을 걷어낼 수 있다 — 퍼즈가 떠 있는 동안 클릭을 소비하므로
- 2-2의 EventSystem 액션 끊기도 재검토 대상이 된다
- 모달이 늘어나도(예: 확인 팝업) 각자 게이트를 새로 만들 필요가 없다

**착수 판단 기준**: 입력 관련 버그가 또 나오거나 모달이 하나 더 늘어나면.
지금은 소비자가 4곳뿐이고 서로 시간상 겹치지 않아 비용 대비 이득이 작다.

---

## 4. 검토했지만 하지 않기로 한 것 (재논의 방지)

### 4-1. 4곳의 `Update`를 하나로 합치고 이벤트로 뿌리기

"입력 감지 컴포넌트 하나 + 나머지는 반응" 형태. **순이득이 없다고 판단.**

- `InputManager`에는 이미 `Update`가 없다. 프로퍼티를 물어볼 때 `WasPressedThisFrame()`을 읽을 뿐이라,
  **감지는 이미 한 곳에 모여 있다.** 4곳의 `Update`는 감지가 아니라 *질의*다
- 소비자 4곳은 각자 활성 조건을 안다(§1-3). 중앙에서 뿌리면 둘 중 하나가 된다:
  - **모두에게 보내고 각자 게이트** → 같은 검사를 하면서 구독/해제 생명주기만 추가 (순손해)
  - **중앙이 누가 활성인지 안다** → 입력 계층이 UI 상태를 아는 결합 발생
- `UiNavigatePrev`를 쓰는 두 곳(`CharacterSelectPanelUI` / `RoguelikeChoicePanel`)은 **다른 씬**이라 동시에 존재하지도 않는다

> 진짜로 값어치가 나오는 조건은 §3의 **우선순위/소비**다. "Update 개수 줄이기"가 목표가 되면 안 된다.

### 4-2. 폴링 → C# 이벤트 기반

§1-4의 프레임 단위 제어(`_enableInputAtFrame`)가 폴링 구조에 의존한다.
이벤트는 이미 발생한 뒤 도착하므로 "한 프레임 미루기"를 표현하기 까다롭다.
그리고 게이트를 핸들러마다 다시 확인해야 한다.

### 4-3. `.inputactions` 생성 래퍼(`InputSystem_Actions.cs`) 도입

현재 `generateWrapperCode: 0`(제거된 상태). 도입하면 액션 이름이 **컴파일 검사**된다:

```csharp
_actions.Battle.CyclePrev            // 진짜 C# 프로퍼티
new("이전", new[] { _actions.Battle.CyclePrev, _actions.Menu.NavPrev })  // 표에서 문자열 소멸
```

**보류 이유**: 이미 `throwIfNotFound: true`로 **시작 즉시 예외**가 나므로 "컴파일 에러 vs 실행 즉시 예외"의 차이뿐이고,
조용히 망가지던 문제(§1-2)는 이미 해결됐다.

도입 시 알아둘 점:
- ✅ 인스펙터에 `.inputactions`를 할당하는 함정이 사라진다(래퍼는 코드에서 `new`)
- ⚠️ **EventSystem과 에셋 인스턴스가 갈라진다.** 두 씬의 EventSystem이 같은 에셋(GUID `052faaac…`)을 참조 중인데,
  래퍼는 자기 인스턴스를 만든다. 마우스는 디바이스를 직접 읽으니 기능 문제는 없지만
  "EventSystem도 이 맵을 공유한다"는 서술이 더 이상 사실이 아니게 된다
- ⚠️ 생성 코드는 커밋되는 산출물이라, 재생성이 누락되면 낡은 래퍼가 그대로 컴파일된다

### 4-4. `InputActionReference`를 인스펙터로 주입

문자열이 코드에서 완전히 사라지고 액션 이름을 바꿔도 안 깨진다(GUID 참조).
**보류 이유**: 인스펙터에 9개를 일일이 드래그해야 하고 빠뜨리면 런타임 null —
이미 경고하고 있는 "인스펙터 할당 함정"이 9배가 된다.

---

## 5. 손댈 때 주의할 것

- **게이트를 맵 Enable/Disable로 대체하지 말 것** — 퍼즈의 프레임 정밀 복구가 `IsGameplayInputEnabled`에 의존한다
- **`PauseToggle`은 게이트에 묶지 말 것** — 묶으면 퍼즈 중에 ESC로 풀 수 없다
- **EventSystem의 `m_MoveAction`/`m_SubmitAction`을 다시 연결하지 말 것** (§2-2)
- **폴링을 이벤트로 바꾼다면 4곳을 한 번에** — 일부만 바꾸면 두 방식이 섞여 추적이 어려워진다
- **두 씬(Intro/Battle)의 InputManager에 `.inputactions` 에셋 할당 필요** — 현재 방식 유지 시
