Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models

Namespace Services
    Public Class ShiftService
        Public Shared Function OpenShift(userId As Integer) As Integer
            Using db As New AppDbContext()
                Dim s = New Shift() With {
                    .UserId = userId,
                    .ShiftStart = Date.Now,
                    .Status = "OPEN"
                }
                db.Shifts.Add(s)
                db.SaveChanges()
                Return s.Id
            End Using
        End Function

        Public Shared Sub CloseShift(shiftId As Integer)
            Using db As New AppDbContext()
                Dim s = db.Shifts.FirstOrDefault(Function(x) x.Id = shiftId)
                If s IsNot Nothing Then
                    s.ShiftEnd = Date.Now
                    s.Status = "CLOSED"
                    db.SaveChanges()
                End If
            End Using
        End Sub
    End Class
End Namespace
