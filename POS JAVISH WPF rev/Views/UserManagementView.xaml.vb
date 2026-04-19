Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Helpers
Imports BCrypt.Net
Imports POS_JAVISH_WPF.Services

Namespace Views
    Partial Public Class UserManagementView
        Inherits UserControl

        Private _selectedUserId As Integer = 0

        Public Sub New()
            InitializeComponent()
            LoadUsers()
            CheckActivation()
        End Sub

        Private Sub CheckActivation()
            If Not LicensingService.IsActivated() Then
                BtnSave.IsEnabled = False
                BtnSave.ToolTip = "Fitur dinonaktifkan di versi Demo. Silakan Aktivasi Aplikasi."
                TxtUsername.IsEnabled = False
                TxtFullName.IsEnabled = False
                TxtPassword.IsEnabled = False
                CmbRole.IsEnabled = False
                ChkActive.IsEnabled = False
            End If
        End Sub

        Private Async Sub LoadUsers()
            Try
                Using db As New AppDbContext()
                    Dim users = Await db.Users.AsNoTracking().OrderBy(Function(u) u.Username).ToListAsync()
                    UserGrid.ItemsSource = users
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal memuat data user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Sub BtnClear_Click(sender As Object, e As RoutedEventArgs)
            ClearForm()
        End Sub

        Private Sub ClearForm()
            _selectedUserId = 0
            TxtUsername.Text = ""
            TxtFullName.Text = ""
            TxtPassword.Password = ""
            CmbRole.SelectedIndex = -1
            ChkActive.IsChecked = True
            BtnSave.Content = "Simpan"
            UserGrid.SelectedItem = Nothing
            TxtUsername.IsEnabled = True ' Username can be edited only for new users usually, but let's allow editing for simplicity or restrict it if needed
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
            Dim username = TxtUsername.Text.Trim()
            Dim fullName = TxtFullName.Text.Trim()
            Dim password = TxtPassword.Password
            Dim roleItem = TryCast(CmbRole.SelectedItem, ComboBoxItem)
            Dim isActive = ChkActive.IsChecked.GetValueOrDefault()

            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(fullName) OrElse roleItem Is Nothing Then
                MessageBox.Show("Mohon lengkapi Username, Nama Lengkap, dan Role.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim role = roleItem.Content.ToString()

            ' Validation: Prevent changing own status/role to lock out
            If _selectedUserId = Session.CurrentUser.Id Then
                If Not isActive Then
                    MessageBox.Show("Anda tidak dapat menonaktifkan akun Anda sendiri yang sedang login.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
                If role <> "Admin" Then
                    MessageBox.Show("Anda tidak dapat mengubah Role Anda sendiri menjadi selain Admin.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
            End If

            Try
                Using db As New AppDbContext()
                    If _selectedUserId = 0 Then
                        ' Add New
                        If String.IsNullOrWhiteSpace(password) Then
                            MessageBox.Show("Password wajib diisi untuk user baru.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Return
                        End If
                        
                        If password.Length < 4 Then
                            MessageBox.Show("Password minimal 4 karakter.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Return
                        End If

                        ' Check if username exists
                        If Await db.Users.AnyAsync(Function(u) u.Username = username) Then
                            MessageBox.Show("Username sudah digunakan.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                            Return
                        End If

                        Dim newUser As New User With {
                            .Username = username,
                            .FullName = fullName,
                            .Role = role,
                            .IsActive = isActive,
                            .PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                        }
                        db.Users.Add(newUser)
                    Else
                        ' Edit Existing
                        Dim user = Await db.Users.FindAsync(_selectedUserId)
                        If user Is Nothing Then Return

                        user.Username = username
                        user.FullName = fullName
                        user.Role = role
                        user.IsActive = isActive

                        If Not String.IsNullOrWhiteSpace(password) Then
                            If password.Length < 4 Then
                                MessageBox.Show("Password minimal 4 karakter.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                                Return
                            End If
                            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                        End If
                    End If

                    Await db.SaveChangesAsync()
                    MessageBox.Show("Data user berhasil disimpan.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information)
                    ClearForm()
                    LoadUsers()
                End Using
            Catch ex As Exception
                MessageBox.Show($"Gagal menyimpan user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Sub UserGrid_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim user = TryCast(UserGrid.SelectedItem, User)
            If user Is Nothing Then Return

            _selectedUserId = user.Id
            TxtUsername.Text = user.Username
            TxtFullName.Text = user.FullName
            
            ' Select Role
            For Each item As ComboBoxItem In CmbRole.Items
                If item.Content.ToString() = user.Role Then
                    CmbRole.SelectedItem = item
                    Exit For
                End If
            Next

            ChkActive.IsChecked = user.IsActive
            BtnSave.Content = "Update"
            TxtPassword.Password = "" ' Clear password field
            TxtUsername.IsEnabled = False ' Don't allow changing username for existing users to prevent conflicts easily
        End Sub

        Private Sub ResetPassword_Click(sender As Object, e As RoutedEventArgs)
            ' This is handled in the main form now by selecting the user and entering a new password
            ' But if we want a direct button action:
            Dim btn = TryCast(sender, Button)
            Dim user = TryCast(btn.DataContext, User)
            If user Is Nothing Then Return

            ' Select the user to edit
            UserGrid.SelectedItem = user
            TxtPassword.Focus()
            MessageBox.Show("Silakan masukkan password baru di form sebelah kanan lalu klik Update.", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        Private Async Sub DeleteUser_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Dim user = TryCast(btn.DataContext, User)
            If user Is Nothing Then Return

            If user.Id = Session.CurrentUser.Id Then
                MessageBox.Show("Anda tidak dapat menghapus akun Anda sendiri.", "Validasi", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            If MessageBox.Show($"Apakah Anda yakin ingin menghapus user '{user.Username}'?", "Konfirmasi", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
                Try
                    Using db As New AppDbContext()
                        Dim u = Await db.Users.FindAsync(user.Id)
                        If u IsNot Nothing Then
                            db.Users.Remove(u)
                            Await db.SaveChangesAsync()
                            LoadUsers()
                            ClearForm()
                        End If
                    End Using
                Catch ex As Exception
                    MessageBox.Show($"Gagal menghapus user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace