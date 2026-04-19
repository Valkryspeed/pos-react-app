Imports System.Collections.ObjectModel
Imports POS_JAVISH_WPF.Helpers
Imports POS_JAVISH_WPF.Models
Imports POS_JAVISH_WPF.Services

Namespace ViewModels
    Public Class PosViewModel
        Inherits Helpers.BaseViewModel

        Private ReadOnly _productService As New ProductService()
        Private _searchQuery As String
        Private _items As New ObservableCollection(Of CartItem)()
        Private _diskon As Decimal
        Private _bayar As Decimal

        Public Property SearchQuery As String
            Get
                Return _searchQuery
            End Get
            Set(value As String)
                SetProperty(_searchQuery, value)
            End Set
        End Property

        Public ReadOnly Property Items As ObservableCollection(Of CartItem)
            Get
                Return _items
            End Get
        End Property

        Public Property Diskon As Decimal
            Get
                Return _diskon
            End Get
            Set(value As Decimal)
                SetProperty(_diskon, value)
                OnPropertyChanged(NameOf(Total))
                OnPropertyChanged(NameOf(Kembalian))
            End Set
        End Property

        Public Property Bayar As Decimal
            Get
                Return _bayar
            End Get
            Set(value As Decimal)
                SetProperty(_bayar, value)
                OnPropertyChanged(NameOf(Kembalian))
            End Set
        End Property

        Public ReadOnly Property Subtotal As Decimal
            Get
                Return Items.Sum(Function(i) i.Subtotal)
            End Get
        End Property

        Public ReadOnly Property Total As Decimal
            Get
                Return Math.Max(0D, Subtotal - Diskon)
            End Get
        End Property

        Public ReadOnly Property Kembalian As Decimal
            Get
                Return Math.Max(0D, Bayar - Total)
            End Get
        End Property

        Public ReadOnly Property AddByKodeCommand As RelayCommand = New RelayCommand(Async Sub(param)
                                                                                         Dim kode = TryCast(param, String)
                                                                                         If String.IsNullOrWhiteSpace(kode) Then Return
                                                                                         Dim b = Await _productService.GetByKodeAsync(kode)
                                                                                         If b Is Nothing Then
                                                                                             Return
                                                                                         End If
                                                                                         Dim existing = Items.FirstOrDefault(Function(i) i.BarangId = b.Id)
                                                                                         If existing Is Nothing Then
                                                                                             Items.Add(New CartItem With {
                                 .BarangId = b.Id,
                                 .KodeBarang = b.KodeBarang,
                                 .NamaBarang = b.NamaBarang,
                                 .HargaJual = b.HargaJual,
                                 .Qty = 1
                             })
                                                                                         Else
                                                                                             existing.Qty += 1
                                                                                         End If
                                                                                         OnPropertyChanged(NameOf(Subtotal))
                                                                                         OnPropertyChanged(NameOf(Total))
                                                                                         OnPropertyChanged(NameOf(Kembalian))
                                                                                     End Sub)

        Public ReadOnly Property RemoveItemCommand As RelayCommand = New RelayCommand(Sub(param)
                                                                                          Dim item = TryCast(param, CartItem)
                                                                                          If item Is Nothing Then Return
                                                                                          Items.Remove(item)
                                                                                          OnPropertyChanged(NameOf(Subtotal))
                                                                                          OnPropertyChanged(NameOf(Total))
                                                                                          OnPropertyChanged(NameOf(Kembalian))
                                                                                      End Sub)
    End Class
End Namespace
