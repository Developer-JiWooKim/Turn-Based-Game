using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 캐릭터 선택 패널 뷰. 이 패널이 보이는 동안에만 3D 프리뷰 리그를 켜고,
    /// 프리뷰 카메라의 RenderTexture를 프리뷰 영역의 background-image로 배선한다.
    /// </summary>
    public class CharacterSelectPanelUI : BasePanelUI
    {
        [Header("3D 캐릭터 프리뷰 → RenderTexture")]
        [Tooltip("프리뷰 카메라 + 캐릭터 모델 + 조명을 묶은 루트. 이 패널이 보일 때만 활성화된다.")]
        [SerializeField] private GameObject _previewRig;
        [Tooltip("프리뷰 카메라의 Target Texture. 프리뷰 영역의 background-image로 배선된다.")]
        [SerializeField] private RenderTexture _previewTexture;

        public event Action OnBackClicked;
        public event Action OnBattleClicked;

        protected override void Start()
        {
            base.Start();

            if (_previewTexture != null)
            {
                Root.Q<VisualElement>("character-preview").style.backgroundImage =
                    Background.FromRenderTexture(_previewTexture);
            }

            Root.Q<Button>("select-back-button").clicked += () => OnBackClicked?.Invoke();
            Root.Q<Button>("battle-button").clicked += () => OnBattleClicked?.Invoke();

            // TODO: prev/next 버튼 → 캐릭터 6종 순환 + 프리뷰 모델 교체 + 스탯 갱신 (다음 단계)
        }

        public override void Show()
        {
            base.Show();
            SetPreviewRigActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            SetPreviewRigActive(false);
        }

        private void SetPreviewRigActive(bool active)
        {
            if (_previewRig != null)
            {
                _previewRig.SetActive(active);
            }
        }
    }
}
