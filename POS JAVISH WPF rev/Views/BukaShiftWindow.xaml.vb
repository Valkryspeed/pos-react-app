Imports System.Windows
Imports System.Windows.Input
Imports System.Text.RegularExpressions
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class BukaShiftWindow
        Inherits Window

        Public Sub New()
            InitializeComponent()
            ModalAwalBox.Focus()
        End Sub

        Private Sub NumberValidationTextBox(sender As Object, e As TextCompositionEventArgs)
            Dim regex As New Regex("[^0-9]+")
            e.Handled = regex.IsMatch(e.Text)
        End Sub

        Private Sub BtnBukaShift_Click(sender As Object, e As RoutedEventArgs)
            Dim modalAwal As Decimal = 0
            If Not Decimal.TryParse(ModalAwalBox.Text, modalAwal) Then
                MessageBox.Show("Modal awal tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            Try
                Using db As New AppDbContext()
                    Dim newShift As New Shift With {
                        .UserId = Session.CurrentUser.Id,
                        .ShiftStart = DateTime.Now,
                        .ModalAwal = modalAwal,
                        .TotalPenjualan = 0,
                        .TotalCash = 0,
                        .UangFisik = 0,
                        .Selisih = 0,
                        .Status = "OPEN"
                    }
                    db.Shifts.Add(newShift)
                    db.SaveChanges()
                    
                    Session.CurrentShiftId = newShift.Id
                    
                    ' Log activity
                    db.ActivityLog.Add(New ActivityLog With {
                        .UserId = Session.CurrentUser.Id,
                        .Username = Session.CurrentUser.Username,
                        .Activity = "OPEN_SHIFT",
                        .Timestamp = DateTime.Now,
                        .Detail = $"Buka shift dengan modal awal: {modalAwal:C}"
                    })
                    db.SaveChanges()
                    
                    Me.DialogResult = True
                    Me.Close()
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal membuka shift: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace