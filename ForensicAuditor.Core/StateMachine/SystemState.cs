namespace ForensicAuditor.Core.StateMachine
{
    public enum SystemState
    {
        Safe,
        Suspicious,
        UnderAttack,
        Mitigating,
        Compromised
    }
}
