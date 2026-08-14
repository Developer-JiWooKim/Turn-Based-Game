using System.Collections.Generic;
using Assets.MyAssets.Scripts.Localization;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 키 설정 목록 UI.
    /// 
    /// <see cref="InputManager"/>의 논리 컨트롤(이전/다음/확정/퍼즈/정보 창)마다
    /// "이름 + 현재 키 버튼" 행을 만들고, 재설정 대기 표시와 라벨 갱신을 담당한다.
    ///
    /// 캡처·저장·초기화 같은 실제 동작은 전부 <see cref="InputManager"/>가 하고 여기서는 화면 표현만 맡는다. 
    /// </summary>
    public sealed class KeybindListView
    {
        /// <summary>현재 키를 표시하는 버튼들</summary>
        private readonly List<Button> _keyButtons = new();

        /// <summary>컨트롤 이름 라벨들. 언어가 바뀌면 다시 그려야 하므로 들고 있는다.</summary>
        private readonly List<Label> _nameLabels = new();

        private InputManager _input;

        /// <summary>
        /// 컨테이너에 키 설정 행을 채우고 재설정 버튼을 연결한다.
        /// </summary>
        public void Build(VisualElement container, Button resetButton)
        {
            _keyButtons.Clear();
            _nameLabels.Clear();
            _input = InputManager.Instance;

            if (container == null || _input == null)
            {
                if (container != null)
                {
                    container.style.display = DisplayStyle.None;
                }

                if (resetButton != null)
                {
                    resetButton.style.display = DisplayStyle.None;
                }

                return;
            }

            for (int i = 0; i < _input.RebindControlCount; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("keybind-row");

                // InputManager가 주는 것은 문자열 키다 — 번역은 표시하는 쪽인 여기서 한다.
                var nameLabel = new Label(Loc.Get(_input.GetRebindLabel(i)));
                nameLabel.AddToClassList("keybind-name");

                var keyButton = new Button { text = _input.GetRebindDisplay(i) };
                keyButton.AddToClassList("btn");
                keyButton.AddToClassList("keybind-key");

                int index = i; // 클로저 캡처 고정
                keyButton.clicked += () => BeginRebind(index);

                row.Add(nameLabel);
                row.Add(keyButton);
                container.Add(row);
                _keyButtons.Add(keyButton);
                _nameLabels.Add(nameLabel);
            }

            if (resetButton != null)
            {
                resetButton.clicked += ResetBindings;
            }
        }

        /// <summary>컨트롤 이름을 현재 언어로 다시 그린다(키 표시는 언어와 무관하므로 건드리지 않는다).</summary>
        public void RefreshNames()
        {
            for (int i = 0; i < _nameLabels.Count; i++)
            {
                _nameLabels[i].text = Loc.Get(_input.GetRebindLabel(i));
            }
        }

        /// <summary>버튼을 눌러 다음 키 입력을 대기한다(InputManager가 캡처·저장까지 처리).</summary>
        private void BeginRebind(int index)
        {
            Button keyButton = _keyButtons[index];
            keyButton.text = "...";
            keyButton.AddToClassList("keybind-key--waiting");

            _input.StartRebind(index, onFinished: () =>
            {
                keyButton.RemoveFromClassList("keybind-key--waiting");
                RefreshLabels();
            });
        }

        private void ResetBindings()
        {
            _input.ResetBindings();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            for (int i = 0; i < _keyButtons.Count; i++)
            {
                _keyButtons[i].text = _input.GetRebindDisplay(i);
            }
        }
    }
}