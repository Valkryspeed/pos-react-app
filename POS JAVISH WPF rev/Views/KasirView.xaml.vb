Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Linq
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class KasirView
        Inherits UserControl

        Private Items As New System.Collections.ObjectModel.ObservableCollection(Of CartItem)()
        Private CurrentDiscount As Decimal = 0

        Private WithEvents _pullTimer As New System.Windows.Threading.DispatcherTimer()

        Public Sub New()
            InitializeComponent()
            CartGrid.ItemsSource = Items
            
            ' Register Key Shortcuts using AddHandler for VB.NET
            AddHandler Me.PreviewKeyDown, AddressOf OnPreviewKeyDown
            
            ' Register Barcode Engine Event
            AddHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned

            AddHandler Me.Unloaded, Sub()
                                        RemoveHandler BarcodeEngine.Instance.BarcodeScanned, AddressOf OnBarcodeScanned
                                        RemoveHandler Me.PreviewKeyDown, AddressOf OnPreviewKeyDown
                                        _pullTimer.Stop()
                                    End Sub
            
            ' Fokus ke KodeInput setelah load
            AddHandler Me.Loaded, Sub() KodeInput.Focus()
            
            ' FITUR SHIFT: Check if shift is open
            CheckShift()

            ' Mobile Scanner Pulling
            _pullTimer.Interval = TimeSpan.FromMilliseconds(500)
            _pullTimer.Start()
        End Sub

        Private Async Sub OnPullTimerTick(sender As Object, e As EventArgs) Handles _pullTimer.Tick
            If Not Me.IsVisible Then Return
            
            Try
                ' Simple local HTTP call to our own API
                Using client As New System.Net.Http.HttpClient()
                    Dim response = Await client.GetAsync("http://localhost:5050/api/kasir/pull")
                    If response.StatusCode = System.Net.HttpStatusCode.OK Then
                        Dim content = Await response.Content.ReadAsStringAsync()
                        ' Manual JSON parsing for simplicity or use System.Text.Json
                        If content.Contains("""barcode"":""") Then
                            Dim startIdx = content.IndexOf("""barcode"":""") + 11
                            Dim endIdx = content.IndexOf("""", startIdx)
                            Dim barcode = content.Substring(startIdx, endIdx - startIdx)
                            Await AddItemToCart(barcode)
                        End If
                    End If
                End Using
            Catch
                ' Ignore network errors for background pull
            End Try
        End Sub

        Private Sub CheckShift()
            ' If no active shift, prompt to open
            If Session.CurrentShiftId = 0 Then
                Using db As New AppDbContext()
                    ' Check if user has an open shift in database (in case they logged out and back in)
                    Dim openShift = db.Shifts.FirstOrDefault(Function(s) s.UserId = Session.CurrentUser.Id AndAlso s.Status = "OPEN")
                    If openShift IsNot Nothing Then
                        Session.CurrentShiftId = openShift.Id
                    Else
                        Dim w = New BukaShiftWindow()
                        w.Owner = Window.GetWindow(Me)
                        If Not w.ShowDialog() Then
                            ' User closed dialog without opening shift
                            ' In real POS, we might want to navigate back to dashboard or logout
                            MessageBox.Show("Shift harus dibuka sebelum bertransaksi.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                        End If
                    End If
                End Using
            End If
        End Sub

        Private Async Sub OnBarcodeScanned(barcode As String)
            ' Gunakan Dispatcher untuk memastikan UI thread
            Me.Dispatcher.InvokeAsync(Async Function()
                                          If Not Me.IsVisible Then Return
                                          
                                          ' Bersihkan input box jika ada isinya (mencegah double input)
                                          KodeInput.Clear()
                                          
                                          Await AddItemToCart(barcode)
                                          
                                          ' Pastikan fokus kembali ke KodeInput
                                          KodeInput.Focus()
                                      End Function)
        End Sub

        Private Async Sub OnPreviewKeyDown(sender As Object, e As KeyEventArgs)
            ' GLOBAL SHORTCUTS
            Select Case e.Key
                Case Key.F1
                    KodeInput.Focus()
                    e.Handled = True
                Case Key.F2
                    BayarBox.Focus()
                    BayarBox.SelectAll()
                    e.Handled = True
                Case Key.F5
                    If AddBtn.IsEnabled Then AddBtn_Click(Nothing, Nothing)
                    e.Handled = True
                Case Key.F12
                    PayBtn_Click(Nothing, Nothing)
                    e.Handled = True
                Case Key.Delete
                    ' Jangan tangkap Delete jika sedang mengetik di TextBox
                    If TypeOf e.OriginalSource Is TextBox Then Return
                    
                    Dim item = TryCast(CartGrid.SelectedItem, CartItem)
                    If item IsNot Nothing Then
                        Items.Remove(item)
                        UpdateTotals()
                        e.Handled = True
                    End If
                Case Key.Add, Key.OemPlus
                    ' Shortcut + untuk tambah qty di baris yang dipilih
                    If Not (TypeOf e.OriginalSource Is TextBox) Then
                        Dim item = TryCast(CartGrid.SelectedItem, CartItem)
                        If item IsNot Nothing Then
                            QtyPlus_Click(New Button With {.DataContext = item}, Nothing)
                            e.Handled = True
                        End If
                    End If
                Case Key.Subtract, Key.OemMinus
                    ' Shortcut - untuk kurangi qty di baris yang dipilih
                    If Not (TypeOf e.OriginalSource Is TextBox) Then
                        Dim item = TryCast(CartGrid.SelectedItem, CartItem)
                        If item IsNot Nothing Then
                            QtyMinus_Click(New Button With {.DataContext = item}, Nothing)
                            e.Handled = True
                        End If
                    End If
            End Select
        End Sub

        Private Async Sub AddBtn_Click(sender As Object, e As RoutedEventArgs)
            Await AddItemToCart(KodeInput.Text)
        End Sub

        Private Async Sub KodeInput_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then
                ' Jika input kosong, pindah ke bayar
                If String.IsNullOrWhiteSpace(KodeInput.Text) Then
                    BayarBox.Focus()
                    BayarBox.SelectAll()
                    Return
                End If
                Await AddItemToCart(KodeInput.Text)
            End If
        End Sub

        Private Sub BayarBox_GotFocus(sender As Object, e As RoutedEventArgs)
            ' Pilih semua teks saat fokus agar bisa langsung ditimpa
            BayarBox.SelectAll()
        End Sub

        Private Sub BayarBox_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            ' Jika belum fokus, fokuskan dan cegah mouse membatalkan seleksi
            If Not BayarBox.IsFocused Then
                BayarBox.Focus()
                e.Handled = True
            End If
        End Sub

        Private Async Function AddItemToCart(kode As String) As Task
            If String.IsNullOrWhiteSpace(kode) Then Return

            Using db As New AppDbContext()
                Dim lowerKode = kode.ToLower()
                Dim b = Await db.Barang.AsNoTracking().FirstOrDefaultAsync(Function(x) x.KodeBarang.ToLower() = lowerKode)
                If b Is Nothing Then
                    BarcodeEngine.Instance.BeepError()
                    MessageBox.Show("Barang tidak ditemukan!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                    Return
                End If

                BarcodeEngine.Instance.BeepSuccess()

                ' VALIDASI STOK SAAT SCAN/TAMBAH
                If b.Stok <= 0 Then
                    BarcodeEngine.Instance.BeepError()
                    MessageBox.Show("STOK BARANG HABIS" & vbCrLf & vbCrLf &
                                    $"Barang '{b.NamaBarang}' tidak dapat dijual karena stok = 0." & vbCrLf &
                                    "Silakan lakukan restock terlebih dahulu.",
                                    "Stok Habis", MessageBoxButton.OK, MessageBoxImage.Warning)

                    ' LOG AKTIVITAS: Jual stok habis
                    db.ActivityLog.Add(New ActivityLog With {
                        .UserId = If(Session.CurrentUser?.Id, 0),
                        .Username = If(Session.CurrentUser?.Username, "System"),
                        .Activity = "STOK_HABIS_DENIED",
                        .Timestamp = Date.Now,
                        .Detail = $"Kasir mencoba menjual barang dengan stok habis: {b.KodeBarang} - {b.NamaBarang}"
                    })
                    Await db.SaveChangesAsync()
                    Return
                End If

                Dim existing = Items.FirstOrDefault(Function(i) i.BarangId = b.Id)
                If existing Is Nothing Then
                    Items.Add(New CartItem With {
                        .BarangId = b.Id,
                        .KodeBarang = b.KodeBarang,
                        .NamaBarang = b.NamaBarang,
                        .HargaJual = b.HargaJual,
                        .Qty = 1,
                        .Subtotal = b.HargaJual
                    })
                Else
                    ' VALIDASI TAMBAH QTY
                    If existing.Qty + 1 > b.Stok Then
                        BarcodeEngine.Instance.BeepError()
                        MessageBox.Show("STOK TIDAK MENCUKUPI" & vbCrLf & vbCrLf &
                                        $"Stok tersedia: {b.Stok}" & vbCrLf &
                                        "Quantity tidak boleh bertambah.",
                                        "Peringatan Stok", MessageBoxButton.OK, MessageBoxImage.Warning)
                        Return
                    End If

                    existing.Qty += 1
                    existing.Subtotal = existing.HargaJual * existing.Qty
                    ' Trigger update bindings
                    CartGrid.Items.Refresh()
                End If
            End Using

            KodeInput.Clear()
            KodeInput.Focus()
            UpdateTotals()
        End Function

        Private Sub UpdateTotals()
            Dim subtotal = Items.Sum(Function(i) i.Subtotal)
            SubtotalText.Text = subtotal.ToString("C")
            
            Dim totalBesar = Math.Max(0D, subtotal - CurrentDiscount)
            TotalText.Text = totalBesar.ToString("C")
            DiskonText.Text = $"- {CurrentDiscount:C}"
            
            CalculateKembalian()
            SyncToCustomerDisplay("TRANSACTING")
        End Sub

        Private Sub SyncToCustomerDisplay(status As String)
            Try
                Dim bayar As Decimal = 0
                Decimal.TryParse(BayarBox.Text, bayar)
                
                Dim subtotal = Items.Sum(Function(i) i.Subtotal)
                Dim totalBesar = Math.Max(0D, subtotal - CurrentDiscount)
                Dim kembalian = Math.Max(0D, bayar - totalBesar)

                ' Update Global Display Data
                Dim data = MobileScanQueue.CurrentDisplay
                data.StoreName = SettingsService.Current.StoreName
                data.Status = status
                data.Subtotal = subtotal
                data.Diskon = CurrentDiscount
                data.Total = totalBesar
                data.Bayar = bayar
                data.Kembalian = kembalian
                
                data.Items.Clear()
                For Each it In Items
                    data.Items.Add(New CustomerCartItem With {
                        .Nama = it.NamaBarang,
                        .Qty = it.Qty,
                        .Harga = it.HargaJual,
                        .Subtotal = it.Subtotal
                    })
                Next
            Catch
                ' Ignore sync errors
            End Try
        End Sub

        Private Sub CalculateKembalian()
            Dim subtotal = Items.Sum(Function(i) i.Subtotal)
            Dim totalBesar = Math.Max(0D, subtotal - CurrentDiscount)
            Dim bayar As Decimal = 0
            Decimal.TryParse(BayarBox.Text, bayar)
            
            Dim kembalian = Math.Max(0D, bayar - totalBesar)
            KembalianText.Text = kembalian.ToString("C")
        End Sub

        Private Sub BayarBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            CalculateKembalian()
        End Sub

        Private Sub BayarBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then
                ProcessPayment(confirm:=True)
            End If
        End Sub

        Private Async Sub QtyMinus_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, CartItem)
            If item IsNot Nothing Then
                If item.Qty > 1 Then
                    item.Qty -= 1
                    item.Subtotal = item.HargaJual * item.Qty
                    CartGrid.Items.Refresh()
                    UpdateTotals()
                End If
            End If
            ' Kembalikan fokus ke input utama jika tidak sedang di bayar
            If Not BayarBox.IsFocused Then KodeInput.Focus()
        End Sub

        Private Async Sub QtyPlus_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, CartItem)
            If item IsNot Nothing Then
                Using db As New AppDbContext()
                    Dim b = Await db.Barang.AsNoTracking().FirstOrDefaultAsync(Function(x) x.Id = item.BarangId)
                    If b IsNot Nothing Then
                        If item.Qty + 1 > b.Stok Then
                            BarcodeEngine.Instance.BeepError()
                            MessageBox.Show("STOK TIDAK MENCUKUPI" & vbCrLf & vbCrLf &
                                            $"Stok tersedia: {b.Stok}" & vbCrLf &
                                            "Quantity tidak boleh bertambah.",
                                            "Peringatan Stok", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Return
                        End If
                    End If
                End Using

                item.Qty += 1
                item.Subtotal = item.HargaJual * item.Qty
                CartGrid.Items.Refresh()
                UpdateTotals()
            End If
            ' Kembalikan fokus ke input utama jika tidak sedang di bayar
            If Not BayarBox.IsFocused Then KodeInput.Focus()
        End Sub

        Private Sub DeleteRow_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim item = TryCast(btn.DataContext, CartItem)
            If item IsNot Nothing Then
                Items.Remove(item)
                UpdateTotals()
            End If
        End Sub

        Private Async Sub SearchBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            Dim q = SearchBox.Text?.Trim()
            If String.IsNullOrWhiteSpace(q) Then
                SearchResultsList.ItemsSource = Nothing
                Return
            End If

            Using db As New AppDbContext()
                Dim lowerQ = q.ToLower()
                Dim list = Await db.Barang.AsNoTracking().
                    Where(Function(b) b.NamaBarang.ToLower().Contains(lowerQ) Or b.KodeBarang.ToLower().Contains(lowerQ)).
                    OrderBy(Function(b) b.NamaBarang).
                    Take(20).ToListAsync()
                
                ' Add Stock Status Info
                Dim results = list.Select(Function(b) New With {
                    b.Id,
                    b.NamaBarang,
                    b.KodeBarang,
                    b.HargaJual,
                    b.Stok,
                    b.StokMinimum,
                    .IsStokHabis = (b.Stok <= 0),
                    .IsStokMenipis = (b.Stok > 0 AndAlso b.Stok <= b.StokMinimum),
                    .StokStatus = If(b.Stok <= 0, "STOK HABIS", If(b.Stok <= b.StokMinimum, "STOK MENIPIS", "STOK: " & b.Stok))
                }).ToList()

                SearchResultsList.ItemsSource = results
            End Using
        End Sub

        Private Async Sub SearchResultsList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selected = SearchResultsList.SelectedItem
            If selected IsNot Nothing Then
                ' Use dynamic or reflection to get KodeBarang since we used anonymous type
                Dim kode As String = selected.KodeBarang
                Dim stok As Integer = selected.Stok
                
                If stok <= 0 Then
                    BarcodeEngine.Instance.BeepError()
                    MessageBox.Show("Barang ini tidak dapat dipilih karena stok habis.", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                    SearchResultsList.SelectedItem = Nothing
                    Return
                End If

                Await AddItemToCart(kode)
                SearchResultsList.SelectedItem = Nothing
                SearchBox.Clear()
            End If
        End Sub

        Private Sub PayBtn_Click(sender As Object, e As RoutedEventArgs)
            ProcessPayment(confirm:=False)
        End Sub

        Private Async Sub ProcessPayment(confirm As Boolean)
            ' CEK AKTIVASI (DEMO MODE 100 TRANSAKSI)
            If Not LicensingService.IsActivated() Then
                Dim canTrx = Await LicensingService.CanMakeTransactionAsync()
                If Not canTrx Then
                    MessageBox.Show("BATAS TRANSAKSI DEMO TERCAPAI" & vbCrLf & vbCrLf &
                                    "Anda telah mencapai batas 100 transaksi di versi Demo." & vbCrLf &
                                    "Silakan hubungi Admin untuk aktivasi aplikasi agar dapat terus bertransaksi.",
                                    "Aktivasi Diperlukan", MessageBoxButton.OK, MessageBoxImage.Warning)
                    
                    ' Tampilkan Jendela Aktivasi
                    Dim w As New ActivationWindow()
                    w.Owner = Window.GetWindow(Me)
                    w.ShowDialog()
                    Return
                End If
            End If

            ' FITUR SHIFT: Ensure shift is still open
            CheckShift()
            If Session.CurrentShiftId = 0 Then Return

            If Items.Count = 0 Then
                MessageBox.Show("Keranjang belanja masih kosong!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            ' FITUR 7 — ANTI KECURANGAN: Shift Check
            ' Ideally we should check if a shift is open here.

            Dim subtotal = Items.Sum(Function(i) i.Subtotal)
            Dim totalBesar = Math.Max(0D, subtotal - CurrentDiscount)
            Dim bayar As Decimal = 0
            
            ' Validasi input angka
            If Not Decimal.TryParse(BayarBox.Text, bayar) Then
                 MessageBox.Show("Input pembayaran tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                 Return
            End If

            ' Validasi jumlah bayar
            If bayar < totalBesar Then
                MessageBox.Show($"Pembayaran kurang! Total: {totalBesar:C}, Bayar: {bayar:C}", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If
            
            ' Konfirmasi jika diminta (khusus untuk Enter key)
            If confirm Then
                Dim result = MessageBox.Show($"Proses pembayaran sebesar {totalBesar:C}?" & vbCrLf & $"Bayar: {bayar:C}" & vbCrLf & $"Kembalian: {bayar - totalBesar:C}", "Konfirmasi Pembayaran", MessageBoxButton.YesNo, MessageBoxImage.Question)
                If result <> MessageBoxResult.Yes Then Return
            End If

            Dim kembalian = bayar - totalBesar
            
            Dim trxId = Await SaveTransactionAsync(totalBesar, CurrentDiscount, bayar, kembalian)
            If String.IsNullOrWhiteSpace(trxId) Then Return
            
            ' Cetak Struk hanya jika AutoPrint aktif
            Dim settings = SettingsService.Current
            If settings.AutoPrint Then
                Try
                    PrintService.PrintStruk(trxId, Items.ToList(), totalBesar, bayar, kembalian)
                Catch ex As Exception
                    MessageBox.Show($"Gagal mencetak struk: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                End Try
            End If

            MessageBox.Show("Transaksi berhasil disimpan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
            
            ' Reset All
            Items.Clear()
            BayarBox.Clear()
            CurrentDiscount = 0
            
            ' Update Customer Display to SUCCESS state before clearing
            SyncToCustomerDisplay("SUCCESS")
            
            ' Reset display after 5 seconds
            Task.Delay(5000).ContinueWith(Sub()
                                              MobileScanQueue.CurrentDisplay = New CustomerDisplayData With {.StoreName = SettingsService.Current.StoreName}
                                          End Sub)

            UpdateTotals()
            
            ' Pastikan fokus kembali ke input barcode
            Me.Dispatcher.BeginInvoke(Sub() KodeInput.Focus())
        End Sub
        
        Private Function GetTerminalCode() As String
            Dim t = SettingsService.Current?.TerminalCode
            If String.IsNullOrWhiteSpace(t) Then t = Environment.MachineName
            Dim cleaned = New String(t.Where(Function(ch) Char.IsLetterOrDigit(ch)).ToArray())
            If String.IsNullOrWhiteSpace(cleaned) Then cleaned = "POS"
            cleaned = cleaned.ToUpperInvariant()
            If cleaned.Length > 6 Then cleaned = cleaned.Substring(0, 6)
            Return cleaned
        End Function

        Private Async Function GenerateNoTransaksiAsync(db As AppDbContext) As Task(Of String)
            Dim dateKey = Date.Now.ToString("yyyyMMdd")
            Dim terminal = GetTerminalCode()

            Dim seq = Await db.InvoiceSequences.FirstOrDefaultAsync(Function(x) x.DateKey = dateKey AndAlso x.Terminal = terminal)
            Dim nextNumber As Integer
            If seq Is Nothing Then
                nextNumber = 1
                db.InvoiceSequences.Add(New InvoiceSequence With {
                    .DateKey = dateKey,
                    .Terminal = terminal,
                    .LastNumber = nextNumber
                })
            Else
                nextNumber = Math.Max(0, seq.LastNumber) + 1
                seq.LastNumber = nextNumber
                db.InvoiceSequences.Update(seq)
            End If

            Return $"TRX-{dateKey}-{terminal}-{nextNumber:00000}"
        End Function

        Private Async Function SaveTransactionAsync(totalBesar As Decimal, diskon As Decimal, bayar As Decimal, kembalian As Decimal) As Task(Of String)
            Try
                Using db As New AppDbContext()
                    Dim userId As Integer = If(Session.CurrentUser?.Id, 0)
                    For attempt = 1 To 3
                        Using tx = Await db.Database.BeginTransactionAsync()
                            Try
                                For Each it In Items
                                    Dim b = Await db.Barang.AsNoTracking().FirstOrDefaultAsync(Function(x) x.Id = it.BarangId)
                                    If b Is Nothing Then
                                        Throw New Exception($"Barang '{it.NamaBarang}' tidak ditemukan.")
                                    End If
                                    If b.Stok <= 0 OrElse b.Stok < it.Qty Then
                                        MessageBox.Show("TRANSAKSI DIBATALKAN" & vbCrLf & vbCrLf &
                                                        "Terdapat barang dengan stok tidak mencukupi." & vbCrLf &
                                                        $"Produk: {b.NamaBarang} (Stok: {b.Stok})" & vbCrLf &
                                                        "Periksa kembali keranjang belanja.",
                                                        "Stok Tidak Mencukupi", MessageBoxButton.OK, MessageBoxImage.Error)
                                        Return Nothing
                                    End If
                                Next

                                Dim noTransaksi = Await GenerateNoTransaksiAsync(db)

                                Dim t As New SaleTransaction With {
                                    .NoTransaksi = noTransaksi,
                                    .Tanggal = Date.Now,
                                    .UserId = userId,
                                    .Total = totalBesar,
                                    .Diskon = diskon,
                                    .Bayar = bayar,
                                    .Kembalian = kembalian,
                                    .Items = New List(Of SaleItem)()
                                }

                                For Each it In Items
                                    Dim b = Await db.Barang.FirstOrDefaultAsync(Function(x) x.Id = it.BarangId)
                                    Dim qtyAwal = b.Stok
                                    b.Stok -= it.Qty
                                    db.Barang.Update(b)

                                    db.StockLogs.Add(New StockLog With {
                                        .BarangId = b.Id,
                                        .KodeBarang = b.KodeBarang,
                                        .NamaBarang = b.NamaBarang,
                                        .Tanggal = Date.Now,
                                        .Tipe = "PENJUALAN",
                                        .QtyAwal = qtyAwal,
                                        .QtyPerubahan = -it.Qty,
                                        .QtyAkhir = b.Stok,
                                        .Keterangan = $"Transaksi {noTransaksi}",
                                        .UserId = userId,
                                        .Username = If(Session.CurrentUser?.Username, "System")
                                    })

                                    t.Items.Add(New SaleItem With {
                                        .BarangId = it.BarangId,
                                        .NamaBarang = it.NamaBarang,
                                        .KodeBarang = it.KodeBarang,
                                        .Qty = it.Qty,
                                        .HargaJual = it.HargaJual,
                                        .HargaModal = b.HargaModal,
                                        .Subtotal = it.Subtotal
                                    })
                                Next

                                db.Transaksi.Add(t)

                                db.ActivityLog.Add(New ActivityLog With {
                                    .UserId = userId,
                                    .Username = If(Session.CurrentUser?.Username, "System"),
                                    .Activity = "TRANSAKSI",
                                    .Timestamp = Date.Now,
                                    .Detail = $"Simpan Transaksi {noTransaksi}, Total={totalBesar:C}, Shift={Session.CurrentShiftId}"
                                })

                                Dim currentShift = Await db.Shifts.FindAsync(Session.CurrentShiftId)
                                If currentShift IsNot Nothing Then
                                    currentShift.TotalPenjualan += totalBesar
                                    currentShift.TotalDiskon += diskon
                                    currentShift.TotalCash += totalBesar
                                    currentShift.TransaksiCount += 1
                                    db.Shifts.Update(currentShift)
                                End If

                                Await db.SaveChangesAsync()
                                Await tx.CommitAsync()
                                Return noTransaksi
                            Catch ex As DbUpdateException
                                Dim msg = ex.InnerException?.Message
                                If attempt < 3 AndAlso msg IsNot Nothing AndAlso msg.ToLowerInvariant().Contains("unique") Then
                                    db.ChangeTracker.Clear()
                                    Continue For
                                End If
                                Throw
                            End Try
                        End Using
                    Next
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal menyimpan transaksi: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
            Return Nothing
        End Function

        Private Sub Discount_Click(sender As Object, e As RoutedEventArgs)
            Dim subtotal = Items.Sum(Function(i) i.Subtotal)
            If subtotal = 0 Then
                MessageBox.Show("Keranjang masih kosong!", "Info", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            ' Minta otorisasi Admin jika bukan admin
            Dim isAdmin = Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin"
            If Not isAdmin Then
                Dim pass = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Password Admin untuk Diskon Manual:", "Otorisasi Diskon")
                If String.IsNullOrWhiteSpace(pass) Then Return

                Using db As New AppDbContext()
                    Dim adminUser = db.Users.FirstOrDefault(Function(u) u.Role = "Admin" AndAlso u.IsActive)
                    If adminUser Is Nothing OrElse Not BCrypt.Net.BCrypt.Verify(pass, adminUser.PasswordHash) Then
                        MessageBox.Show("Password Admin salah!", "Gagal", MessageBoxButton.OK, MessageBoxImage.Error)
                        Return
                    End If
                End Using
            End If

            Dim input = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Diskon (Nominal atau %):" & vbCrLf & "Contoh: 5000 atau 10%", "Diskon Manual", "0")
            If String.IsNullOrWhiteSpace(input) Then Return

            Try
                If input.EndsWith("%") Then
                    Dim percent As Double = 0
                    If Double.TryParse(input.Replace("%", ""), percent) Then
                        CurrentDiscount = Decimal.Round(subtotal * (percent / 100))
                    End If
                Else
                    Decimal.TryParse(input, CurrentDiscount)
                End If

                If CurrentDiscount > subtotal Then
                    MessageBox.Show("Diskon tidak boleh melebihi total!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                    CurrentDiscount = 0
                End If

                UpdateTotals()
            Catch
                MessageBox.Show("Input tidak valid!", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace
