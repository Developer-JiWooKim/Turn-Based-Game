# TODO / 로드맵

이 문서는 앞으로 해야 할 작업을 정리한 목록입니다.
게임 기획은 `README.md`, 코드 작업 규칙·현재 코드 상태는 `CLAUDE.md`를 참고하세요.
UI 비주얼 규칙은 `UI_DesignReference.md`에 있습니다(입력 구조는 별도 문서 없이 `CLAUDE.md`의 InputManager 항목에 통합돼 있음).

> 우선순위 표기: 🔴 높음(핵심 재미·구조) / 🟡 중간 / 🟢 낮음(폴리시·후순위)
> 상태 표기: 미착수 / 진행중 / 부분구현

---

## ✅ 지금까지 완료 (검증됨)

전체 세로 슬라이스가 실제 Unity에서 작동함:
- **씬 흐름**: IntroScene(타이틀 → 캐릭터 선택) → BattleScene(전투) → 승리 시 다음 스테이지 / 전멸 시 결과 화면 → IntroScene 복귀
- **전투**: Core(턴 순서·데미지·크리·보스 스킬·이벤트) ↔ View(스폰·애니·체력바·클릭 타겟팅·카메라 쉐이크)
- **런/진행**: RunData(파티·스테이지), 캐릭터 선택 → 전투 반영, 웨이브별 스테이지 루프, **파티 영입/교체(최대 4명)**, **파티 시너지**(동일 캐릭터 임계 인원 이상 시 전투 중에만 스탯 보너스, 사망으로 조건 깨지면 즉시 소멸)
- **HUD**: 스테이지·턴 순서·현재 행동 유닛·플레이어 차례 프롬프트·발동 중인 시너지 표시
- **로그라이크 9종 전부**: 파티 강화 5종(공격력/스피드/방어형/크리/회복) + 몬스터 디버프 3종(행동불가/체력감소/공격력감소, 다음 스테이지 1회 예약) + 파티원 영입(꽉 찼으면 교체 대상 선택)
- **스폰 패턴**: 1~5스테이지 수동 설계 + 6스테이지 이후 랜덤 풀 추첨 + 배수 스테이지 보스 강제
- **결과 화면**: 전멸 시 도달 스테이지 + 최고 기록 팝업(신기록 여부 표시) 후 타이틀 복귀
- **로컬 영구 저장**: `SaveData`/`SaveService`(최고 스테이지, 카테고리별 포인트, 옵션값을 `save.json`으로 영속화)
- **성향 포인트 배분**: 캐릭터 선택 화면의 '성향' 버튼 → 오버레이 팝업에서 카테고리별(9종) 영구 포인트 투자/리스펙, `RoguelikeRewardService`의 선택지 추첨 가중치에 실시간 반영
- **사운드**: BGM 크로스페이드(씬 전환·전투/보스 전환) + 유닛 전투음/크리티컬/UI클릭·이동·확정/승리·패배 스팅어 SFX + 옵션 팝업 볼륨 슬라이더(마스터/BGM/SFX)
- **입력 허브 (`InputManager`)**: 흩어진 플레이어 입력을 한곳으로 통합. 바인딩은 `.inputactions` 에셋의 액션맵(Battle/Menu/UI)에서 오고(코드 하드코딩·생성 래퍼 제거), `TargetingController`가 `Mouse.current` 직접 사용 대신 허브 경유. 키 리바인딩 + 세이브 저장, 옵션 팝업 키설정 UI 포함
- **키보드 조작**: 배틀 타겟팅(좌/우 방향키 순환=화면 좌→우 정렬, Enter/Space 확정), 캐릭터 선택(방향키=prev/next), 로그라이크 선택지(방향키 카드 겨냥+Enter/Space 선택). 마우스와 병행, 각 조작음 재생
- **배틀 퍼즈**: ESC 키 / HUD 우상단 버튼으로 열고 닫음. 퍼즈 화면에 현재 스테이지·이전 최고 기록 표시, '계속하기'와 '배틀 중단'(즉시 결과 화면) 제공. `Time.timeScale` 대신 Core의 `IPauseGate`를 유닛 행동 직전에 await하는 방식
- **스폰 구조 분리**: 웨이브 선택·몬스터 생성을 `BattleDirector`에서 `MonsterSpawner`로 분리
- **오브젝트 풀링**: 유닛 View를 파괴하지 않고 프리팹별 `ObjectPool`에 반납·재사용(무한 스테이지 대비). 재사용 시 애니메이터/아웃라인 초기화
- **어셈블리 분리(asmdef)**: `Game.Core`→`Data`→`Progression`→`Systems`→`View` 단방향. Core는 `noEngineReferences`라 UnityEngine 참조 시 **컴파일 에러로 차단**됨(Core/View 분리를 컴파일러가 강제). Core 유닛 테스트 도입의 전제조건도 해결
- **리팩터링 패스**: `BattleRunFlow` 분리(런 경계 ↔ 스테이지 루프), 배틀 패널 3종 `BasePanelUI` 통합, `PendingSignal<T>`로 TCS 패턴 통합, 시너지 조회 이중 구현 제거. 성능은 `Camera.main` 캐싱(`MainCameraCache` — 파괴/비활성 시 자동 재조회라 수동 무효화 불필요)·렌더러 1회 캐싱·`BattleState`/`TurnOrder` 버퍼 재사용·상태이상 이벤트 유닛당 1회로 정리
- **상태이상 + 몬스터 스킬 에셋**: `SkillSO` 3종 생성·연결 완료(Mage=DefDown / Rogue=Poison / Warrior=Line+Stun) — Elite/Boss가 실제로 스킬과 상태이상을 사용한다
- **파티 시너지 수치**: 캐릭터 6종 전부 설정 완료(Knight=DEF+RES, Barbarian=ATK, Mage=CritDmg, Ranger=SPD+RES, Rogue_Dagger=CritRate, Rogue_Crossbow=ATK+SPD)
- **배틀 정보 UI 3종(2026-08)**: 하단 파티 스탯 표기 + 좌상단 시너지 패널 + 화면 배치 인덱스(`A1`/`E2`) — 아래 3-6 참고
- **데미지 숫자 팝업(2026-08)**: 일반/크리티컬/도트 3종 — 아래 4-1 참고
- **코드 컨벤션 일괄 정리(2026-08, 커밋 `1da61a4`·`15400cd`)**: 한 줄짜리 `if`/`for`도 전부 중괄호로 감싸도록 코드베이스 전체를 통일. 앞으로 작성하는 코드도 이 규칙을 따른다(`CLAUDE.md` 작업 원칙 7)

