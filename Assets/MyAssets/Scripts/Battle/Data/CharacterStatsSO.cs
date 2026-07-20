using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 플레이어 캐릭터 6종의 스탯 데이터. 캐릭터는 공격만 하므로 스킬 필드가 없다.
    /// (파티 시너지 등 캐릭터 고유 요소는 추후 이 SO에 확장.)
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Character Stats", fileName = "CharacterStats")]
    public sealed class CharacterStatsSO : UnitStatsSO
    {
    }
}
