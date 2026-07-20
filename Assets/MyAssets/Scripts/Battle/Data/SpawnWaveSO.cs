using System.Collections.Generic;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 한 스테이지에 등장하는 몬스터 구성. README 기준 1~5스테이지는 에디터에서 수동 설계하므로
    /// 이 SO를 스테이지 번호 순서대로 나열해 배열 형태로 관리한다.
    /// (6스테이지 이후 랜덤 스폰 패턴 풀은 추후 별도 시스템으로 확장 예정 — 지금은 미구현.)
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Spawn Wave", fileName = "SpawnWave")]
    public sealed class SpawnWaveSO : ScriptableObject
    {
        [SerializeField] private MonsterStatsSO[] _monsters;

        public IReadOnlyList<MonsterStatsSO> Monsters => _monsters;
    }
}
