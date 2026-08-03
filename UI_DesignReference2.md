# Endless Expedition — UI Toolkit 구현 가이드

Unity **UI Toolkit** (UXML + USS)로 타이틀 화면 로고·버튼을 구현하는 방법.
대상: Unity 2022.3 LTS 이상 (`UnityEngine.UIElements`).

---

## 0. 파일 구조

```
Assets/
  UI/
    TitleScreen.uxml
    Styles/
      Theme.uss          // 변수 + 공통
      Buttons.uss        // 버튼 컴포넌트
      TitleScreen.uss    // 화면 전용 레이아웃
    Fonts/
      GrenzeGotisch-ExtraBold.ttf        → FontAsset
      BarlowCondensed-SemiBold.ttf       → FontAsset
    Textures/
      Background.png
      logo_endless_expedition.png        // 투명 배경 PNG (2571×852)
    TitleScreen.cs
```

폰트는 `Grenze Gotisch 800`, `Barlow Condensed 600` (Google Fonts, OFL).
임포트 후 **Create > Text > Font Asset** 으로 SDF 폰트 에셋 생성.

---

## 1. 디자인 토큰 (Theme.uss)

```css
:root {
    /* palette */
    --c-mint:        rgb(122, 240, 190);   /* #7AF0BE */
    --c-mint-bright: rgb(168, 255, 217);   /* #A8FFD9 */
    --c-mint-text:   rgb(234, 255, 247);   /* #EAFFF7 */
    --c-violet:      rgb(191, 174, 240);   /* #BFAEF0 */
    --c-violet-dim:  rgb(107, 95, 146);    /* #6B5F92 */
    --c-violet-text: rgb(222, 214, 245);   /* #DED6F5 */
    --c-danger:      rgb(224, 139, 139);   /* #E08B8B */
    --c-outline:     rgb(11, 7, 22);       /* #0B0716 다크 아웃라인 */
    --c-disabled-bd: rgb(58, 52, 80);
    --c-disabled-tx: rgb(106, 99, 131);

    /* surface gradients (USS엔 그라디언트 없음 → 중간색 사용) */
    --s-primary:     rgb(34, 55, 63);      /* #2B4A52 → #16262F 중간 */
    --s-primary-hov: rgb(43, 74, 84);
    --s-primary-prs: rgb(25, 45, 55);
    --s-secondary:   rgba(29, 22, 45, 0.88);
    --s-secondary-hov: rgba(40, 30, 62, 0.94);
    --s-danger:      rgb(85, 27, 41);

    /* metrics */
    --r:             5px;   /* border-radius */
    --bd:            2px;   /* 컬러 테두리 */
    --outline:       3px;   /* 다크 아웃라인 */
}
```

> **주의:** USS는 CSS 변수를 지원하지만 `var()`는 값 전체에만 쓸 수 있습니다.
> `border-color`처럼 4방향 속성엔 개별 지정하세요.

---

## 2. 이중 테두리를 만드는 법 (중요)

웹에서는 `border: 2px solid mint` + `box-shadow: 0 0 0 3px dark` 로 처리했지만
**USS에는 spread 방식 box-shadow가 없습니다.** 두 가지 중 하나를 씁니다.

**방법 A — 래퍼 엘리먼트 (권장, 픽셀 정확)**

```
<VisualElement class="btn-outline">      ← 다크 3px, radius 8px
    <VisualElement class="btn btn--primary">  ← 민트 2px, radius 5px
        <Label text="NEW EXPEDITION" />
    </VisualElement>
</VisualElement>
```

```css
.btn-outline {
    border-width: 3px;
    border-color: rgb(11, 7, 22);
    border-radius: 8px;
    align-self: center;
}
```

**방법 B — 9-slice 스프라이트**
테두리·내부 광원까지 한 장에 구운 PNG를 `background-image` + `-unity-slice-*`로.
셰이더 없이 발광까지 표현되므로 최종 프로덕션에는 이 쪽을 추천.

```css
.btn--primary {
    background-image: url("project://database/Assets/UI/Textures/btn_primary.png");
    -unity-slice-left: 24;  -unity-slice-right: 24;
    -unity-slice-top: 16;   -unity-slice-bottom: 16;
    -unity-slice-scale: 1;
}
```

---

## 3. Buttons.uss

