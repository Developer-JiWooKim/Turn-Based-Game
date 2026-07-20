using Assets.MyAssets.Scripts.Battle.Core;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 캐릭터/몬스터가 공유하는 기준 스탯 + 프리팹 데이터의 베이스 SO.
    /// 수치는 임시값이며 밸런싱 단계에서 조정한다(하드코딩 대신 이 SO로 관리).
    /// </summary>
    public abstract class UnitStatsSO : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("이 유닛의 3D 모델 프리팹. UnitView 컴포넌트를 포함해야 한다.")]
        [SerializeField] private GameObject _prefab;

        [Header("Stats (임시 값, 밸런싱 예정)")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _atk = 20;
        [Tooltip("턴 순서 결정. 버프/디버프로 전투 중 변동 가능.")]
        [SerializeField] private int _spd = 10;
        [SerializeField] private int _def = 10;
        [Range(0f, 1f)][SerializeField] private float _critRate = 0.05f;
        [Tooltip("치명타 배율 (1.5 = 150%).")]
        [SerializeField] private float _critDmg = 1.5f;
        [Range(0f, 1f)][SerializeField] private float _res = 0f;

        public string DisplayName => _displayName;
        public GameObject Prefab => _prefab;

        /// <summary>이 SO 기준값으로 독립적인 런타임 스탯 인스턴스를 만든다.</summary>
        public Stats CreateStats() => new Stats(_maxHp, _atk, _spd, _def, _critRate, _critDmg, _res);
    }
}
