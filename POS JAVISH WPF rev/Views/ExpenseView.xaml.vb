Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class ExpenseView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadExpenses()
        End Sub

        Private Async Function LoadExpenses() As Task
            Try
                Using db As New AppDbContext()
                    ' Load last 30 days of expenses
                    Dim thirtyDaysAgo = Date.Today.AddDays(-30)
                    Dim list = Await db.Pengeluaran.AsNoTracking().
                                    Where(Function(x) x.Tanggal >= thirtyDaysAgo).
                                    OrderByDescending(Function(x) x.Tanggal).
                                    ToListAsync()
                    
                    ExpenseGrid.ItemsSource = list
                    TotalExpenseText.Text = list.Sum(Function(x) x.Jumlah).ToString("C0")
                End Using
            Catch exc As Exception
                MessageBox.Show($"Gagal memuat data pengeluaran: {exc.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Function

        Private Async Sub AddBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim kategori = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Kategori Pengeluaran (Contoh: Listrik, Gaji, Sewa):", "Tambah Pengeluaran")
            If String.IsNullOrWhiteSpace(kategori) Then Return

            Dim keterangan = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Keterangan:", "Tambah Pengeluaran")
            If String.IsNullOrWhiteSpace(keterangan) Then Return

            Dim jumlahStr = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Jumlah (Rp):", "Tambah Pengeluaran")
            Dim jumlah As Decimal = 0
            If Not Decimal.TryParse(jumlahStr, jumlah) OrElse jumlah <= 0 Then
                MessageBox.Show("Jumlah harus berupa angka positif!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Try
                Using db As New AppDbContext()
                    Dim newExpense As New Expense With {
                        .Tanggal = Date.Now,
                        .Kategori = kategori,
                        .Keterangan = keterangan,
                        .Jumlah = jumlah,
                        .UserId = If(Session.CurrentUser?.Id, 0)
                    }
                    db.Pengeluaran.Add(newExpense)
                    Await db.SaveChangesAsync()
                End Using
                Await LoadExpenses()
                MessageBox.Show("Pengeluaran berhasil dicatat!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
            Catch exc As Exception
                MessageBox.Show($"Gagal mencatat pengeluaran: {exc.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Async Sub DeleteBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim exItem = TryCast(btn.DataContext, Expense)
            If exItem Is Nothing Then Return

            ' Check if current user is admin
            If Not (Session.CurrentUser IsNot Nothing AndAlso Session.CurrentUser.Role = "Admin") Then
                MessageBox.Show("Hanya Admin yang dapat menghapus data pengeluaran.", "Akses Ditolak", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If MessageBox.Show($"Hapus catatan pengeluaran '{exItem.Keterangan}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        Dim existing = Await db.Pengeluaran.FindAsync(exItem.Id)
                        If existing IsNot Nothing Then
                            db.Pengeluaran.Remove(existing)
                            Await db.SaveChangesAsync()
                        End If
                    End Using
                    Await LoadExpenses()
                    MessageBox.Show("Data pengeluaran berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                Catch err As Exception
                    MessageBox.Show($"Gagal menghapus pengeluaran: {err.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace
