using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.MyAssets.Scripts.Battle.Core;
using Assets.MyAssets.Scripts.Battle.Data;
using Assets.MyAssets.Scripts.Progression.Run;
using Assets.MyAssets.Scripts.Progression.Save;
using Assets.MyAssets.Scripts.Systems;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Battle.View
{
    /// <summary>영입 선택 결과. 교체가 아니면 ReplacedOut은 null.</summary>
    public readonly struct RecruitResult
    {
        public readonly RunMember Recruited;
        public readonly RunMember ReplacedOut;

        public RecruitResult(RunMember recruited, RunMember replacedOut)
        {
            Recruited = recruited;
            ReplacedOut = replacedOut;
        }
    }

    /// <summary>
    /// 스테이지 클리어 후 성장 선택지를 뽑아 제시하고, 고른 결과를 런 데이터에 넘기는 담당.
    /// 효과를 어떻게 적용할지는 <see cref="RunData.ApplyChoice"/>(로직)가 정하며,
    /// 여기서는 "무엇을 보여주고 무엇을 골랐는지"만 다룬다.
    /// </summary>
    public sealed class RoguelikeRewardService : MonoBehaviour
    {
        [Header("선택지")]
        [Tooltip("승리 시 이 풀에서 가중치에 따라 뽑아 제시한다.")]
        [SerializeField] private RoguelikeChoiceSO[] _choicePool;
        [Tooltip("제시할 선택지 개수.")]
        [SerializeField] private int _choiceCount = 3;
        [Tooltip("파티원 영입 선택지가 뽑을 캐릭터 목록(캐릭터 선택 화면과 같은 로스터).")]
        [SerializeField] private CharacterRosterSO _roster;
        [Tooltip("성향 포인트 1점당 해당 카테고리 등장 가중치에 더해지는 값(밸런싱).")]
        [SerializeField] private float _weightPerPoint = 1f;

        [Header("영입")]
        [Tooltip("영입 선택지를 골랐을 때 제시할 후보 수. 로스터에서 중복 없이 무작위로 뽑는다.")]
        [SerializeField] private int _recruitCandidateCount = 3;

        [Header("연결")]
        [SerializeField] private RoguelikeChoicePanel _panel;

        private const string SkipRecruitTitle = "영입 안 함";
        private const string SkipRecruitDescription = "현재 파티를 유지합니다.";

        private void Awake() => ValidateReferences();

        /// <summary>
        /// 인스펙터 연결 누락 보고한다.
        /// 아래 <see cref="PresentAsync"/>·<see cref="PickChoices"/>는 누락 시 조용히 빈 결과로 넘어가는데,
        /// 그러면 "스테이지를 클리어했는데 아무 일도 안 일어나는" 증상만 남아 원인을 찾기 어렵다.
        /// </summary>
        private void ValidateReferences()
        {
            NullCheck.LogIfMissing(_panel, nameof(_panel), this, "성장 선택지가 표시되지 않습니다");
            NullCheck.LogIfEmpty(_choicePool, nameof(_choicePool), this, "뽑을 선택지가 없습니다");
            NullCheck.LogIfMissing(_roster, nameof(_roster), this, "파티원 영입 선택지가 제외됩니다");
        }

        /// <summary>
        /// 선택지를 제시하고 고른 효과를 런에 적용한다.
        /// 영입 선택지면 이어서 후보 캐릭터를 제시하고 플레이어가 고른 1명을 합류시킨다.
        /// </summary>
        /// <returns>영입 결과(영입 선택지가 아니거나 취소되면 Recruited가 null)</returns>
        public async Task<RecruitResult> PresentAsync(RunData run, IRandom rng, StageScaling scaling, CancellationToken ct)
        {
            if (_panel == null) return default;

            List<RoguelikeChoiceSO> choices = PickChoices(run, rng);
            if (choices.Count == 0) return default;

            RoguelikeChoiceSO picked = await _panel.PresentAsync(choices, ct);
            if (picked == null) return default;

            RoguelikeEffect effect = picked.CreateEffect();
            run.ApplyChoice(effect);
            if (!effect.Recruit) return default;

            return await PresentRecruitAsync(run, rng, scaling, ct);
        }

        /// <summary>
        /// 영입 후보를 제시하고 플레이어가 고른 캐릭터를 파티에 합류시킨다.
        /// 파티가 꽉 찬 경우 "영입 안 함"을 후보에 추가하고, 실제로 영입을 고르면 교체 대상을 이어서 묻는다.
        /// </summary>
        private async Task<RecruitResult> PresentRecruitAsync(RunData run, IRandom rng, StageScaling scaling, CancellationToken ct)
        {
            List<CharacterStatsSO> candidates = PickRecruitCandidates(rng);
            if (candidates.Count == 0) return default;

            bool needsReplace = !run.CanRecruit;
            var cards = candidates.Select(c => new ChoiceCard(c.DisplayName, DescribeCandidate(c, run, scaling), c.Icon)).ToList();
            if (needsReplace)
                cards.Add(new ChoiceCard(SkipRecruitTitle, SkipRecruitDescription));

            int index = await _panel.PresentAsync("동료 영입", cards, ct);
            if (index < 0 || index >= candidates.Count) return default; // 취소 또는 "영입 안 함"

            CharacterStatsSO chosen = candidates[index];
            if (!needsReplace) return new RecruitResult(run.Recruit(chosen, scaling), null);

            RunMember outgoing = await PresentReplaceTargetAsync(run, ct);
            if (outgoing == null) return default;

            RunMember recruited = run.ReplaceMember(outgoing, chosen, scaling);
            return recruited == null ? default : new RecruitResult(recruited, outgoing);
        }

        /// <summary>파티가 꽉 찼을 때 누구를 내보낼지 현재 파티원 카드로 묻는다.</summary>
        private async Task<RunMember> PresentReplaceTargetAsync(RunData run, CancellationToken ct)
        {
            var cards = run.Members.Select(m => new ChoiceCard(m.DisplayName, DescribeMember(m))).ToList();
            int index = await _panel.PresentAsync("교체할 파티원 선택", cards, ct);
            return index < 0 || index >= run.Members.Count ? null : run.Members[index];
        }

        /// <summary>로스터에서 중복 없이 무작위로 후보를 뽑는다(가중치 없음 — 균등).</summary>
        private List<CharacterStatsSO> PickRecruitCandidates(IRandom rng)
        {
            var pool = new List<CharacterStatsSO>();
            if (_roster == null) return pool;

            for (int i = 0; i < _roster.Count; i++)
            {
                if (_roster[i] != null) pool.Add(_roster[i]);
            }

            var picked = new List<CharacterStatsSO>();
            while (picked.Count < _recruitCandidateCount && pool.Count > 0)
            {
                int i = rng.Range(0, pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }

        /// <summary>
        /// 후보 카드에 표시할 스탯 요약. SO 기준값이 아니라 "지금 영입하면 실제로 갖게 될" 스탯을 보여줘야
        /// 카드와 합류 결과가 어긋나지 않고, 교체 대상(성장 누적된 파티원)과도 같은 기준으로 비교된다.
        /// 새 파티원은 최대치로 합류하므로 현재 HP = 최대 HP.
        /// </summary>
        private static string DescribeCandidate(CharacterStatsSO character, RunData run, in StageScaling scaling)
        {
            Stats s = run.PreviewRecruitStats(character, scaling);
            return DescribeStats(s, s.MaxHp);
        }

        /// <summary>교체 대상 카드에 표시할 현재 파티원 상태 요약.</summary>
        private static string DescribeMember(RunMember m) => DescribeStats(m.Stats, m.CurrentHp);

        /// <summary>영입 후보와 교체 대상이 같은 형식으로 비교되도록 스탯 표기를 한 곳에서 만든다.</summary>
        private static string DescribeStats(Stats s, int currentHp) =>
            $"HP {currentHp}/{s.MaxHp}\nATK {s.Atk}\nSPD {s.Spd}\nDEF {s.Def}";

        /// <summary>
        /// 선택지 풀에서 중복 없이 뽑는다(SO의 가중치 반영).
        /// 로스터가 없으면 영입 선택지는 후보에서 제외한다. 파티가 꽉 찬 경우에도 영입 선택지는
        /// 후보에 남겨두고(교체 여부를 플레이어가 고르게 함), 대신 가중치가 빈자리 기준으로 낮게 유지된다.
        /// </summary>
        private List<RoguelikeChoiceSO> PickChoices(RunData run, IRandom rng)
        {
            bool allowRecruit = _roster != null && _roster.Count > 0;

            var pool = _choicePool == null
                ? new List<RoguelikeChoiceSO>()
                : _choicePool.Where(c => c != null && (allowRecruit || !c.RequiresPartySlot)).ToList();

            int emptySlots = RunData.MaxPartySize - run.Members.Count;
            List<float> weights = pool.Select(c =>
                c.GetWeight(emptySlots) + SaveService.Current.GetPoints(c.Category) * _weightPerPoint).ToList();

            return WeightedPicker.PickDistinct(weights, _choiceCount, rng)
                                 .Select(i => pool[i])
                                 .ToList();
        }

#if UNITY_EDITOR
        private void OnValidate() => ValidateReferences();
#endif
    }
}
