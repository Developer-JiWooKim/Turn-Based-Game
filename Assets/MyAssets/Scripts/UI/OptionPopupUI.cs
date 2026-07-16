using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
    public class OptionPopupUI : BasePanelUI
    {
        protected override void Start()
        {
            base.Start();

            Root.Q<Button>("option-close-button").clicked += Hide;
        }
    }
}
