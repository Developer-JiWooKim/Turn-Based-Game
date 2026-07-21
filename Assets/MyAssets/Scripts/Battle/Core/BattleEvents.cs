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
