using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using ForensicAuditor.Core.Models;
using ForensicAuditor.UI.Dashboard.ViewModels;

namespace ForensicAuditor.UI.Dashboard.Views
{
    public partial class RealTimeLogView : UserControl
    {
        public RealTimeLogView()
        {
            InitializeComponent();
        }

        /// Logika evaluasi penyaringan dan pencarian log dinamis.
        /// Menggunakan ICollectionView agar sinkronisasi data real-time tetap efisien tanpa memblokir UI thread.
        private void ApplyFilter()
        {
            object source = LogDataGrid?.ItemsSource;
            if (source == null && DataContext is MainViewModel vm)
            {
                source = vm.Alerts;
            }

            if (source == null) return;

            ICollectionView view = CollectionViewSource.GetDefaultView(source);
            if (view == null) return;

            string searchText = SearchTextBox?.Text?.Trim().ToLower() ?? "";
            int severityIndex = SeverityComboBox?.SelectedIndex ?? 0;

            view.Filter = (obj) =>
            {
                if (obj is RegistryEvent ev)
                {
                    bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                         (ev.DetectionRule != null && ev.DetectionRule.ToLower().Contains(searchText)) ||
                                         (ev.ProcessName != null && ev.ProcessName.ToLower().Contains(searchText)) ||
                                         (ev.Sha256Hash != null && ev.Sha256Hash.ToLower().Contains(searchText)) ||
                                         (ev.SubKeyPath != null && ev.SubKeyPath.ToLower().Contains(searchText)) ||
                                         (ev.ValueName != null && ev.ValueName.ToLower().Contains(searchText)) ||
                                         (ev.ReputationResult != null && ev.ReputationResult.ToLower().Contains(searchText));

                    if (!matchesSearch) return false;

                    switch (severityIndex)
                    {
                        case 1: // Kritis (Skor >= 8.0)
                            return ev.RiskScore >= 8.0;
                        case 2: // Waspada (Skor 4.0 - 7.9)
                            return ev.RiskScore >= 4.0 && ev.RiskScore < 8.0;
                        case 3: // Aman / Info (Skor < 4.0)
                            return ev.RiskScore < 4.0;
                        default: // Semua Tingkat Bahaya
                            return true;
                    }
                }
                return false;
            };

            view.Refresh();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null) SearchTextBox.Text = string.Empty;
            if (SeverityComboBox != null) SeverityComboBox.SelectedIndex = 0;
            ApplyFilter();
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ApplyFilter();
        }

        /// Mengekspor daftar log yang tampil pada DataGrid (mendukung filter aktif) ke format CSV atau JSON.
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var items = LogDataGrid?.Items;
            if (items == null || items.Count == 0)
            {
                MessageBox.Show(
                    "Tidak ada data log yang tersedia untuk diekspor saat ini.",
                    "Ekspor Laporan Forensik",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Ekspor Laporan Audit Forensik",
                Filter = "CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json",
                FileName = $"Forensic_Audit_Report_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    if (extension == ".json")
                    {
                        ExportToJson(filePath, items);
                    }
                    else
                    {
                        ExportToCsv(filePath, items);
                    }

                    MessageBox.Show(
                        $"Laporan audit forensik berhasil disimpan ke:\n{filePath}",
                        "Ekspor Berhasil",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Gagal mengekspor laporan:\n{ex.Message}",
                        "Kesalahan Ekspor",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ExportToCsv(string filePath, ItemCollection items)
        {
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("Timestamp,Aturan Deteksi,Nama Proses,SHA-256 Hash,Reputasi Cloud,Jalur Registri,Skor Risiko");

            foreach (var item in items)
            {
                if (item is RegistryEvent ev)
                {
                    string timestamp = ev.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    string rule = EscapeCsvField(ev.DetectionRule);
                    string process = EscapeCsvField(ev.ProcessName);
                    string hash = EscapeCsvField(ev.Sha256Hash);
                    string reputation = EscapeCsvField(ev.ReputationResult ?? "N/A");
                    string path = EscapeCsvField(ev.SubKeyPath);
                    string score = ev.RiskScore.ToString("F1");

                    csvContent.AppendLine($"{timestamp},{rule},{process},{hash},{reputation},{path},{score}");
                }
            }

            File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
        }

        private void ExportToJson(string filePath, ItemCollection items)
        {
            var list = new System.Collections.Generic.List<RegistryEvent>();
            foreach (var item in items)
            {
                if (item is RegistryEvent ev)
                {
                    list.Add(ev);
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(list, options);
            File.WriteAllText(filePath, jsonString, Encoding.UTF8);
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        // HANDLER MITIGASI MANUAL & DETEKSI INVESTIGASI

        /// Menghentikan paksa proses berbahaya (Process Kill).
        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            if (DetailPanel.DataContext is RegistryEvent ev)
            {
                var confirmResult = MessageBox.Show(
                    $"Apakah Anda yakin ingin mematikan paksa proses berikut?\n\n" +
                    $"PID: {ev.ProcessId}\n" +
                    $"Nama: {ev.ProcessName}\n" +
                    $"Path: {ev.ProcessPath}",
                    "Konfirmasi Mitigasi Proses",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        var quarantine = new ForensicAuditor.Infrastructure.Mitigation.ProcessQuarantine();
                        quarantine.TerminateProcess(ev.ProcessId);

                        // Perbarui data model di tempat
                        ev.ReputationResult = "QUARANTINED (Terminated)";
                        ev.RiskScore = 0.0;
                        ev.Severity = SeverityLevel.Informational;

                        MessageBox.Show(
                            "Proses berhasil dihentikan secara permanen.",
                            "Karantina Sukses",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        LogDataGrid.Items.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Gagal menghentikan proses: {ex.Message}",
                            "Kesalahan Mitigasi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        /// Mengembalikan nilai registry asal sebelum modifikasi (Registry Rollback).
        private void RollbackRegistry_Click(object sender, RoutedEventArgs e)
        {
            if (DetailPanel.DataContext is RegistryEvent ev)
            {
                var confirmResult = MessageBox.Show(
                    $"Apakah Anda yakin ingin membatalkan perubahan dan melakukan Rollback pada Registry berikut?\n\n" +
                    $"Jalur: {ev.SubKeyPath}\n" +
                    $"Value Name: {ev.ValueName}",
                    "Konfirmasi Rollback Registry",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        var rollback = new ForensicAuditor.Infrastructure.Mitigation.RegistryRollbackEngine();
                        bool success = rollback.PerformRollback(ev);

                        if (success)
                        {
                            ev.DetectionRule = $"{ev.DetectionRule} [ROLLED BACK]";
                            ev.RiskScore = 0.0;
                            ev.Severity = SeverityLevel.Informational;

                            MessageBox.Show(
                                "Nilai Registry berhasil dikembalikan ke kondisi asal.",
                                "Rollback Sukses",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "Gagal melakukan rollback. Kunci target mungkin tidak dapat diakses atau nilai sudah tidak ada.",
                                "Kegagalan Rollback",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }

                        LogDataGrid.Items.Refresh();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Terjadi kesalahan saat rollback: {ex.Message}",
                            "Kesalahan Mitigasi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        /// Menandai aktivitas aman dan menormalkan kembali status deteksi.
        private void MarkSafe_Click(object sender, RoutedEventArgs e)
        {
            if (DetailPanel.DataContext is RegistryEvent ev)
            {
                ev.RiskScore = 0.0;
                ev.Severity = SeverityLevel.Informational;
                ev.ReputationResult = "Approved (Manual Whitelist)";
                ev.DetectionRule = $"{ev.DetectionRule} (Aman)";

                MessageBox.Show(
                    "Aktivitas berhasil ditandai sebagai aman. Skor risiko dinetralkan.",
                    "Sistem Diperbarui",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LogDataGrid.Items.Refresh();
            }
        }
    }
}