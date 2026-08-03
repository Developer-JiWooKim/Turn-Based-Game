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

## 배경

- 타이틀/캐릭터 선택: 실제 배경 이미지 사용(`Title_background.jpg` 밤하늘, `CharacterSelect-background.jpg`) — 그라디언트 배경은 쓰지 않음
- 팝업/오버레이: 반투명 다크 남색(`rgba(16, 27, 48, 0.85)` 전후)으로 화면을 덮은 뒤 그 위에 `.panel-frame` 카드를 띄움

## 아이콘 (미착수)

- Tabler outline 아이콘 등을 원형 배지 안에 배치하는 구조는 유지 예정(영입 카드/캐릭터 리스트 아이콘화, TODO 3-1/3-3)
- 배지 색은 골드가 아니라 **민트/바이올렛 계열**로 — CTA 성격이면 민트, 일반 정보면 바이올렛/크림

## 적용 화면 현황

- **버튼(Title/OptionPopup/PointAllocation/BattlePause/BattleResult)**: ✅ 신규 다크 민트/바이올렛 버튼 시스템 적용 완료
- **패널/팝업 배경**: ✅ 다크 바이올렛(`--color-panel`)으로 전환, 텍스트 색 반전 완료
- **배틀 HUD 턴 순서 칩**: ✅ 모서리·트랜지션만 버튼과 통일(색상 유지)
- **캐릭터 선택 nav-arrow·인디케이터 점·스탯 바 강조색**: ⬜ 아직 구 시안/네이비 톤 — 다음 폴리시 패스 대상
- **로그라이크 선택지 카드 아이콘화, 옵션 메뉴 해상도/언어 UI, 보스/엘리트 스킬 이펙트**: ⬜ 미착수 (TODO.md 참고)

## 참고 레퍼런스

- KayKit - Adventurers Character Pack (캐릭터 아트 스타일 기준, UI 톤과는 별개)
- "Endless Expedition" UI Toolkit 버튼 구현 가이드(민트/바이올렛 팔레트, 이중 테두리 래퍼 기법, `:disabled` 패턴의 출처) — 내용은 이 문서와 `Common.uss`에 흡수되어 별도 파일로는 보관하지 않음
