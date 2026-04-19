Imports Microsoft.Data.Sqlite
Imports POS_JAVISH_WPF.Services
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Data
Imports POS_JAVISH_WPF.Models
Imports Microsoft.EntityFrameworkCore

Namespace Services
    Public Class PembelianService
        Private Shared Function GetConn() As SqliteConnection
            Dim dbPath = BackupService.GetDbPath()
            Return New SqliteConnection($"Data Source={dbPath}")
        End Function

        Public Shared Sub SimpanRestock(kodeBarang As String, qty As Integer, hargaModal As Decimal, supplierId As Integer, userId As Integer)
            Dim noFaktur = "PB-" & DateTime.Now.ToString("yyyyMMdd-HHmmss")
            Dim total = hargaModal * qty
            Using conn = GetConn()
                conn.Open()
                Using tx = conn.BeginTransaction()
                    Dim barangId As Integer = 0
                    Dim namaBarang As String = ""
                    Dim satuan As String = ""
                    Dim hargaJual As Decimal = 0D
                    Dim stokBefore As Integer = 0
                    Using cmdInfo = conn.CreateCommand()
                        cmdInfo.Transaction = tx
                        cmdInfo.CommandText = "SELECT id_barang, nama_barang, satuan, harga_jual, stok FROM Barang WHERE kode_barang = $kode"
                        cmdInfo.Parameters.AddWithValue("$kode", kodeBarang)
                        Using r = cmdInfo.ExecuteReader()
                            If r.Read() Then
                                barangId = r.GetInt32(0)
                                namaBarang = r.GetString(1)
                                satuan = If(Not r.IsDBNull(2), r.GetString(2), "")
                                hargaJual = r.GetDecimal(3)
                                stokBefore = r.GetInt32(4)
                            End If
                        End Using
                    End Using

                    BarangService.UpdateStokDanHargaModal(kodeBarang, qty, hargaModal, tx)

                    Dim pembelianId As Long
                    Using cmd2 = conn.CreateCommand()
                        cmd2.Transaction = tx
                        cmd2.CommandText = "INSERT INTO Pembelian (NoFaktur, Tanggal, SupplierId, Total, UserId) VALUES ($no, $tgl, $sup, $tot, $uid)"
                        cmd2.Parameters.AddWithValue("$no", noFaktur)
                        cmd2.Parameters.AddWithValue("$tgl", DateTime.Now)
                        cmd2.Parameters.AddWithValue("$sup", supplierId)
                        cmd2.Parameters.AddWithValue("$tot", total)
                        cmd2.Parameters.AddWithValue("$uid", userId)
                        cmd2.ExecuteNonQuery()
                    End Using
                    Using cmdId = conn.CreateCommand()
                        cmdId.Transaction = tx
                        cmdId.CommandText = "SELECT last_insert_rowid()"
                        pembelianId = CLng(cmdId.ExecuteScalar())
                    End Using

                    Using cmd3 = conn.CreateCommand()
                        cmd3.Transaction = tx
                        cmd3.CommandText = "INSERT INTO PembelianItems (PurchaseId, BarangId, Qty, HargaBeli, Subtotal) " &
                                           "SELECT $pid, id_barang, $qty, $harga, $sub FROM Barang WHERE kode_barang = $kode"
                        cmd3.Parameters.AddWithValue("$pid", pembelianId)
                        cmd3.Parameters.AddWithValue("$qty", qty)
                        cmd3.Parameters.AddWithValue("$harga", hargaModal)
                        cmd3.Parameters.AddWithValue("$sub", total)
                        cmd3.Parameters.AddWithValue("$kode", kodeBarang)
                        cmd3.ExecuteNonQuery()
                    End Using

                    ' Activity Log
                    Using cmd4 = conn.CreateCommand()
                        cmd4.Transaction = tx
                        cmd4.CommandText = "INSERT INTO ActivityLog (UserId, Username, Activity, Timestamp, Detail) VALUES ($uid, $uname, $act, $ts, $detail)"
                        Dim uname = If(Session.CurrentUser?.Username, "")
                        Dim supplierName As String = ""
                        If supplierId > 0 Then
                            Using cmdSup = conn.CreateCommand()
                                cmdSup.Transaction = tx
                                cmdSup.CommandText = "SELECT nama FROM Suppliers WHERE id = $id"
                                cmdSup.Parameters.AddWithValue("$id", supplierId)
                                Dim res = cmdSup.ExecuteScalar()
                                If res IsNot Nothing AndAlso Not IsDBNull(res) Then supplierName = CStr(res)
                            End Using
                        End If
                        Dim role = If(Session.CurrentUser?.Role, "")
                        Dim stokAfter = stokBefore + qty
                        Dim detail = $"NoFaktur={noFaktur}; BarangId={barangId}; Kode={kodeBarang}; Nama={namaBarang}; Satuan={satuan}; Qty={qty}; StokBefore={stokBefore}; StokAfter={stokAfter}; HargaModal={hargaModal}; HargaJual={hargaJual}; Total={total}; SupplierId={supplierId}; SupplierName={supplierName}; UserId={userId}; Role={role}"
                        cmd4.Parameters.AddWithValue("$uid", userId)
                        cmd4.Parameters.AddWithValue("$uname", uname)
                        cmd4.Parameters.AddWithValue("$act", "RESTOCK")
                        cmd4.Parameters.AddWithValue("$ts", DateTime.Now)
                        cmd4.Parameters.AddWithValue("$detail", detail)
                        cmd4.ExecuteNonQuery()
                    End Using

                    ' Stock Log
                    Using cmdStockLog = conn.CreateCommand()
                        cmdStockLog.Transaction = tx
                        cmdStockLog.CommandText = "INSERT INTO StockLog (BarangId, KodeBarang, NamaBarang, Tanggal, Tipe, QtyAwal, QtyPerubahan, QtyAkhir, Keterangan, UserId, Username) " &
                                                 "VALUES ($bid, $kode, $nama, $ts, $tipe, $qawal, $qchg, $qakhir, $ket, $uid, $uname)"
                        cmdStockLog.Parameters.AddWithValue("$bid", barangId)
                        cmdStockLog.Parameters.AddWithValue("$kode", kodeBarang)
                        cmdStockLog.Parameters.AddWithValue("$nama", namaBarang)
                        cmdStockLog.Parameters.AddWithValue("$ts", DateTime.Now)
                        cmdStockLog.Parameters.AddWithValue("$tipe", "PEMBELIAN")
                        cmdStockLog.Parameters.AddWithValue("$qawal", stokBefore)
                        cmdStockLog.Parameters.AddWithValue("$qchg", qty)
                        cmdStockLog.Parameters.AddWithValue("$qakhir", stokBefore + qty)
                        cmdStockLog.Parameters.AddWithValue("$ket", $"Pembelian No. Faktur {noFaktur}")
                        cmdStockLog.Parameters.AddWithValue("$uid", userId)
                        cmdStockLog.Parameters.AddWithValue("$uname", If(Session.CurrentUser?.Username, "System"))
                        cmdStockLog.ExecuteNonQuery()
                    End Using

                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Shared Async Function HapusPembelianAsync(noFaktur As String, logId As Integer) As Task
            Using db As New AppDbContext()
                Using tx = Await db.Database.BeginTransactionAsync()
                    Try
                        ' 1. Find the purchase record
                        Dim purchase = Await db.Pembelian.Include(Function(p) p.Items).
                            FirstOrDefaultAsync(Function(p) p.NoFaktur = noFaktur)

                        If purchase IsNot Nothing Then
                            ' 2. Revert stock changes
                            For Each item In purchase.Items
                                Dim barang = Await db.Barang.FindAsync(item.BarangId)
                                If barang IsNot Nothing Then
                                    Dim qtyAwal = barang.Stok
                                    barang.Stok -= item.Qty
                                    
                                    ' LOG STOK: Pembatalan Pembelian
                                    db.StockLogs.Add(New StockLog With {
                                        .BarangId = barang.Id,
                                        .KodeBarang = barang.KodeBarang,
                                        .NamaBarang = barang.NamaBarang,
                                        .Tanggal = Date.Now,
                                        .Tipe = "VOID_PEMBELIAN",
                                        .QtyAwal = qtyAwal,
                                        .QtyPerubahan = -item.Qty,
                                        .QtyAkhir = barang.Stok,
                                        .Keterangan = $"Batal Faktur {noFaktur}",
                                        .UserId = If(Session.CurrentUser?.Id, 0),
                                        .Username = If(Session.CurrentUser?.Username, "System")
                                    })
                                End If
                            Next
                            ' 3. Delete purchase items & purchase
                            db.PembelianItems.RemoveRange(purchase.Items)
                            db.Pembelian.Remove(purchase)
                        End If

                        ' 4. Delete the activity log entry
                        Dim log = Await db.ActivityLog.FindAsync(logId)
                        If log IsNot Nothing Then
                            db.ActivityLog.Remove(log)
                        End If

                        Await db.SaveChangesAsync()
                        Await tx.CommitAsync()
                    Catch ex As Exception
                        Try
                            tx.Rollback()
                        Catch
                            ' Ignore rollback error
                        End Try
                        Throw
                    End Try
                End Using
            End Using
        End Function
    End Class
End Namespace
