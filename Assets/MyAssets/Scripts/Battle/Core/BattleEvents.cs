using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    public abstract class PlaybackEventArgs : EventArgs
    {
        private List<Task> _tasks;

        public void RegisterPlayback(Task playback)
        {
            if (playback == null)
            {
                return;
            }

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

    public sealed class ActorTurnEventArgs : EventArgs
    {
        public readonly Unit Actor;
        public ActorTurnEventArgs(Unit actor) => Actor = actor;
    }

    public sealed class ActionResolvedEventArgs : PlaybackEventArgs
    {
        public readonly ActionResult Result;
        public ActionResolvedEventArgs(ActionResult result) => Result = result;
    }

    public sealed class UnitDiedEventArgs : PlaybackEventArgs
    {
        public readonly Unit Unit;
        public UnitDiedEventArgs(Unit unit) => Unit = unit;
    }

    /// <summary>상태이상이 변화했을 때 발생하는 이벤트.</summary>
    public sealed class StatusChangedEventArgs : EventArgs
    {
        public readonly Unit Unit;


        public readonly StatusKind? Kind; // 어떤 상태이상에 대한 변화인지

        /// <summary>
        /// <see cref="StatusChangeReason.Ticked"/>는
        /// "이 유닛의 상태가 한 턴 진행됐다"는 유닛 단위 알림이라 종류가 없다(null).
        /// </summary>
        public readonly StatusChangeReason Reason;

        public StatusChangedEventArgs(Unit unit, StatusKind? kind, StatusChangeReason reason)
        {
            Unit = unit;
            Kind = kind;
            Reason = reason;
        }
    }

    /// <summary>도트 피해 이벤트(자기 차례 시작 시). 피격 연출을 기다린다.</summary>
    public sealed class StatusTickedEventArgs : PlaybackEventArgs
    {
        public readonly Unit Unit;

        /// <summary>계산된 도트 피해량.</summary>
        public readonly int Damage;

        public StatusTickedEventArgs(Unit unit, int damage)
        {
            Unit = unit;
            Damage = damage;
        }
    }

    public sealed class BattleEndedEventArgs : EventArgs
    {
        public readonly BattleOutcome Outcome;
        public readonly IReadOnlyList<Unit> Survivors;

        public BattleEndedEventArgs(BattleOutcome outcome, IReadOnlyList<Unit> survivors)
        {
            Outcome = outcome;
            Survivors = survivors;
        }
    }
}
