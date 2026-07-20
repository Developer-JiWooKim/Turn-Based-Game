using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 연출 대기를 지원하는 이벤트 인자의 베이스. View는 핸들러 안에서 자신의 애니메이션 Task를
    /// <see cref="RegisterPlayback"/>로 등록하고, 시뮬레이션은 등록된 모든 Task가 끝날 때까지 진행을 멈춘다.
    /// 이렇게 Core(로직 즉시 계산)와 View(연출 시간) 사이의 순서를 이벤트만으로 동기화한다.
    /// </summary>
    public abstract class PlaybackEventArgs : EventArgs
    {
        private List<Task> _tasks;

        public void RegisterPlayback(Task playback)
        {
            if (playback == null) return;
            (_tasks ??= new List<Task>()).Add(playback);
        }

        public Task WhenPlaybackComplete() =>
            _tasks == null || _tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(_tasks);
    }

    public sealed class TurnStartedEventArgs : EventArgs
    {
        public readonly int TurnNumber;
        /// <summary>이번 턴의 SPD 기준 행동 순서.</summary>
        public readonly IReadOnlyList<Unit> Order;

        public TurnStartedEventArgs(int turnNumber, IReadOnlyList<Unit> order)
        {
            TurnNumber = turnNumber;
            Order = order;
        }
    }

    public sealed class TurnEndedEventArgs : EventArgs
    {
        public readonly int TurnNumber;
        public TurnEndedEventArgs(int turnNumber) => TurnNumber = turnNumber;
    }

    /// <summary>한 유닛의 행동 차례가 시작됨. HUD가 현재 행동 유닛을 표시하는 데 쓴다.</summary>
    public sealed class ActorTurnEventArgs : EventArgs
    {
        public readonly Unit Actor;
        public ActorTurnEventArgs(Unit actor) => Actor = actor;
    }

    /// <summary>한 행동이 해소됨(데미지 적용 완료). View는 공격/피격 애니메이션을 재생한다.</summary>
    public sealed class ActionResolvedEventArgs : PlaybackEventArgs
    {
        public readonly ActionResult Result;
        public ActionResolvedEventArgs(ActionResult result) => Result = result;
    }

    /// <summary>유닛 사망. View는 사망 애니메이션을 재생한다. (파티 영구 추방 등 런 단위 처리는 상위에서.)</summary>
    public sealed class UnitDiedEventArgs : PlaybackEventArgs
    {
        public readonly Unit Unit;
        public UnitDiedEventArgs(Unit unit) => Unit = unit;
    }

    public sealed class BattleEndedEventArgs : EventArgs
    {
        public readonly BattleOutcome Outcome;
        /// <summary>전투 종료 시점에 살아남은 아군(다음 스테이지로 이어짐).</summary>
        public readonly IReadOnlyList<Unit> Survivors;

        public BattleEndedEventArgs(BattleOutcome outcome, IReadOnlyList<Unit> survivors)
        {
            Outcome = outcome;
            Survivors = survivors;
        }
    }
}
