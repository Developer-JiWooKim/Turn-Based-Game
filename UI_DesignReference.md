# UI Design Reference

이 문서는 게임 UI의 비주얼 스타일 가이드입니다. 새 화면/컴포넌트를 만들 때 이 규칙을 기준으로 삼습니다.
실제 토큰 정의는 `Assets/MyAssets/UI/Common.uss`의 `:root`에 있으며, 이 문서는 그 값들의 의도와 사용 규칙을 설명합니다.

> 2026-08 개정: 기존 골드/나무 텍스처 카툰 톤을 걷어내고, 실제 타이틀 에셋(`Title_background.jpg` 밤하늘 배경 + `Title_Logo.png` 시안·바이올렛 네온 로고)과 어울리는 **다크 판타지 + 네온 민트/바이올렛** 톤으로 교체했다. 버튼 구조는 외부 참고 자료("Endless Expedition" UI Toolkit 가이드)에서 가져와 프로젝트 톤에 맞게 이식했고, 해당 참고 문서는 내용이 여기 흡수되어 더 이상 별도로 보관하지 않는다.

## 컨셉

**Dark Fantasy / Adventure / Neon Accent**

- 어두운 남색·바이올렛 바탕 위에 민트(주요 액션)·바이올렛(보조 액션) 네온 컬러로 포인트를 주는 톤
- 타이틀 로고·배경이 이미 이 톤(밤하늘 + 시안/바이올렛 네온)이었으므로, UI 전반을 여기 맞춰 통일
- 두꺼운 카툰 베벨 대신 **얇은 컬러 테두리 + 플랫한 모서리**(border-radius 6px 내외)로 정리된 느낌
- 캐릭터 에셋(KayKit Adventurers, 스타일라이즈드 로우폴리) 자체의 형태·애니메이션 톤은 그대로 유지 — UI 크롬(버튼/패널/HUD)만 다크 네온 톤

## 색상

토큰명은 전부 `Assets/MyAssets/UI/Common.uss`의 `--color-*` 변수와 1:1 대응.

| 용도 | 색상 | 토큰 |
|---|---|---|
| 아웃라인(공통 테두리·텍스트 스트로크) | 거의 검정 | `--color-outline` `#16202A` |
| 패널/팝업 배경 | 다크 바이올렛 카드 | `--color-panel` `#2A2440` |
| 배경 톤(캐릭터 선택 스탯바 트랙 등) | 심연 남색 | `--color-abyss` `#0A1220` |
| 밝은 텍스트(패널·카드 위 기본 텍스트) | 크림 | `--color-cream` `#EAF6FF` |
| **Primary 버튼** 면 / hover / press | 다크 청록 | `--color-surface-primary` `#22373F` / `-hover #2B4A54` / `-press #192D37` |
| **Primary 버튼** 테두리 / hover 테두리 | 민트 | `--color-mint` `#7AF0BE` / `--color-mint-bright` `#A8FFD9` |
| **Primary 버튼** 텍스트 | 옅은 민트 | `--color-mint-text` `#EAFFF7` |
| **Secondary 버튼** 면 / hover | 반투명 다크 바이올렛 | `--color-surface-secondary` `rgba(29,22,45,.88)` / `-hover rgba(40,30,62,.94)` |
| **Secondary 버튼** 테두리 / hover 테두리 | 바이올렛 | `--color-violet-dim` `#6B5F92` / `--color-violet` `#BFAEF0` |
| **Secondary 버튼** 텍스트 | 옅은 바이올렛 | `--color-violet-text` `#DED6F5` |
| **Danger 버튼**(배틀 중단 등) 면 / 테두리 / 텍스트 | 짙은 적갈 / 연분홍 / 연분홍 | `--color-surface-danger` `#551B29` / `--color-danger` `#E08B8B` / `--color-danger-text` `#FFDEDE` |
| **Disabled 버튼** 면 / 테두리 / 텍스트 | 무채색 다크 | `--color-surface-disabled` / `--color-disabled-border` `#3A3450` / `--color-disabled-text` `#6A6383` |
| 회복/방어 계열 (로그라이크 카테고리) | 초록 | `--color-heal` `#6FBF3F` |
| 영입/특수 계열 | 보라 | `--color-recruit` `#9A5FD4` |
| 정보/속도 계열 | 파랑 | `--color-info` `#3A7AC9` |

