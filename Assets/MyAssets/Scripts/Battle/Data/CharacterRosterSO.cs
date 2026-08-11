using Assets.MyAssets.Scripts.Battle.Core;
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

        /// <summary>
        /// 에셋 이름으로 캐릭터를 찾는다(세이브 복원용, 없으면 null).
        /// 표시 이름은 번역을 거친 값이라 언어에 따라 달라지고, 인덱스는 로스터 순서를 바꾸면
        /// 조용히 다른 캐릭터가 되므로 둘 다 식별자로 쓰지 않는다.
        /// </summary>
        public CharacterStatsSO Find(string assetName)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_characters[i] != null && _characters[i].name == assetName)
                {
                    return _characters[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 로스터 전체에서 스탯별 최댓값. 선택 화면의 스탯 바가 "이 로스터 안에서 얼마나 높은가"를
        /// 그릴 때 기준으로 쓴다(밸런싱 값을 UI 코드에 박지 않기 위해 데이터에서 구한다).
        ///
        /// 최댓값이 곧 로스터의 성질이라 UI가 아니라 여기가 소유한다.
        /// 비율 스탯(치명타·저항)은 그 자체가 0~1이라 계산 대상이 아니다.
        /// </summary>
        public Stats CreateStatCeiling()
        {
            var ceiling = new Stats(1, 1, 1, 1, 1f, 1f, 1f);
            for (int i = 0; i < Count; i++)
            {
                if (_characters[i] == null)
                {
                    continue;
                }

                Stats s = _characters[i].CreateStats();
                ceiling.MaxHp = Mathf.Max(ceiling.MaxHp, s.MaxHp);
                ceiling.Atk = Mathf.Max(ceiling.Atk, s.Atk);
                ceiling.Spd = Mathf.Max(ceiling.Spd, s.Spd);
                ceiling.Def = Mathf.Max(ceiling.Def, s.Def);
            }

            return ceiling;
        }
    }
}
