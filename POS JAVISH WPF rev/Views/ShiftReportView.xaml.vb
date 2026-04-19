Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Services
Imports System.IO

Namespace Views
    Partial Public Class ShiftReportView
        Inherits UserControl

        Public Class ShiftReportViewModel
            Public Property Id As Integer
            Public Property UserId As Integer
            Public Property Username As String
            Public Property ShiftStart As Date
            Public Property ShiftEnd As Date?
            Public Property ModalAwal As Decimal
            Public Property TotalPenjualan As Decimal
            Public Property TotalCash As Decimal
            Public Property UangFisik As Decimal
            Public Property Selisih As Decimal
            Public Property Status As String
            Public ReadOnly Property IsMinus As Boolean
                Get
                    Return Selisih < 0
                End Get
            End Property
            Public ReadOnly Property IsPlus As Boolean
                Get
                    Return Selisih > 0
                End Get
            End Property
            Public ReadOnly Property IsOk As Boolean
                Get
                    Return Selisih = 0
                End Get
            End Property
        End Class

        Public Sub New()
            InitializeComponent()
            StartDatePicker.SelectedDate = DateTime.Now.Date
            EndDatePicker.SelectedDate = DateTime.Now.Date
            LoadUsers()
            LoadReport()
        End Sub

        Private Async Sub LoadUsers()
            Using db As New AppDbContext()
                Dim users = Await db.Users.AsNoTracking().ToListAsync()
                Dim allUser = New User With {.Id = 0, .Username = "-- Semua Kasir --"}
                users.Insert(0, allUser)
                UserCombo.ItemsSource = users
                UserCombo.SelectedIndex = 0
            End Using
        End Sub

        Private Async Sub LoadReport()
            Dim startD = StartDatePicker.SelectedDate.GetValueOrDefault().Date
            Dim endD = EndDatePicker.SelectedDate.GetValueOrDefault().Date.AddDays(1).AddSeconds(-1)
            Dim selectedUserId = If(UserCombo.SelectedValue IsNot Nothing, CInt(UserCombo.SelectedValue), 0)

            Using db As New AppDbContext()
                Dim query = db.Shifts.AsNoTracking().
                    Where(Function(s) s.ShiftStart >= startD AndAlso s.ShiftStart <= endD)

                If selectedUserId > 0 Then
                    query = query.Where(Function(s) s.UserId = selectedUserId)
                End If

                Dim shifts = Await query.OrderByDescending(Function(s) s.ShiftStart).ToListAsync()
                Dim users = Await db.Users.AsNoTracking().ToDictionaryAsync(Function(u) u.Id, Function(u) u.Username)

                Dim viewModels = shifts.Select(Function(s)
                                                   Dim username As String = "Unknown"
                                                   users.TryGetValue(s.UserId, username)

                                                   Return New ShiftReportViewModel With {
                                                       .Id = s.Id,
                                                       .UserId = s.UserId,
                                                       .Username = username,
                                                       .ShiftStart = s.ShiftStart,
                                                       .ShiftEnd = s.ShiftEnd,
                                                       .ModalAwal = s.ModalAwal,
                                                       .TotalPenjualan = s.TotalPenjualan,
                                                       .TotalCash = s.TotalCash,
                                                       .UangFisik = s.UangFisik,
                                                       .Selisih = s.Selisih,
                                                       .Status = s.Status
                                                   }
                                               End Function).ToList()

                ShiftGrid.ItemsSource = viewModels
            End Using
        End Sub

        Private Sub BtnFilter_Click(sender As Object, e As RoutedEventArgs)
            LoadReport()
        End Sub

        Private Sub BtnExport_Click(sender As Object, e As RoutedEventArgs)
            Dim items = TryCast(ShiftGrid.ItemsSource, List(Of ShiftReportViewModel))
            If items Is Nothing OrElse items.Count = 0 Then Return

            Dim sfd As New Microsoft.Win32.SaveFileDialog With {
                .Filter = "CSV File (*.csv)|*.csv",
                .FileName = $"ShiftReport_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            }

            If sfd.ShowDialog() = True Then
                Try
                    Using sw As New StreamWriter(sfd.FileName)
                        sw.WriteLine("Mulai;Selesai;Kasir;Modal;Penjualan;Cash;Fisik;Selisih;Status")
                        For Each it In items
                            sw.WriteLine($"{it.ShiftStart:dd/MM/yyyy HH:mm};{it.ShiftEnd:dd/MM/yyyy HH:mm};{it.Username};{it.ModalAwal};{it.TotalPenjualan};{it.TotalCash};{it.UangFisik};{it.Selisih};{it.Status}")
                        Next
                    End Using
                    MessageBox.Show("Export berhasil.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                Catch ex As Exception
                    MessageBox.Show($"Export gagal: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub PrintShift_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn?.DataContext, ShiftReportViewModel)
            If item Is Nothing Then Return

            If MessageBox.Show($"Cetak ulang laporan shift kasir {item.Username}?", "Cetak Ulang", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        Dim shift = Await db.Shifts.FindAsync(item.Id)
                        If shift IsNot Nothing Then
                            PrintService.PrintShiftReport(shift)
                        End If
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Gagal mencetak: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace