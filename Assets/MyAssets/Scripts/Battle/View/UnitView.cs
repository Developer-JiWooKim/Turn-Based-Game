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
        [Tooltip("인스펙터가 비어 있으면 자식에서 찾아 채운다 — 프리팹마다 일일이 연결하지 않아도 되도록")]
        [SerializeField] private UnitAnimator _unitAnimator;
        [SerializeField] private UnitHealthBar _unitHealthBar;
        [Tooltip("이 유닛의 전투음(등장/공격/스킬/피격/사망). 비워두면 소리 없이 진행한다.")]
        [SerializeField] private UnitSfxSO _sfx;

        [Header("연출")]
        [Tooltip("피해량 숫자가 뜨는 높이(발밑 기준, 월드 단위). 체력바와 비슷한 머리 높이가 기준이며 유닛 키에 맞춰 조정한다.")]
        [SerializeField] private float _popupHeight = 3f;

        [Tooltip("타격 이펙트가 터지는 높이(발밑 기준, 월드 단위). 숫자 팝업보다 낮은 몸통 높이가 기준이다.")]
        [SerializeField] private float _hitEffectHeight = 1.2f;

        [Header("근접 이동 연출")]
        [Tooltip("공격할 때 대상 앞으로 이동했다가 돌아온다(근접 캐릭터). 끄면 제자리에서 공격한다. " +
                 "원거리(메이지·레인저·석궁)는 꺼둘 것.")]
        [SerializeField] private bool _approachTarget;

        [Tooltip("대상으로부터 얼마나 앞에 설지(월드 단위). 너무 작으면 모델이 겹친다.")]
        [SerializeField] private float _approachDistance = 1.5f;

        [Tooltip("편도 이동 시간(초). 왕복이라 전투 한 번에 이 값의 2배가 더해진다 — 페이싱에 직접 영향.")]
        [SerializeField] private float _approachDuration = 0.25f;

        [Tooltip("점프 궤적의 최고 높이(월드 단위). 0이면 미끄러지듯 이동한다.")]
        [SerializeField] private float _approachJumpHeight = 0.8f;

        [Tooltip("제자리에서 대상 쪽으로 도는 시간(초) — 원거리 유닛용. " +
                 "근접 유닛은 이동에 회전이 포함되므로 이 값을 쓰지 않는다.")]
        [SerializeField] private float _turnDuration = 0.15f;

        [Header("공격 이펙트 (전부 선택 — 비워두면 그 연출만 생략된다)")]
        [Tooltip("발사할 투사체 프리팹(순수 ParticleSystem). 비워두면 투사체 없이 즉시 명중한다 — 근접 유닛은 비워둘 것.")]
        [SerializeField] private ParticleSystem _projectile;

        [Tooltip("발사 순간 총구에 터뜨릴 섬광 프리팹.")]
        [SerializeField] private ParticleSystem _muzzleFlash;

        [Tooltip("이 유닛의 공격이 명중했을 때 대상에게 터질 이펙트. 비워두면 HitEffectSpawner의 기본 이펙트를 쓴다.")]
        [SerializeField] private ParticleSystem _hitEffect;

        [Tooltip("투사체·총구 섬광이 나오는 위치(프리팹 안의 MuzzlePoint). 무기·손 본 아래에 두면 공격 모션을 따라 움직인다.")]
        [SerializeField] private Transform _muzzlePoint;

        [Header("스킬 전용 이펙트 (비워두면 위 일반 공격 이펙트를 그대로 쓴다)")]
        [Tooltip("스킬 사용 시 투사체.")]
        [SerializeField] private ParticleSystem _skillProjectile;

        [Tooltip("스킬 사용 시 발사 지점에 터질 섬광. 낙하 연출에서는 대상 머리 위에서 기운이 모이는 표현이 된다.")]
        [SerializeField] private ParticleSystem _skillMuzzleFlash;

        [Tooltip("스킬이 명중했을 때 대상에게 터질 이펙트.")]
        [SerializeField] private ParticleSystem _skillHitEffect;

        [Tooltip("켜면 스킬 투사체가 시전자가 아니라 대상 머리 위에서 대상으로 떨어진다(번개 낙하 연출).")]
        [SerializeField] private bool _skillFallsOnTarget;

        [Tooltip("낙하 시작 높이 — 대상 머리 위로 이만큼 위에서 출발한다(월드 단위).")]
        [SerializeField] private float _skillFallHeight = 5f;

        [Tooltip("켜면 스킬 사용 시 대상 앞이 아니라 전장 한가운데로 나가서 시전한다(전체 공격 연출).")]
        [SerializeField] private bool _skillMovesToCenter;

        public int UnitId { get; private set; }

        /// <summary>
        /// 화면 배치 인덱스 라벨(예: "A1"). <see cref="UnitViewRegistry"/>가 스폰 시 정하며,
        /// 체력바 표기와 상단 턴 순서 칩이 이 같은 값을 쓴다.
        /// </summary>
        public string SlotLabel { get; private set; }

        /// <summary>
        /// 피해량 팝업을 띄울 월드 좌표(머리 위).
        /// 앵커 오브젝트를 두는 대신 높이 값만 갖는다 — 유닛 프리팹 계층을 건드리지 않기 위함.
        /// </summary>
        public Vector3 PopupOrigin => transform.position + Vector3.up * _popupHeight;

        /// <summary>타격 이펙트를 터뜨릴 월드 좌표(몸통 높이). 팝업과 같이 앵커 없이 높이 값만 갖는다.</summary>
        public Vector3 HitEffectOrigin => transform.position + Vector3.up * _hitEffectHeight;

        /// <summary>한 번의 공격에 쓸 이펙트 묶음(일반 공격과 스킬이 서로 다를 수 있다).</summary>
        public readonly struct AttackEffects
        {
            public readonly ParticleSystem Projectile;
            public readonly ParticleSystem MuzzleFlash;
            public readonly ParticleSystem HitEffect;

            public AttackEffects(ParticleSystem projectile, ParticleSystem muzzleFlash, ParticleSystem hitEffect)
            {
                Projectile = projectile;
                MuzzleFlash = muzzleFlash;
                HitEffect = hitEffect;
            }
        }

        /// <summary>
        /// 이번 행동에 쓸 이펙트를 고른다. 스킬 슬롯이 비어 있으면 일반 공격 것을 그대로 쓰므로,
        /// 스킬 연출이 평타와 같은 유닛은 스킬 슬롯을 채울 필요가 없다.
        /// </summary>
        public AttackEffects ResolveEffects(bool isSkill) => new(
            isSkill && _skillProjectile != null ? _skillProjectile : _projectile,
            isSkill && _skillMuzzleFlash != null ? _skillMuzzleFlash : _muzzleFlash,
            isSkill && _skillHitEffect != null ? _skillHitEffect : _hitEffect);

        /// <summary>스킬을 전장 한가운데로 나가서 시전하는 유닛인지(전체 공격 연출).</summary>
        public bool MovesToCenterOnSkill => _skillMovesToCenter;

        /// <summary>
        /// 투사체가 출발할 월드 좌표.
        /// 기본은 시전자의 <see cref="ProjectileOrigin"/>이지만, 낙하 연출(<see cref="_skillFallsOnTarget"/>)이면
        /// <b>대상 머리 위</b>가 출발점이 된다 — 대상마다 다르므로 대상별로 호출해야 한다.
        /// </summary>
        public Vector3 ResolveLaunchOrigin(bool isSkill, Vector3 targetPosition) =>
            isSkill && _skillFallsOnTarget
                ? targetPosition + Vector3.up * _skillFallHeight
                : ProjectileOrigin;

        /// <summary>
        /// 투사체와 총구 섬광이 나가는 월드 좌표.
        ///
        /// 높이·전방 오프셋 값 대신 프리팹 안의 앵커(<see cref="_muzzlePoint"/>)를 쓴다 —
        /// 무기나 손 본 아래에 두면 <b>공격 모션을 따라 움직여</b> 실제 무기 위치에서 발사된다.
        /// 발사 시점에 읽으므로(<c>BattlePresenter.FlyProjectilesAsync</c>) 그 순간의 자세가 반영된다.
        ///
        /// 앵커가 없으면 몸통 높이로 대신한다 — 발밑에서 날아가는 것보다는 낫고,
        /// 어차피 <see cref="ValidateReferences"/>가 연결 누락을 로그로 알린다.
        /// </summary>
        public Vector3 ProjectileOrigin => _muzzlePoint != null ? _muzzlePoint.position : HitEffectOrigin;

        /// <summary>
        /// 공격 시 대상 앞으로 이동하는 근접 유닛인지.
        /// 호출부는 근접·원거리를 구분하지 않으므로(<see cref="FaceTargetAsync"/>가 흡수한다) 공개하지 않는다.
        /// </summary>
        private bool ApproachesTarget => _approachTarget && _approachDuration > 0f;

        /// <summary>
        /// 스폰된 배치 슬롯의 위치·회전. 근접 이동 후 정확히 이 상태로 돌아온다.
        /// 슬롯 Transform을 들고 있지 않고 값을 찍어두는 이유 — 레지스트리가 배치를 정한 뒤
        /// <see cref="Initialize"/>를 부르므로 여기서 스냅샷을 뜨면 충분하고, View가 배치 규칙을 알 필요가 없다.
        /// </summary>
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        /// <summary>
        /// 지금 제자리를 떠나 있는가(공격 연출로 이동한 상태).
        /// 복귀를 근접 토글이 아니라 이 값으로 판단해야 한다 — 근접이 아닌 유닛도
        /// 전체 공격 연출(<see cref="MoveToAsync"/>)로 전장 중앙까지 나갈 수 있기 때문.
        /// </summary>
        private bool _isDisplaced;

        /// <summary>
        /// 아웃라인 색을 결정하는 3D 모델 렌더러들과 프리팹 원본 레이어(복원용).
        /// 인스턴스 계층은 변하지 않으므로 인스턴스당 1회만 수집한다. 
        /// 
        /// 버그 사례 ㅡ 스폰마다 다시 캐싱하면 매번 계층 탐색과 배열 할당이 생기고,
        /// 무엇보다 "겨냥 레이어가 걸린 상태"에서 캐싱될 경우 그 레이어가 원본으로 굳어버린다(풀 재사용 시 실제로 발생하던 함정).
        /// </summary>
        private Renderer[] _modelRenderers;
        private int[] _originalLayers;

        private void CacheRenderers()
        {
            if (_modelRenderers != null)
            {
                return;
            }

            _modelRenderers = GetComponentsInChildren<Renderer>(true);
            _originalLayers = new int[_modelRenderers.Length];
            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                _originalLayers[i] = _modelRenderers[i].gameObject.layer;
            }
        }

        private void Awake()
        {
            // includeInactive:true 하는 이유 ㅡ 사망 시 숨긴 채로 풀에 반납된 인스턴스는 체력바가 비활성 상태다.
            if (_unitAnimator == null)
            {
                _unitAnimator = GetComponentInChildren<UnitAnimator>(true);
            }

            if (_unitHealthBar == null)
            {
                _unitHealthBar = GetComponentInChildren<UnitHealthBar>(true);
            }

            ValidateReferences();
        }

        /// <summary>
        /// 자동 탐색까지 실패했으면 보고한다 — 인스펙터가 아니라 프리팹 계층에 컴포넌트가 없다는 뜻이다.
        /// (인스펙터 공란은 정상이므로 OnValidate는 두지 않는다. 자동 탐색 전에는 항상 비어 보인다.)
        /// </summary>
        private void ValidateReferences()
        {
            // 자동 탐색까지 거친 값이라 LogIfMissing이 아니다 — "(인스펙터 확인)"이 붙으면 정상인 빈 슬롯을 뒤지게 된다.
            NullCheck.LogIfNullObject(_unitHealthBar, nameof(_unitHealthBar), this, "체력바를 갱신할 수 없습니다");
            NullCheck.LogIfNullObject(_unitAnimator, nameof(_unitAnimator), this, "연출 없이 즉시 진행됩니다");

            // 발사 위치는 투사체를 쓰는 유닛에게만 필요하다 — 근접 유닛은 비어 있는 게 정상이라 조건부로 본다.
            if (_projectile != null)
            {
                NullCheck.LogIfMissing(_muzzlePoint, nameof(_muzzlePoint), this, "몸통 높이에서 발사됩니다");
            }
        }

        public void Initialize(int unitId, int currentHp, int maxHp, string slotLabel)
        {
            UnitId = unitId;
            SlotLabel = slotLabel;

            // 레지스트리가 슬롯 배치로 옮긴 뒤 부르므로 지금 위치·회전이 곧 이 유닛의 제자리다.
            _homePosition = transform.position;
            _homeRotation = transform.rotation;

            CacheRenderers();

            if (_unitHealthBar == null)
            {
                return;
            }

            _unitHealthBar.SetVisible(true); // 사망으로 숨겨진 채 재사용됐을 수도 있으니 활성화 먼저

            // 등장하는 유닛의 게이지는 채워진 상태로 시작해야 한다 — 여기서 연출을 쓰면
            // 풀에서 물려받은 이전 전투의 잔량에서 스르륵 차오른다.
            _unitHealthBar.SetImmediate(currentHp, maxHp);
            _unitHealthBar.SetSlotLabel(slotLabel);

            // 풀에서 재사용된 인스턴스에 이전 전투의 표기가 남지 않도록 둘 다 초기화한다.
            // 상태이상을 먼저 비워야 뒤이은 스폰 디버프 갱신이 이전 유닛의 목록을 다시 그리지 않는다.
            _unitHealthBar.SetStatuses(null);
            _unitHealthBar.SetSpawnDebuff(null);
        }

        /// <summary>걸려 있는 상태이상 표기를 갱신한다.</summary>
        public void RefreshStatuses(IReadOnlyList<ActiveStatus> statuses)
        {
            if (_unitHealthBar != null)
            {
                _unitHealthBar.SetStatuses(statuses);
            }
        }

        /// <summary>스폰 시 적용된 로그라이크 디버프를 표기한다(이번 전투 내내 유지).</summary>
        public void SetSpawnDebuff(string label)
        {
            if (_unitHealthBar != null)
            {
                _unitHealthBar.SetSpawnDebuff(label);
            }
        }

        /// <summary>
        /// 풀에서 재사용되기 직전에 이전 전투의 흔적을 지운다(사망 포즈, 겨냥 아웃라인).
        /// 원본 레이어는 인스턴스당 1회만 캐싱되므로 <see cref="Initialize"/>와의 호출 순서는 상관없다.
        /// </summary>
        public void ResetForSpawn()
        {
            CacheRenderers(); // 첫 스폰(Initialize 전) 호출에도 대비
            ResetOutlineLayer();
            _isDisplaced = false; // 자리를 떠난 채 반납됐을 수 있다(레지스트리가 위치는 이미 슬롯으로 옮겨놨다)

            if (_unitAnimator != null)
            {
                _unitAnimator.ResetToSpawn();
            }
        }

        /// <summary>
        /// 아웃라인 색을 바꾸기 위해 모델 렌더러의 레이어를 지정 레이어로 옮긴다
        /// (렌더러별 아웃라인 기능이 레이어로 색을 결정). 콜라이더는 건드리지 않아 타겟 클릭 판정은 그대로.
        /// </summary>
        public void SetOutlineLayer(int layer)
        {
            if (_modelRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                if (_modelRenderers[i] != null)
                {
                    _modelRenderers[i].gameObject.layer = layer;
                }
            }
        }

        /// <summary>모델 렌더러 레이어를 스폰 당시 원래 값으로 되돌린다(기본 검정 아웃라인 복귀).</summary>
        public void ResetOutlineLayer()
        {
            if (_modelRenderers == null || _originalLayers == null)
            {
                return;
            }

            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                if (_modelRenderers[i] != null)
                {
                    _modelRenderers[i].gameObject.layer = _originalLayers[i];
                }
            }
        }

        /// <summary>
        /// 체력바를 켜고 끈다(공격 중 숨김용).
        /// 사망 시 숨기는 <see cref="PlayDieAsync"/>와 같은 경로를 쓰지만, 공격하는 쪽은 자기 차례에 죽지 않으므로
        /// 둘이 겹치지 않는다.
        /// </summary>
        private void SetHealthBarVisible(bool visible)
        {
            if (_unitHealthBar != null)
            {
                _unitHealthBar.SetVisible(visible);
            }
        }

        /// <summary>
        /// 체력바 갱신
        /// </summary>
        public void RefreshHealth(int currentHp, int maxHp)
        {
            if (_unitHealthBar != null)
            {
                _unitHealthBar.Set(currentHp, maxHp);
            }
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

        /// <summary>
        /// 이 유닛의 공격/스킬 연출에서 타격이 맞는 시점까지 기다린다.
        /// 클립에 타격 이벤트를 심어 뒀으면 그 프레임에, 아니면 <paramref name="fallbackSeconds"/> 뒤에 풀린다
        /// (판단은 <see cref="UnitAnimator.WaitForImpactAsync"/>가 한다).
        /// </summary>
        public Task WaitForImpactAsync(float fallbackSeconds, CancellationToken ct = default) =>
            _unitAnimator != null
                ? _unitAnimator.WaitForImpactAsync(fallbackSeconds, ct)
                : WaitSecondsAsync(fallbackSeconds, ct); // 애니메이터가 없으면 기존 고정 지연으로 진행

        private static async Task WaitSecondsAsync(float seconds, CancellationToken ct) =>
            await Awaitable.WaitForSecondsAsync(Mathf.Max(0f, seconds), ct);

        /// <summary>
        /// 공격 대상을 향한다(공격 연출의 전반부). 근접·원거리의 차이는 여기서 흡수하므로
        /// 호출부(<see cref="BattlePresenter"/>)는 둘을 구분하지 않는다.
        ///  - 근접(<see cref="_approachTarget"/>): 대상 앞까지 점프해 이동하며 그쪽을 바라본다
        ///  - 원거리: 제자리에서 방향만 돌린다
        ///
        /// 근접이 서는 자리는 대상에서 <b>자기 제자리 쪽으로</b> <see cref="_approachDistance"/>만큼 물러난 지점이다 —
        /// 아군은 화면 아래, 적군은 위에 서 있으므로 고정 방향으로 계산하면 한쪽 진영이 대상 뒤로 넘어간다.
        ///
        /// 체력바는 공격하는 동안 숨긴다(근접·원거리 공통) — 공격 연출 위로 게이지가 겹쳐 떠 있으면
        /// 타격이 잘 보이지 않고, 근접은 대상 위로 옮겨가 누구 것인지도 알 수 없게 된다.
        /// 다시 켜는 곳은 <see cref="RestorePoseAsync"/>와 <see cref="SnapHome"/> 두 곳뿐이다.
        /// </summary>
        public Task FaceTargetAsync(Vector3 targetPosition, CancellationToken ct = default)
        {
            SetHealthBarVisible(false);

            if (!ApproachesTarget)
            {
                return TurnAsync(LookRotation(targetPosition - _homePosition), ct);
            }

            Vector3 toHome = _homePosition - targetPosition;
            toHome.y = 0f;

            return MoveToAsync(targetPosition + toHome.normalized * _approachDistance, targetPosition, ct);
        }

        /// <summary>
        /// 지정한 지점으로 점프해 이동하며 <paramref name="lookAt"/> 쪽을 바라본다.
        /// 전체 공격을 전장 한가운데서 시전하는 것처럼, 대상 앞이 아닌 임의 지점으로 나갈 때 쓴다
        /// (<see cref="_skillMovesToCenter"/>). 근접 토글과 무관하게 동작한다.
        /// </summary>
        public Task MoveToAsync(Vector3 destination, Vector3 lookAt, CancellationToken ct = default)
        {
            SetHealthBarVisible(false);

            _isDisplaced = true;
            destination.y = _homePosition.y; // 높이는 제자리 기준을 유지(지면이 평평하지 않아도 뜨거나 묻히지 않게)

            return MoveAsync(destination, LookRotation(lookAt - destination), ct);
        }

        /// <summary>
        /// 공격이 끝난 뒤 제자리 배치(위치·회전)로 되돌아오고 체력바를 다시 켠다(공격 연출의 후반부).
        /// 회전을 이동과 함께 보간하므로 착지 후 방향이 튀지 않는다.
        ///
        /// 되돌리는 방식은 근접 토글이 아니라 <b>실제로 자리를 떠났는지</b>로 정한다 —
        /// 근접이 아닌 유닛도 전체 공격 연출로 전장 중앙까지 나갈 수 있기 때문.
        /// </summary>
        public async Task RestorePoseAsync(CancellationToken ct = default)
        {
            if (_isDisplaced)
            {
                await MoveAsync(_homePosition, _homeRotation, ct);
                _isDisplaced = false;
            }
            else
            {
                await TurnAsync(_homeRotation, ct);
            }

            SetHealthBarVisible(true);
        }

        /// <summary>
        /// 즉시 제자리 상태(위치·회전·체력바)로 되돌린다. 취소·예외로 왕복이 중간에 끊겼을 때 쓴다 —
        /// 빼먹으면 그 유닛만 엉뚱한 자리에 체력바도 없이 선 채 다음 턴이 진행된다.
        /// </summary>
        public void SnapHome()
        {
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
            _isDisplaced = false;
            SetHealthBarVisible(true);
        }

        /// <summary>수평 방향만 보는 회전(위아래로 기울지 않도록). 방향이 0이면 현재 회전을 유지한다.</summary>
        private Quaternion LookRotation(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude < 0.0001f ? transform.rotation : Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// 제자리에서 회전만 보간한다(원거리 유닛의 조준·복귀).
        ///
        /// 이미 거의 그쪽을 보고 있으면 즉시 맞추고 끝낸다 — 슬롯이 애초에 상대 진영을 향하고 있어
        /// 정면 대상은 각도 차가 몇 도뿐인데, 그걸 매 공격마다 왕복으로 기다리면
        /// <b>눈에 보이지도 않는 연출에 페이싱만 쓰게 된다</b>.
        /// </summary>
        private async Task TurnAsync(Quaternion destinationRotation, CancellationToken ct)
        {
            const float MinTurnAngle = 5f;

            if (_turnDuration <= 0f || Quaternion.Angle(transform.rotation, destinationRotation) < MinTurnAngle)
            {
                transform.rotation = destinationRotation;
                return;
            }

            Quaternion originRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < _turnDuration)
            {
                await Awaitable.NextFrameAsync(ct);
                elapsed += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(originRotation, destinationRotation,
                                                      Mathf.Clamp01(elapsed / _turnDuration));
            }

            transform.rotation = destinationRotation;
        }

        /// <summary>
        /// 목표 지점까지 포물선을 그리며 이동하고, 회전도 같은 구간에 걸쳐 함께 돌린다.
        /// 점프 클립이 없으므로(애니메이터가 Spawned/Idle/Attack/Hit/Die 구조) 트랜스폼만 움직인다 —
        /// 나중에 점프 클립이 생기면 여기서 트리거를 함께 재생하면 된다.
        /// </summary>
        private async Task MoveAsync(Vector3 destination, Quaternion destinationRotation, CancellationToken ct)
        {
            Vector3 origin = transform.position;
            Quaternion originRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < _approachDuration)
            {
                await Awaitable.NextFrameAsync(ct);
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / _approachDuration);
                Vector3 position = Vector3.Lerp(origin, destination, t);
                position.y += Mathf.Sin(t * Mathf.PI) * _approachJumpHeight; // 0 → 최고점 → 0

                transform.SetPositionAndRotation(position, Quaternion.Slerp(originRotation, destinationRotation, t));
            }

            // 마지막 프레임 오차를 남기지 않는다.
            transform.SetPositionAndRotation(destination, destinationRotation);
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
            {
                _unitHealthBar.SetVisible(false);
            }

            await Awaitable.WaitForSecondsAsync(_unitAnimator.PlayDie(), ct);
        }
    }
}
