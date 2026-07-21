# 사운드 시스템 (BGM + SFX)

## Context

프로젝트에 오디오가 **완전히 백지 상태**다 — 오디오 파일 0개, AudioMixer 없음, `AudioSource`/`AudioClip` 관련 코드 0줄. `SaveData.OptionsData.MasterVolume` 필드만 존재하고 읽는 코드조차 없다.

이번 작업은 **재생 시스템과 훅을 먼저 구축**하고, 실제 사운드 파일은 이후 인스펙터에서 연결하는 방식으로 진행한다(오디오 파일 자체는 코드로 만들 수 없음). 따라서 모든 재생 경로는 클립이 비어 있어도 조용히 넘어가야 한다(null 가드 필수).

**확정된 설계 결정**
1. 볼륨 채널: **Master / BGM / SFX 3단 분리**
2. 제어 방식: **AudioSource 직접 제어** (AudioMixer 미사용 — `.mixer` 에셋은 에디터에서만 만들 수 있고, 단순 볼륨 조절엔 과함)
3. 옵션 UI(볼륨 슬라이더) **이번에 함께 구현**
4. 범위: BGM(타이틀/전투/보스) + 유닛 전투음 + UI 클릭음 + 승리/패배 스팅어 + 크리티컬

## 선결 과제: `Singleton<T>` 중복 인스턴스 버그

`Assets/MyAssets/Scripts/Singleton/Singleton.cs:13`의 조건이 반대로 되어 있다.

```csharp
if (Instance != null && Instance == this)   // ← Instance != this 여야 함
{
    Destroy(gameObject);
    return;
}
Instance = (T)this;
```

기존 인스턴스가 있어도 `Instance == this`는 false라 파괴되지 않고 `Instance`만 덮어쓴다. IntroScene → BattleScene → (전멸) → IntroScene 왕복마다 `DontDestroyOnLoad` GameManager가 하나씩 쌓인다. **AudioManager에서는 이 버그가 "BGM이 겹쳐서 여러 개 재생"으로 즉시 드러나므로** 먼저 고친다(`Instance != this`로 수정).

## 구현

### 1. 신규 폴더 `Assets/MyAssets/Scripts/Audio/`

**`AudioManager.cs`** — `Singleton<AudioManager>` 상속, `Awake`에서 `DontDestroyOnLoad`(GameManager와 동일 패턴)
- AudioSource 3개: BGM용 2개(크로스페이드 A/B 교대) + SFX용 1개
- `PlayBgm(AudioClip clip)` — **이미 같은 클립이 재생 중이면 무시**(스테이지마다 BGM이 처음부터 다시 시작되는 것을 방지). 다르면 `FadeScreenEffect`와 같은 `Awaitable` 프레임 루프로 크로스페이드
- `PlaySfx(AudioClip clip)` — 단일 AudioSource에 `PlayOneShot`. 이 방식은 동시 재생이 기본 지원되므로 별도 풀이 불필요하고, 카메라가 고정된 턴제 게임이라 2D 사운드로 충분하다
- 볼륨: `Master × Bgm`, `Master × Sfx`를 실제 AudioSource 볼륨에 반영. `SetMasterVolume`/`SetBgmVolume`/`SetSfxVolume` 제공(즉시 반영)
- `Awake`에서 `SaveService.Current.Options`를 읽어 초기 볼륨 적용
- `SceneManager.sceneLoaded` 구독 → 라이브러리에서 씬 이름에 대응하는 BGM 재생(`GameManager`를 건드리지 않고 씬 BGM이 자동 전환됨)
- ⚠️ **AudioListener를 붙이지 말 것** — 세 씬 모두 Main Camera에 이미 하나씩 있어 중복 경고가 난다

**`AudioLibrarySO.cs`** — 공용 클립 모음(하드코딩 금지 원칙에 따라 SO로)
- BGM: 씬 이름 ↔ 클립 매핑 배열, 전투 일반 BGM, 보스 BGM
- SFX: UI 클릭, 승리 스팅어, 패배 스팅어, 크리티컬

**`UnitSfxSO.cs`** — 유닛 1종의 전투음 묶음(공격/스킬/피격/사망/등장). 캐릭터·몬스터 프리팹별로 다른 에셋을 물린다

**`UiClickSfx.cs`** — UIDocument의 `rootVisualElement`에 `ClickEvent` 콜백을 걸어 버튼 클릭음을 재생하는 독립 컴포넌트
- 버튼 클릭 핸들러가 5개 파일에 11개 흩어져 있지만, UI Toolkit의 ClickEvent는 루트까지 버블링되므로 **기존 UI 코드를 한 줄도 수정하지 않고** 전부 커버된다
- ⚠️ `BasePanelUI.Start()`에 공통 코드를 넣는 방식은 쓰지 말 것 — 서브클래스 3종이 모두 `base.Start()`를 호출하지 않아 실행되지 않는다

