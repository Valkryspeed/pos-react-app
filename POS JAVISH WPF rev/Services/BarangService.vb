Imports Microsoft.Data.Sqlite
Imports POS_JAVISH_WPF.Models

Namespace Services
    Public Class BarangService
        Private Shared Function GetConn() As SqliteConnection
            Dim dbPath = BackupService.GetDbPath()
            Return New SqliteConnection($"Data Source={dbPath}")
        End Function

        Public Shared Function GetByKode(kode As String) As Barang
            Using conn = GetConn()
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT id_barang,kode_barang,nama_barang,harga_modal,harga_jual,satuan,stok,stok_minimum FROM Barang WHERE kode_barang=$kode"
                    cmd.Parameters.AddWithValue("$kode", kode)
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then
                            Dim b As New Barang()
                            b.Id = r.GetInt32(0)
                            b.KodeBarang = r.GetString(1)
                            b.NamaBarang = r.GetString(2)
                            b.HargaModal = r.GetDecimal(3)
                            b.HargaJual = r.GetDecimal(4)
                            b.Satuan = If(Not r.IsDBNull(5), r.GetString(5), Nothing)
                            b.Stok = r.GetInt32(6)
                            b.StokMinimum = r.GetInt32(7)
                            Return b
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Shared Sub UpdateStokDanHargaModal(kode As String, qty As Integer, hargaModal As Decimal, tx As SqliteTransaction)
            Using cmd = tx.Connection.CreateCommand()
                cmd.Transaction = tx
                cmd.CommandText = "UPDATE Barang SET stok = stok + $qty, harga_modal = $harga WHERE kode_barang = $kode"
                cmd.Parameters.AddWithValue("$qty", qty)
                cmd.Parameters.AddWithValue("$harga", hargaModal)
                cmd.Parameters.AddWithValue("$kode", kode)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Public Shared Function GetSuppliers() As List(Of Tuple(Of Integer, String))
            Dim list As New List(Of Tuple(Of Integer, String))()
            Using conn = GetConn()
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT id, nama FROM Suppliers WHERE IsActive = 1 ORDER BY nama"
                    Using r = cmd.ExecuteReader()
                        While r.Read()
                            list.Add(Tuple.Create(r.GetInt32(0), r.GetString(1)))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function
    End Class
End Namespace
