using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Audio.Data;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// Unit 하나에 대응하는 연출 담당 컴포넌트
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        [Header("Reference Components")]
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private UnitHealthBar _unitHealthBar;
        [Tooltip("이 유닛의 전투음(등장/공격/스킬/피격/사망). 비워두면 소리 없이 진행한다.")]
        [SerializeField] private UnitSfxSO _sfx;

        public int UnitId { get; private set; }

        /// <summary>아웃라인 색을 결정하는 3D 모델 렌더러들과 프리팹 원본 레이어(복원용).
        /// 월드스페이스 체력바는 uGUI(CanvasRenderer)라 Renderer로 잡히지 않아 자동 제외된다.
        ///
        /// 인스턴스 계층은 변하지 않으므로 <b>인스턴스당 1회</b>만 수집한다. 스폰마다 다시 캐싱하면
        /// 매번 계층 탐색과 배열 할당이 생기고, 무엇보다 "겨냥 레이어가 걸린 상태"에서 캐싱될 경우
        /// 그 레이어가 원본으로 굳어버린다(풀 재사용 시 실제로 발생하던 함정).</summary>
        private Renderer[] _modelRenderers;
        private int[] _originalLayers;

        private void CacheRenderers()
        {
            if (_modelRenderers != null) return;

            _modelRenderers = GetComponentsInChildren<Renderer>(true);
            _originalLayers = new int[_modelRenderers.Length];
            for (int i = 0; i < _modelRenderers.Length; i++)
                _originalLayers[i] = _modelRenderers[i].gameObject.layer;
        }

        private void Awake()
        {
            // 인스펙터가 비어 있으면 자식에서 찾아 채운다 — 프리팹마다 일일이 연결하지 않아도 되도록.
            // includeInactive: 사망 시 숨긴 채로 풀에 반납된 인스턴스는 체력바가 비활성 상태다.
            if (_unitAnimator == null) _unitAnimator = GetComponentInChildren<UnitAnimator>(true);
            if (_unitHealthBar == null) _unitHealthBar = GetComponentInChildren<UnitHealthBar>(true);

            ValidateReferences();
        }

        /// <summary>
        /// 자동 탐색까지 실패했으면 보고한다 — 인스펙터가 아니라 프리팹 계층에 컴포넌트가 없다는 뜻이다.
        /// (인스펙터 공란은 정상이므로 <c>OnValidate</c>는 두지 않는다. 자동 탐색 전에는 항상 비어 보인다.)
        /// </summary>
        private void ValidateReferences()
        {
            NullCheck.LogIfMissing(_unitHealthBar, nameof(_unitHealthBar), this, "체력바를 갱신할 수 없습니다");
            NullCheck.LogIfMissing(_unitAnimator, nameof(_unitAnimator), this, "연출 없이 즉시 진행됩니다");
        }

        public void Initialize(int unitId, int currentHp, int maxHp)
        {
            UnitId = unitId;
            CacheRenderers();

            if (_unitHealthBar == null) return; // 누락은 Awake에서 보고 완료

            _unitHealthBar.SetVisible(true); // 사망으로 숨겨진 채 재사용된 경우 복구
            _unitHealthBar.Set(currentHp, maxHp);

            // 풀에서 재사용된 인스턴스에 이전 전투의 표기가 남지 않도록 둘 다 초기화한다.
            // 상태이상을 먼저 비워야 뒤이은 스폰 디버프 갱신이 이전 유닛의 목록을 다시 그리지 않는다.
            _unitHealthBar.SetStatuses(null);
            _unitHealthBar.SetSpawnDebuff(null);
        }

        /// <summary>걸려 있는 상태이상 표기를 갱신한다.</summary>
        public void RefreshStatuses(IReadOnlyList<ActiveStatus> statuses)
        {
            if (_unitHealthBar != null)
                _unitHealthBar.SetStatuses(statuses);
        }

        /// <summary>스폰 시 적용된 로그라이크 디버프를 표기한다(이번 전투 내내 유지).</summary>
        public void SetSpawnDebuff(string label)
        {
            if (_unitHealthBar != null)
                _unitHealthBar.SetSpawnDebuff(label);
        }

        /// <summary>
        /// 풀에서 재사용되기 직전에 이전 전투의 흔적을 지운다(사망 포즈, 겨냥 아웃라인).
        /// 원본 레이어는 인스턴스당 1회만 캐싱되므로 <see cref="Initialize"/>와의 호출 순서는 상관없다.
        /// </summary>
        public void ResetForSpawn()
        {
            CacheRenderers(); // 첫 스폰(Initialize 전) 호출에도 대비
            ResetOutlineLayer();

            if (_unitAnimator != null)
                _unitAnimator.ResetToSpawn();
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

            // 쓰러진 유닛 위에 0/N 게이지와 상태이상 표기가 계속 떠 있지 않도록 사망 연출과 함께 숨긴다.
            if (_unitHealthBar != null)
                _unitHealthBar.SetVisible(false);

            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayDie(), ct);
        }
    }
}