---

## 🔴 1순위 — 핵심 시스템 완성

### 1-1. 보스/엘리트 스킬 실체화  (상태: 부분구현 — 이펙트만 남음)
- 설계: **Elite=단일 대상 스킬, Boss=전체(라인) 대상 스킬**, Normal은 스킬 없음(2026-07-23 확정)
- ✅ 스킬 전용 애니 클립 준비 및 연결 완료 — `Skill_SkeletonMage`/`Skill_SkeletonRogue`/`Skill_SkeletonWarrior`가 각 override 컨트롤러에 물려 있음(더 이상 Attack 클립 공유 아님)
- ✅ `MonsterStatsSO` 세팅 완료 — `MinionSO`=Normal·스킬 없음, `MageSO`/`RogueSO`=Elite·단일 스킬, `WarriorSO`=Boss·전체 스킬
- ⬜ **남은 것: 스킬 연출(단일/라인) 이펙트** — 외부 파티클 에셋 필요, 4-1과 같은 에셋으로 처리 가능
- 참고: `SpawnWaveSO.IsBossWave`(보스 BGM·보스 웨이브 강제 판정)는 `Tier==Boss`만 보므로, Elite만으로 구성된 웨이브는 스킬을 써도 "보스 웨이브" 취급은 아니다(의도된 동작)

### 1-2. 상태이상 시스템  (상태: ✅ 완료 — 2026-08 아이콘 표기까지 끝)
- 계기: `Stats.Res`(디버프 저항)가 선택지·시너지로 값은 쌓이지만 소비하는 곳이 없어 사실상 죽은 스탯이었음(2026-07-23 확인) → **이제 RES가 상태이상 저항으로 소비된다**
- ✅ `StatusKind` 5종(Stun/Poison/AtkDown/DefDown/SpdDown), 부여 정의·진행 상태·저항 판정, 턴 루프 처리(도트→감소→기절), 체력바 상태 표기까지 구현
- ✅ **확장성**: 스킬 데이터를 `SkillSO`(유닛 종류에 안 묶인 별도 에셋)로 분리 — 캐릭터 스킬 추가 시 `CharacterStatsSO`에 참조 필드 하나만 더하면 되고 상태이상 코드는 그대로 재사용
- ✅ `SkillSO` 에셋 3개 생성 + 몬스터 SO 연결 완료(`ScriptableObjects/Skill/`). **현재 실제 수치**(설계 초안에서 밸런싱으로 조정됨):
  - `Skill_SkeletonMage` (MageSO) — 쿨타임 5 / Single / 배율 1.4 / **방어력 감소(DefDown)**, 지속 2턴, 크기 0.5, 부여 확률 0.7
  - `Skill_SkeletonRogue` (RogueSO) — 쿨타임 5 / Single / 배율 1.1 / **중독(Poison)**, 지속 3턴, 크기 0.05, 부여 확률 0.8
  - `Skill_SkeltonWarrior` (WarriorSO) — 쿨타임 3 / **Line** / 배율 1.8 / 기절(Stun), 지속 1턴, 크기 0, 부여 확률 0.35
