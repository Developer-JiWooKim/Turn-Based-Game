# TODO / 로드맵

이 문서는 앞으로 해야 할 작업을 정리한 목록입니다.
게임 기획은 `README.md`, 코드 작업 규칙·현재 코드 상태는 `CLAUDE.md`를 참고하세요.
**적용된 계산식과 밸런싱 기준은 `SystemFormulaBalance.md`에 따로 정리돼 있습니다.**
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
- **전투 연출 일괄(2026-08-06)**: 타격 순간 시퀀스(애니메이션 이벤트 → 파티클·피격·숫자·히트스톱) + 근접 이동/회전 + 원거리 투사체 + 스킬별 연출 분기(메이지=머리 위 낙하 / 워리어=중앙 이동). 유닛 10종 프리팹 배정과 씬 배선까지 완료 — 아래 4-1 참고
- **스탯 10배 리스케일 + 시너지 비율화(2026-08-06)**: 반올림 해상도 확보와 후반 시너지 유효성 확보. `DefenseConstant`도 함께 이동 — 아래 밸런싱 항목 참고
- **공식 문서화**: 적용된 계산식 14종을 `SystemFormulaBalance.md`에 정리(입력·출력·예시·상한/하한·조심할 점)
- **코드 컨벤션 일괄 정리(2026-08, 커밋 `1da61a4`·`15400cd`)**: 한 줄짜리 `if`/`for`도 전부 중괄호로 감싸도록 코드베이스 전체를 통일. 앞으로 작성하는 코드도 이 규칙을 따른다(`CLAUDE.md` 작업 원칙 7)

---

## 🔴 1순위 — 핵심 시스템 완성

### 1-1. 보스/엘리트 스킬 실체화  (상태: ✅ 완료 — 2026-08-06 이펙트까지)
- 설계: **Elite=단일 대상 스킬, Boss=전체(라인) 대상 스킬**, Normal은 스킬 없음(2026-07-23 확정)
- ✅ 스킬 전용 애니 클립 준비 및 연결 완료 — `Skill_SkeletonMage`/`Skill_SkeletonRogue`/`Skill_SkeletonWarrior`가 각 override 컨트롤러에 물려 있음(더 이상 Attack 클립 공유 아님)
- ✅ `MonsterStatsSO` 세팅 완료 — `MinionSO`=Normal·스킬 없음, `MageSO`/`RogueSO`=Elite·단일 스킬, `WarriorSO`=Boss·전체 스킬
- ✅ **스킬 이펙트 완료**: 세 몬스터 모두 `UnitView._skillProjectile` 배정. 연출도 종류별로 갈린다 —
  - Skeleton_Mage: `_skillFallsOnTarget` ON → 번개가 대상 머리 위에서 떨어진다
  - Skeleton_Warrior: `_skillMovesToCenter` ON → 전장 한가운데로 나가서 전체 공격
  - Skeleton_Rogue: 평타와 같은 발사 방식에 스킬 전용 투사체만 교체
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

### 3-4. 옵션 메뉴 — 해상도/언어  (상태: ✅ 코드 완료 — 2026-08-10 / 에디터 세팅 남음)
- 볼륨(마스터/BGM/SFX) 슬라이더는 이전에 완료
- ✅ **해상도**: 화면 모드 프리셋 2종(`창모드 1280×720` / `전체화면 1920×1080`)을 `GameSettings.DisplayPresets`에 두고 옵션 팝업에서 버튼으로 고른다. 적용은 `GameSettings.ApplyDisplay()` → `GameManager.Awake`에서 1회
  - 해상도와 전체화면을 **한 프리셋으로 묶었다** — 따로 두면 "창모드인데 1920×1080" 같은 어긋난 조합이 저장된다. 그래서 `SaveData.Options.Fullscreen`(bool)은 제거했다(JsonUtility가 없는 필드를 버리므로 기존 세이브와 호환)
  - ⚠️ 프리셋을 늘릴 때는 **배열 뒤에 추가**할 것 — 세이브에 인덱스가 저장되므로 순서를 바꾸면 기존 설정이 다른 모드를 가리킨다
  - ⚠️ 에디터 Game 뷰는 `Screen.SetResolution`을 따르지 않는다 — **확인은 빌드에서** 해야 한다
