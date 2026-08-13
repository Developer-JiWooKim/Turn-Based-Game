using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.MyAssets.Scripts.Battle.Core
{
    /// <summary>
    /// 결과가 들어올 때까지 기다리는 한 번짜리 신호.
    ///
    /// 전투 흐름 곳곳에서 같은 패턴이 반복된다 — 플레이어 타겟 입력, 선택지 카드 클릭,
    /// 결과 화면 확인, 퍼즈 해제. 전부 "TCS 생성 → 취소 토큰 연결 → await → 정리"라
    /// 매번 <see cref="TaskCompletionSource{TResult}"/>를 직접 다루면 다음 두 가지를 빠뜨리기 쉽다.
    ///  - <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>
    ///    (없으면 완료를 알리는 쪽 스레드에서 이어지는 코드가 그대로 실행된다)
    ///  - <c>ct.Register</c> 해제 (없으면 토큰에 콜백이 쌓인다)
    /// </summary>
    /// <typeparam name="T">기다릴 결과 타입.</typeparam>
    public sealed class PendingSignal<T>
    {
        private TaskCompletionSource<T> _pending;

        /// <summary>지금 결과를 기다리는 중인지.</summary>
        public bool IsWaiting => _pending != null;

        /// <summary>
        /// 결과가 들어올 때까지 대기. 
        /// 취소되면 <see cref="OperationCanceledException"/>.
        /// 이미 대기 중이면 이전 대기는 취소된다(같은 신호를 두 번 열지 않도록).
        /// </summary>
        public async Task<T> WaitAsync(CancellationToken ct)
        {
            _pending?.TrySetCanceled();

            var pending = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = pending;

            try
            {
                using (ct.Register(() => pending.TrySetCanceled(ct)))
                {
                    return await pending.Task;
                }
            }
            finally
            {
                // 이 대기가 아직 현재 것일 때만 지운다 — 새 대기가 시작됐다면 그쪽을 남겨야 한다.
                if (_pending == pending)
                {
                    _pending = null;
                }
            }
        }

        /// <summary>결과를 넣어 대기를 끝낸다. 대기 중이 아니면 무시된다.</summary>
        public bool Complete(T result) => _pending?.TrySetResult(result) ?? false;

        /// <summary>대기를 취소로 끝낸다. 대기 중이 아니면 무시된다.</summary>
        public bool Cancel() => _pending?.TrySetCanceled() ?? false;
    }
}