```css
/* ── 공통 ───────────────────────────────── */
.btn {
    flex-direction: row;
    align-items: center;
    justify-content: center;
    border-radius: 5px;
    border-width: 2px;
    padding-left: 24px;
    padding-right: 24px;
    transition-property: background-color, border-color, scale;
    transition-duration: 0.12s;
}

.btn__label {
    -unity-font-definition: url("project://database/Assets/UI/Fonts/BarlowCondensed-SemiBold%20SDF.asset");
    -unity-font-style: normal;
    letter-spacing: 6px;          /* 웹 0.28em ≈ 24px * 0.28 */
    -unity-text-align: middle-center;
}

/* ── Primary (민트) ─────────────────────── */
.btn--primary {
    height: 62px;
    min-width: 300px;
    background-color: rgb(34, 55, 63);
    border-color: rgb(122, 240, 190);
}
.btn--primary .btn__label { font-size: 24px; color: rgb(234, 255, 247); }

.btn--primary:hover {
    background-color: rgb(48, 82, 93);
    border-color: rgb(168, 255, 217);
}
.btn--primary:active {
    background-color: rgb(25, 45, 55);
    translate: 0 2px;             /* 눌림 */
}

/* ── Secondary (바이올렛) ───────────────── */
.btn--secondary {
    height: 54px;
    min-width: 250px;
    background-color: rgba(29, 22, 45, 0.88);
    border-color: rgb(107, 95, 146);
}
.btn--secondary .btn__label { font-size: 21px; color: rgb(222, 214, 245); }
.btn--secondary:hover { border-color: rgb(191, 174, 240); background-color: rgba(40, 30, 62, 0.94); }

/* ── Compact (인게임) ───────────────────── */
.btn--compact        { height: 44px; padding-left: 26px; padding-right: 26px; }
.btn--compact .btn__label { font-size: 19px; letter-spacing: 4px; }

/* ── Danger ─────────────────────────────── */
.btn--danger { background-color: rgb(85, 27, 41); border-color: rgb(224, 139, 139); }
.btn--danger .btn__label { color: rgb(255, 222, 222); }

/* ── Disabled ───────────────────────────── */
.btn:disabled {
    background-color: rgba(20, 16, 29, 0.7);
    border-color: rgb(58, 52, 80);
    opacity: 1;                   /* 기본 0.5 페이드 끄기 */
}
.btn:disabled .btn__label { color: rgb(106, 99, 131); }

/* ── 마름모 장식 (프라이머리 좌우) ───────── */
.btn__gem {
    position: absolute;
    width: 8px; height: 8px;
    background-color: rgb(168, 255, 217);
    rotate: 45deg;
}
.btn__gem--left  { left: 14px; }
.btn__gem--right { right: 14px; }
```

**웹 → USS 치환표**

| 웹 CSS | UI Toolkit |
|---|---|
| `box-shadow: 0 0 0 3px` | 래퍼 엘리먼트 or 9-slice |
| `linear-gradient()` 배경 | 단색 + 그라디언트 텍스처 |
| `text-shadow` | `text-shadow: 0 2px 6px rgba(...)` (2022.2+ 지원) |
| `filter: blur()` | 미지원 → 블러 구운 PNG 레이어 |
| `background-clip: text` | 미지원 → 로고는 PNG로 |
| `letter-spacing: 0.28em` | `letter-spacing: <px>` (font-size × 0.28) |
| `transform: rotate(45deg)` | `rotate: 45deg` |
| `cursor: pointer` | `cursor: link` |

---

## 4. TitleScreen.uxml

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <Style src="project://database/Assets/UI/Styles/Theme.uss" />
  <Style src="project://database/Assets/UI/Styles/Buttons.uss" />
  <Style src="project://database/Assets/UI/Styles/TitleScreen.uss" />

  <ui:VisualElement class="screen">
    <ui:VisualElement class="screen__vignette" />

    <ui:VisualElement class="logo">
      <ui:Image class="logo__img" />
    </ui:VisualElement>

    <ui:VisualElement class="menu">
      <ui:VisualElement class="btn-outline">
        <ui:Button name="btn-new" class="btn btn--primary">
          <ui:VisualElement class="btn__gem btn__gem--left" />
          <ui:Label text="NEW EXPEDITION" class="btn__label" />
          <ui:VisualElement class="btn__gem btn__gem--right" />
        </ui:Button>
      </ui:VisualElement>

      <ui:VisualElement class="btn-outline">
        <ui:Button name="btn-continue" class="btn btn--secondary">
          <ui:Label text="CONTINUE" class="btn__label" />
        </ui:Button>
      </ui:VisualElement>

      <ui:VisualElement class="menu__row">
        <ui:VisualElement class="btn-outline">
          <ui:Button name="btn-options" class="btn btn--secondary btn--compact">
            <ui:Label text="OPTIONS" class="btn__label" />
          </ui:Button>
        </ui:VisualElement>
        <ui:VisualElement class="btn-outline">
          <ui:Button name="btn-quit" class="btn btn--secondary btn--compact">
            <ui:Label text="QUIT" class="btn__label" />
          </ui:Button>
        </ui:VisualElement>
      </ui:VisualElement>
    </ui:VisualElement>
  </ui:VisualElement>
