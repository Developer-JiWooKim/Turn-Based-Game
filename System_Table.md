# 데이터 테이블 정리

프로젝트에서 **표(테이블) 형태로 관리해야 할 데이터**의 목록과, 2026-08-10 기준 현재 값입니다.
표를 실제로 만드는 작업(스프레드시트/에디터 툴)은 이 문서를 원본으로 삼으면 됩니다.

- 계산식과 밸런싱 기준은 `SystemFormulaBalance.md`, 코드 규칙은 `CLAUDE.md`, 남은 작업은 `TODO.md`, 끝난 작업의 기록은 `Completed.md` 참고
- 값의 **진짜 원본은 항상 SO 에셋/인스펙터**입니다. 이 문서는 스냅샷이며, 표를 만든 뒤에도 에셋이 정본입니다

## 표기 규칙 (모든 표 공통)

- **정수 스탯**(HP/ATK/SPD/DEF)은 2026-08 기준 10배 리스케일된 값입니다. 캐릭터 HP 800~1200, ATK 180~350 규모
- **비율 스탯**(치명타/치명피해/저항/성장률/스킬 배율/상태이상 크기)은 리스케일 대상이 아닙니다. `0.3` = 30%
- **배율**은 1.0이 "변화 없음"입니다. `0.7` = 30% 감소
- `(없음)` = 에셋에 키 자체가 없어 코드 기본값이 적용되는 항목. `(EMPTY)` = 슬롯은 있으나 미할당
- 에셋 파일명과 표시 이름(`_displayName`)이 다른 경우가 있어, 표에는 **둘 다** 넣는 것을 권장합니다

---

# A. 표 목록

## A-1. 지금 데이터가 있는 표 (8종)

1. 캐릭터 기본 스탯 — 6행
2. 캐릭터 파티 시너지 — 6행
3. 몬스터 기본 스탯 — 4행
4. 스킬 — 3행
5. 상태이상 종류 정의 — 5행 (코드 사양)
6. 로그라이크 선택지 — 9행
7. 스테이지 스케일링 — 1행(파라미터 6종 + 토글)
8. UI 문자열(한/영) — 79행 ※ 이미 SO 에셋으로 존재

---

# B. 표별 상세

## 1. 캐릭터 기본 스탯

- **1행 = 플레이어 캐릭터 1종**
- **키**: 에셋 파일명 (`CharacterStatsSO`)
- **출처**: `Assets/MyAssets/ScriptableObjects/Character/*.asset` (베이스 필드는 `UnitStatsSO`)
- **로스터 순서**: `CharacterRosterSO.asset`의 배열 순서 = 캐릭터 선택 화면 순환 순서

**열**

- `RosterOrder` — int, 1~6, 선택 화면 순서 (로스터 SO 배열 인덱스+1)
- `AssetName` — string, 에셋 파일명
- `DisplayName` — string, 화면 표시 이름 (`_displayName`)
- `Prefab` — 참조, `Prefabs/Character/*.prefab`
- `Icon` — 참조, 캐릭터 아이콘 스프라이트
- `MaxHp` — int, 최대 HP
- `Atk` — int, 공격력
- `Spd` — int, 속도 (턴 순서 결정에만 사용)
- `Def` — int, 방어력 (감쇠식 `K/(K+DEF)`, K=1000)
- `CritRate` — float, 0~1, 치명타 확률
- `CritDmg` — float, 치명타 배율 (1.5 = 150%)
- `Res` — float, 0~1, 상태이상 저항 (1.0이면 완전 면역)

**현재 값**

```csv
RosterOrder,AssetName,DisplayName,Prefab,MaxHp,Atk,Spd,Def,CritRate,CritDmg,Res
1,CH_KnightSO,Knight,Knight.prefab,1000,200,100,150,0.25,1.5,0.2
2,CH_BarbarianSO,Barbarian,Barbarian.prefab,1200,270,80,40,0.3,2.5,0
3,CH_MageSO,Mage,Mage.prefab,800,350,30,10,0.3,3,0
4,CH_Rogue_CrossbowSO,Rogue(Crossbow),Rogue_Crossbow.prefab,1000,220,150,60,0.62,3,0.1
5,CH_Rogue_DaggerSO,Rogue(Dagger),Rogue_Dagger.prefab,1000,180,150,80,0.5,2.5,0.1
6,CH_RangerSO,Ranger,Ranger.prefab,900,300,100,30,0.4,2.1,0
```

