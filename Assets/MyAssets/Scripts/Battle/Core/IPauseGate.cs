using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 전투 진행을 잠시 멈추는 게이트(순수 계약, UnityEngine 비의존).
    /// <see cref="BattleSimulation"/>이 각 유닛 행동 직전에 이 대기를 통과한다.
    ///
    /// Time.timeScale로 멈추지 않는 이유: 연출 대기(<c>Awaitable.WaitForSecondsAsync</c>)가 timeScale에
    /// 영향을 받는지 보장되지 않아, 애니메이션만 얼고 시뮬레이션은 계속 진행될 위험이 있다.
    /// 씬 전환 페이드도 같은 Awaitable 기반이라 timeScale을 건드리면 함께 멈출 수 있다.
    ///
    /// 행동 "중간"이 아니라 행동 "직전"에만 멈추므로 연출이 잘리지 않는다(턴제에 자연스러운 경계).
    /// </summary>
    public interface IPauseGate
    {
        /// <summary>퍼즈 중이면 해제될 때까지 기다린다. 퍼즈가 아니면 즉시 반환한다.</summary>
        Task WaitWhilePausedAsync(CancellationToken ct);
    }
}
