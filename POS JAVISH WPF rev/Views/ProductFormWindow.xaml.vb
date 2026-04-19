Imports System.Windows
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class ProductFormWindow
        Inherits Window

        Public Property ProductId As Integer? = Nothing
        Private _autoFocusRegistered As Boolean = False

        Public Sub New()
            InitializeComponent()
            ' Pause global scanner interceptor
            BarcodeEngine.Instance.IsPaused = True
            AddHandler Me.Closed, Sub() BarcodeEngine.Instance.IsPaused = False
            RegisterAutoFocusKodeBox()
        End Sub

        Public Sub New(id As Integer)
            InitializeComponent()
            ProductId = id
            ' Pause global scanner interceptor
            BarcodeEngine.Instance.IsPaused = True
            AddHandler Me.Closed, Sub() BarcodeEngine.Instance.IsPaused = False
            RegisterAutoFocusKodeBox()
            LoadProduct(id)
        End Sub

        Private Sub RegisterAutoFocusKodeBox()
            If _autoFocusRegistered Then Return
            _autoFocusRegistered = True

            AddHandler Me.Loaded, Sub()
                                     Me.Dispatcher.BeginInvoke(Sub()
                                                                  KodeBox.Focus()
                                                                  KodeBox.SelectAll()
                                                              End Sub, System.Windows.Threading.DispatcherPriority.Input)
                                 End Sub
        End Sub

        Private Async Sub LoadProduct(id As Integer)
            Using db As New AppDbContext()
                Dim item = Await db.Barang.FindAsync(id)
                If item IsNot Nothing Then
                    KodeBox.Text = item.KodeBarang
                    NamaBox.Text = item.NamaBarang
                    HargaModalBox.Text = item.HargaModal.ToString("F0")
                    HargaJualBox.Text = item.HargaJual.ToString("F0")
                    StokBox.Text = item.Stok.ToString()
                    SatuanBox.Text = item.Satuan
                End If
            End Using
        End Sub

        Private Sub KodeBox_KeyDown(sender As Object, e As Input.KeyEventArgs)
            If e.Key = Input.Key.Enter Then
                ' Mencegah Enter memicu tombol Simpan otomatis
                e.Handled = True
                
                ' Jika discan pakai barcode, pindahkan fokus ke Nama Barang
                NamaBox.Focus()
                NamaBox.SelectAll()
            End If
        End Sub

        Private Sub NamaBox_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles NamaBox.KeyDown
            If e.Key = Input.Key.Enter Then
                e.Handled = True
                HargaModalBox.Focus()
                HargaModalBox.SelectAll()
            End If
        End Sub

        Private Sub HargaModalBox_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles HargaModalBox.KeyDown
            If e.Key = Input.Key.Enter Then
                e.Handled = True
                HargaJualBox.Focus()
                HargaJualBox.SelectAll()
            End If
        End Sub

        Private Sub HargaJualBox_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles HargaJualBox.KeyDown
            If e.Key = Input.Key.Enter Then
                e.Handled = True
                StokBox.Focus()
                StokBox.SelectAll()
            End If
        End Sub

        Private Sub StokBox_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles StokBox.KeyDown
            If e.Key = Input.Key.Enter Then
                e.Handled = True
                SatuanBox.Focus()
                SatuanBox.SelectAll()
            End If
        End Sub

        Private Sub SatuanBox_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles SatuanBox.KeyDown
            If e.Key = Input.Key.Enter Then
                ' Jika sudah di kolom terakhir, baru panggil Save
                SaveBtn_Click(Nothing, Nothing)
                e.Handled = True
            End If
        End Sub

        Private Async Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrWhiteSpace(KodeBox.Text) Then
                MessageBox.Show("Kode Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If String.IsNullOrWhiteSpace(NamaBox.Text) Then
                MessageBox.Show("Nama Barang harus diisi!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim modal, jual As Decimal
            Dim stok As Integer

            If Not Decimal.TryParse(HargaModalBox.Text, modal) Then modal = 0
            If Not Decimal.TryParse(HargaJualBox.Text, jual) Then jual = 0
            If Not Integer.TryParse(StokBox.Text, stok) Then stok = 0

            Try
                Using db As New AppDbContext()
                    ' Cek kode unik jika tambah baru
                    If Not ProductId.HasValue Then
                        Dim exists = Await db.Barang.AnyAsync(Function(b) b.KodeBarang = KodeBox.Text)
                        If exists Then
                            MessageBox.Show($"Kode Barang '{KodeBox.Text}' sudah ada!", "Duplikat", MessageBoxButton.OK, MessageBoxImage.Error)
                            Return
                        End If
                    End If

                    Dim item As Barang
                    If ProductId.HasValue Then
                        item = Await db.Barang.FindAsync(ProductId.Value)
                        If item Is Nothing Then Return ' Should not happen
                    Else
                        item = New Barang()
                        db.Barang.Add(item)
                    End If

                    item.KodeBarang = KodeBox.Text
                    item.NamaBarang = NamaBox.Text
                    item.HargaModal = modal
                    item.HargaJual = jual
                    item.Stok = stok
                    item.Satuan = SatuanBox.Text
                    item.StokMinimum = 5 ' Default

                    Await db.SaveChangesAsync()
                    DialogResult = True
                    Close()
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal menyimpan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace
