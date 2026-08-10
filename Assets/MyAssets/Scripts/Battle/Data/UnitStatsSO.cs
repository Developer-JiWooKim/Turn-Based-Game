using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Localization;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 캐릭터/몬스터가 공유하는 기준 스탯 + 프리팹 데이터의 베이스 SO.
    /// 수치는 임시값이며 밸런싱 단계에서 조정한다.
    /// </summary>
    public abstract class UnitStatsSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;

        [Header("Stats")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _atk = 20;
        [SerializeField] private int _spd = 10;
        [SerializeField] private int _def = 10;
        [Range(0f, 1f)][SerializeField] private float _critRate = 0.05f;
        [Tooltip("치명타 배율")]
        [SerializeField] private float _critDmg = 1.5f;
        [Tooltip("저항 배율")]
        [Range(0f, 1f)][SerializeField] private float _res = 0f;

        /// <summary>
        /// 화면 표시 이름. 에셋에 적힌 값이 곧 문자열 표의 키이므로,
        /// 표에 행이 없으면 에셋의 원문이 그대로 나온다(<see cref="Loc.Get"/>).
        /// 덕분에 번역을 얹어도 에셋을 고칠 필요가 없다.
        /// </summary>
        public string DisplayName => Loc.Get(_displayName);

        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;

        public Stats CreateStats() => new Stats(
            maxHp: _maxHp,
            atk: _atk,
            spd: _spd,
            def: _def,
            critRate: _critRate,
            critDmg: _critDmg,
            res: _res);
    }
}