**주의**

- 2026-08-10에 에셋 이름이 `CH_`(캐릭터)/`MO_`(몬스터) 접두어로 정리되어 캐릭터·몬스터 동명 문제(`MageSO`)가 사라졌습니다. GUID는 그대로라 참조도 유지됩니다
- 선택 화면 스탯 바의 최댓값은 이 표에서 파생됩니다(`CharacterRosterSO.CreateStatCeiling`) — 새 캐릭터가 최댓값을 갱신하면 기존 캐릭터의 바 길이가 전부 짧아집니다

---

## 3. 몬스터 기본 스탯

- **1행 = 몬스터 1종**
- **출처**: `ScriptableObjects/Monster/*.asset` (`MonsterStatsSO`)

**열**

- `AssetName` / `DisplayName` / `Prefab` — 1번 표와 동일
- `Tier` — enum, `Normal(0)` / `Elite(1)` / `Boss(2)`
- `MaxHp` `Atk` `Spd` `Def` `CritRate` `CritDmg` `Res` — 1번 표와 동일
- `Skill` — 참조, `SkillSO` (비우면 일반 공격만)

**현재 값**

```csv
AssetName,DisplayName,Tier,MaxHp,Atk,Spd,Def,CritRate,CritDmg,Res,Skill
MO_MinionSO,Skeleton Minion,Normal,500,100,70,80,0.1,1.5,0,(없음)
MO_MageSO,Skeleton Mage,Elite,800,200,40,20,0.15,3,0,Skill_SkeletonMage
MO_RogueSO,Skeleton Rogue,Elite,800,170,130,50,0.3,2,0.1,Skill_SkeletonRogue
MO_WarriorSO,Skeleton Warrior,Boss,1500,230,100,200,0.1,3,0.3,Skill_SkeletonWarrior
```

**주의**

- `Tier`는 **보스 웨이브 판정(보스 BGM)에만** 쓰입니다. AI의 스킬 사용 판단은 Tier가 아니라 스킬 유무를 봅니다 — Elite만 있는 웨이브는 스킬을 써도 보스 웨이브가 아닙니다
- `MO_MinionSO.asset`에는 옛 필드(`_hasSkill`/`_skillCooldown`/`_skillScope`/`_skillPowerMultiplier`)가 남아 있을 수 있습니다. 현재 코드는 읽지 않으며, 표에 옮기지 마세요
- ~~`Skill_SkeltonWarrior` 오타~~ → 2026-08-10에 `Skill_SkeletonWarrior`로 수정 완료(GUID 유지)

---

## 4. 스킬

- **1행 = 스킬 1종.** 유닛에 묶이지 않은 독립 에셋이라 여러 유닛이 공유 가능
- **출처**: `ScriptableObjects/Skill/*.asset` (`SkillSO`)

**열**

- `AssetName` — string
- `DisplayName` — string, UI/로그 표기용 (전투 규칙 무관)
- `Cooldown` — int, 재사용까지 대기 턴 (0이면 매 턴)
- `Scope` — enum, `Single(0)` = 단일 대상 / `Line(1)` = 적 진영 전체
- `PowerMultiplier` — float, 일반 공격 대비 데미지 배율
- `StatusKind` — enum, 5번 표 참조
- `StatusDuration` — int, 지속 턴 (0이면 상태이상 없음)
- `StatusMagnitude` — float, 종류별 의미 상이 (5번 표 참조)
- `StatusApplyChance` — float, 0~1, **저항 적용 전** 기본 확률. 최종 = 이 값 × (1 − 대상 RES)

**현재 값**

```csv
AssetName,DisplayName,Cooldown,Scope,PowerMultiplier,StatusKind,StatusDuration,StatusMagnitude,StatusApplyChance
Skill_SkeletonMage,스킬,5,Single,1.4,DefDown,2,0.5,0.7
Skill_SkeletonRogue,스킬,5,Single,1.1,Poison,3,0.05,0.8
Skill_SkeletonWarrior,스킬,3,Line,1.8,Stun,1,0,0.35
```

