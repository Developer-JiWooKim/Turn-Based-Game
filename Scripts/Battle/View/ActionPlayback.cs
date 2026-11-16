using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 유닛 행동 1회의 연출 시퀀스 전담 — 대상 바라보기 → 공격 애니메이션 → 타격 시점 대기 →
    /// 투사체 비행 → 피격·이펙트·숫자 일괄 발동 → 제자리 복귀까지를 한 줄기로 재생한다.
    ///
    /// <see cref="PartyStatusBarView"/> 선례대로 <b>순수 C# 클래스</b>이며 <see cref="BattlePresenter"/>가
    /// 필드로 들고 쓴다. MonoBehaviour로 두면 인스펙터 슬롯이 새 오브젝트로 흩어져
    /// 씬 배선을 다시 해야 하는데, 참조는 전부 Presenter가 이미 들고 있으므로 생성자로 넘겨받으면 된다.
    ///
    /// 여기서 갈라낸 이유는 <see cref="BattlePresenter"/>가 "Core 이벤트 → HUD·강조 반영"과
    /// "한 번의 행동을 어떻게 보여줄 것인가"라는 서로 다른 두 일을 겸하고 있었기 때문이다.
    /// 시뮬레이션을 대기시키는 <c>RegisterPlayback</c>은 Presenter에 남는다 — 등록은 이벤트의 몫이다.
    /// </summary>
    public sealed class ActionPlayback
    {
        private readonly UnitViewRegistry _registry;
        private readonly CancellationToken _ct;

        /// <summary>클립에 타격 이벤트가 없을 때만 쓰이는 폴백 지연(초).</summary>
        private readonly float _impactDelay;

        // 아래 넷은 전부 선택 참조다 — 없으면 그 연출만 빠지고 나머지는 그대로 재생된다.
        private readonly DamagePopupSpawner _damagePopups;
        private readonly HitEffectSpawner _hitEffects;
        private readonly ProjectileSpawner _projectiles;
        private readonly CameraShake _cameraShake;

        public ActionPlayback(UnitViewRegistry registry, CancellationToken ct, float impactDelay,
                              DamagePopupSpawner damagePopups, HitEffectSpawner hitEffects,
                              ProjectileSpawner projectiles, CameraShake cameraShake)
        {
            // registry는 BattleDirector가 이미 검증한 뒤 넘겨주므로 여기서 다시 확인하지 않는다.
            _registry = registry;
            _ct = ct;
            _impactDelay = impactDelay;
            _damagePopups = damagePopups;
            _hitEffects = hitEffects;
            _projectiles = projectiles;
            _cameraShake = cameraShake;
        }

        /// <summary>행동 1회를 처음부터 끝까지 재생한다. 반환 Task의 완료가 곧 "연출이 끝났다"는 뜻이다.</summary>
        public async Task PlayAsync(ActionResult result)
        {
            if (!_registry.TryGet(result.Actor.Id, out UnitView actorView))
            {
                return;
            }

            // 공격 전에 대상을 향한다 — 근접은 앞으로 이동까지, 원거리는 회전만.
            // 둘의 차이는 UnitView가 흡수하므로 여기서는 구분하지 않는다.
            TryGetFacingTarget(result, out UnitView facingTarget);

            try
            {
                if (facingTarget != null)
                {
                    // 전체 공격을 전장 한가운데서 쓰는 유닛(보스 등)만 예외적으로 목적지가 다르다.
                    if (result.Kind == ActionKind.Skill && actorView.MovesToCenterOnSkill)
                    {
                        await actorView.MoveToAsync(_registry.GetBattlefieldCenter(),
                                                    GetTargetsCenter(result, facingTarget.transform.position), _ct);
                    }
                    else
                    {
                        await actorView.FaceTargetAsync(facingTarget.transform.position, _ct);
                    }
                }

                await PlayStrikeAsync(result, actorView);

                if (facingTarget != null)
                {
                    await actorView.RestorePoseAsync(_ct);
                }
            }
            finally
            {
                // 취소(씬 종료·배틀 중단)로 중간에 끊기면 그 유닛만 돌아선 채(근접이면 엉뚱한 자리에) 남는다.
                if (facingTarget != null)
                {
                    actorView.SnapHome();
                }
            }
        }

        /// <summary>
        /// 맞는 대상들의 평균 위치. 전장 한가운데서 전체 공격을 쓸 때 어느 쪽을 보고 시전할지 정한다 —
        /// 첫 대상만 보면 끝자리 대상을 향해 비스듬히 서게 된다.
        /// </summary>
        private Vector3 GetTargetsCenter(ActionResult result, Vector3 fallback)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (HitResult hit in result.Hits)
            {
                if (_registry.TryGet(hit.Target.Id, out UnitView view))
                {
                    sum += view.transform.position;
                    count++;
                }
            }

            return count > 0 ? sum / count : fallback;
        }

        /// <summary>
        /// 바라볼 대상 — 여러 명을 때리는 라인 스킬이면 첫 대상을 기준으로 삼는다.
        /// 이미 죽어 View가 사라진 대상은 건너뛴다.
        /// </summary>
        private bool TryGetFacingTarget(ActionResult result, out UnitView view)
        {
            foreach (HitResult hit in result.Hits)
            {
                if (_registry.TryGet(hit.Target.Id, out view))
                {
                    return true;
                }
            }

            view = null;
            return false;
        }

        /// <summary>
        /// 원거리 유닛의 투사체를 대상마다 쏘고 <b>전부 도착할 때까지</b> 기다린다.
        /// 스포너나 프리팹이 없으면 즉시 통과하므로 근접 유닛은 영향을 받지 않는다.
        ///
        /// 대상이 여럿(라인 스킬)이면 마지막 하나가 닿을 때까지 기다린다 — 가까운 대상이 먼저 맞는 것처럼
        /// 보이지는 않지만, 피해는 이미 계산이 끝나 있어 결과에는 영향이 없다(연출 순서만의 문제).
        /// </summary>
        private async Task FlyProjectilesAsync(ActionResult result, UnitView actorView, UnitView.AttackEffects effects)
        {
            if (_projectiles == null || effects.Projectile == null)
            {
                return;
            }

            bool isSkill = result.Kind == ActionKind.Skill;

            // 대상별 발사 구간을 먼저 모은다 — 총구 발사는 출발점이 하나지만
            // 낙하 연출은 "대상 머리 위"라 대상마다 다르기 때문.
            // (총구 발사의 출발점은 회전이 끝난 뒤 계산해야 대상 쪽에서 나간다 — FaceTargetAsync가 이미 돌려놨다.)
            var launches = new List<(Vector3 From, Vector3 To)>();
            foreach (HitResult hit in result.Hits)
            {
                if (_registry.TryGet(hit.Target.Id, out UnitView targetView))
                {
                    // 도착 지점은 타격 이펙트가 터지는 자리와 같다 — 닿은 곳에서 이펙트가 나야 자연스럽다.
                    Vector3 to = targetView.HitEffectOrigin;
                    launches.Add((actorView.ResolveLaunchOrigin(isSkill, to), to));
                }
            }

            if (launches.Count == 0)
            {
                return;
            }

            // 발사 섬광이 먼저 터지고 한 박자 뒤에 투사체가 출발한다.
            foreach ((Vector3 from, Vector3 to) in launches)
            {
                _projectiles.SpawnMuzzle(effects.MuzzleFlash, from, to);
            }

            await _projectiles.WaitMuzzleLeadAsync(_ct);

            var flights = new List<Task>();
            foreach ((Vector3 from, Vector3 to) in launches)
            {
                flights.Add(_projectiles.FlyAsync(effects.Projectile, from, to, _ct));
            }

            await Task.WhenAll(flights);
        }

        /// <summary>공격 애니메이션 → 타격 시점 대기 → 투사체 비행 → 피격 연출 일괄 발동 → 연출 완료 대기.</summary>
        private async Task PlayStrikeAsync(ActionResult result, UnitView actorView)
        {
            // 1) 공격/스킬 애니메이션 시작. 이펙트도 행동 종류에 따라 갈리므로 여기서 한 번만 고른다.
            bool isSkill = result.Kind == ActionKind.Skill;
            UnitView.AttackEffects effects = actorView.ResolveEffects(isSkill);

            Task actorAnim = isSkill
                ? actorView.PlaySkillAsync(_ct)
                : actorView.PlayAttackAsync(_ct);

            // 2) 타격 시점까지 대기 — 클립에 심은 애니메이션 이벤트가 있으면 그 프레임, 없으면 고정 지연.
            //    원거리 유닛에게 이 시점은 "명중"이 아니라 "발사"다.
            await actorView.WaitForImpactAsync(_impactDelay, _ct);

            // 2-b) 투사체가 있으면 여기서 쏘고 도착할 때까지 기다린다 — 화살이 닿기 전에 피해 숫자가 뜨면
            //      순서가 거꾸로 보인다. 투사체가 없는 유닛은 즉시 통과한다.
            await FlyProjectilesAsync(result, actorView, effects);

            // 3) 타격 순간에 피격 연출·이펙트·숫자를 한꺼번에 터뜨린다.
            var playback = new List<Task> { actorAnim };
            bool anyCritical = false;
            foreach (HitResult hit in result.Hits)
            {
                if (hit.IsCritical)
                {
                    anyCritical = true;
                }
                if (_registry.TryGet(hit.Target.Id, out UnitView targetView))
                {
                    playback.Add(targetView.PlayHitAsync(hit.Target.CurrentHp, hit.Target.Stats.MaxHp, _ct));

                    // 이펙트와 팝업은 연출 대기에 넣지 않는다 — 장식이라 전투 페이싱을 늦출 이유가 없다.
                    // 명중 이펙트는 맞은 쪽이 아니라 <b>때린 쪽</b>의 것을 쓴다 — 공격의 성질(화살/마법)을 나타내기 때문.
                    if (_hitEffects != null)
                    {
                        _hitEffects.SpawnHit(targetView.HitEffectOrigin, hit.IsCritical, effects.HitEffect);
                    }

                    if (_damagePopups != null)
                    {
                        _damagePopups.Spawn(targetView.PopupOrigin, hit.Damage,
                                            hit.IsCritical ? DamageKind.Critical : DamageKind.Normal);
                    }
                }
            }

            if (anyCritical)
            {
                if (_cameraShake != null)
                {
                    _cameraShake.Shake();
                }

                AudioManager.Sfx(AudioManager.Library?.Critical);
            }

            await Task.WhenAll(playback);
        }
    }
}
