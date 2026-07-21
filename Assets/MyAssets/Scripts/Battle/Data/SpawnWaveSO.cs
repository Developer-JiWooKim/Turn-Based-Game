using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 한 스테이지에 등장하는 몬스터 구성. README 기준 1~5스테이지는 에디터에서 수동 설계하므로
    /// 이 SO를 스테이지 번호 순서대로 나열해 배열 형태로 관리한다.
    /// 6스테이지 이후는 <see cref="Weight"/>/<see cref="IsBossWave"/>를 이용해 랜덤 풀에서 뽑는다.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Spawn Wave", fileName = "SpawnWave")]
    public sealed class SpawnWaveSO : ScriptableObject
    {
        [SerializeField] private MonsterStatsSO[] _monsters;
        [Tooltip("랜덤 스폰 풀 안에서 이 웨이브가 뽑힐 상대적 가중치.")]
        [SerializeField] private float _weight = 1f;

        public IReadOnlyList<MonsterStatsSO> Monsters => _monsters;
        public float Weight => _weight;

        /// <summary>몬스터 중 하나라도 Boss Tier면 보스 웨이브로 취급한다.</summary>
        public bool IsBossWave => _monsters != null && _monsters.Any(m => m != null && m.Tier == MonsterTier.Boss);
    }
}
