using System;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Audio.Data
{
    /// <summary>
    /// 공용(씬 공통) 오디오 클립 모음 SO.
    /// 유닛별 전투음은 <see cref="UnitSfxSO"/>가 따로 담당한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrarySO : ScriptableObject
    {
        /// <summary>씬 이름 하나에 대응하는 BGM</summary>
        [Serializable]
        public struct SceneBgmEntry
        {
            public string SceneName;
            public AudioClip Clip;
        }

        [Header("BGM")]
        [Tooltip("씬 진입 시 자동 재생할 BGM(씬 이름 ↔ 클립). BattleScene은 스테이지별로 BattleDirector가 직접 지정하므로 보통 여기 넣지 않는다.")]
        [SerializeField] private SceneBgmEntry[] _sceneBgm;
        [Tooltip("전투 일반 BGM")]
        [SerializeField] private AudioClip _battleBgm;
        [Tooltip("보스 스테이지 BGM")]
        [SerializeField] private AudioClip _bossBgm;

        [Header("SFX")]
        [SerializeField] private AudioClip _uiClick;
        [Tooltip("방향키로 선택지/카드를 옮길 때의 이동음(hover tick). 비우면 UI 클릭음으로 대체.")]
        [SerializeField] private AudioClip _uiNavigate;
        [Tooltip("배틀 타겟 확정 등 '확정' 순간의 소리. 비우면 UI 클릭음으로 대체.")]
        [SerializeField] private AudioClip _confirm;
        [SerializeField] private AudioClip _victoryStinger;
        [SerializeField] private AudioClip _defeatStinger;
        [SerializeField] private AudioClip _critical;

        public AudioClip BattleBgm => _battleBgm;
        public AudioClip BossBgm => _bossBgm;
        public AudioClip UiClick => _uiClick;

        /// <summary>방향키 이동음. 전용 클립이 없으면 UI 클릭음으로 대체</summary>
        public AudioClip UiNavigate => _uiNavigate ?? _uiClick;

        /// <summary>확정음. 전용 클립이 없으면 UI 클릭음으로 대체</summary>
        public AudioClip Confirm => _confirm ?? _uiClick;

        public AudioClip VictoryStinger => _victoryStinger;
        public AudioClip DefeatStinger => _defeatStinger;
        public AudioClip Critical => _critical;

        /// <summary>보스 여부에 따라 전투 BGM을 선택</summary>
        public AudioClip GetBattleBgm(bool boss) => boss ? _bossBgm : _battleBgm;

        /// <summary>씬 이름에 대응하는 BGM(등록 안 됐으면 null)</summary>
        public AudioClip GetSceneBgm(string sceneName)
        {
            if (_sceneBgm == null)
            {
                return null;
            }

            foreach (SceneBgmEntry entry in _sceneBgm)
            {
                if (entry.SceneName == sceneName)
                {
                    return entry.Clip;
                }
            }

            return null;
        }
    }
}
