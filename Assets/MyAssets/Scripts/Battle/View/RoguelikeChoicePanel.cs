using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 스테이지 클리어 후 성장 선택지를 제시하는 팝업(UI Toolkit). 카드 클릭을 await로 받아
    /// 선택한 SO를 반환한다(PlayerActionSelector와 동일한 입력-대기 패턴).
    /// </summary>
    public sealed class RoguelikeChoicePanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _root;
        private List<Button> _cards;
        private IReadOnlyList<RoguelikeChoiceSO> _current;
        private TaskCompletionSource<RoguelikeChoiceSO> _pending;

        private void Awake()
        {
            _root = _document.rootVisualElement.Q<VisualElement>("roguelike-panel");
            _cards = _document.rootVisualElement.Query<Button>(className: "choice-card").ToList();

            for (int i = 0; i < _cards.Count; i++)
            {
                int index = i; // 클로저 캡처 고정
                _cards[i].clicked += () => OnPick(index);
            }

            Hide();
        }

        /// <summary>선택지를 띄우고 플레이어가 하나 고를 때까지 기다린다.</summary>
        public async Task<RoguelikeChoiceSO> PresentAsync(IReadOnlyList<RoguelikeChoiceSO> choices, CancellationToken ct)
        {
            _current = choices;
            _pending = new TaskCompletionSource<RoguelikeChoiceSO>(TaskCreationOptions.RunContinuationsAsynchronously);

            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < choices.Count)
                {
                    _cards[i].style.display = DisplayStyle.Flex;
                    _cards[i].Q<Label>("card-title").text = choices[i].Title;
                    _cards[i].Q<Label>("card-desc").text = choices[i].Description;
                }
                else
                {
                    _cards[i].style.display = DisplayStyle.None;
                }
            }

            Show();
            using (ct.Register(() => _pending.TrySetCanceled(ct)))
            {
                RoguelikeChoiceSO picked = await _pending.Task;
                _pending = null;
                _current = null;
                Hide();
                return picked;
            }
        }

        private void OnPick(int index)
        {
            if (_pending == null || _current == null || index >= _current.Count) return;
            _pending.TrySetResult(_current[index]);
        }

        private void Show() => _root.style.display = DisplayStyle.Flex;
        private void Hide() => _root.style.display = DisplayStyle.None;
    }
}
