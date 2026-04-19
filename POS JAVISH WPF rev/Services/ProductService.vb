Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models

Namespace Services
    Public Class ProductService
        Public Async Function GetByKodeAsync(kode As String) As Task(Of Barang)
            Using db As New AppDbContext()
                Return Await db.Barang.AsNoTracking().FirstOrDefaultAsync(Function(b) b.KodeBarang = kode)
            End Using
        End Function

        Public Async Function SearchAsync(query As String, Optional limit As Integer = 50) As Task(Of List(Of Barang))
            Using db As New AppDbContext()
                Dim q = query?.Trim()
                If String.IsNullOrWhiteSpace(q) Then
                    Return Await db.Barang.AsNoTracking().OrderBy(Function(b) b.NamaBarang).Take(limit).ToListAsync()
                End If
                Return Await db.Barang.AsNoTracking().
                    Where(Function(b) b.NamaBarang.Contains(q) Or b.KodeBarang.Contains(q)).
                    OrderBy(Function(b) b.NamaBarang).
                    Take(limit).
                    ToListAsync()
            End Using
        End Function

        Public Async Function AddAsync(item As Barang) As Task
            Using db As New AppDbContext()
                db.Barang.Add(item)
                Await db.SaveChangesAsync()
            End Using
        End Function

        Public Async Function UpdateAsync(item As Barang) As Task
            Using db As New AppDbContext()
                db.Barang.Update(item)
                Await db.SaveChangesAsync()
            End Using
        End Function

        Public Async Function DeleteAsync(id As Integer) As Task
            Using db As New AppDbContext()
                Dim existing = Await db.Barang.FindAsync(id)
                If existing IsNot Nothing Then
                    db.Barang.Remove(existing)
                    Await db.SaveChangesAsync()
                End If
            End Using
        End Function
    End Class
End Namespace
