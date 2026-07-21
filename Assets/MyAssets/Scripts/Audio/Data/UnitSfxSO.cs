using UnityEngine;

namespace Assets.MyAssets.Scripts.Audio.Data
{
    /// <summary>
    /// 유닛 1종의 전투음 묶음(등장/공격/스킬/피격/사망).
    /// 캐릭터·몬스터 프리팹의 UnitView에 프리팹별로 다른 에셋을 물린다.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Unit SFX", fileName = "UnitSfx")]
    public sealed class UnitSfxSO : ScriptableObject
    {
        [SerializeField] private AudioClip _spawn;
        [SerializeField] private AudioClip _attack;
        [SerializeField] private AudioClip _skill;
        [SerializeField] private AudioClip _hit;
        [SerializeField] private AudioClip _die;

        public AudioClip Spawn => _spawn;
        public AudioClip Attack => _attack;
        public AudioClip Skill => _skill;
        public AudioClip Hit => _hit;
        public AudioClip Die => _die;
    }
}
