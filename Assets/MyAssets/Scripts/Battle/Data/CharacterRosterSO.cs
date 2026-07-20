using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.Data
{
    /// <summary>
    /// 선택 화면에 노출할 플레이어 캐릭터 6종 목록. 캐릭터 선택 패널이 이 SO를 참조해 순환한다.
    /// (하드코딩 대신 SO로 관리 — 캐릭터 추가/순서 변경을 에디터에서 처리.)
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Character Roster", fileName = "CharacterRoster")]
    public sealed class CharacterRosterSO : ScriptableObject
    {
        [SerializeField] private CharacterStatsSO[] _characters;

        public int Count => _characters != null ? _characters.Length : 0;

        public CharacterStatsSO this[int index] => _characters[index];
    }
}