- ✅ **2026-08 아이콘 연결**: 태그 조립은 `UnitHealthBar.IconTag(spriteName)` 한 곳에 모였고, 상태이상(`Label`)과 스폰 디버프(`MonsterSpawner.DescribeDebuff`)가 같이 쓴다(크기·기준선이 두 표기에서 어긋나지 않도록)
- ✅ **에디터 세팅 완료**: Debuff 아이콘 6종으로 TMP Sprite Asset 개별 생성(`Textures/Icons/Debuff/SpriteAssets/`) → `Debuff_AttackDown.asset`을 `HealthBar.prefab`의 `_statusText`에 할당 → 나머지 5종을 **그 에셋의 Fallback**에 등록. 아이콘 추가 시 같은 체인의 시작점에 붙일 것(다른 에셋에 걸면 조용히 무시된다)
- ⚠️ **겪은 함정(1d9b459 → 해결)**: `<sprite>`가 인식하는 속성은 name/index/anim/color/tint뿐인데 크기를 `scale=`로 주려다 태그 5종 전부가 문자열로 그대로 출력됐다. 증상이 "스프라이트 이름 못 찾음"과 똑같아 엉뚱한 곳을 오래 뒤졌다 — **크기는 바깥의 `<size=%>`로 준다**. 글자로 보이면 ①잘못된 속성 ②이름 불일치 순으로 의심할 것

---

## 🔴 2순위 — 플레이 테스트에서 발견된 수정 사항 (2026-08-06)

> 실제로 플레이하며 뽑은 목록. 아래 원인 분석은 코드를 확인해 채운 것이라 그대로 착수 가능하다.

### 2-1. 교체 대상 카드가 화면 배치 순서와 어긋남  (✅ 수정 완료 — 2026-08-06)
- 증상: 파티가 꽉 찬 상태에서 영입 → 교체 대상 선택 화면에서 **1번 자리가 바바리안인데 1번 카드는 레인저**로 뜬다
- **원인**: 순서의 출처가 둘로 갈라져 있다
  - 화면 순서 = `UnitViewRegistry._slotOccupants` — `SpawnMember`가 `Array.IndexOf(_slotOccupants, null)`로 **앞쪽 빈자리를 재사용**한다
  - 카드 순서 = `RunData.Members` — `Recruit`이 리스트 **끝에 추가**한다
  - 즉 사망·추방으로 중간 슬롯이 빈 뒤 영입하면 그때부터 두 순서가 영구히 어긋난다. 초기 파티(추방 없음)에서는 재현되지 않는다
- **선례**: `PartyStatusBarView`가 같은 함정을 이미 피해 갔다 — 좌표를 `Members` 순서가 아니라 `UnitId → UnitViewRegistry.TryGet`으로 얻는다. 여기도 같은 방식으로 맞추면 된다
- ✅ **수정 내용**: `UnitViewRegistry.GetPartySlots()`가 슬롯 순서대로 `PartySlot`(멤버 + 배치 라벨)을 돌려주고, `PresentReplaceTargetAsync`가 그 순서로 카드를 만든다. 레지스트리는 인스펙터 슬롯을 새로 두지 않고 **`Initialize(registry)` 주입**으로 받는다(`BattlePresenter`/`MonsterSpawner`/`TargetingController`와 같은 규약)
- ✅ 카드 제목에 **배치 라벨을 같이 실었다**(`A1 바바리안`) — 순서에 기대지 않고도 짝이 눈에 보인다. 라벨은 `CreateSlotLabel`이 유일한 출처라 체력바·턴 순서 칩과 어긋날 수 없다(`GetPartySlots`가 내부에서 호출하므로 `private` 유지)
- ⚠️ 멤버와 라벨을 **한 struct로 묶어** 돌려준 이유: 순서와 라벨을 따로 조회하면 둘이 어긋날 여지가 다시 생긴다

### 2-2. 교체 대상 카드에 캐릭터 아이콘이 없음  (✅ 수정 완료 — 2026-08-06)
- 원인은 `PresentReplaceTargetAsync`가 아이콘 인자를 안 넘긴 것(후보 쪽은 `c.Icon`을 넘김). `RunMember.Source`(`CharacterStatsSO`)에서 `Icon`을 꺼내 넘기도록 수정

### 2-3. 카드 스탯 표기를 7종 전부로  (✅ 수정 완료 — 2026-08-06)
- `DescribeStats`가 7종(HP/ATK/SPD/DEF/치명타/치명피해/저항)을 만든다. 영입 후보와 교체 대상이 같은 함수를 쓰므로 양쪽에 함께 반영됐다
- 배율 3종은 `PartyStatusBarView`와 같은 기준으로 정수 %(치명피해 1.5 → `150%`). **항목·순서·% 표기를 그쪽과 일부러 맞춰 뒀다** — 같은 캐릭터를 두 화면이 다른 항목 수로 보여주면 비교가 되지 않기 때문
- 레이아웃은 3-1의 제목 축소와 함께 처리(카드 크기 220×300은 그대로 — 7줄이 들어간다)

### 2-4. 적 행동 중 '배틀 중단'이 먹지 않는 것처럼 보임  (✅ A안으로 수정 완료 — 2026-08-06)
- 증상: 적이 행동하는 동안 퍼즈 → '배틀 중단'을 눌러도 반응이 없어 보이고, 적의 공격이 끝까지 재생된 뒤에야 결과 화면으로 넘어간다
- **원인**: 중단은 `_stageCts.Cancel()`인데, 시뮬레이션은 그 순간 `await actionArgs.WhenPlaybackComplete()`에 들어가 있다. `WhenPlaybackComplete()`는 **취소 토큰을 받지 않고**, 재생 중인 연출은 스테이지 토큰이 아니라 씬 토큰(`_cts.Token`)으로 돌기 때문에 취소가 전달되지 않는다. 결국 **다음 유닛 행동 직전의 `ThrowIfCancellationRequested`**까지 가서야 중단이 반영된다
- 참고: 퍼즈 오버레이가 뜨는 시점과 시뮬레이션이 실제로 멈추는 시점이 다른 것은 **의도된 설계**다(`IPauseGate`는 진행 중인 연출을 자르지 않으려고 행동 직전에만 대기). 이 증상은 그 설계의 부작용이지 게이트 자체의 버그가 아니다
- **채택: (A) 버튼을 잠근다** (2026-08-06 결정). (B) 중단 즉시 반영은 `WhenPlaybackComplete(ct)`로 Core 시그니처를 바꾸고 연출을 중간에 자르게 되므로, "진행 중인 연출은 자르지 않는다"는 기존 설계와 어긋나 채택하지 않았다
- ✅ **수정 내용**
  - `BattlePresenter.IsPlayingBack` — 재생 중인 연출 수를 세어 노출. 등록 경로가 셋(행동/사망/도트)이라 `RegisterPlayback(args, task)` 한 곳으로 모아 증감 짝이 어긋나지 않게 했다(`finally`에서 감소하므로 취소·예외로 끝나도 샌다)
  - `BattlePausePanel`이 이 값을 보고 '배틀 중단'을 `SetEnabled(false)`로 잠근다. 퍼즈 중에도 연출은 계속 재생되므로 `Update`에서 매 프레임 확인해 **연출이 끝나는 순간 잠금이 풀린다**
  - 프레젠터는 `Initialize(presenter)` 주입(인스펙터 슬롯 추가 없음). 주입이 없으면 잠그지 않고 기존과 동일하게 동작한다
  - USS: `.pause-button:disabled { opacity: 0.4 }` — `Common.uss`의 `.btn:disabled`는 `opacity: 1`(성향 배분 +/- 버튼용)이라 여기서 다시 낮춘다. 특정도가 같아 나중에 로드되는 `BattlePause.uss`가 이긴다
- ⚠️ **조건이 "연출 재생 중"인 것이 핵심**이다 — 플레이어 차례(타겟 입력 대기)와 게이트 대기는 취소 토큰을 받아 즉시 중단되므로 잠그면 안 된다. 시뮬레이션의 await 중 **토큰을 받지 않는 것은 `WhenPlaybackComplete()`뿐**이라 이 조건이 정확히 들어맞는다

---

## 🟡 3순위 — UI / UX

### 3-1. 영입 후보 카드 아이콘화  (상태: ✅ 완료 — 2026-08-06 글자 크기·스탯 항목까지)
- `UnitStatsSO`에 `_icon`(Sprite) 추가, `CharacterStatsSO` 6종 에셋에 `Textures/Icons/CharacterProfile/` 아이콘 연결
- `ChoiceCard`(`RoguelikeChoicePanel.cs`)에 `Icon` 필드 추가, `RoguelikeRewardService.PresentRecruitAsync`가 후보 캐릭터의 아이콘을 카드에 전달
- `RoguelikeChoice.uxml`에 `card-icon` 슬롯 추가 + `RoguelikeChoicePanel.PresentAsync`가 바인딩(아이콘 없는 카드는 자동 숨김)
- ✅ **이름 폰트 축소**: `.card-title` 22px → **16px**, 아래 여백 16px → 10px(스탯 7줄에 자리를 내주기 위함)
  - 요청은 "절반 정도"였으나 11px면 `.card-desc`(15px)보다 작아져 제목-본문 위계가 뒤집힌다. 더 줄이려면 `.card-desc`를 함께 낮춰야 한다 — 값 하나 수정이라 언제든 조정 가능
- ✅ **스탯 항목 수**: 카드가 2-3에서 7종이 되어 전투 하단 파티 표기와 일치. 남은 차이는 **캐릭터 선택 화면 6종**(`CharacterStatBarsView` — 치명피해 없음)뿐이며, 그쪽은 막대 그래프라 표현 방식이 달라 3-3에서 별도 판단

### 3-2. 타겟팅 피드백  (상태: 부분구현)
- 마우스 2단계 클릭 + 방향키 순환/Enter·Space 확정은 구현 완료. 겨냥된 **단일** 대상은 빨강 아웃라인 표시
- 남은 것: 플레이어 차례에 **유효 대상 전체** 하이라이트 / 커서 피드백(어디를 겨냥할 수 있는지)
- `UnitView.SetOutlineLayer`/`ResetOutlineLayer`가 이미 있으므로 겨냥용(빨강)과 다른 레이어를 하나 더 두면 되는 구조

### 3-3. 캐릭터 선택 화면 폴리시  (상태: 부분구현 — 2026-08 아이콘 추가)
- 스탯 바(로스터 대비 비율)는 구현 완료
- ✅ 캐릭터 아이콘 추가: `TitleScreen.uxml`의 `character-name` 옆에 `character-icon` 슬롯, `CharacterSelectPanelUI.ApplySelection()`이 바인딩(3-1과 같은 `UnitStatsSO.Icon` 재사용)
- 남은 것: nav-arrow·인디케이터 점·스탯 바 강조색의 구 시안 톤 이전(3-5 참고)

### 3-6-보조. 로그라이크 선택지 9종 카테고리 아이콘  (상태: ✅ 완료 — 2026-08, TODO 신규 항목)
- `RoguelikeChoiceSO`에 `_icon`(Sprite) 추가, 9개 에셋에 `Textures/Icons/Buff`/`Debuff` 아이콘 연결(EnemyStun/EnemyHpDown/EnemyAtkDown은 상태이상 Debuff 아이콘과 재사용)
- `RoguelikeChoice.uxml`/`.uss`에 `card-icon` 슬롯 및 스타일 추가(3-1과 동일 슬롯 공유)

### 3-4. 옵션 메뉴 — 해상도/언어  (상태: 미착수)
- 볼륨(마스터/BGM/SFX) 슬라이더는 구현 완료
- 해상도(창모드 3종 + 전체화면), 언어는 `SaveData.Options` 필드(`ResolutionIndex`/`Language`)만 있고 UI·적용 로직 미구현
- ✅ 선행 준비 완료: `Systems/GameSettings`가 옵션 값의 단일 진입점이라, 해상도는 여기에 프로퍼티 + `ApplyDisplay()`를 추가하고 `GameManager.Awake`에서 한 번 부르면 된다. UI는 드롭다운/토글만 붙이면 끝
- ❓ **선결 사항: 언어 설정을 뺄지 결정** — `SaveData.cs`의 `Language` 필드에 "언어 설정 기능 뺄 것 같음" 주석이 있다. 빼기로 하면 해상도만 구현하는 작은 작업이 되고, 외부 에셋이 필요 없어 **지금 바로 착수 가능한 유일한 기능 작업**이다