- ✅ **언어(Ko/En)**: 문자열 표(`LocalizationTableSO`) + 조회 진입점(`Loc`)을 새로 만들고 UI 문구 79종을 옮겼다. 언어를 바꾸면 **즉시** 다시 그려진다(`Loc.LanguageChanged`)
  - ✅ **한글 로고 교체**: 한국어면 `Title_Logo_Ko.png`로 바뀐다. 이미지 경로는 USS(`.title-logo--ko`)가 들고 `TitlePanelUI`는 클래스만 토글하며, 로고와 그림자를 **함께** 바꾼다(같은 이미지를 쓰므로 한쪽만 바꾸면 그림자만 영문으로 남는다)
  - 자세한 키 규약·구조는 `CLAUDE.md`의 "로컬라이제이션" 항목 참고
- ⬜ **남은 에디터 세팅**: 두 씬(Intro/Battle)의 `GameManager` 오브젝트에서 `_stringTable` 슬롯에 `ScriptableObjects/Localization/UiStringTable.asset`을 할당할 것. 비어 있으면 UI에 `ui.…` 키가 그대로 보인다
- 참고: 월드스페이스 체력바(TMP)는 슬롯 라벨·숫자·아이콘만 찍으므로 **한글 폰트 작업이 필요 없다**. 화면 UI는 UI Toolkit이라 애초에 이 제약이 없다

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
- ✅ **타격 이펙트**: 스폰 구조(`HitEffectSpawner`)까지 완료 — 프리팹 연결만 남았다(아래 4-1-a)

#### 4-1-a. 타격 순간 연출 시퀀스  (✅ 코드 완료 — 2026-08-06 / 에디터 세팅 남음)

팝업·피격 연출을 `_impactDelay`(고정값) 대신 **공격 클립의 애니메이션 이벤트** 시점에 맞추고, 그 한 지점에서 넷을 함께 터뜨린다.

| 연출 | 상태 | 담당 |
|---|---|---|
| 타격 파티클 | ✅ 코드 완료 (프리팹 연결 남음) | `HitEffectSpawner` (신규) |
| 피격(Hit) 애니메이션 | ✅ 시점 이동 완료 | `UnitView.PlayHitAsync` |
| 데미지 팝업 | ✅ 시점 이동 완료 | `DamagePopupSpawner` |
| 히트 스톱 | ✅ 코드 완료 | `HitStop` (신규) |

- **타격 시점**: `UnitAnimator.WaitForImpactAsync(fallback, ct)`가 판단한다. 유닛별 토글 `_useImpactEvent`가 켜져 있으면 클립의 `OnImpactFrame` 이벤트를 기다리고, 꺼져 있으면 `BattlePresenter._impactDelay`를 그대로 쓴다(**기존 동작**) — 클립 작업을 유닛 하나씩 점진적으로 옮길 수 있다
  - ⚠️ 이벤트를 켜 뒀는데 클립에 이벤트가 없으면 전투가 멈추므로, 연출 길이(`_attackDuration`/`_skillDuration` 중 큰 값)를 **안전 타임아웃**으로 두고 타임아웃으로 풀리면 **경고 로그**를 남긴다. 조용히 넘어가지 않는다
  - ⚠️ 애니메이션 이벤트는 **Animator와 같은 GameObject의 컴포넌트만** 호출할 수 있다. `UnitAnimator`가 `[RequireComponent(typeof(Animator))]`로 그 자리에 묶여 있어 성립하는 구조다. 클립은 메서드를 **이름 문자열로** 참조하므로 `OnImpactFrame`을 rename하면 이벤트가 조용히 끊긴다
- **히트 스톱**: `Time.timeScale` 대신 **관여한 유닛(때린 쪽 + 맞은 쪽)의 `Animator.speed`만** 낮춘다. 인스펙터 값 3개(지속시간/느려지는 정도/크리티컬 배율)이며 지속시간 0이면 꺼진다
  - ⚠️ 늦춘 만큼 애니메이션이 뒤로 밀린다. 길게 잡으면 `UnitAnimator`의 연출 시간을 그만큼 늘려야 끝이 잘리지 않는다
  - `finally`에서 속도를 되돌리고 `ResetToSpawn`도 `speed = 1`로 초기화한다 — 취소나 풀 반납 중에 느려진 채로 굳지 않도록
- **파티클**: `HitEffectSpawner`가 `DamagePopupSpawner`와 같은 풀 구조(단, 프리팹이 여러 종일 수 있어 `UnitViewRegistry`처럼 **프리팹별** 풀). 프리팹은 스크립트 없는 순수 `ParticleSystem`이면 되고, 크기·오프셋·수명·크리티컬 배율을 인스펙터에서 조정한다(수명 0 = 파티클 설정에서 자동 계산)
  - 이펙트·팝업은 `RegisterPlayback`에 넣지 않는다(장식) / 히트 스톱은 **기다린다**(늦추는 동안 다음 연출이 겹치면 효과가 사라진다)
  - 스폰 위치는 `UnitView.HitEffectOrigin`(`_hitEffectHeight` 값 하나) — 팝업과 같이 앵커 오브젝트를 두지 않아 유닛 프리팹 계층을 건드리지 않는다
  - 도트 피해(`OnStatusTicked`)에는 붙이지 않았다 — 중독 피해에 물리 타격 스파클은 어울리지 않는다. 필요하면 전용 이펙트를 따로 둘 것

**✅ 에디터 세팅 완료 (2026-08-06)**
- `HitEffectSpawner`·`HitStop` 씬 배치 + `BattlePresenter` 슬롯 연결 완료
- **애니메이션 이벤트 전면 적용** — 유닛 10종 전부 `_useImpactEvent: 1`이고, 실제 재생되는 Attack/Skill 클립에 `OnImpactFrame`이 심겨 있다. 이제 타격 시점이 고정 지연이 아니라 클립 프레임에서 나온다(`_impactDelay 0.35`는 폴백으로만 남음)
- 유일한 예외는 `Skill_SkeletonMinion.anim`(이벤트 없음) — Minion은 `Tier=Normal`이라 스킬 자체가 없어 재생되지 않으므로 무해하다

#### 4-1-c. 공격 시 대상 바라보기 + 근접 이동 연출  (✅ 코드 완료 — 2026-08-06 / 조정 남음)
- 공격 전 대상을 향하고 공격 후 제자리로 되돌아온다. **근접·원거리 차이는 `UnitView`가 흡수**하므로 `BattlePresenter`는 구분하지 않는다(`FaceTargetAsync` → 공격 → `RestorePoseAsync`)
  - 근접(`_approachTarget` ON — 바바리안·기사·도적(대거)·스켈레톤 미니언): 대상 앞까지 점프 이동 + 회전
  - 원거리(OFF — 메이지·레인저·석궁·스켈레톤 메이지/로그/워리어): 제자리 회전만(`_turnDuration` 0.15초)
  - 공격하는 동안 자기 체력바를 숨긴다(양쪽 공통) — 게이지가 공격 연출 위로 겹치지 않게
  - ⚠️ 복귀 판정은 근접 토글이 아니라 **`_isDisplaced`(실제로 자리를 떠났는지)**로 한다 — 근접이 아닌 워리어도 전체 공격 연출로 중앙까지 나가기 때문
- **점프 클립 없이 트랜스폼 포물선**으로 구현했다 — 애니메이터에 점프 상태가 없기 때문. 이동 중에는 Idle 포즈로 미끄러지듯 날아간다
- 각도 차 5도 미만이면 회전을 건너뛴다 — 정면 대상까지 매번 왕복 회전을 기다리면 페이싱만 쓰게 된다
- ⬜ **인스펙터 조정 필요**: `_approachDistance`(1.5) 모델이 겹치지 않는지, `_approachJumpHeight`(0.8) 궤적이 자연스러운지, `_approachDuration`(0.25) 페이싱이 늘어지지 않는지
- ⬜ **선택**: 점프/이동 클립을 만들어 애니메이터에 추가하면 `MoveAsync`에서 트리거만 같이 재생하면 된다. 지금도 동작하므로 급하지 않다
- ✅ **회전**: 갈 때 대상 쪽, 올 때 원래 회전으로 `Slerp` 보간(이동과 같은 구간). 착지 후 스냅이 없다
- ✅ **체력바**: 이동하는 동안 숨기고 복귀 후 다시 켠다(대상 위에 겹쳐 떠다니지 않도록). 취소로 끊겨도 `SnapHome`이 되살린다
- 참고: 왕복이 연출 대기에 포함돼 **공격 1회당 0.5초**가 페이싱에 더해진다. 늘어지면 `_approachDuration`부터 줄일 것

#### 4-1-d. 투사체 + 스킬별 연출 분기  (✅ 완료 — 2026-08-06, 프리팹 배정까지)
- `ProjectileSpawner`(신규) — 발사 → 비행 → 도착. **도착을 기다린 뒤에** 피격 연출이 나간다(화살이 닿기 전에 숫자가 뜨지 않도록)
- 타격 시점의 의미가 원거리에게는 **"명중"이 아니라 "발사"**다. 비행 시간(거리÷속도, 상한 있음)이 그 사이에 들어간다
- 이펙트 3종(`_projectile`/`_muzzleFlash`/`_hitEffect`)은 **유닛별**이고, `_skill*` 3종을 채우면 스킬에만 쓰인다(비우면 평타 것 재사용)
- 에셋: `Assets/MasterStylizedProjectiles/Projectiles/` 24종, 각 폴더에 `*Bullet`/`*Hit`/`*Muzzle` 3종. 루트에 `ParticleSystem`이 있고 이동 스크립트가 없어 그대로 쓸 수 있다
- **씬 배선 완료**: `ProjectileSpawner` 배치 + `BattlePresenter._projectiles` 연결
- **프리팹 배정 완료**: 유닛 10종 전부 `_projectile`·`_muzzlePoint` 배정. 근접 4종(Barbarian/Knight/Rogue_Dagger/Skeleton_Minion)도 투사체를 쓰는데, 대상 앞으로 점프한 뒤 짧게 날아가므로 참격 계열 이펙트로 기능한다

**⬜ 남은 자잘한 것**
- `_hitEffect`가 비어 있는 유닛 3종(Knight / Rogue_Dagger / Skeleton_Minion) — `HitEffectSpawner`의 전역 기본값으로 떨어지므로 동작은 정상이다. 무기별 타격감을 주려면 채울 것
- `_muzzleFlash`는 근접 4종에 비어 있다(총구가 없는 무기라 자연스러움 — 필요 없으면 그대로 둘 것)
- 페이싱 조정: `ProjectileSpawner._speed`(18) / `_arcHeight`(0이면 직선, 올리면 곡사) / `_muzzleLeadSeconds`(0.05)

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

> **2026-08-10 성장 곡선 대조표 작성 완료 → `BalanceCurve.md`.** 조정 순서는 그 문서의 결론을 따른다:
> 1. ~~**선택지 계열 간 격차**~~ → ✅ **1차 조정 완료(2026-08-10)**. SpeedUp(Hp 100·Atk 50 추가)·DefensiveUp(Hp 60·Atk 35)·CritUp(치명피해 0.25)을 조정해 50스테이지 전략 간 편차를 15.5배 → 2.6배로 좁혔다. AttackUp은 기준선이라 미변경
> 2. ~~**방어 강화가 전투를 늘어뜨린다**~~ → ✅ 위 조정으로 처치 라운드 30.1 → 10.0
> 3. ⬜ **파티 인원이 난이도를 지배한다** — 솔로 안전율 1.1~1.6 vs 4인 11~22. 난이도 최저점은 솔로 5~7스테이지. 손잡이는 `Recruit.asset`의 `_weightPerEmptySlot`(현재 4)이지만 **"4인 파티가 정상 상태인가"를 먼저 정해야** 방향이 나온다
> 4. ⬜ 복리 대응(선택지 % 전환) — 2차 조정으로 균형 플레이가 50 전후에서 막히도록 맞춰졌다. 그보다 더 멀리 보낼 생각이면 그때 검토
> 5. ⬜ % 전환 시 8%는 과하다(50스테이지 128.5배). 3~4%부터 대조표로 재확인
>
> **2차 조정 완료(2026-08-10)** — 난이도 곡선 + 속도의 가치. `StageScalingSO`에 필드 5개 추가:
> - 30스테이지까지 기존 성장률, 이후 **×1.5 가속**(`_accelStartStage` / `_accelMultiplier`) → 균형 플레이 안전율 S29 `17.4` → S36 `8.6` → S42 `3.7` → S48 `1.6`, 벽은 50 전후
> - 5스테이지 이후 **몬스터 SPD 3%/스테이지**(`_monsterSpdRate` / `_spdStartStage`) → 속도 미투자 시 18스테이지부터 선공 상실(안전율 −38%)
> - **보스 처치당 플레이어 SPD +8%**(`_bossSpdRate`) — 완충용이며 몬스터 증가분을 따라잡지 못한다(의도)
> - ⚠️ SPD 스케일링은 원래 배제됐던 결정을 뒤집은 것이다. 근거와 주의사항은 `BalanceCurve.md` D-0b절과 `StageScaling.cs` 주석 참고
>
> ⚠️ **1·2차 조정 모두 모델 계산 기반이고 실제 플레이 검증은 아직이다.** 특히 방어·속도는 저항·턴 순서를 모델이 단순화해 다뤄 오차가 크므로, 플레이에서 과하게 느껴지면 그 둘부터 내릴 것
>
> ⚠️ **난이도를 잴 때 표본 스테이지가 5의 배수(보스)에 몰리지 않게 할 것** — 1차 조정이 실제로 그 오류로 보스전 기준에 맞춰졌다가 재측정으로 바로잡혔다

> **2026-08-06 스탯 10배 리스케일 + 시너지 비율화 + 시너지 1차 조정 완료.** 자세한 규칙은 `CLAUDE.md`의 "스탯 스케일"·"파티 시너지" 항목 참고.
> - **10배 리스케일은 양측을 함께 올렸으므로 상대 밸런스를 바꾸지 않았다**(`DefenseConstant`도 같이 올림). 몬스터를 여기에 맞춰 올릴 이유가 없다 — 올리면 리스케일 전보다 어려워진다
> - 실질 강화는 **시너지 비율화뿐**이며, 그것도 같은 캐릭터를 2명 이상 모은 파티에만 적용된다. 그래서 전역인 몬스터 성장률이 아니라 시너지 수치로 조정했다

### ⚠️ 시너지 조정 시 반드시 볼 것 — 스탯마다 같은 %의 가치가 다르다

| 스탯 | 실제 효과 | 조정 감각 |
|---|---|---|
| ATK | 피해량에 **선형** 반영 | +20% = 피해 +20% |
| 치명타/치명피해 | 기대 피해 배율에 반영 | 현재 크리율에 따라 달라짐 |
| DEF | `K/(K+DEF)` 감쇠라 **수익 급감** | DEF 150→300(+100%)이 받는 피해 −11.5%뿐 |
| SPD | 모든 유닛이 턴당 1회 행동 → **순서만** 바뀜 | 전투력 환산 가치가 가장 낮음 |

비율 필드는 `Range(0, 2)`라 DEF처럼 큰 값이 필요한 스탯도 표현할 수 있다.

**1차 조정 결과(효과 기준 +15~18% 대로 정렬):**

| 캐릭터 | 값 | 실제 효과 |
|---|---|---|
| Barbarian | ATK +18% | 피해 +18% |
| Rogue_Dagger | 치명타 +20%p | 피해 +17% |
| Mage | 치명피해 +90%p | 피해 +17% |
| Knight | DEF +100%, RES +15%p | 받는 피해 −11.5% (실효 HP +13%) + 저항 |
| Rogue_Crossbow | ATK +12%, SPD +20% | 피해 +12% + 선공 |
| Ranger | SPD +50%, RES +15%p | 선공 + 저항 (유틸리티 성격) |

- 조정 전에는 최상(Barbarian 피해 +30%)과 최하(Ranger 사실상 0)가 3배 넘게 벌어져 있었다. 표시값만 보면 Knight(+53%)·Ranger(+50%)가 과해 보였지만 **실제로 과했던 건 Barbarian**이다
- Ranger는 수치로 환산되지 않는 유틸리티 성격이라 정렬이 어렵다 — 플레이해보고 약하면 RES를 더 올리는 게 자연스럽다

- 캐릭터/몬스터 세부 스탯 (`CharacterStatsSO`/`MonsterStatsSO`)
- 로그라이크 선택지 수치 및 **카테고리별 등장 가중치** (`RoguelikeChoiceSO`)
- 데미지 공식 상수 (`DamageCalculator.DefenseConstant` 등)
- 스폰 패턴 구성 (`SpawnWaveSO` / 패턴 풀)
- 보스/엘리트 스탯 배율

---

## 📝 기타 미정 (README TBD)

- 게임 타이틀(제목)
- 모바일 확장 여부
- ~~다국어 지원 여부~~ → **한국어/영어 2종으로 확정(2026-08-10)**. 언어를 늘리려면 `LanguageCode`에 항목을 더하고 `LocalizationTableSO.Entry`에 열을 추가한 뒤 `OptionPopupUI.LanguageOrder`에 버튼을 넣으면 된다
