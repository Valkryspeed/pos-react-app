Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers

Namespace Views
    Partial Public Class SupplierView
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
            AddHandler Me.Loaded, AddressOf OnLoaded
        End Sub

        Private Async Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            Await LoadSuppliers()
        End Sub

        Private Async Function LoadSuppliers() As Task
            Try
                Using db As New AppDbContext()
                    Dim list = Await db.Suppliers.Where(Function(s) s.IsActive).OrderBy(Function(s) s.Nama).ToListAsync()
                    SupplierGrid.ItemsSource = list
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal memuat data supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Function

        Private Async Sub AddBtn_Click(sender As Object, e As RoutedEventArgs)
            ' Using simple InputBoxes for now, or you can create a specific Window
            Dim nama = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Nama Supplier:", "Tambah Supplier")
            If String.IsNullOrWhiteSpace(nama) Then Return

            Dim telp = Microsoft.VisualBasic.Interaction.InputBox("Masukkan No. Telepon:", "Tambah Supplier")
            Dim email = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Email:", "Tambah Supplier")
            Dim alamat = Microsoft.VisualBasic.Interaction.InputBox("Masukkan Alamat:", "Tambah Supplier")

            Try
                Using db As New AppDbContext()
                    Dim s As New Supplier With {
                        .Nama = nama,
                        .Telepon = telp,
                        .Email = email,
                        .Alamat = alamat,
                        .IsActive = True
                    }
                    db.Suppliers.Add(s)
                    Await db.SaveChangesAsync()
                End Using
                Await LoadSuppliers()
                MessageBox.Show("Supplier berhasil ditambahkan!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
            Catch ex As Exception
                MessageBox.Show($"Gagal menambah supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Async Sub EditBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim s = TryCast(btn.DataContext, Supplier)
            If s Is Nothing Then Return

            Dim nama = Microsoft.VisualBasic.Interaction.InputBox("Edit Nama Supplier:", "Edit Supplier", s.Nama)
            If String.IsNullOrWhiteSpace(nama) Then Return

            Dim telp = Microsoft.VisualBasic.Interaction.InputBox("Edit No. Telepon:", "Edit Supplier", s.Telepon)
            Dim email = Microsoft.VisualBasic.Interaction.InputBox("Edit Email:", "Edit Supplier", s.Email)
            Dim alamat = Microsoft.VisualBasic.Interaction.InputBox("Edit Alamat:", "Edit Supplier", s.Alamat)

            Try
                Using db As New AppDbContext()
                    Dim existing = Await db.Suppliers.FindAsync(s.Id)
                    If existing IsNot Nothing Then
                        existing.Nama = nama
                        existing.Telepon = telp
                        existing.Email = email
                        existing.Alamat = alamat
                        Await db.SaveChangesAsync()
                    End If
                End Using
                Await LoadSuppliers()
                MessageBox.Show("Supplier berhasil diperbarui!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
            Catch ex As Exception
                MessageBox.Show($"Gagal memperbarui supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Async Sub DeleteBtn_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim s = TryCast(btn.DataContext, Supplier)
            If s Is Nothing Then Return

            If MessageBox.Show($"Hapus supplier '{s.Nama}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        Dim existing = Await db.Suppliers.FindAsync(s.Id)
                        If existing IsNot Nothing Then
                            ' Soft Delete
                            existing.IsActive = False
                            Await db.SaveChangesAsync()
                        End If
                    End Using
                    Await LoadSuppliers()
                    MessageBox.Show("Supplier berhasil dihapus!", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                Catch ex As Exception
                    MessageBox.Show($"Gagal menghapus supplier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace
