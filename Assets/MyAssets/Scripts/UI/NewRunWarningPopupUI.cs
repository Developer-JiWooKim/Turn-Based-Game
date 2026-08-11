using System;

namespace Assets.MyAssets.Scripts.UI
{
    /// <summary>
    /// 저장된 이어하기 데이터가 있는데 새 런을 시작하려 할 때 뜨는 경고 팝업.
    ///
    /// 화면만 담당한다 — "저장된 런이 있는가"의 판정도, 체크포인트 삭제도 하지 않고
    /// 확인 이벤트만 낸다(<see cref="GameUIController"/>가 처리). 취소는 팝업만 닫는다.
    /// </summary>
    public sealed class NewRunWarningPopupUI : BasePanelUI
    {
        protected override string RootElementName => "newrun-popup";

        /// <summary>플레이어가 기존 데이터를 지우고 시작을 골랐을 때 발생할 이벤트.</summary>
        public event Action OnConfirmed;

        protected override void InitPanel()
        {
            BindButton("newrun-cancel-button", Hide);
            BindButton("newrun-confirm-button", OnConfirm);
        }

        private void OnConfirm()
        {
            Hide();
            OnConfirmed?.Invoke();
        }
    }
}
