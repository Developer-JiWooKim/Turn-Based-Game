using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Audio.Data;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Unit 하나에 대응하는 연출 담당 컴포넌트
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private UnitHealthBar _unitHealthBar;
        [Tooltip("이 유닛의 전투음(등장/공격/스킬/피격/사망). 비워두면 소리 없이 진행한다.")]
        [SerializeField] private UnitSfxSO _sfx;

        public int UnitId { get; private set; }

        /// <summary>아웃라인 색을 결정하는 3D 모델 렌더러들과 원래 레이어(복원용).
        /// 월드스페이스 체력바는 uGUI(CanvasRenderer)라 Renderer로 잡히지 않아 자동 제외된다.</summary>
        private Renderer[] _modelRenderers;
        private int[] _originalLayers;

        public void Initialize(int unitId, int currentHp, int maxHp)
        {
            UnitId = unitId;

            _modelRenderers = GetComponentsInChildren<Renderer>(true);
            _originalLayers = new int[_modelRenderers.Length];
            for (int i = 0; i < _modelRenderers.Length; i++)
                _originalLayers[i] = _modelRenderers[i].gameObject.layer;

            if (_unitAnimator == null)
            {
                Debug.LogWarning("_unitAnimator is null");
                _unitAnimator = GetComponentInChildren<UnitAnimator>();
            }

            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
            else
            {
                Debug.LogWarning("_unitHealthBar is null");
                _unitHealthBar = GetComponentInChildren<UnitHealthBar>();
                _unitHealthBar.Set(currentHp, maxHp);
            }
        }

        /// <summary>
        /// 아웃라인 색을 바꾸기 위해 모델 렌더러의 레이어를 지정 레이어로 옮긴다
        /// (렌더러별 아웃라인 기능이 레이어로 색을 결정). 콜라이더는 건드리지 않아 타겟 클릭 판정은 그대로.
        /// </summary>
        public void SetOutlineLayer(int layer)
        {
            if (_modelRenderers == null) return;
            for (int i = 0; i < _modelRenderers.Length; i++)
                if (_modelRenderers[i] != null)
                    _modelRenderers[i].gameObject.layer = layer;
        }

        /// <summary>모델 렌더러 레이어를 스폰 당시 원래 값으로 되돌린다(기본 검정 아웃라인 복귀).</summary>
        public void ResetOutlineLayer()
        {
            if (_modelRenderers == null || _originalLayers == null) return;
            for (int i = 0; i < _modelRenderers.Length; i++)
                if (_modelRenderers[i] != null)
                    _modelRenderers[i].gameObject.layer = _originalLayers[i];
        }

        /// <summary>
        /// 체력바 갱신
        /// </summary>
        public void RefreshHealth(int currentHp, int maxHp)
        {
            if (_unitHealthBar != null)
                _unitHealthBar.Set(currentHp, maxHp);
        }

        /// <summary>
        /// Spawned는 Animator의 기본 진입 상태라 트리거 없이 자동 재생되므로,
        /// 그 클립 길이(_spawnDuration)만큼 기다림
        /// </summary>
        public async Task PlaySpawnAsync(CancellationToken ct = default)
        {
            AudioManager.Sfx(_sfx?.Spawn);
            await Awaitable.WaitForSecondsAsync(_unitAnimator.SpawnDuration, ct);
        }

        public async Task PlayAttackAsync(CancellationToken ct = default)
        {
            AudioManager.Sfx(_sfx?.Attack);
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayAttack(), ct);
        }

        public async Task PlaySkillAsync(CancellationToken ct = default)
        {
            AudioManager.Sfx(_sfx?.Skill);
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlaySkill(), ct);
        }

        public async Task PlayHitAsync(int currentHp, int maxHp, CancellationToken ct = default)
        {
            AudioManager.Sfx(_sfx?.Hit);
            Awaitable hitTask = Awaitable.WaitForSecondsAsync(_unitAnimator.PlayHit(), ct);
            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
            await hitTask;
        }

        public async Task PlayDieAsync(CancellationToken ct = default)
        {
            AudioManager.Sfx(_sfx?.Die);
            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayDie(), ct);
        }
    }
}
