Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class LaporanView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            StartDatePicker.SelectedDate = Date.Today
            EndDatePicker.SelectedDate = Date.Today
        End Sub

        Private Async Sub FilterBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim startDate = StartDatePicker.SelectedDate
            Dim endDate = EndDatePicker.SelectedDate

            If Not startDate.HasValue OrElse Not endDate.HasValue Then
                MessageBox.Show("Pilih rentang tanggal terlebih dahulu!", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            ' Adjust end date to end of day
            Dim endD = endDate.Value.Date.AddDays(1).AddTicks(-1)
            Dim startD = startDate.Value.Date

            Try
                Using db As New AppDbContext()
                    ' Use client-side evaluation for Sum to avoid SQLite decimal aggregate issue
                    Dim list = Await db.Transaksi.AsNoTracking().
                                    Include(Function(t) t.Items).
                                    Where(Function(t) t.Tanggal >= startD AndAlso t.Tanggal <= endD).
                                    OrderByDescending(Function(t) t.Tanggal).
                                    ToListAsync()

                    ReportGrid.ItemsSource = list

                    ' Calculate Summary
                    Dim totalOmset = list.Sum(Function(t) t.Total)
                    Dim totalTrx = list.Count

                    TotalOmsetText.Text = totalOmset.ToString("C0")
                    TotalTrxText.Text = totalTrx.ToString()
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal memuat laporan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Async Sub Reprint_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim trx = TryCast(btn.DataContext, SaleTransaction)
            If trx Is Nothing Then Return

            If MessageBox.Show($"Cetak ulang struk untuk Transaksi #{trx.NoTransaksi}?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        ' Load full transaction with items
                        Dim fullTrx = Await db.Transaksi.Include(Function(t) t.Items).FirstOrDefaultAsync(Function(t) t.Id = trx.Id)
                        
                        If fullTrx IsNot Nothing Then
                            ' Convert SaleItem to CartItem for printing
                            Dim cartItems = fullTrx.Items.Select(Function(i) New CartItem With {
                                .NamaBarang = i.NamaBarang,
                                .Qty = i.Qty,
                                .HargaJual = i.HargaJual,
                                .Subtotal = i.Subtotal
                            }).ToList()

                            PrintService.PrintStruk(fullTrx.NoTransaksi, cartItems, fullTrx.Total, fullTrx.Bayar, fullTrx.Kembalian)
                        End If
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Gagal mencetak ulang: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace