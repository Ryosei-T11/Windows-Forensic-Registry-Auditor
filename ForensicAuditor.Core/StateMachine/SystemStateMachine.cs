using System;
using ForensicAuditor.Core.Models;

namespace ForensicAuditor.Core.StateMachine
{
    /// Mengelola transisi status keamanan sistem secara thread-safe menggunakan State Machine.
    /// Catatan: Enum SystemState diimpor langsung dari SystemState.cs untuk menghindari duplikasi tipe.
    public class SystemStateMachine
    {
        private readonly object _lock = new();

        public SystemState CurrentState { get; private set; } = SystemState.Safe;

        // Event yang dipicu ketika terjadi perubahan status sistem
        public event Action<SystemState, SystemState, string>? OnStateChanged;

        /// Mengubah status sistem berdasarkan analisis heuristik real-time.
        public void TransitionTo(SystemState newState, string reason)
        {
            lock (_lock)
            {
                if (CurrentState == newState) return;

                // Validasi transisi yang sah (State Guard)
                bool isValidTransition = ValidateTransition(CurrentState, newState);

                if (isValidTransition)
                {
                    SystemState oldState = CurrentState;
                    CurrentState = newState;
                    OnStateChanged?.Invoke(oldState, newState, reason);
                }
            }
        }

        private static bool ValidateTransition(SystemState current, SystemState target)
        {
            return current switch
            {
                SystemState.Safe => target == SystemState.Suspicious || target == SystemState.UnderAttack,
                SystemState.Suspicious => target == SystemState.Safe || target == SystemState.UnderAttack || target == SystemState.Mitigating,
                SystemState.UnderAttack => target == SystemState.Mitigating || target == SystemState.Compromised,
                SystemState.Mitigating => target == SystemState.Safe || target == SystemState.Compromised,
                SystemState.Compromised => target == SystemState.Mitigating || target == SystemState.Safe,
                _ => false
            };
        }

        /// Evaluasi otomatis skor risiko untuk memperbarui state sistem.
        public void EvaluateRiskScore(double aggregateRiskScore, string triggerRule)
        {
            if (aggregateRiskScore >= 8.0)
            {
                TransitionTo(SystemState.UnderAttack, $"Skor risiko kritis ({aggregateRiskScore:F1}) dipicu oleh aturan: {triggerRule}");
            }
            else if (aggregateRiskScore >= 4.0)
            {
                TransitionTo(SystemState.Suspicious, $"Skor risiko sedang ({aggregateRiskScore:F1}) dipicu oleh aturan: {triggerRule}");
            }
            else
            {
                if (CurrentState == SystemState.Suspicious || CurrentState == SystemState.UnderAttack)
                {
                    TransitionTo(SystemState.Safe, "Skor risiko kembali di bawah ambang batas bahaya.");
                }
            }
        }
    }
}