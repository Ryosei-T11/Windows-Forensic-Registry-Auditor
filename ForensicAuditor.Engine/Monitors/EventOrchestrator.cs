using System;
using Serilog;
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
            Log.Information("Processing raw registry event {EventId} Correlation={Correlation} for {Hive} {Key}", rawEvent.EventId, rawEvent.CorrelationId, rawEvent.Hive, rawEvent.SubKeyPath);
            var evaluated = _ruleEvaluator.EvaluateRegistry(rawEvent);
            evaluated.RiskScore = _scorer.AdjustScore(evaluated);
            Log.Information("Evaluated event {EventId} Correlation={Correlation}: Rule={Rule}, RiskScore={Score}", evaluated.EventId, evaluated.CorrelationId, evaluated.DetectionRule, evaluated.RiskScore);
            _stateMachine.EvaluateRiskScore(evaluated.RiskScore, evaluated.DetectionRule);
            Log.Debug("State machine evaluated risk -> state updated if applicable");
            OnOrchestratedEvent?.Invoke(evaluated);
        }
    }
}
