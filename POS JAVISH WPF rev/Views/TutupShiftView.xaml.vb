Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Services
Imports System.Text.RegularExpressions

Namespace Views
    Partial Public Class TutupShiftView
        Inherits UserControl

        Private _currentShift As Shift

        Public Sub New()
            InitializeComponent()
            LoadShiftData()
        End Sub

        Private Sub NumberValidationTextBox(sender As Object, e As TextCompositionEventArgs)
            Dim regex As New Regex("[^0-9]+")
            e.Handled = regex.IsMatch(e.Text)
        End Sub

        Private Async Sub LoadShiftData()
            If Session.CurrentShiftId = 0 Then
                MessageBox.Show("Tidak ada shift aktif yang ditemukan.", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            Using db As New AppDbContext()
                _currentShift = Await db.Shifts.FindAsync(Session.CurrentShiftId)
                If _currentShift IsNot Nothing Then
                    ModalAwalText.Text = _currentShift.ModalAwal.ToString("C0")
                    TotalPenjualanText.Text = _currentShift.TotalPenjualan.ToString("C0")
                    TotalCashText.Text = _currentShift.TotalCash.ToString("C0")
                    
                    Dim expectedCash = _currentShift.ModalAwal + _currentShift.TotalCash
                    ExpectedCashText.Text = expectedCash.ToString("C0")
                    
                    UpdateSelisih(0)
                End If
            End Using
        End Sub

        Private Sub UangFisikBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            Dim uangFisik As Decimal = 0
            If Decimal.TryParse(UangFisikBox.Text, uangFisik) Then
                UpdateSelisih(uangFisik)
            End If
        End Sub

        Private Sub UpdateSelisih(uangFisik As Decimal)
            If _currentShift Is Nothing Then Return

            Dim expectedCash = _currentShift.ModalAwal + _currentShift.TotalCash
            Dim selisih = uangFisik - expectedCash
            
            SelisihText.Text = selisih.ToString("C0")
            
            If selisih = 0 Then
                SelisihBorder.Background = New System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)) ' Green
                SelisihStatusText.Text = "OK"
            ElseIf selisih < 0 Then
                SelisihBorder.Background = New System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)) ' Red
                SelisihStatusText.Text = "MINUS"
            Else
                SelisihBorder.Background = New System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(234, 179, 8)) ' Yellow
                SelisihStatusText.Text = "LEBIH"
            End If
        End Sub

        Private Async Sub BtnTutupShift_Click(sender As Object, e As RoutedEventArgs)
            Dim uangFisik As Decimal = 0
            If Not Decimal.TryParse(UangFisikBox.Text, uangFisik) Then
                MessageBox.Show("Uang fisik tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            If MessageBox.Show("Apakah Anda yakin ingin menutup shift sekarang?", "Konfirmasi Tutup Shift", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        Dim shift = Await db.Shifts.FindAsync(Session.CurrentShiftId)
                        If shift IsNot Nothing Then
                            Dim expectedCash = shift.ModalAwal + shift.TotalCash
                            shift.UangFisik = uangFisik
                            shift.Selisih = uangFisik - expectedCash
                            shift.ShiftEnd = DateTime.Now
                            shift.Status = "CLOSED"
                            
                            ' Log activity
                            db.ActivityLog.Add(New ActivityLog With {
                                .UserId = Session.CurrentUser.Id,
                                .Username = Session.CurrentUser.Username,
                                .Activity = "CLOSE_SHIFT",
                                .Timestamp = DateTime.Now,
                                .Detail = $"Tutup shift. Fisik: {uangFisik:C}, Selisih: {shift.Selisih:C}"
                            })
                            
                            Await db.SaveChangesAsync()
                            
                            ' Print Report (Bonus Alfamart Level)
                            Try
                                PrintService.PrintShiftReport(shift)
                                MessageBox.Show("Laporan shift dicetak.", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                            Catch ex As Exception
                                MessageBox.Show($"Gagal mencetak laporan: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                            End Try

                            MessageBox.Show("Shift berhasil ditutup.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                            
                            Session.CurrentShiftId = 0
                            ' Refresh navigation or go back to dashboard
                            ' ...
                        End If
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Gagal menutup shift: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace