Imports System.Windows.Data
Imports System.Globalization

Namespace Helpers
    Public Class PosNegConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If TypeOf value Is Integer Then
                Dim val = DirectCast(value, Integer)
                If val > 0 Then Return "Positif"
                If val < 0 Then Return "Negatif"
            End If
            Return "Netral"
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