</ui:UXML>
```

> `<ui:Button>` 안에 자식을 넣으면 기본 `text`는 비워두세요 (`text` + 자식 동시 사용 금지).

---

## 5. TitleScreen.uss

```css
.screen {
    flex-grow: 1;
    background-image: url("project://database/Assets/UI/Textures/Background.png");
    -unity-background-scale-mode: scale-and-crop;
    align-items: center;
    justify-content: space-between;
    padding-top: 44px;
    padding-bottom: 44px;
}

/* 상단 어둡게 — 로고 가독성 확보 */
.screen__vignette {
    position: absolute;
    left: 0; right: 0; top: 0; height: 45%;
    background-image: url("project://database/Assets/UI/Textures/vignette_top.png");
}

.logo__img {
    width: 857px;    /* 원본 2571 ÷ 3 */
    height: 284px;
    background-image: url("project://database/Assets/UI/Textures/logo_endless_expedition.png");
    -unity-background-scale-mode: scale-to-fit;
}

.menu       { align-items: center; }
.menu > *   { margin-bottom: 12px; }
.menu__row  { flex-direction: row; }
.menu__row > * { margin-left: 6px; margin-right: 6px; }
```

**해상도 대응** — PanelSettings에서
`Scale Mode: Scale With Screen Size`, `Reference Resolution: 1920×1080`,
`Screen Match Mode: Match Width Or Height`, `Match: 0.5`.

---

## 6. TitleScreen.cs

```csharp
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TitleScreen : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        root.Q<Button>("btn-new").clicked      += () => GameFlow.NewRun();
        root.Q<Button>("btn-continue").clicked += () => GameFlow.Continue();
        root.Q<Button>("btn-options").clicked  += () => Menus.Open(Menu.Options);
        root.Q<Button>("btn-quit").clicked     += Application.Quit;

        // 세이브 없으면 CONTINUE 비활성
        root.Q<Button>("btn-continue").SetEnabled(SaveSystem.HasSave);

        // 게임패드/키보드 진입 시 첫 버튼 포커스
        root.Q<Button>("btn-new").Focus();
    }
}
```

**포커스 스타일**(패드 필수):

```css
.btn:focus { border-color: rgb(168, 255, 217); }
```

---

## 7. 로고 처리 방침

로고 글자는 그라디언트 채움 + 블러 글로우 + 3중 스트로크라 **텍스트로 재현하지 말고 PNG**를 쓰세요.

- `logo_endless_expedition.png` (2571×852, 투명 배경) 임포트
- Texture Type: **Sprite (2D and UI)**, Alpha Is Transparency ✔, Mip Maps ✘
- Max Size 4096, Compression: **High Quality** (알파 그라디언트 밴딩 방지)
- 1920 기준 표시폭 약 857px

**발광 애니메이션을 직접 붙일 때** — 글로우만 분리한 PNG를 로고 뒤에 한 장 더 깔고
`opacity`를 `experimental.animation` 또는 USS transition으로 왕복시키면 됩니다.

```csharp
glow.experimental.animation
    .Start(0.25f, 0.5f, 1800, (e, v) => e.style.opacity = v)
    .Ease(Easing.InOutSine)
    .KeepAlive();
```

---

## 8. 체크리스트

- [ ] 폰트 SDF 에셋 2종 생성 (Grenze Gotisch 800 / Barlow Condensed 600)
- [ ] 로고 PNG + 배경 PNG + 상단 비네트 PNG 임포트
- [ ] Theme / Buttons / TitleScreen USS 3종 배치
- [ ] PanelSettings 1920×1080, Match 0.5
- [ ] 버튼 최소 히트 영역 44px 이상 유지
- [ ] 게임패드 포커스 링(`:focus`) 확인
- [ ] 1280×720 / 2560×1440 / 21:9에서 로고 잘림 확인