### 2. 저장 — `Assets/MyAssets/Scripts/Save/SaveData.cs`

`OptionsData`에 `BgmVolume`/`SfxVolume`(기본 `1f`) 추가. JsonUtility는 JSON에 없는 필드에 초기값을 유지하므로 **기존 세이브와 그대로 호환**되며 `Version`을 올릴 필요가 없다.

### 3. 옵션 UI — `OptionPopup.uxml` / `OptionPopupUI.cs`

현재 "(준비 중)" 라벨뿐인 `Assets/MyAssets/UI/OptionPopup.uxml:10`을 슬라이더 3개(마스터/BGM/SFX)로 교체. 이 UXML은 여러 씬에서 `<ui:Template>`으로 재사용되므로 한 번 고치면 전 씬에 반영된다.

`OptionPopupUI.Start()`에서 각 슬라이더 초기값을 세이브에서 채우고 `RegisterValueChangedCallback`으로 `AudioManager`에 즉시 반영. **저장은 팝업을 닫을 때 1회**만(`Hide` 오버라이드) — 슬라이더를 드래그하는 내내 파일 I/O가 발생하는 것을 막는다.

### 4. 훅 연결

| 대상 | 위치 | 내용 |
|---|---|---|
| 유닛 전투음 | `Battle/View/UnitView.cs`의 `Play*Async` 5개 | `[SerializeField] UnitSfxSO _sfx` 추가 후 각 메서드 첫 줄에서 재생 |
| 크리티컬 | `Battle/View/BattlePresenter.cs`의 `anyCritical` 분기 | 기존 `CameraShake.Shake()` 옆 — 쉐이크와 자동 동기화 |
| 보스/일반 BGM | `Battle/View/BattleDirector.cs`의 `ResolveWave` 직후 | `wave.IsBossWave`로 이미 판정 가능. `PlayBgm`이 동일 클립을 무시하므로 매 스테이지 호출해도 안전 |
| 승리 스팅어 | `BattleDirector.BeginBattleAsync`의 `_run.CurrentStage++` 직전 | 스테이지 클리어 확정 지점 |
| 패배 스팅어 | `BattleDirector.HandleDefeatAsync` 시작부 | |
| UI 클릭음 | 각 씬 UIDocument 오브젝트에 `UiClickSfx` 부착 | 코드 수정 0줄 |

Core(`Battle/Core/*`)는 어느 훅에도 등장하지 않아 로직/연출 분리 원칙이 유지된다.

### 5. 에디터에서 해야 할 작업 (코드로 불가)

- `Assets/MyAssets/Audio/` 폴더에 실제 BGM/SFX 파일 추가
- **SO 에셋 생성**: 새 스크립트는 컴파일 후에야 GUID가 생기므로 `.asset` 파일을 미리 만들 수 없다. Unity에서 우클릭 → Create 메뉴로 `AudioLibrary` 1개와 유닛별 `UnitSfx` 에셋을 만들어 클립을 연결
- `AudioManager` 프리팹을 **IntroScene과 BattleScene 양쪽에** 배치(BattleScene 단독 실행 테스트를 위해). 수정된 Singleton이 중복을 파괴한다
- 각 씬 UIDocument에 `UiClickSfx` 부착, 캐릭터·몬스터 프리팹의 `UnitView`에 `UnitSfxSO` 연결

## 검증

Unity Editor 플레이 테스트(CLI 테스트 파이프라인 없음):

1. **클립 없이 먼저 실행** — 오디오 파일을 하나도 연결하지 않은 상태에서 전체 흐름(타이틀→선택→전투→전멸)이 예외 없이 돌아가는지. null 가드가 제대로 걸렸는지 확인하는 회귀 테스트
2. **싱글톤 중복** — IntroScene → BattleScene → 전멸 → IntroScene을 2~3회 왕복한 뒤 Hierarchy에서 `GameManager`/`AudioManager`가 각각 **1개만** 남아 있는지, BGM이 겹쳐 들리지 않는지
3. **BGM 전환** — 씬 전환 시 화면 페이드와 함께 자연스럽게 크로스페이드되는지, 스테이지가 넘어갈 때 같은 BGM이 처음부터 다시 시작되지 않는지, 보스 스테이지(`_bossStageInterval` 배수)에서 보스 BGM으로 바뀌는지
4. **볼륨** — 옵션 팝업에서 BGM/SFX 슬라이더를 각각 0으로 내렸을 때 해당 채널만 무음이 되는지, 팝업을 닫고 게임을 재시작해도 값이 유지되는지(`SaveData/save.json` 직접 확인 가능)
5. **SFX** — 공격/피격/사망/크리티컬/UI 클릭이 각각 의도한 타이밍에 나는지. 특히 공격음(휘두르기)과 피격음이 `BattlePresenter._impactDelay` 만큼 벌어져 들리는지