### 3-5. UI 비주얼 폴리시  (상태: 부분구현 — 2026-08 버튼/패널 톤 교체 완료)
- ✅ 기존 골드/나무 텍스처 카툰 톤을 다크 민트/바이올렛 네온 톤으로 전면 교체(`UI_DesignReference.md` 참고). 버튼(`Common.uss` `.btn` 계열) + 패널·팝업 배경(`.panel-frame`) + 배틀 HUD 턴 순서 칩 모서리·트랜지션까지 적용
- ⬜ 캐릭터 선택 화면의 nav-arrow·인디케이터 점·스탯 바 강조색은 아직 구 시안/네이비 톤 그대로 — 새 팔레트로 이전 필요
- ⬜ **버튼 크기 조정** — 현재 `.btn` 계열이 레퍼런스 이미지 대비 작음. 톤(색상)은 유지하고 크기만 키우는 방향(2026-08 확인, 레퍼런스 이미지 별도 공유 예정)
- 아이콘 작업(3-1/3-3)과 함께 처리하면 톤을 한 번에 맞출 수 있다

### 3-6. 배틀 정보 표시 확장  (상태: 부분구현 — 2026-08 하단 파티 스탯 바 완료)
- ✅ **파티 스탯 표기**: 화면 하단에 파티원별 스탯 7종(HP/ATK/SPD/DEF/치명타/치명피해/저항) 패널. 각 패널이 그 캐릭터의 **스폰 위치 아래로 정렬**된다(`WorldToScreenPoint` → `RuntimePanelUtils.ScreenToPanel`). `PartyStatusBarView`(순수 C#)
  - 한 행이 `현재값 (+성장) (+시너지) (-디버프)` 형태의 색 구분 텍스트다(막대 없음). 앞 숫자는 실제 적용 중인 유효 스탯이고 괄호는 출처 내역, 0인 항목은 생략
- ✅ **선택지로 증가한 스탯 표**: 위 하단 표기의 `(+성장)`이 `RunMember.Stats − BaseStats`(선택지 + 스테이지 자동 성장)라 이 항목을 흡수했다
- ✅ **시너지 패널**(2026-08): 좌상단에 `아이콘 + 이름 + ×요구인원 + 효과` 행 목록(`SynergyPanelView`). **아직 인원이 모자란 시너지도 흐리게 함께 표시**해 "한 명만 더 모으면 발동"이 보이므로 영입 선택지의 판단 근거가 된다
  - 부수 수정: `×N`이 현재 파티 인원을 찍고 있어 숫자가 흔들리던 것을 **요구 인원 고정**으로 바로잡음. 발동 여부는 농담(濃淡)으로만 구분
  - `ActiveSynergy` → `PartySynergy`로 rename(미발동도 담게 되어 이름이 거짓이 됨), 발동 여부는 `IsActive`로 파생
- ✅ **화면 배치 인덱스**(2026-08): 체력바 왼쪽에 `[A1]`, 상단 턴 순서 칩에는 이름 대신 `A1`을 찍어 **칩과 화면 위 유닛을 1:1로 맞춘다**(같은 캐릭터를 2명 영입해도 구분됨). A=아군/E=적군. 문자열은 `UnitViewRegistry.CreateSlotLabel` 한 곳에서만 만들어 두 표기가 어긋날 수 없다
  - `UnitHealthBar._indexText`는 **선택 참조**라 프리팹에 연결하지 않으면 체력바 표기만 조용히 빠지고 칩은 정상 동작한다
- ⬜ 남은 것: 스킬 쿨타임·다음 행동 예고 등 전투 중 정보는 아직 없음(필요성 재검토 후 결정)

---

## 🟢 4순위 — 연출 / 폴리시

### 4-1. 히트 이펙트 / 데미지 표시  (상태: 부분구현 — 2026-08 데미지 팝업 완료)
- ✅ **데미지 숫자 팝업**: `DamagePopup`(팝업 1개의 표현 + 떠오르며 사라지는 연출) + `DamagePopupSpawner`(`ObjectPool` 소유, 월드 좌표에 스폰). 일반=흰색 / 크리티컬=빨강+확대 / 도트=별도 색 3종. `BattlePresenter`의 히트 루프와 `OnStatusTicked` 두 곳에서 스폰하며, **연출 대기(`RegisterPlayback`)에는 넣지 않아** 전투 페이싱은 그대로다. 위치는 `UnitView.PopupOrigin`(`_popupHeight` 값 하나, 유닛 프리팹 계층 수정 없음)
  - ⚠️ 팝업은 스포너의 자식이다 — 유닛 View는 풀 반납 시 비활성화되므로 유닛 밑에 두면 재생 중인 팝업이 함께 꺼진다
- ⬜ **타격 이펙트**: 기존에 만들어뒀던 파티클 효과를 다시 가져와 연결. 스폰 위치는 데미지 팝업과 같은 히트 루프(`BattlePresenter.PlayActionAsync`)

#### 4-1-a. 타격 순간 연출 시퀀스  (설계 확정 — 2026-08-06)

현재 팝업·피격 연출은 `_impactDelay`(인스펙터 고정값) 뒤에 일괄 재생된다. 이걸 **공격 애니메이션 클립의 애니메이션 이벤트**로 옮기고, 그 한 시점에 아래 넷을 함께 터뜨린다.

| 연출 | 현재 상태 | 비고 |
|---|---|---|
| 타격 파티클 | ⬜ 미구현 | 외부 에셋 필요(1-1 스킬 이펙트와 같은 에셋으로 처리) |
| 피격(Hit) 애니메이션 | ✅ 있음 | 재생 시점만 이벤트로 이동 |
| 데미지 팝업 | ✅ 있음 | 재생 시점만 이벤트로 이동 |
| **히트 스톱**(타격 순간 시간을 잠깐 느리게) | ⬜ 신규 | 아래 주의사항 참고 |

- ⚠️ **히트 스톱에 `Time.timeScale`을 쓰면 안 된다.** 전투 연출·씬 전환 페이드가 전부 `Awaitable`(`WaitForSecondsAsync`) 기반이라 timeScale에 함께 묶일 수 있고, `BattlePausePanel`이 timeScale을 쓰지 않기로 한 것도 같은 이유다. **연출 대상(애니메이터 `speed`, 파티클)만 골라서 늦추는** 방식이 이 프로젝트 구조에 맞는다
- ⚠️ **애니메이션 이벤트는 클립 에셋에 박히므로 Core가 관여할 수 없다.** 시뮬레이션은 `RegisterPlayback`으로 "연출이 끝났다"만 기다리는 구조를 유지하고, 이벤트→시퀀스 트리거는 전부 View(`UnitAnimator`/`BattlePresenter`) 안에서 끝내야 한다
- 구현 방식 TBD: 이벤트 하나에 여러 연출을 묶는 시퀀스 구조가 필요하다. 유닛 프리팹 10종 + 클립 다수에 이벤트를 심어야 하므로 **작업량은 코드보다 에셋 쪽이 크다**

#### 4-1-b. 체력바 감소 애니메이션  (⬜ 신규 — 2026-08-06)
- 현재 `UnitHealthBar`가 `_fill.fillAmount`를 즉시 대입해 게이지가 뚝 끊긴다. 목표 값까지 부드럽게 줄어드는 연출 추가
- 기존 `CameraShake`/`DamagePopup`과 같은 `async Awaitable` 프레임 루프 + `destroyCancellationToken` 패턴을 그대로 쓰면 된다
- ⚠️ **`RegisterPlayback`에 넣지 말 것**(데미지 팝업과 같은 판단) — 장식이라 시뮬레이션을 기다리게 할 이유가 없고, 넣으면 전투 페이싱이 느려진다
- ⚠️ **풀에서 재사용되는 인스턴스**라 진행 중인 트윈이 다음 스폰까지 살아남지 않게 `Initialize`/`ResetForSpawn`에서 즉시 목표값으로 끊을 것. 사망 시 `SetVisible(false)`와 겹치는 순서도 확인 필요
- 4-1-a와 같이 하면 자연스럽다 — 히트 스톱으로 느려진 순간에 게이지가 줄어드는 그림이 타격감의 핵심

### 4-2. 입력 시스템 정식화  (상태: ✅ 완료)
- ✅ 바인딩을 `.inputactions` 에셋의 액션맵(Battle/Menu/UI)으로 이전, 코드 하드코딩 제거, 생성 래퍼 삭제
- ✅ 키 리바인딩 + 세이브 저장(`SaveData.Options.InputBindingOverrides`), 옵션 팝업에 키설정 UI
- ✅ **에디터 할당 완료**: 두 씬(Intro/Battle)의 InputManager 오브젝트 `_actions` 슬롯에 `.inputactions` 에셋 연결됨
- ✅ **씬 정리 완료**: 두 씬 모두 잔여 `PlayerInput` 컴포넌트 0건, Missing 스크립트(`m_Script: {fileID: 0}`) 0건

---

## 🔧 기술 부채 / 알려진 이슈

- ~~**영입 스탯 불일치 의심**(2026-08 발견)~~ → **버그 아님으로 종결(2026-08)**. 영입한 캐릭터가 파티에 이미 있던 같은 캐릭터보다 약한 건 계산 오류가 아니라 **의도된 규칙**이다 — `ApplyCatchUp`은 스테이지 자동 성장만 소급하고 로그라이크 선택지 성장은 소급하지 않는다("그건 그 시점 파티가 벌어들인 몫"). 하단 파티 스탯 표기의 파란 `(+선택지)` 괄호가 새 영입자에게만 비어 있는 것이 이 규칙의 정상적인 결과다. 규칙 자체를 바꾸려면 밸런싱 패스에서 다룰 것(전부 소급 / 일정 비율만 소급)
- **미사용 USS 변수**: `Common.uss`의 `--color-cyan-mid`가 버튼 팔레트 교체(3-5) 이후 실사용처 없음 — 정리 시 삭제. 반면 `--color-cyan-light`는 시너지 패널의 `×N`(`.synergy-count`)에서 새로 쓰이므로 **남겨둘 것**(캐릭터 선택 잔여 시안 톤과 함께 지우지 말 것)
- **에셋 이름 오타**: `ScriptableObjects/Skill/Skill_SkeltonWarrior.asset` — `Skeleton`이 `Skelton`으로 빠져 있다. 참조가 늘기 전에 고치는 편이 낫다
- **`MinionSO.asset`에 구 필드 잔여**: `_skillCooldown`/`_skillScope`/`_skillPowerMultiplier` — `SkillSO` 분리 이전의 인라인 필드가 YAML에 남아 있다. Unity가 무시하므로 동작엔 무해하고, 에디터에서 해당 에셋을 한 번 수정·저장하면 사라진다
- **Core 유닛 테스트 미도입**: asmdef 분리로 전제조건은 해결됐고 `com.unity.test-framework`도 설치돼 있으나 테스트 어셈블리가 아직 없다. 회귀 가치가 높은 후보 — `DamageCalculator`(비율 감소+크리), `TurnOrder`(SPD 정렬), `StageScaling.CreatePlayerGrowth`(반올림 누적 방지), `Unit.TryApplyStatus`(RES 저항 확률)
- **패널 폴더 정리 미완**: 리팩터링 계획의 "배틀 패널 3종을 `Battle/View/Panels/`로 모은다" 중 `BasePanelUI` 상속만 적용됐고 폴더 이동은 안 했다(현재 `Battle/View/` 직속). 순수 정리 작업이라 우선순위 낮음
- **UXML `<Style src>` 링크**: 에디터 밖에서 파일 생성 시 임포트 순서 때문에 USS 링크가 안 걸릴 수 있음 → 해당 UXML Reimport(또는 에디터 재시작)로 해결. 신규 UI 추가 시 주의
- **BattleScene 직접 실행 시 씬 전환 없음**: `GameManager`가 없어 `LoadScene`이 조용히 건너뛰어진다(결과 화면까지는 정상). 테스트 파티 폴백과 짝을 이루는 의도된 동작이지만 로그가 없어 원인 파악이 늦어질 수 있음
- **연출 시간 하드코딩**: `UnitView`의 spawn/attack/skill/hit/die 지속시간이 인스펙터 수동 값. 애니 클립 길이 자동 추출을 고려할 수 있음(선택)
- **밸런싱 수치 전반 TBD**: 아래 참고

---

## ⚖️ 밸런싱 (TBD — 별도 패스)

기획·구현과 분리해 수치만 조정하는 단계. 전부 SO 데이터로 관리 중이라 코드 수정 없이 조정 가능.
- 캐릭터/몬스터 세부 스탯 (`CharacterStatsSO`/`MonsterStatsSO`)
- 로그라이크 선택지 수치 및 **카테고리별 등장 가중치** (`RoguelikeChoiceSO`)
- 데미지 공식 상수 (`DamageCalculator.DefenseConstant` 등)
- 스폰 패턴 구성 (`SpawnWaveSO` / 패턴 풀)
- 보스/엘리트 스탯 배율

---

## 📝 기타 미정 (README TBD)

- 게임 타이틀(제목)
- 모바일 확장 여부
- 다국어 지원 여부
