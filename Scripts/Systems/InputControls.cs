namespace Assets.MyAssets.Scripts.Systems
{
    /// <summary>
    /// `.inputactions` 에셋을 가리키는 이름들의 유일한 출처.
    ///
    /// <see cref="InputManager"/>의 액션 캐싱과 아래 <see cref="Rebindable"/> 표가 같은 상수를 쓰므로
    /// 두 곳이 서로 어긋날 수 없다 — 에셋에서 액션 이름을 바꾸면 여기 한 곳만 고치면 된다.
    /// (과거엔 같은 경로 문자열이 양쪽에 따로 적혀 있어서, 한쪽만 갱신하면 키 설정 UI가 조용히 망가졌다.)
    ///
    /// 컴파일러가 에셋과 대조해주지는 않는다. 에셋에서 액션을 지우면 런타임에야 드러나므로,
    /// 캐싱은 `throwIfNotFound: true`로 시작 즉시 터뜨리고 리바인딩 경로는 로그를 남긴다.
    /// </summary>
    public static class InputControls
    {
        // ── 액션 맵 ──
        public const string BattleMap = "Battle";
        public const string MenuMap = "Menu";

        // ── 액션 경로("맵/액션") ──
        public const string BattleCyclePrev = "Battle/CyclePrev";
        public const string BattleCycleNext = "Battle/CycleNext";
        public const string BattleConfirm = "Battle/Confirm";

        public const string MenuNavPrev = "Menu/NavPrev";
        public const string MenuNavNext = "Menu/NavNext";
        public const string MenuSubmit = "Menu/Submit";
        public const string MenuPause = "Menu/Pause";

        /// <summary>
        /// 몬스터 정보 창(Tab) 토글. 전투 중에만 쓰지만 <b>Battle 맵이 아니라 Menu 맵</b>에 둔다 —
        /// Battle 맵은 <c>IsGameplayInputEnabled</c> 게이트를 타는데, 창을 열면서 그 게이트를 닫으므로
        /// Battle 맵에 두면 <b>같은 키로 다시 닫을 수 없다</b>. 퍼즈(ESC)를 Menu 맵에 둔 것과 같은 이유다.
        /// </summary>
        public const string MenuMonsterInfo = "Menu/MonsterInfo";

        public const string UiPoint = "UI/Point";
        public const string UiClick = "UI/Click";

        /// <summary>
        /// 리바인딩 UI가 나열할 논리 컨트롤.
        /// 방향키·확정은 Battle/Menu 두 맵에 같은 키로 존재하므로 하나로 묶어 함께 재설정한다
        /// (배열의 첫 번째가 대표 — 이 액션으로 키를 캡처하고 나머지에 같은 값을 복사한다).
        /// </summary>
        public static readonly RebindControl[] Rebindable =
        {
            new("ui.keybind.prev", new[] { BattleCyclePrev, MenuNavPrev }),
            new("ui.keybind.next", new[] { BattleCycleNext, MenuNavNext }),
            new("ui.keybind.confirm", new[] { BattleConfirm, MenuSubmit }),
            new("ui.keybind.pause", new[] { MenuPause }),
            new("ui.keybind.monsterInfo", new[] { MenuMonsterInfo }),
        };
    }

    /// <summary>리바인딩 UI가 다루는 논리 컨트롤 1종(표시 이름 키 + 함께 재설정할 액션들).</summary>
    public readonly struct RebindControl
    {
        /// <summary>화면에 보일 이름의 문자열 키. 번역은 표시하는 쪽(<c>KeybindListView</c>)이 한다.</summary>
        public readonly string Label;

        /// <summary>이 컨트롤이 함께 재설정할 액션들(첫 번째가 대표).</summary>
        public readonly string[] ActionPaths;

        public RebindControl(string label, string[] actionPaths)
        {
            Label = label;
            ActionPaths = actionPaths;
        }
    }
}
