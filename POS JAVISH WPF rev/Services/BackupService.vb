Namespace Services
    Public Class BackupService
        Public Shared Function GetDbPath() As String
            ' Sinkronkan dengan AppDbContext: Bedakan folder untuk mode TRIAL
            Dim folderName = "POS_JAVISH"
            If Not LicensingService.IsActivated() Then
                folderName = "POS_JAVISH_TRIAL"
            End If

            Dim appDataPath = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName)
            Return IO.Path.Combine(appDataPath, "sys_data.bin")
        End Function

        Public Shared Function BackupNow(Optional backupDir As String = Nothing) As String
            Dim source = GetDbPath()
            If Not IO.File.Exists(source) Then Return Nothing ' Jika db belum ada

            If String.IsNullOrWhiteSpace(backupDir) Then
                backupDir = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup")
            End If
            IO.Directory.CreateDirectory(backupDir)
            Dim dest = IO.Path.Combine(backupDir, $"backup_pos_{Date.Now:yyyyMMdd_HHmmss}.db")
            IO.File.Copy(source, dest, True)
            Return dest
        End Function

        ''' <summary>
        ''' Melakukan backup otomatis ke folder khusus setiap kali aplikasi dijalankan.
        ''' Hanya menyimpan 7 backup terakhir untuk menghemat ruang.
        ''' </summary>
        Public Shared Sub AutoBackup()
            Try
                Dim source = GetDbPath()
                If Not IO.File.Exists(source) Then Return

                Dim autoBackupDir = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup", "Auto")
                IO.Directory.CreateDirectory(autoBackupDir)

                ' Nama file: auto_pos_20240322.db (per hari satu file)
                Dim dest = IO.Path.Combine(autoBackupDir, $"auto_pos_{Date.Now:yyyyMMdd}.db")
                
                ' Jika file hari ini sudah ada, tidak perlu backup lagi
                If Not IO.File.Exists(dest) Then
                    IO.File.Copy(source, dest, True)
                End If

                ' PEMBERSIHAN: Hapus backup yang lebih tua dari 7 hari
                Dim dirInfo As New IO.DirectoryInfo(autoBackupDir)
                Dim files = dirInfo.GetFiles("auto_pos_*.db").OrderByDescending(Function(f) f.CreationTime).ToList()
                
                If files.Count > 7 Then
                    For i As Integer = 7 To files.Count - 1
                        files(i).Delete()
                    Next
                End If
            Catch
                ' Abaikan error saat auto backup agar tidak mengganggu jalannya aplikasi
            End Try
        End Sub
    End Class
End Namespace
