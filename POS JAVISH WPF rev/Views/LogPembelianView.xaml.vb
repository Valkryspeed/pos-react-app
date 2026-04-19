Imports System.Windows
Imports System.Windows.Controls
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class LogPembelianView
        Inherits UserControl

        Private Class RestockLogRow
            Public Property LogId As Integer
            Public Property Timestamp As Date
            Public Property Username As String
            Public Property NoFaktur As String
            Public Property Kode As String
            Public Property Nama As String
            Public Property Satuan As String
            Public Property Qty As Integer
            Public Property StokBefore As Integer
            Public Property StokAfter As Integer
            Public Property HargaModal As Decimal
            Public Property HargaJual As Decimal
            Public Property Total As Decimal
            Public Property SupplierId As Integer
            Public Property SupplierName As String
        End Class

        Public Sub New()
            InitializeComponent()
            StartDatePicker.SelectedDate = New Date(DateTime.Now.Year, DateTime.Now.Month, 1)
            EndDatePicker.SelectedDate = DateTime.Now
            LoadData()
        End Sub

        Private Async Sub DeleteLog_Click(sender As Object, e As RoutedEventArgs)
            ' Check permission again
            If Not (Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin") Then
                MessageBox.Show("Hanya Admin yang dapat menghapus log pembelian.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn?.DataContext, RestockLogRow)
            If item Is Nothing Then Return

            Dim msg = $"Anda yakin ingin menghapus data pembelian ini?{vbCrLf}{vbCrLf}" &
                      $"No Faktur: {item.NoFaktur}{vbCrLf}" &
                      $"Barang: {item.Nama}{vbCrLf}" &
                      $"Qty: {item.Qty}{vbCrLf}{vbCrLf}" &
                      $"PERHATIAN: Stok barang akan dikurangi kembali sebanyak {item.Qty}."

            If MessageBox.Show(msg, "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                Try
                    Await PembelianService.HapusPembelianAsync(item.NoFaktur, item.LogId)
                    MessageBox.Show("Data pembelian berhasil dihapus dan stok telah diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    LoadData() ' Refresh list
                Catch ex As Exception
                    MessageBox.Show($"Gagal menghapus data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub

        Private Async Sub LoadData()
            Dim startD = StartDatePicker.SelectedDate.GetValueOrDefault(Date.Today).Date
            Dim endD = EndDatePicker.SelectedDate.GetValueOrDefault(Date.Today).Date.AddDays(1).AddTicks(-1)
            Dim q = SearchBox.Text?.Trim().ToLower()
            Using db As New AppDbContext()
                Dim items = Await db.ActivityLog.AsNoTracking().
                    Where(Function(a) a.Activity = "RESTOCK" AndAlso a.Timestamp >= startD AndAlso a.Timestamp <= endD).
                    OrderByDescending(Function(a) a.Timestamp).ToListAsync()
                Dim rows = items.Select(Function(a) ToRow(a)).ToList()
                If Not String.IsNullOrWhiteSpace(q) Then
                    rows = rows.Where(Function(r) (r.Username IsNot Nothing AndAlso r.Username.ToLower().Contains(q)) OrElse
                                             (r.NoFaktur IsNot Nothing AndAlso r.NoFaktur.ToLower().Contains(q)) OrElse
                                             (r.Kode IsNot Nothing AndAlso r.Kode.ToLower().Contains(q)) OrElse
                                             (r.Nama IsNot Nothing AndAlso r.Nama.ToLower().Contains(q)) OrElse
                                             (r.SupplierName IsNot Nothing AndAlso r.SupplierName.ToLower().Contains(q))).ToList()
                End If
                LogGrid.ItemsSource = rows
            End Using
        End Sub

        Private Sub FilterBtn_Click(sender As Object, e As RoutedEventArgs)
            LoadData()
        End Sub

        Private Shared Function ToRow(a As Models.ActivityLog) As RestockLogRow
            Dim dict As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If Not String.IsNullOrWhiteSpace(a.Detail) Then
                For Each part In a.Detail.Split(";"c)
                    Dim kv = part.Split({"="c}, 2)
                    If kv.Length = 2 Then
                        dict(kv(0).Trim()) = kv(1).Trim()
                    End If
                Next
            End If
            Dim row As New RestockLogRow()
            row.LogId = a.Id
            row.Timestamp = a.Timestamp
            row.Username = a.Username
            row.NoFaktur = GetStr(dict, "NoFaktur")
            row.Kode = GetStr(dict, "Kode")
            row.Nama = GetStr(dict, "Nama")
            row.Satuan = GetStr(dict, "Satuan")
            row.SupplierName = GetStr(dict, "SupplierName")
            row.SupplierId = GetInt(dict, "SupplierId")
            row.Qty = GetInt(dict, "Qty")
            row.StokBefore = GetInt(dict, "StokBefore")
            row.StokAfter = GetInt(dict, "StokAfter")
            row.HargaModal = GetDec(dict, "HargaModal")
            row.HargaJual = GetDec(dict, "HargaJual")
            row.Total = GetDec(dict, "Total")
            Return row
        End Function

        Private Shared Function GetStr(dict As Dictionary(Of String, String), key As String) As String
            Dim v As String = Nothing
            If dict.TryGetValue(key, v) Then Return v
            Return Nothing
        End Function
        Private Shared Function GetInt(dict As Dictionary(Of String, String), key As String) As Integer
            Dim s As String = Nothing
            If dict.TryGetValue(key, s) Then
                Dim n As Integer
                If Integer.TryParse(s, n) Then Return n
            End If
            Return 0
        End Function
        Private Shared Function GetDec(dict As Dictionary(Of String, String), key As String) As Decimal
            Dim s As String = Nothing
            If dict.TryGetValue(key, s) Then
                ' Remove currency/formatting if any
                Dim cleaned = s.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) _
                               .Replace(",", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                Dim d As Decimal
                If Decimal.TryParse(cleaned, d) Then Return d
            End If
            Return 0D
        End Function
    End Class
End Namespace
