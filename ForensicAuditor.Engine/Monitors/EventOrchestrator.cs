using System;
using ForensicAuditor.Core.Models;
using ForensicAuditor.Core.StateMachine;
using ForensicAuditor.Engine.Heuristics;

namespace ForensicAuditor.Engine.Monitors
{
    public class EventOrchestrator
    {
        private readonly RuleEvaluator _ruleEvaluator;
        private readonly BehaviorScorer _scorer;
        private readonly SystemStateMachine _stateMachine;

        public event Action<RegistryEvent>? OnOrchestratedEvent;

        public EventOrchestrator(RuleEvaluator ruleEvaluator, BehaviorScorer scorer, SystemStateMachine stateMachine)
        {
            _ruleEvaluator = ruleEvaluator;
            _scorer = scorer;
            _stateMachine = stateMachine;
        }

        public void ProcessRawRegistryEvent(RegistryEvent rawEvent)
        {
            var evaluated = _ruleEvaluator.EvaluateRegistry(rawEvent);
            evaluated.RiskScore = _scorer.AdjustScore(evaluated);
            _stateMachine.EvaluateRiskScore(evaluated.RiskScore, evaluated.DetectionRule);
            OnOrchestratedEvent?.Invoke(evaluated);
        }
    }
}
