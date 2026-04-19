Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema

Namespace Models
    <Table("Barang")>
    Public Class Barang
        <Key>
        <Column("id_barang")>
        Public Property Id As Integer

        <Required>
        <Column("kode_barang")>
        Public Property KodeBarang As String

        <Required>
        <Column("nama_barang")>
        Public Property NamaBarang As String

        <Required>
        <Column("harga_modal", TypeName:="DECIMAL(15,2)")>
        Public Property HargaModal As Decimal

        <Required>
        <Column("harga_jual", TypeName:="DECIMAL(15,2)")>
        Public Property HargaJual As Decimal

        <Column("satuan")>
        Public Property Satuan As String

        <Column("stok")>
        Public Property Stok As Integer

        <Column("stok_minimum")>
        Public Property StokMinimum As Integer
    End Class
End Namespace
