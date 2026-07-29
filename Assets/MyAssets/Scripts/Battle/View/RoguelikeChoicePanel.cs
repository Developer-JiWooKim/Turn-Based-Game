using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Systems;
using Assets.MyAssets.Scripts.UI;
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
    public sealed class RoguelikeChoicePanel : BasePanelUI
    {
        // 방향키로 겨냥한 카드에 씌우는 클래스(마우스 :hover와 같은 스타일을 USS에서 정의).
        private const string HoverClass = "choice-card--active";

        protected override string RootElementName => "roguelike-panel";

        private Label _header;
        private List<Button> _cards;
        private int _cardCount;
        private int _hoveredIndex = -1; // 방향키 겨냥 카드(-1 = 아직 없음)
        private readonly PendingSignal<int> _pending = new();

        private void Awake()
        {
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
            SetHover(-1); // 방향키 겨냥 초기화(첫 방향키 입력 전까지 겨냥 없음)

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

            try
            {
                return await _pending.WaitAsync(ct);
            }
            finally
            {
                Hide(); // 취소(씬 종료 등)로 빠져나가도 패널이 열린 채 남지 않도록
            }
        }

        private void Update()
        {
            if (!_pending.IsWaiting || _cardCount == 0) return;

            InputManager input = InputManager.Instance;
            if (input == null) return;

            // 방향키: 카드 겨냥 이동(마우스 호버와 같은 하이라이트 + 이동음)
            if (input.UiNavigateNextPressed)
            {
                SetHover(_hoveredIndex < 0 ? 0 : (_hoveredIndex + 1) % _cardCount);
                AudioManager.UiNavigate();
            }
            else if (input.UiNavigatePrevPressed)
            {
                SetHover(_hoveredIndex < 0 ? _cardCount - 1 : (_hoveredIndex - 1 + _cardCount) % _cardCount);
                AudioManager.UiNavigate();
            }

            // Enter/Space: 겨냥한 카드 선택. 마우스 클릭은 UiClickSfx가 재생하므로 키보드 경로에서만 소리를 낸다.
            if (input.UiSubmitPressed && _hoveredIndex >= 0)
            {
                AudioManager.UiClick();
                OnPick(_hoveredIndex);
            }
        }

        /// <summary>방향키 겨냥 카드를 index로 옮기고 하이라이트 클래스를 갱신한다(-1이면 겨냥 없음).</summary>
        private void SetHover(int index)
        {
            _hoveredIndex = index;
            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].EnableInClassList(HoverClass, i == index);
            }
        }

        private void OnPick(int index)
        {
            if (!_pending.IsWaiting || index >= _cardCount) return;

            _pending.Complete(index);
        }

        public override void Hide()
        {
            SetHover(-1); // 닫을 때 겨냥 하이라이트 정리
            base.Hide();
        }
    }
}
