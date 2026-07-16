using System;
using UnityEngine.UIElements;

namespace Assets.MyAssets.Scripts.UI
{
  public class TitlePanelUI : BasePanelUI
  {

    public event Action OnPlayClicked;
    public event Action OnOptionClicked;
    public event Action OnQuitClicked;

    protected override void Start()
    {
      base.Start();

      Root.Q<Button>("start-button").clicked += () => OnPlayClicked?.Invoke();
      Root.Q<Button>("option-button").clicked += () => OnOptionClicked?.Invoke();
      Root.Q<Button>("quit-button").clicked += () => OnQuitClicked?.Invoke();
    }
  }
}
