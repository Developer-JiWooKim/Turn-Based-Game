using System.Collections.Generic;
using Assets.MyAssets.Scripts.Battle.Data;

namespace Assets.MyAssets.Scripts.Run
{
    /// <summary>
    /// 한 번의 런(도전) 동안 유지되는 세션 데이터
    /// 매 런 시작 시 새로 만들고, 전멸(리타이어) 시 폐기
    /// 로그라이크 무한 타워의 런 단위 상태(파티, 진행 스테이지, 누적 성장) 담고 있음
    /// </summary>
    public sealed class RunData
    {
        public readonly List<CharacterStatsSO> Party = new(); // 현재 파티(1명으로 시작, 로그라이크 영입으로 최대 4명)

        public int CurrentStage = 1;// 현재 도전 중인 스테이지(1부터 시작)

        public RunData(CharacterStatsSO starter)
        {
            if (starter != null)
                Party.Add(starter);
        }
    }
}
