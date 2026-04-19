Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports BCrypt.Net

Namespace Services
    Public Class AuthService
        Public Async Function LoginAsync(username As String, password As String) As Task(Of User)
            Using db As New AppDbContext()
                Dim user = Await db.Users.FirstOrDefaultAsync(Function(u) u.Username = username AndAlso u.IsActive)
                If user Is Nothing Then
                    Await LogAsync(username, "LOGIN_FAILED", "User tidak ditemukan")
                    Return Nothing
                End If
                If BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) Then
                    Await LogAsync(user.Username, "LOGIN_SUCCESS", $"Role={user.Role}")
                    Return user
                Else
                    Await LogAsync(user.Username, "LOGIN_FAILED", "Password salah")
                    Return Nothing
                End If
            End Using
        End Function

        Public Async Function LogAsync(username As String, activity As String, detail As String) As Task
            Using db As New AppDbContext()
                Dim u = Await db.Users.AsNoTracking().FirstOrDefaultAsync(Function(x) x.Username = username)
                db.ActivityLog.Add(New ActivityLog With {
                    .UserId = If(u?.Id, 0),
                    .Username = username,
                    .Activity = activity,
                    .Detail = detail,
                    .Timestamp = Date.Now
                })
                Await db.SaveChangesAsync()
            End Using
        End Function
    End Class
End Namespace
