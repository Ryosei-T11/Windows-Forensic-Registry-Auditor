using System;

namespace ForensicAuditor.Core.StateMachine
{
    public class StateTransitionEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public SystemState PreviousState { get; set; }
        public SystemState NewState { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}