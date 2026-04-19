Imports System.Windows
Imports Microsoft.Data.Sqlite
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class RestockBarangWindow
        Inherits Window

        Private ReadOnly _kodeBarang As String
        Private _supplierIdMap As New Dictionary(Of String, Integer)

        Public Sub New(kodeBarang As String)
            InitializeComponent()
            _kodeBarang = kodeBarang
            LoadBarang()
            LoadSuppliers()
        End Sub

        Private Sub LoadBarang()
            Dim b = BarangService.GetByKode(_kodeBarang)
            If b Is Nothing Then
                MessageBox.Show("Barang tidak ditemukan.", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                DialogResult = False
                Close()
                Return
            End If
            KodeBarangBox.Text = b.KodeBarang
            NamaBarangBox.Text = b.NamaBarang
            HargaJualBox.Text = b.HargaJual.ToString("F0")
            SatuanBox.Text = If(b.Satuan, "")
            StokSekarangBox.Text = b.Stok.ToString()
        End Sub

        Private Sub LoadSuppliers()
            SupplierCombo.Items.Clear()
            _supplierIdMap.Clear()
            SupplierCombo.Items.Add("(Tanpa Supplier)")
            _supplierIdMap("(Tanpa Supplier)") = 0
            For Each t In BarangService.GetSuppliers()
                SupplierCombo.Items.Add(t.Item2)
                _supplierIdMap(t.Item2) = t.Item1
            Next
            SupplierCombo.SelectedIndex = 0
        End Sub

        Private Sub OnHargaQtyChanged(sender As Object, e As RoutedEventArgs)
            Dim harga As Decimal = 0
            Decimal.TryParse(HargaModalBox.Text, harga)
            Dim qty As Integer = 0
            Integer.TryParse(QtyBox.Text, qty)
            Dim total = Math.Max(0D, harga * qty)
            TotalBox.Text = total.ToString("F0")
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
        End Sub

        Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
            If Not (Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin") Then
                MessageBox.Show("Hanya Admin yang dapat menyimpan pembelian/restock.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            Dim harga As Decimal
            Dim qty As Integer
            If Not Decimal.TryParse(HargaModalBox.Text, harga) OrElse harga <= 0D Then
                MessageBox.Show("Harga modal harus > 0.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            If Not Integer.TryParse(QtyBox.Text, qty) OrElse qty <= 0 Then
                MessageBox.Show("Qty harus > 0.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim supplierName = TryCast(SupplierCombo.SelectedItem, String)
            Dim supplierId As Integer = 0
            If supplierName IsNot Nothing AndAlso _supplierIdMap.ContainsKey(supplierName) Then
                supplierId = _supplierIdMap(supplierName)
            End If

            Dim userId = If(Session.CurrentUser?.Id, 0)
            PembelianService.SimpanRestock(_kodeBarang, qty, harga, supplierId, userId)

            DialogResult = True
            Close()
        End Sub
    End Class
End Namespace