**주의**

- `DisplayName`이 3종 모두 `"스킬"`입니다 — 표를 만들 때 채워 넣기 좋은 항목입니다
- `Duration`이나 `ApplyChance` 중 하나라도 0이면 상태이상이 **통째로 없는 것**으로 처리됩니다(`StatusEffect.IsValid`)
- 현재 캐릭터는 스킬을 쓰지 않지만 `CharacterStatsSO`가 같은 타입을 참조하면 코드 변경 없이 동작합니다 → 17번 표

---

## 5. 상태이상 종류 정의 (사양표)

- **1행 = `StatusKind` 1종.** 데이터가 아니라 **코드가 정한 사양**이며, 스킬 표(4번)를 채울 때의 참조표입니다
- **출처**: `Scripts/Battle/Core/StatusEffect.cs`

**열**

- `Kind` — enum 이름
- `EnumValue` — int, 에셋 YAML에 저장되는 값
- `Magnitude 의미` — string
- `처리 시점` — string
- `Icon` — 13번 표 참조

**현재 값**

```csv
Kind,EnumValue,Magnitude 의미,처리 시점
Stun,0,사용 안 함,자기 차례 시작 시 행동 통째로 스킵
Poison,1,최대 HP 대비 비율(0.05 = 5%),자기 차례 시작 시 도트 피해
AtkDown,2,ATK 감소 비율(0.3 = 30% 감소),피해 계산 시 유효 스탯으로 반영
DefDown,3,DEF 감소 비율,피해 계산 시 유효 스탯으로 반영
SpdDown,4,SPD 감소 비율,턴 순서 정렬 시 유효 스탯으로 반영
```

**주의**

- 처리 순서는 **도트 피해 → 지속 턴 감소 → 기절 판정** 고정입니다
- 상태이상은 **전투(스테이지) 단위**로 초기화됩니다. 스테이지를 넘기면 사라집니다
- 스탯 감소형은 `Stats`를 직접 고치지 않고 조회 시점에 반영됩니다(파티 시너지와 필드가 충돌하지 않도록)

---

## 6. 로그라이크 선택지

- **1행 = 선택지 1종.** 승리 시 이 풀에서 3개를 가중치 추첨해 제시
- **출처**: `ScriptableObjects/RoguelikeChoice/*.asset` (`RoguelikeChoiceSO`)
- 같은 9종이 **영구 포인트 배분 화면의 카테고리 목록**으로도 재사용됩니다

**열**

- `AssetName` — string
- `Title` — string, 카드 제목
- `Description` — string, 카드 본문
- `Category` — enum, `AttackUp(0) SpeedUp(1) DefensiveUp(2) CritUp(3) Heal(4) EnemyStun(5) EnemyHpDown(6) EnemyAtkDown(7) Recruit(8)`
- `Icon` — 참조
- `HpFlat` `AtkFlat` `SpdFlat` `DefFlat` — int, 최대 스탯 **영구 증가**(런 종료까지)
- `HealFlat` — int, 즉시 회복량 (최대 HP 증가가 아님)
- `ResFlat` — float, 0~1
- `CritRateFlat` — float, 0~1
- `CritDmgFlat` — float
- `EnemyHpMul` — float, 0.1~1, 다음 스테이지 몬스터 최대 HP 배율
- `EnemyAtkMul` — float, 0.1~1, 다음 스테이지 몬스터 ATK 배율
- `EnemySkipFirstTurn` — bool, 다음 스테이지 첫 턴 몬스터 전체 행동 불가(1턴 기절 확정 부여)
- `Weight` — float, 기본 등장 가중치
- `WeightPerEmptySlot` — float, 파티 빈자리 1개당 추가 가중치 (영입 전용)

**현재 값**

```csv
AssetName,Title,Category,HpFlat,AtkFlat,SpdFlat,DefFlat,HealFlat,ResFlat,CritRateFlat,CritDmgFlat,EnemyHpMul,EnemyAtkMul,EnemySkipFirstTurn,Weight,WeightPerEmptySlot
AttackUp,Attack Up,AttackUp,50,120,0,0,0,0,0,0,1,1,FALSE,1,0
SpeedUp,Speed Up,SpeedUp,100,50,50,0,0,0,0,0,1(없음),1(없음),FALSE(없음),1(없음),0(없음)
DefensiveUp,Defensive Up,DefensiveUp,60,35,0,80,0,0.1,0,0,1,1,FALSE,1,0
CritUp,Crit Up,CritUp,50,0,0,0,0,0,0.08,0.25,1,1,FALSE,1,0
Heal,Heal,Heal,0,0,0,0,1000,0,0,0,1,1,FALSE,1,0
EnemyStun,Stun,EnemyStun,0,0,0,0,0,0,0,0,1,1,TRUE,1(없음),0(없음)
EnemyHpDown,Weaken,EnemyHpDown,0,0,0,0,0,0,0,0,0.7,1,FALSE,1(없음),0(없음)
EnemyAtkDown,Disarm,EnemyAtkDown,0,0,0,0,0,0,0,0,1,0.7,FALSE,1(없음),0(없음)
Recruit,Recruit,Recruit,0,0,0,0,0,0,0,0,1,1,FALSE,1,4
```


```csv
AssetName,현재 Description,실제 효과
AttackUp,"HP +5 / ATK +12",HP +50 / ATK +120
SpeedUp,"HP +5 / SPD +5",HP +50 / SPD +50
DefensiveUp,"HP +15 / DEF +8 / RES +0.1",HP +150 / DEF +80 / RES +0.1
CritUp,"HP +5 / Crit Rate +0.08 / Crit Dmg +0.4",HP +50 (나머지는 일치)
Heal,"Restore 100 HP",1000 회복
```

**주의**

- 설명문은 수치를 손으로 적어둔 것이라 **값을 바꿔도 자동으로 따라오지 않습니다.** 표에서 설명을 수치로부터 생성하는 열(수식)을 두면 이런 어긋남이 재발하지 않습니다
- 영입 선택지는 파티가 꽉 차도 계속 등장하며, 고르면 교체 대상을 플레이어가 선택합니다. `WeightPerEmptySlot`은 "빈자리가 많을수록 자주 뜨게" 할 뿐입니다
- 최종 가중치 = `Weight + WeightPerEmptySlot × 빈자리` + `투자 포인트 × _weightPerPoint`(9번 표)
- **2026-08-10 1차 밸런싱**으로 SpeedUp·DefensiveUp·CritUp 수치가 바뀌었습니다(위 CSV는 반영본). 근거와 조정 전후 비교는 `BalanceCurve.md`의 D-0절 참고

---

## 7. 스테이지 스케일링

- **1행짜리 파라미터 표** (`StageScalingSO.asset` 1개)
- 표로 만든다면 "파라미터 이름 / 값 / 대상 / 적용 방식" 세로 표가 읽기 좋습니다

**현재 값**

```csv
Param,Value,대상,적용 방식
PlayerHpRate,0.05,플레이어,기준 스탯 대비 스테이지당 flat 증가(선형 누적)
PlayerAtkRate,0.04,플레이어,동일
PlayerDefRate,0.03,플레이어,동일
MonsterHpRate,0.08,몬스터,스폰 시 배율(복리)
MonsterAtkRate,0.06,몬스터,스폰 시 배율(복리)
MonsterDefRate,0.03,몬스터,스폰 시 배율(복리)
MonsterCompound,TRUE,몬스터,복리(ON) / 선형(OFF)
AccelStartStage,30,몬스터,이 스테이지까지는 위 성장률 그대로
AccelMultiplier,1.5,몬스터,가속 구간 성장률 배수(HP 8%→12% 등). 올리면 벽이 앞당겨짐
MonsterSpdRate,0.03,몬스터,스테이지당 SPD 증가율(가속 미적용)
SpdStartStage,5,몬스터,이 스테이지 이후부터 SPD가 오름
BossSpdRate,0.08,플레이어,보스 1회 처치당 기준 SPD 대비 증가(몬스터 증가분보다 작게 유지할 것)
```

**주의**

- 플레이어는 HP를 이어받으므로 배율이 아니라 **누적 총량의 차분**으로 증가합니다(반올림 오차 누적 방지)
- SPD·치명타·저항은 스케일링 대상이 아닙니다 — 로그라이크 선택지로만 성장합니다
- 몬스터 성장률을 플레이어보다 높게 두는 것이 전제입니다(플레이어는 선택지로 원하는 스탯을 몰아 올릴 수 있으므로)
- 영입 캐릭터 소급 성장(`ApplyCatchUp`)이 **플레이어 성장률과 같은 함수**를 씁니다 — 한쪽만 바꿀 수 없습니다

---

## 8. UI 문자열 (한/영)

- **1행 = 화면에 나오는 문구 1종**
- **출처**: `ScriptableObjects/Localization/UiStringTable.asset` (`LocalizationTableSO`) — 이미 79행이 들어 있어 새로 만들 필요는 없고, 늘릴 때 참고용입니다
- **할당처**: 두 씬의 `GameManager._stringTable`

**열**

- `Key` — string, 아래 규약 참고
- `Ko` — string, 한국어
- `En` — string, 영어 (비우면 한국어로 물러섭니다)

**키 규약 3종** (출처가 달라 나뉜 것이며, 표에 행이 없으면 원문/키가 그대로 나옵니다)

```csv
접두어,대상,키 예시,비고
ui.,UI 문구(UXML text/label + 코드),ui.pause.resume,BasePanelUI가 이 접두어로 "번역 대상"과 "코드가 채우는 값"을 구분 — 빼면 조용히 번역 안 됨
choice.,로그라이크 선택지,choice.AttackUp.title,본문이 여러 줄이라 카테고리 enum에서 키를 만든다
(없음),유닛 표시 이름,Knight,에셋의 _displayName 원문이 곧 키
```

**주의**

- 서식이 있는 문구는 `{0}` 자리를 씁니다(`ui.allocation.points` = `보유 {0} / 총 {1}`) — 번역할 때 자리 개수를 맞춰야 합니다
- 언어를 늘리려면 `LanguageCode`에 항목 + `Entry`에 열 + `OptionPopupUI.LanguageOrder`에 버튼을 추가합니다
- **6번 표의 `Description` 어긋남은 이 표가 해결했습니다** — 카드에 실제로 표시되는 문구는 이제 `choice.*.desc` 행이고 올바른 수치(HP +50 / ATK +120 등)로 적혀 있습니다. 에셋의 `_description`은 폴백으로만 남아 있어 여전히 옛 수치이므로, **에셋 쪽 값을 표의 근거로 삼지 마세요**

---

# D. 표를 만들 때 체크리스트

1. ~~**키는 폴더 포함 경로로.**~~ → 2026-08-10 `CH_`/`MO_` 접두어 정리로 파일명만으로 충분해졌습니다
2. **설명문을 수치에서 생성하세요.** 6번 표의 에셋 `Description`이 리스케일 후 갱신되지 않아 어긋나 있습니다(화면에 실제로 뜨는 문구는 8번 표가 대신하고 있지만, 수치를 또 바꾸면 8번 표를 손으로 고쳐야 하는 것은 같습니다)
3. **파생값을 열로 저장하지 마세요.** `IsBossWave`(몬스터 Tier에서 파생), 영구 포인트 획득량(`BestStage`에서 파생)은 계산 열로 두어야 어긋나지 않습니다
4. **정수 스탯과 비율 스탯을 한 열에 섞지 마세요.** 리스케일 시 대상이 갈립니다
5. **시너지의 `%`와 `%p`를 구분 표기하세요.** 정수 스탯은 비율, 비율 스탯은 가산입니다
6. **에셋에 키가 없는 항목**(`(없음)` 표기)은 코드 기본값이 적용된 상태입니다. 표에는 실제 적용값을 적고, 에디터에서 한 번 저장해 키를 생성해 두면 이후 혼동이 없습니다
7. 표를 SO로 되돌릴 계획이라면 **열 이름을 `_` 없는 필드명**으로 맞춰두면 임포터 작성이 쉬워집니다 (`_maxHp` → `MaxHp`)