Imports Microsoft.EntityFrameworkCore
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Services

Namespace Data
    Public Class AppDbContext
        Inherits DbContext

        Public Property Barang As DbSet(Of Barang)
        Public Property Users As DbSet(Of User)
        Public Property Suppliers As DbSet(Of Supplier)
        Public Property Pembelian As DbSet(Of Purchase)
        Public Property PembelianItems As DbSet(Of PurchaseItem)
        Public Property Transaksi As DbSet(Of SaleTransaction)
        Public Property TransaksiItems As DbSet(Of SaleItem)
        Public Property Shifts As DbSet(Of Shift)
        Public Property Pengeluaran As DbSet(Of Expense)
        Public Property ActivityLog As DbSet(Of ActivityLog)
        Public Property ScannerLog As DbSet(Of ScannerLog)
        Public Property StockOpname As DbSet(Of StockOpname)
        Public Property RestockLog As DbSet(Of RestockLog)
        Public Property StockLogs As DbSet(Of StockLog)
        Public Property InvoiceSequences As DbSet(Of InvoiceSequence)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                ' 1. Tentukan lokasi database yang AMAN (AppData/Local)
                ' Gunakan folder yang berbeda untuk versi TRIAL agar tidak mengambil data dari versi pengembangan
                Dim folderName = "POS_JAVISH"
                If Not LicensingService.IsActivated() Then
                    folderName = "POS_JAVISH_TRIAL"
                End If
                
                Dim appDataPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName)
                Dim dbPath = IO.Path.Combine(appDataPath, "sys_data.bin") ' Nama file disamarkan
                
                ' 2. Buat direktori jika belum ada
                If Not IO.Directory.Exists(appDataPath) Then
                    IO.Directory.CreateDirectory(appDataPath)
                End If

                ' 3. MIGRASI OTOMATIS: Pindahkan database lama jika ditemukan
                ' (Fitur ini dinonaktifkan untuk versi Trial agar user selalu mulai dengan DB bersih)
                ' Dim oldDbPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "pos.db")
                ' If IO.File.Exists(oldDbPath) AndAlso Not IO.File.Exists(dbPath) Then
                '     Try
                '         IO.File.Move(oldDbPath, dbPath)
                '         Dim oldDir = IO.Path.GetDirectoryName(oldDbPath)
                '         If IO.Directory.GetFiles(oldDir).Length = 0 Then
                '             IO.Directory.Delete(oldDir)
                '         End If
                '     Catch
                '         dbPath = oldDbPath
                '     End Try
                ' End If

                optionsBuilder.UseSqlite($"Data Source={dbPath}")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Barang)(Sub(entity)
                                               entity.HasKey(Function(b) b.Id)
                                               entity.Property(Function(b) b.Id).HasColumnName("id_barang").ValueGeneratedOnAdd()
                                               entity.Property(Function(b) b.KodeBarang).HasColumnName("kode_barang").IsRequired()
                                               entity.Property(Function(b) b.NamaBarang).HasColumnName("nama_barang").IsRequired()
                                               entity.Property(Function(b) b.HargaModal).HasColumnName("harga_modal")
                                               entity.Property(Function(b) b.HargaJual).HasColumnName("harga_jual")
                                               entity.Property(Function(b) b.Satuan).HasColumnName("satuan")
                                               entity.Property(Function(b) b.Stok).HasColumnName("stok")
                                               entity.Property(Function(b) b.StokMinimum).HasColumnName("stok_minimum")
                                               entity.HasIndex(Function(b) b.KodeBarang).HasDatabaseName("idx_barang_kode")
                                               entity.HasIndex(Function(b) b.NamaBarang).HasDatabaseName("idx_barang_nama")
                                           End Sub)

            modelBuilder.Entity(Of User)(Sub(entity)
                                             entity.HasIndex(Function(u) u.Username).IsUnique()
                                         End Sub)

            modelBuilder.Entity(Of Supplier)(Sub(entity)
                                                 entity.HasIndex(Function(s) s.Nama)
                                             End Sub)

            modelBuilder.Entity(Of Purchase)(Sub(entity)
                                                 entity.HasMany(Function(p) p.Items).WithOne().HasForeignKey(Function(i) i.PurchaseId)
                                             End Sub)

            modelBuilder.Entity(Of SaleTransaction)(Sub(entity)
                                                        entity.HasMany(Function(t) t.Items).WithOne().HasForeignKey(Function(i) i.TransaksiId)
                                                        entity.HasIndex(Function(t) t.NoTransaksi).HasDatabaseName("ux_transaksi_notrx").IsUnique()
                                                    End Sub)

            modelBuilder.Entity(Of InvoiceSequence)(Sub(entity)
                                                        entity.HasIndex(Function(x) New With {x.DateKey, x.Terminal}).HasDatabaseName("ux_invoice_seq_key").IsUnique()
                                                    End Sub)
        End Sub
    End Class
End Namespace
