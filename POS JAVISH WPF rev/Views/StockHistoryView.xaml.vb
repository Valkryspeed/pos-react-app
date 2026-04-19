Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class StockHistoryView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            StartDatePicker.SelectedDate = Date.Today.AddDays(-7)
            EndDatePicker.SelectedDate = Date.Today
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadHistory()
        End Sub

        Private Async Sub FilterBtn_Click(sender As Object, e As RoutedEventArgs)
            Await LoadHistory()
        End Sub

        Private Async Function LoadHistory() As Task
            Dim startDate = StartDatePicker.SelectedDate
            Dim endDate = EndDatePicker.SelectedDate
            Dim query = SearchBox.Text?.Trim()

            If Not startDate.HasValue OrElse Not endDate.HasValue Then
                MessageBox.Show("Pilih rentang tanggal!", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            Dim startD = startDate.Value.Date
            Dim endD = endDate.Value.Date.AddDays(1).AddTicks(-1)

            Try
                Using db As New AppDbContext()
                    Dim listQuery = db.StockLogs.AsNoTracking().
                                    Where(Function(l) l.Tanggal >= startD AndAlso l.Tanggal <= endD)
                    
                    If Not String.IsNullOrWhiteSpace(query) Then
                        Dim lowerQ = query.ToLower()
                        listQuery = listQuery.Where(Function(l) l.NamaBarang.ToLower().Contains(lowerQ) Or l.KodeBarang.ToLower().Contains(lowerQ))
                    End If

                    Dim list = Await listQuery.OrderByDescending(Function(l) l.Tanggal).ToListAsync()
                    HistoryGrid.ItemsSource = list
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal memuat riwayat stok: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Function
    End Class
End Namespace