카테고리(버프/회복/디버프/영입 등)는 항상 같은 색을 사용해 의미를 고정합니다. (회복=초록, 영입=보라, 정보/속도=파랑, 위험=danger 버튼과 동일 계열)

> `--color-cyan-light`/`--color-cyan-mid`(시안 계열)와 `--color-deep`(구 네이비)는 캐릭터 선택 화면의 nav-arrow·인디케이터 점·스탯 바 등 **아직 새 팔레트로 옮기지 않은 잔여 요소**에 쓰인다. 버튼/패널 시스템과는 별개이니 그 부분을 만질 때 함께 정리할 것(`--color-cyan-mid`는 현재 실사용처 없음 — 정리 후보).
> 단, `--color-cyan-light`는 2026-08에 시너지 패널의 요구 인원 숫자(`.synergy-count`)에서 **의도적으로 재사용**했으므로 잔여 요소를 정리하더라도 토큰 자체는 남긴다.

## 텍스트 스타일

- 모든 주요 텍스트(타이틀, 버튼 라벨, 카드 제목)에 검은 계열 아웃라인(`-unity-text-outline-color: var(--color-outline)`) 적용
  - 버튼 라벨은 1~2px, 타이틀/카드 제목은 2~3px 정도로 크기에 비례해서 조정
- 폰트 굵기는 굵게(`-unity-font-style: bold`)
- 패널/카드 배경이 어두우므로 본문 텍스트는 기본적으로 `--color-cream`, 덜 중요한 캡션류는 `--color-violet-text`에 `opacity` 0.6~0.85 정도를 줘서 톤다운

## 버튼 구조 (`.btn` 계열, `Common.uss`)

기존의 "두꺼운 검은 테두리 + 하단 두꺼운 베벨" 방식을 걷어내고, 아래 구조로 통일했다.

### 공통 (`.btn`)
- 테두리 2px 단일(색은 variant가 결정), `border-radius: 6px`
- `transition-property: background-color, border-color, translate` — hover/press 시 부드럽게 전환
- press 시 `translate: 0 2px`로 살짝 눌리는 느낌만 주고, 별도 베벨 그림자는 없음
- variant 클래스를 안 붙이면(`.btn` 단독, 예: 성향 배분 +/- 버튼) 테두리·텍스트는 `--color-outline`/`--color-cream` 기본값 사용

### Primary (`.btn-cta`)
- 주요 액션(게임 시작, 전투 시작 등). 다크 청록 면 + 민트 테두리, hover 시 더 밝은 민트

### Secondary (`.btn-secondary`)
- 보조 액션(옵션/닫기/취소/계속하기 등). 반투명 다크 바이올렛 면 + 바이올렛 테두리

### Danger (`.btn-danger`)
- 되돌릴 수 없는 동작 전용(배틀 중단). 짙은 적갈 면 + 연분홍 테두리

### Disabled (`.btn:disabled`)
- `SetEnabled(false)`로 눌러도 반응 없는 버튼(성향 배분 +/- 등). 무채색 톤 + `opacity: 1`(기본 페이드 끔)

> USS는 CSS의 `box-shadow`/`linear-gradient`를 지원하지 않는다. 이중 테두리(색 테두리 + 어두운 외곽선)를 픽셀 정확하게 원하면 래퍼 `VisualElement`를 하나 더 두는 방법이 있으나, 지금은 단일 테두리로 근사하고 있다(구조 변경 없이 색만 바꿀 수 있도록 UXML은 건드리지 않음).

## 패널 / 팝업 프레임 (`.panel-frame`, `Common.uss`)

- 배경 `--color-panel`(다크 바이올렛), 테두리 `--color-outline` 5px
- 적용 대상: 옵션/성향 배분 팝업 프레임, 배틀 퍼즈·결과 박스, 로그라이크 선택지 카드, 캐릭터 선택 상단바·스탯 패널
- 배경이 어두운 만큼 그 위 텍스트는 반드시 `--color-cream` 계열로(위 "텍스트 스타일" 참고)

## 배틀 HUD 턴 순서 칩 (`.turn-chip`, `BattleHUD.uss`)

- 버튼과 같은 `border-radius: 6px` + `transition-property: border-color, scale`(0.12s)로 버튼 시스템과 모양만 통일
- 색상 자체(플레이어=파랑, 적=빨강, 현재 행동=시안 테두리 강조)는 그대로 유지 — 버튼 팔레트로 옮기지 않음
- 칩 사이 여백은 `margin: 6px`, 배경 알파는 secondary 버튼과 비슷한 수준(0.75~0.85)으로 살짝 낮춤

## 배틀 HUD 오버레이 3종 (`BattleHUD.uss`, 2026-08 추가)

3D 전장 위에 **패널 배경 없이 얹히는** 정보 표시라 `.panel-frame`을 쓰지 않는다. 대신 가독성은 그림자·투명도로 확보한다.

### 좌상단 시너지 패널 (`.synergy-panel`)
- 배경은 아주 옅게만(`rgba(42, 36, 64, 0.3)`) 깔고, 글자는 `text-shadow`로 전장 위에서 읽히게 한다
- 한 행 = `아이콘(22px) + 이름 + ×요구인원 + 효과 설명`. 이름은 `--color-cream`, 요구 인원은 `--color-cyan-light`, 효과 설명은 `--color-violet-text`(작게)
- **아직 인원이 모자란 시너지도 같은 목록에 흐리게 표시**한다(`.synergy-row--inactive`, `opacity: 0.2`). 발동 여부를 숫자로 쓰지 않고 **농담(濃淡)으로만** 구분하는 것이 이 패널의 규칙 — `×N`은 요구 인원이라 파티 구성이 바뀌어도 흔들리지 않는다

### 하단 파티 스탯 표기 (`.party-status` / `.party-member`)
- 막대가 아니라 텍스트다. 한 행이 `현재값 (+선택지) (+자동성장) (+시너지) (-디버프)` 형태
- 각 패널은 담당 캐릭터의 **화면 X 좌표에 맞춰 정렬**된다(`left`는 코드가, 가운데 맞춤은 USS `translate: -50% 0`이 담당)
- 사망한 파티원은 턴 순서 칩과 같은 처리(`opacity: 0.35`)
- ⚠️ **괄호 색은 USS 변수를 쓸 수 없다** — 리치 텍스트 태그라 `PartyStatusBarView`의 색 상수가 유일한 출처다. 선택지=`#7FB8FF`(파랑) / 자동성장=`#9FB3C8`(회색빛, 고른 게 아니라 흐릿한 톤) / 시너지=`#FF8A8A`(적) / 디버프 감소=`#FFB367`(주황). 이 네 색을 바꿀 때는 USS가 아니라 그 파일을 고친다

### 화면 배치 인덱스 (`A1` / `E2`)
- 체력바 왼쪽과 턴 순서 칩에 같은 라벨을 찍어 칩과 화면 위 유닛을 1:1로 맞춘다(A=아군/E=적군)
- 칩 색은 기존 `.turn-chip-player`/`.turn-chip-enemy`가 그대로 담당 — 라벨 도입으로 색 규칙은 바뀌지 않았다

## 배경

- 타이틀/캐릭터 선택: 실제 배경 이미지 사용(`Title_background.jpg` 밤하늘, `CharacterSelect-background.jpg`) — 그라디언트 배경은 쓰지 않음
- 팝업/오버레이: 반투명 다크 남색(`rgba(16, 27, 48, 0.85)` 전후)으로 화면을 덮은 뒤 그 위에 `.panel-frame` 카드를 띄움

## 아이콘 (2026-08 도입 완료)

에셋은 전부 `Assets/MyAssets/Textures/Icons/` 아래 세 폴더로 나뉜다.

| 폴더 | 용도 | 쓰이는 곳 |
|---|---|---|
| `CharacterProfile/` | 캐릭터 6종 초상 | 캐릭터 선택 화면(`character-icon`), 영입 후보 카드(`card-icon`) — 둘 다 `UnitStatsSO.Icon` 재사용 |
| `Buff/` | 파티 강화 계열 | 로그라이크 선택지 카드(`RoguelikeChoiceSO.Icon`), 시너지 패널 행 |
| `Debuff/` | 상태이상·몬스터 약화 계열 | 체력바 상태이상/스폰 디버프 표기, 몬스터 디버프 선택지 카드(선택지와 상태이상이 **같은 아이콘을 공유**) |

- UI Toolkit(카드·시너지 패널)은 `background-image`로 그린다: `card-icon` 56px, `synergy-icon` 22px, 둘 다 `-unity-background-scale-mode: scale-to-fit`. 아이콘이 없는 항목은 코드가 요소를 숨긴다
- **월드스페이스 체력바(uGUI/TMP)는 인라인 스프라이트 태그**로 그린다 — `Debuff/SpriteAssets/`의 TMP Sprite Asset을 거치며, 자세한 세팅과 함정은 `CLAUDE.md`의 체력바 아이콘 항목 참고
- 배지 색은 골드가 아니라 **민트/바이올렛 계열** 기준 — CTA 성격이면 민트, 일반 정보면 바이올렛/크림(원형 배지 배경은 아직 도입 안 함, 아이콘만 단독 배치)

## 적용 화면 현황

- **버튼(Title/OptionPopup/PointAllocation/BattlePause/BattleResult)**: ✅ 신규 다크 민트/바이올렛 버튼 시스템 적용 완료
- **패널/팝업 배경**: ✅ 다크 바이올렛(`--color-panel`)으로 전환, 텍스트 색 반전 완료
- **배틀 HUD 턴 순서 칩**: ✅ 모서리·트랜지션만 버튼과 통일(색상 유지) + 배치 인덱스 라벨(`A1`/`E2`) 적용
- **배틀 HUD 오버레이(시너지 패널 / 하단 파티 스탯 / 데미지 팝업)**: ✅ 2026-08 신규 — 위 "배틀 HUD 오버레이 3종" 참고
- **아이콘(캐릭터 초상 / 로그라이크 선택지 / 상태이상)**: ✅ 2026-08 도입 완료
- **캐릭터 선택 nav-arrow·인디케이터 점·스탯 바 강조색**: ⬜ 아직 구 시안/네이비 톤 — 다음 폴리시 패스 대상
- **버튼 크기**: ⬜ 레퍼런스 대비 작음 — 색은 유지하고 크기만 키우는 방향(TODO 3-5)
- **전투 연출(타격 파티클·투사체·히트 스톱·몬스터 스킬 연출)**: ✅ 2026-08-06 완료 — 화면 UI가 아니라 3D 월드 연출이라 이 문서의 대상은 아니지만, HUD 위로 겹치는 요소(데미지 팝업)와 톤을 맞출 것
- **옵션 메뉴 해상도/언어 UI**: ⬜ 미착수 (TODO.md 참고)

## 참고 레퍼런스

- KayKit - Adventurers Character Pack (캐릭터 아트 스타일 기준, UI 톤과는 별개)
- "Endless Expedition" UI Toolkit 버튼 구현 가이드(민트/바이올렛 팔레트, 이중 테두리 래퍼 기법, `:disabled` 패턴의 출처) — 내용은 이 문서와 `Common.uss`에 흡수되어 별도 파일로는 보관하지 않음
