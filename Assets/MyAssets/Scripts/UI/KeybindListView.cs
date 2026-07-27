using System.Collections.Generic;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 키 설정 목록 UI. <see cref="InputManager"/>의 논리 컨트롤(이전/다음/확정/퍼즈)마다
    /// "이름 + 현재 키 버튼" 행을 만들고, 재설정 대기 표시와 라벨 갱신을 담당한다.
    ///
    /// 캡처·저장·초기화 같은 실제 동작은 전부 <see cref="InputManager"/>가 하고
    /// 여기서는 화면 표현만 맡는다. MonoBehaviour가 아니라서 패널이 필드로 들고 쓰면 된다
    /// (Unity 생명주기가 필요 없고, 컨테이너를 넘겨받아 그 안에서만 동작한다).
    /// </summary>
    public sealed class KeybindListView
    {
        // 현재 키를 표시하는 버튼들. 인덱스 = InputManager의 논리 컨트롤 인덱스
        private readonly List<Button> _keyButtons = new();

        /// <summary>
        /// 컨테이너에 키 설정 행을 채우고 재설정 버튼을 연결한다.
        /// <see cref="InputManager"/>가 없는 씬(테스트 등)에서는 빈 섹션이 남지 않도록 영역을 숨긴다.
        /// </summary>
        public void Build(VisualElement container, Button resetButton)
        {
            _keyButtons.Clear();

            InputManager input = InputManager.Instance;
            if (container == null || input == null)
            {
                if (container != null) container.style.display = DisplayStyle.None;
                if (resetButton != null) resetButton.style.display = DisplayStyle.None;
                return;
            }

            for (int i = 0; i < input.RebindControlCount; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("keybind-row");

                var nameLabel = new Label(input.GetRebindLabel(i));
                nameLabel.AddToClassList("keybind-name");

                var keyButton = new Button { text = input.GetRebindDisplay(i) };
                keyButton.AddToClassList("btn");
                keyButton.AddToClassList("keybind-key");

                int index = i; // 클로저 캡처 고정
                keyButton.clicked += () => BeginRebind(index);

                row.Add(nameLabel);
                row.Add(keyButton);
                container.Add(row);
                _keyButtons.Add(keyButton);
            }

            if (resetButton != null)
                resetButton.clicked += ResetBindings;
        }

        /// <summary>버튼을 눌러 다음 키 입력을 대기한다(InputManager가 캡처·저장까지 처리).</summary>
        private void BeginRebind(int index)
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            Button keyButton = _keyButtons[index];
            keyButton.text = "...";
            keyButton.AddToClassList("keybind-key--waiting");

            input.StartRebind(index, () =>
            {
                keyButton.RemoveFromClassList("keybind-key--waiting");
                RefreshLabels();
            });
        }

        private void ResetBindings()
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            input.ResetBindings();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            InputManager input = InputManager.Instance;
            if (input == null) return;

            for (int i = 0; i < _keyButtons.Count; i++)
                _keyButtons[i].text = input.GetRebindDisplay(i);
        }
    }
}