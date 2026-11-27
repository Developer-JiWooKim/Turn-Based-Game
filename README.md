# 끝없는 원정 (Endless Expedition)

SPD 순서로 싸우는 턴제 배틀 로그라이크 무한 타워. Unity 6.5 (URP).

- **포트폴리오 페이지**: https://developer-jiwookim.github.io/Turn-Based-Game/
- **플레이 빌드 (Windows)**: [Releases](https://github.com/Developer-JiWooKim/Turn-Based-Game/releases/latest)

> 이 저장소에는 **직접 작성한 C# 스크립트와 어셈블리 정의(asmdef/asmref)만** 남겨 두었습니다.
> 씬·프리팹·에셋·프로젝트 설정은 포함하지 않으므로 Unity 프로젝트로 열 수 없습니다.

## 스크립트 구조

원래 프로젝트의 `Assets/MyAssets/Scripts/`를 루트로 옮겼습니다. 네임스페이스는 원래 경로 기준(`Assets.MyAssets.Scripts.*`)입니다.

```
Scripts/
├ Battle/
│ ├ Core/        Game.Core — 전투 규칙 (순수 C#, noEngineReferences: UnityEngine 참조 없음)
│ ├ Data/        Game.Data — 유닛·스킬·스폰·선택지 ScriptableObject 정의
│ └ View/        Game.View — 전투 연출, HUD, 타게팅, 스폰
│   └ Panels/    퍼즈·결과·몬스터 정보·로그라이크 선택지 패널
├ Progression/   Game.Progression — 런 상태(Run), 영구 저장(Save, JSON)
├ Systems/       Game.Systems — GameManager, 오디오, 입력·키 리바인딩, 설정
├ UI/            타이틀·캐릭터 선택·옵션 화면, 화면 상태 머신(States)   (asmref → Game.View)
├ Audio/         효과음 데이터(Data → Game.Data), UI 클릭음(View → Game.View)
└ Localization/  문자열 조회 진입점과 테이블 SO                      (asmref → Game.Data)
```

어셈블리 참조 방향: `Core` ← `Data` ← `Progression` ← `Systems` ← `View`. 전투 규칙(Core)은 다른 어셈블리와 Unity 엔진을 참조하지 않습니다.

### 먼저 보면 좋은 파일

| 주제 | 파일 |
|---|---|
| 한 스테이지 전투 진행 (순수 C#) | [`BattleSimulation.cs`](Scripts/Battle/Core/BattleSimulation.cs) |
| 비율 감소 데미지 계산 | [`DamageCalculator.cs`](Scripts/Battle/Core/DamageCalculator.cs) |
| 규칙 기반 몬스터 행동 선택 | [`MonsterAiSelector.cs`](Scripts/Battle/Core/MonsterAiSelector.cs) |
| 시뮬레이션 이벤트 → 연출·HUD 갱신 | [`BattlePresenter.cs`](Scripts/Battle/View/BattlePresenter.cs) |
| 행동 1회 연출 시퀀스 | [`ActionPlayback.cs`](Scripts/Battle/View/ActionPlayback.cs) |
| UI 화면 상태 머신 | [`GameFlowFSM.cs`](Scripts/UI/GameFlowFSM.cs) |

## 사용한 외부 에셋 (저장소 미포함)

- KayKit : Adventurers Character Pack 2.0 / Skeletons 1.1 / Character Animations 1.1 — Kay Lousberg ([kaylousberg.com](https://www.kaylousberg.com)), CC0 1.0
- [RPG Icons Pixel Art](https://assetstore.unity.com/packages/slug/186427) — Asset Store Standard EULA
- [Master Stylized Projectile](https://assetstore.unity.com/packages/slug/255608) — Asset Store Standard EULA
- [Casual_Hit](https://assetstore.unity.com/packages/slug/318002) — Asset Store Standard EULA
- [Toon Shaders Pro for URP](https://assetstore.unity.com/packages/slug/305845) — Asset Store Standard EULA
- [Health System For Dummies](https://assetstore.unity.com/packages/slug/215755) — Asset Store Standard EULA
- Platformer 8 Underworld (ithappy) — Asset Store Standard EULA

사운드 (v1.1부터)

- Techno Battle Themes (Brooklyn Game Audio) — BGM, Asset Store Standard EULA
- [Fantasy User Interface Sounds - Lite Edition](https://assetstore.unity.com/packages/slug/272919) — UI·전투 효과음, Asset Store Standard EULA
- [Punch Sound Pack](https://assetstore.unity.com/packages/slug/58260) (Gamemaster Audio) — 피격·사망음, Asset Store Standard EULA
- Combat Magic Spells — 마법 공격·몬스터 스킬, Asset Store Standard EULA

타이틀·캐릭터 선택 배경, 타이틀 로고, 캐릭터·몬스터 프로필 아이콘은 직접(AI 도구로) 제작했습니다.
