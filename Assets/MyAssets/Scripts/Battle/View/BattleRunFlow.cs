using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>
    /// 런의 경계(어디서 파티를 받아오고, 런이 끝나면 무엇을 하는가)를 담당한다.
    ///
    /// <see cref="BattleDirector"/>에서 떼어낸 이유: 전투 오케스트레이터가 세이브·씬 전환·타이틀 씬 이름까지
    /// 알 필요는 없다. Director는 "한 스테이지를 어떻게 싸우는가"에, 이쪽은 "런이 어떻게 시작하고 끝나는가"에 집중한다.
    /// </summary>
    public sealed class BattleRunFlow : MonoBehaviour
    {
        /// <summary>런이 끝나면 되돌아갈 타이틀/캐릭터 선택 씬</summary>
        private const string IntroSceneName = "IntroScene";

        [Header("Test Party SO")]
        [Tooltip("Title→캐릭터 선택을 거치지 않고 BattleScene을 직접 플레이할 때 쓰는 테스트 파티(폴백)")]
        [SerializeField] private CharacterStatsSO[] _testParty;

        [Header("결과 UI Panel")]
        [Tooltip("런 종료 시 도달 스테이지를 보여주는 결과 팝업(UI Tool Kit). 비워두면 바로 씬을 전환한다.")]
        [SerializeField] private BattleResultPanel _resultPanel;

        /// <summary>런 데이터를 가져오고, 없으면(BattleScene 직접 플레이) 테스트 파티로 임시 런을 만든다.</summary>
        public RunData ResolveRun()
        {
            RunData run = GameManager.Instance != null ? GameManager.Instance.CurrentRun : null;
            if (run != null && run.Members.Count > 0)
            {
                return run;
            }

            if (_testParty == null || _testParty.Length == 0)
            {
                Debug.LogError("[BattleRunFlow] 테스트 파티 데이터가 없습니다.");
                return null;
            }

            Debug.Log("[BattleRunFlow] RunData가 없어 인스펙터 테스트 파티로 진행합니다.");
            var fallback = new RunData(_testParty[0]);
            for (int i = 1; i < _testParty.Length; i++)
            {
                fallback.AddMember(_testParty[i]);
            }

            return fallback;
        }

        /// <summary>
        /// 런이 끝났을 때(전멸 또는 배틀 중단) 기록을 저장하고 결과를 보여준 뒤 타이틀로 돌아간다.
        /// </summary>
        public async Task EndRunAsync(RunData run, CancellationToken ct)
        {
            AudioManager.Sfx(AudioManager.Library?.DefeatStinger);

            int reachedStage = run.CurrentStage;
            Debug.Log($"[BattleRunFlow] 런 종료 — {reachedStage}스테이지에서 리타이어");

            // 패널에는 이번 런 이전까지의 기록을 보여줘야 하므로 저장(RecordStage)보다 먼저 읽어둔다.
            // 신기록 여부는 저장 계층의 판정 결과를 그대로 쓴다(동점은 신기록이 아님).
            int previousBest = SaveService.Current.BestStage;
            bool isNewRecord = SaveService.RecordStage(reachedStage);

            if (_resultPanel != null)
            {
                await _resultPanel.PresentAsync(reachedStage, previousBest, isNewRecord, ct);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(IntroSceneName);
            }
            else
            {
                Debug.Log("[BattleRunFlow] GameManager가 없어 씬 전환을 건너뜀(BattleScene 직접 실행)");
            }
        }
    }
}
