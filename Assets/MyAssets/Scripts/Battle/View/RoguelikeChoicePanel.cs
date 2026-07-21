using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>카드 한 장에 표시할 내용(선택지든 영입 후보든 동일한 형태로 제시된다).</summary>
    public readonly struct ChoiceCard
    {
        public readonly string Title;
        public readonly string Description;

        public ChoiceCard(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }

    /// <summary>
    /// 카드 중 하나를 고르게 하는 팝업(UI Toolkit). 카드 클릭을 await로 받아
    /// 고른 인덱스를 반환한다(PlayerActionSelector와 동일한 입력-대기 패턴).
    ///
    /// 성장 선택지와 동료 영입 후보가 같은 형태(제목 + 설명 카드)라 이 패널을 함께 쓴다.
    /// </summary>
    public sealed class RoguelikeChoicePanel : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _root;
        private Label _header;
        private List<Button> _cards;
        private int _cardCount;
        private TaskCompletionSource<int> _pending;

        private void Awake()
        {
            _root = _document.rootVisualElement.Q<VisualElement>("roguelike-panel");
            _header = _document.rootVisualElement.Q<Label>("panel-header");
            _cards = _document.rootVisualElement.Query<Button>(className: "choice-card").ToList();

            for (int i = 0; i < _cards.Count; i++)
            {
                int index = i; // 클로저 캡처 고정
                _cards[i].clicked += () => OnPick(index);
            }

            Hide();
        }

        /// <summary>성장 선택지를 제시하고 고른 SO를 반환한다(취소 시 null).</summary>
        public async Task<RoguelikeChoiceSO> PresentAsync(IReadOnlyList<RoguelikeChoiceSO> choices, CancellationToken ct)
        {
            var cards = choices.Select(c => new ChoiceCard(c.Title, c.Description)).ToList();
            int index = await PresentAsync("성장 선택", cards, ct);
            return index < 0 ? null : choices[index];
        }

        /// <summary>카드를 띄우고 플레이어가 하나 고를 때까지 기다린다. 고른 인덱스를 반환(취소 시 -1).</summary>
        public async Task<int> PresentAsync(string header, IReadOnlyList<ChoiceCard> cards, CancellationToken ct)
        {
            if (cards == null || cards.Count == 0)
                return -1;

            if (_header != null)
                _header.text = header;

            _cardCount = Mathf.Min(cards.Count, _cards.Count);
            _pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            for (int i = 0; i < _cards.Count; i++)
            {
                if (i < _cardCount)
                {
                    _cards[i].style.display = DisplayStyle.Flex;
                    _cards[i].Q<Label>("card-title").text = cards[i].Title;
                    _cards[i].Q<Label>("card-desc").text = cards[i].Description;
                }
                else
                {
                    _cards[i].style.display = DisplayStyle.None;
                }
            }

            Show();
            using (ct.Register(() => _pending.TrySetCanceled(ct)))
            {
                int picked = await _pending.Task;
                _pending = null;
                Hide();
                return picked;
            }
        }

        private void OnPick(int index)
        {
            if (_pending == null || index >= _cardCount) return;
            _pending.TrySetResult(index);
        }

        private void Show() => _root.style.display = DisplayStyle.Flex;
        private void Hide() => _root.style.display = DisplayStyle.None;
    }
}
