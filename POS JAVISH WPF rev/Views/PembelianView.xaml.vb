Imports System.Windows
Imports System.Windows.Controls
Imports System.Collections.ObjectModel
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class PembelianView
        Inherits UserControl

        ' ViewModel for Purchase Grid
        Public Class PurchaseItemViewModel
            Public Property BarangId As Integer
            Public Property KodeBarang As String
            Public Property NamaBarang As String
            Public Property HargaBeli As Decimal
            Public Property Qty As Integer
            Public Property Subtotal As Decimal
        End Class

        Private PurchaseViewModels As New ObservableCollection(Of PurchaseItemViewModel)()
        Private CurrentItem As Barang
        Private _suppressSearchTextChanged As Boolean = False
        Private _lastInputFromScanner As Boolean = False

        Public Sub New()
            InitializeComponent()
            PurchaseGrid.ItemsSource = PurchaseViewModels
            TanggalPicker.SelectedDate = Date.Today
            LoadSuppliers()

            AddHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned
            AddHandler Me.Unloaded, Sub() RemoveHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned
        End Sub

        Private Sub OnBarcodeScanned(barcode As String)
            If Not Me.IsVisible Then Return
            If String.IsNullOrWhiteSpace(barcode) Then Return

            If Not Dispatcher.CheckAccess() Then
                Dispatcher.Invoke(Sub() OnBarcodeScanned(barcode))
                Return
            End If

            _lastInputFromScanner = True
            SearchBox.Text = barcode.Trim()
            SearchBox.Focus()
            SearchBox.SelectAll()
        End Sub

        Private Sub ApplySelectedBarang(selected As Barang, keepSearchText As String)
            CurrentItem = selected

            SelectedNameText.Text = selected.NamaBarang
            SelectedCodeText.Text = $"Kode: {selected.KodeBarang}"
            SelectedStockText.Text = selected.Stok.ToString()
            SelectedItemPanel.Visibility = Visibility.Visible

            HargaBeliBox.Text = selected.HargaModal.ToString("F0")
            QtyBox.Text = "1"
            QtyBox.Focus()
            QtyBox.SelectAll()

            SearchResultsList.Visibility = Visibility.Collapsed
            SearchResultsList.SelectedItem = Nothing

            _suppressSearchTextChanged = True
            SearchBox.Text = keepSearchText
            _suppressSearchTextChanged = False
        End Sub

        Private Async Sub LoadSuppliers()
            Using db As New AppDbContext()
                Dim suppliers = Await db.Suppliers.AsNoTracking().Where(Function(s) s.IsActive).ToListAsync()
                SupplierCombo.ItemsSource = suppliers
            End Using
        End Sub

        Private Async Sub SearchBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            If _suppressSearchTextChanged Then Return
            Dim q = SearchBox.Text?.Trim()
            If String.IsNullOrWhiteSpace(q) Then
                SearchResultsList.Visibility = Visibility.Collapsed
                _lastInputFromScanner = False
                Return
            End If

            Using db As New AppDbContext()
                Dim lowerQ = q.ToLower()
                Dim list = Await db.Barang.AsNoTracking().
                    Where(Function(b) b.NamaBarang.ToLower().Contains(lowerQ) Or b.KodeBarang.ToLower().Contains(lowerQ)).
                    OrderBy(Function(b) b.NamaBarang).
                    Take(10).ToListAsync()

                Dim exact = list.FirstOrDefault(Function(b) b.KodeBarang IsNot Nothing AndAlso b.KodeBarang.ToLower() = lowerQ)
                If exact IsNot Nothing Then
                    ApplySelectedBarang(exact, q)
                    If _lastInputFromScanner Then
                        BarcodeEngine.Instance.BeepSuccess()
                        _lastInputFromScanner = False
                    End If
                    Return
                End If

                SearchResultsList.ItemsSource = list
                SearchResultsList.Visibility = If(list.Any(), Visibility.Visible, Visibility.Collapsed)

                If _lastInputFromScanner Then
                    If list.Any() Then
                        BarcodeEngine.Instance.BeepSuccess()
                    Else
                        BarcodeEngine.Instance.BeepError()
                    End If
                    _lastInputFromScanner = False
                End If
            End Using
        End Sub

        Private Sub SearchResultsList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = TryCast(SearchResultsList.SelectedItem, Barang)
            If selected IsNot Nothing Then
                ApplySelectedBarang(selected, selected.NamaBarang)
            End If
        End Sub

        Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs)
            If CurrentItem Is Nothing Then
                MessageBox.Show("Pilih barang terlebih dahulu!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim qty As Integer = 0
            Dim harga As Decimal = 0

            If Not Integer.TryParse(QtyBox.Text, qty) OrElse qty <= 0 Then
                MessageBox.Show("Qty harus angka positif!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            If Not Decimal.TryParse(HargaBeliBox.Text, harga) OrElse harga < 0 Then
                MessageBox.Show("Harga beli tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            ' Cek jika item sudah ada di list, update qty
            Dim existing = PurchaseViewModels.FirstOrDefault(Function(x) x.BarangId = CurrentItem.Id)
            If existing IsNot Nothing Then
                existing.Qty += qty
                existing.Subtotal = existing.Qty * existing.HargaBeli
                ' Force refresh (remove/add)
                PurchaseViewModels.Remove(existing)
                PurchaseViewModels.Add(existing)
            Else
                Dim newItem As New PurchaseItemViewModel With {
                    .BarangId = CurrentItem.Id,
                    .KodeBarang = CurrentItem.KodeBarang,
                    .NamaBarang = CurrentItem.NamaBarang,
                    .Qty = qty,
                    .HargaBeli = harga,
                    .Subtotal = qty * harga
                }
                PurchaseViewModels.Add(newItem)
            End If
            
            ' Reset UI
            CurrentItem = Nothing
            SelectedItemPanel.Visibility = Visibility.Collapsed
            SearchBox.Clear()
            HargaBeliBox.Clear()
            QtyBox.Clear()
            
            UpdateTotal()
        End Sub

        Private Sub UpdateTotal()
            Dim total = PurchaseViewModels.Sum(Function(x) x.Subtotal)
            TotalText.Text = total.ToString("C")
        End Sub

        Private Sub DeleteRow_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, PurchaseItemViewModel)
            If item IsNot Nothing Then
                PurchaseViewModels.Remove(item)
                UpdateTotal()
            End If
        End Sub

        Private Sub AddSupplier_Click(sender As Object, e As RoutedEventArgs)
            MessageBox.Show("Fitur Tambah Supplier Cepat belum tersedia. Silakan gunakan menu Master Supplier.", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Async Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs)
            If PurchaseViewModels.Count = 0 Then
                MessageBox.Show("Daftar pembelian masih kosong!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If SupplierCombo.SelectedValue Is Nothing Then
                MessageBox.Show("Pilih Supplier terlebih dahulu!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If String.IsNullOrWhiteSpace(FakturBox.Text) Then
                MessageBox.Show("Masukkan No. Faktur!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Try
                Using db As New AppDbContext()
                    ' FITUR 8 — ADMIN ONLY: Hanya Admin yang dapat menyimpan pembelian/restock
                    If Not (Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin") Then
                        MessageBox.Show("Hanya Admin yang dapat menyimpan pembelian/restock.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning)
                        Return
                    End If

                    Dim currentUserId = If(Session.CurrentUser?.Id, 0)
                    Dim currentUname = If(Session.CurrentUser?.Username, "System")
                    Dim currentRole = If(Session.CurrentUser?.Role, "")

                    ' 1. Create Purchase Header
                    Dim newPurchase As New Purchase With {
                        .NoFaktur = FakturBox.Text,
                        .Tanggal = If(TanggalPicker.SelectedDate, Date.Today),
                        .SupplierId = CInt(SupplierCombo.SelectedValue),
                        .Total = PurchaseViewModels.Sum(Function(x) x.Subtotal),
                        .UserId = currentUserId
                    }
                    
                    ' Ambil nama supplier untuk log
                    Dim supplier = Await db.Suppliers.FindAsync(newPurchase.SupplierId)
                    Dim supplierName = If(supplier?.Nama, "")

                    db.Pembelian.Add(newPurchase)
                    Await db.SaveChangesAsync() ' Get ID

                    ' 2. Create Items & Update Stock
                    For Each vm In PurchaseViewModels
                        Dim item As New PurchaseItem With {
                            .PurchaseId = newPurchase.Id,
                            .BarangId = vm.BarangId,
                            .Qty = vm.Qty,
                            .HargaBeli = vm.HargaBeli,
                            .Subtotal = vm.Subtotal
                        }
                        db.PembelianItems.Add(item)

                        ' Update Stock & Price
                        Dim barang = Await db.Barang.FindAsync(vm.BarangId)
                        If barang IsNot Nothing Then
                            Dim stokBefore = barang.Stok
                            barang.Stok += vm.Qty
                            barang.HargaModal = vm.HargaBeli ' Update last cost price
                            
                            ' LOG 1: RESTOCK (Khusus untuk LogPembelianView)
                            Dim detailLog = $"NoFaktur={newPurchase.NoFaktur}; BarangId={barang.Id}; Kode={barang.KodeBarang}; Nama={barang.NamaBarang}; Satuan={If(barang.Satuan, "")}; Qty={vm.Qty}; StokBefore={stokBefore}; StokAfter={barang.Stok}; HargaModal={vm.HargaBeli}; HargaJual={barang.HargaJual}; Total={vm.Subtotal}; SupplierId={newPurchase.SupplierId}; SupplierName={supplierName}; UserId={currentUserId}; Role={currentRole}"
                            db.ActivityLog.Add(New ActivityLog With {
                                .UserId = currentUserId,
                                .Username = currentUname,
                                .Activity = "RESTOCK",
                                .Timestamp = Date.Now,
                                .Detail = detailLog
                            })

                            ' LOG 2: STOCK LOG (Generic Stock Movement)
                            db.StockLogs.Add(New StockLog With {
                                .BarangId = barang.Id,
                                .KodeBarang = barang.KodeBarang,
                                .NamaBarang = barang.NamaBarang,
                                .Tanggal = Date.Now,
                                .Tipe = "PEMBELIAN",
                                .QtyAwal = stokBefore,
                                .QtyPerubahan = vm.Qty,
                                .QtyAkhir = barang.Stok,
                                .Keterangan = $"Pembelian No. Faktur {newPurchase.NoFaktur}",
                                .UserId = currentUserId,
                                .Username = currentUname
                            })
                        End If
                    Next

                    Await db.SaveChangesAsync()
                    
                    MessageBox.Show("Pembelian berhasil disimpan dan stok telah diperbarui.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    
                    ' Reset All
                    PurchaseViewModels.Clear()
                    FakturBox.Clear()
                    SupplierCombo.SelectedIndex = -1
                    UpdateTotal()
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal menyimpan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace
