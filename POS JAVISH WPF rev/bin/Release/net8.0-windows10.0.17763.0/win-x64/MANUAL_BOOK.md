# MANUAL BOOK - POS JAVISH (Versi Trial)

Selamat datang di **POS JAVISH**, solusi Point of Sale (Kasir) modern untuk bisnis Anda. Dokumen ini akan memandu Anda dalam mengoperasikan aplikasi dari awal hingga akhir.

---

## 1. Persiapan Awal
### Cara Menjalankan Aplikasi
1. Buka folder **POS_JAVISH_TRIAL**.
2. Klik dua kali pada file **POS JAVISH WPF.exe**.
3. Aplikasi akan terbuka. Jika muncul peringatan Windows SmartScreen, klik "More Info" lalu "Run Anyway" (karena aplikasi ini bersifat portable).

### Login Pertama Kali
- **Username Default**: `admin`
- **Password Default**: `admin`
*(Disarankan untuk segera mengubah password di menu Manajemen User demi keamanan).*

---

## 2. Manajemen Produk (Data Barang)
Menu ini digunakan untuk mengelola stok barang Anda.
- **Tambah Barang**: Klik tombol "TAMBAH PRODUK", isi kode barcode, nama, harga modal, harga jual, dan stok awal.
- **Edit/Hapus**: Gunakan ikon pensil untuk mengedit atau ikon tempat sampah untuk menghapus di daftar barang.
- **Fitur Excel (Backup/Restore)**:
    - **Export**: Klik "EXPORT" untuk menyimpan data barang Anda ke file Excel (.xlsx). Ini berguna sebagai backup.
    - **Import**: Klik "IMPORT" untuk memasukkan data barang secara massal dari file Excel. Pastikan format kolom sesuai (Kode, Nama, Modal, Jual, Satuan, Stok, Stok Minimum).

---

## 3. Transaksi Kasir
Menu utama untuk melakukan penjualan.
- **Scan Barcode**: Cukup arahkan scanner ke barcode barang, item akan otomatis masuk ke keranjang.
- **Cari Manual**: Ketik nama barang di kotak pencarian jika tidak ada barcode.
- **Pembayaran**: 
    1. Tekan tombol **BAYAR** atau tekan **Enter** pada kotak nominal bayar.
    2. Masukkan jumlah uang pelanggan.
    3. Tekan Enter untuk konfirmasi dan cetak struk.
- **Hapus Item**: Jika ada kesalahan input, klik ikon tempat sampah merah pada baris barang di keranjang belanja.

---

## 4. Fitur Tambahan (Modern)
### HP Jadi Scanner
1. Buka menu **Pengaturan**.
2. Lihat alamat di bagian **Mobile Monitoring**.
3. Buka alamat tersebut di browser HP Anda.
4. Pilih mode **SCAN KASIR** untuk mulai men-scan barang dari HP.

### Layar Pembeli (Customer Display)
1. Siapkan Tablet atau HP bekas, hadapkan ke arah pembeli.
2. Buka browser dan masukkan alamat **Customer Display** yang ada di menu **Pengaturan**.
3. Layar akan otomatis menampilkan daftar belanjaan setiap kali kasir melakukan scan di PC.

---

## 5. Sistem Shift
Aplikasi ini menggunakan sistem Shift untuk memantau uang masuk per sesi kerja.
- **Buka Shift**: Dilakukan di awal hari/sesi. Masukkan modal awal di laci kasir.
- **Tutup Shift**: Dilakukan di akhir sesi. Aplikasi akan menampilkan total penjualan, uang tunai yang seharusnya ada, dan laporan ringkas.

---

## 5. Dashboard & Laporan
- **Dashboard**: Menampilkan ringkasan penjualan hari ini, total produk, barang yang stoknya habis, dan grafik penjualan sederhana.
- **Laporan**: Anda bisa melihat riwayat transaksi, laporan laba rugi, dan riwayat stok masuk di menu-menu terkait (khusus Admin).

---

## 6. Pengaturan & Aktivasi
### Mode Demo (Trial)
- Anda dapat mencoba seluruh fitur utama (Kasir, Excel, Profil Toko).
- Batas maksimal transaksi adalah **100 Transaksi**.
- Sisa kuota transaksi dapat dilihat di bagian bawah Sidebar kiri.

### Aktivasi Versi Full
1. Buka menu **Pengaturan**.
2. Klik tombol **AKTIVASI**.
3. Salin **Hardware ID** Anda dan kirimkan ke Admin/Developer untuk mendapatkan **Serial Number**.
4. Masukkan Serial Number yang diterima dan klik **AKTIFKAN**.

---

## Tips Keamanan
- Jangan bagikan password Admin Anda kepada staf.
- Lakukan **Export Excel** secara berkala sebagai backup data barang Anda.
- Pastikan printer struk sudah terpasang dan dipilih di menu Pengaturan.

---

## 7. Troubleshooting Koneksi (Penting)
Jika HP Scanner atau Layar Pembeli tidak bisa dibuka, kemungkinan besar diblokir oleh **Windows Firewall**.

### Cara Cepat (Rekomendasi):
1. Klik **Start**, ketik **PowerShell**.
2. Klik kanan **Windows PowerShell**, pilih **Run as Administrator**.
3. Salin dan tempel perintah berikut lalu tekan Enter:
   ```powershell
   New-NetFirewallRule -DisplayName "POS JAVISH Web Services" -Direction Inbound -LocalPort 5050,5051 -Protocol TCP -Action Allow
   ```

---
**POS JAVISH - Enterprise Point of Sale**
*Solusi cerdas untuk kelola bisnis Anda.*
