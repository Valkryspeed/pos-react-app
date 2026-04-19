Imports System.Windows
Imports System.Windows.Input
Imports System.Timers

Namespace Helpers
    Public Class BarcodeEngine
        Private Shared _instance As BarcodeEngine
        Public Shared ReadOnly Property Instance As BarcodeEngine
            Get
                If _instance Is Nothing Then _instance = New BarcodeEngine()
                Return _instance
            End Get
        End Property

        Public Event BarcodeScanned(barcode As String)

        Public Property IsPaused As Boolean = False

        Private _buffer As String = ""
        Private _lastInputTime As DateTime = DateTime.MinValue
        Private _clearTimer As Timer
        Private _isScanMode As Boolean = False
        Private _wasLeaked As Boolean = False
        Private ReadOnly _sync As New Object()

        Private _scannerSpeedThresholdMs As Integer = 120
        Private _autoClearTimeoutMs As Integer = 1000
        Private _minimumBarcodeLength As Integer = 6
        Private _acceptTabTerminator As Boolean = True
        Private _autoSubmitOnTimeout As Boolean = True

        Public Property ScannerSpeedThresholdMs As Integer
            Get
                Return _scannerSpeedThresholdMs
            End Get
            Set(value As Integer)
                _scannerSpeedThresholdMs = Math.Max(10, value)
            End Set
        End Property

        Public Property AutoClearTimeoutMs As Integer
            Get
                Return _autoClearTimeoutMs
            End Get
            Set(value As Integer)
                _autoClearTimeoutMs = Math.Max(100, value)
                If _clearTimer IsNot Nothing Then
                    _clearTimer.Interval = _autoClearTimeoutMs
                End If
            End Set
        End Property

        Public Property MinimumBarcodeLength As Integer
            Get
                Return _minimumBarcodeLength
            End Get
            Set(value As Integer)
                _minimumBarcodeLength = Math.Max(1, value)
            End Set
        End Property

        Public Property AcceptTabTerminator As Boolean
            Get
                Return _acceptTabTerminator
            End Get
            Set(value As Boolean)
                _acceptTabTerminator = value
            End Set
        End Property

        Public Property AutoSubmitOnTimeout As Boolean
            Get
                Return _autoSubmitOnTimeout
            End Get
            Set(value As Boolean)
                _autoSubmitOnTimeout = value
            End Set
        End Property

        Public ReadOnly Property WasLeaked As Boolean
            Get
                Return _wasLeaked
            End Get
        End Property

        Private Sub New()
            _clearTimer = New Timer(_autoClearTimeoutMs)
            AddHandler _clearTimer.Elapsed, AddressOf OnClearTimerElapsed
            _clearTimer.AutoReset = False
        End Sub

        Private Sub OnClearTimerElapsed(sender As Object, e As ElapsedEventArgs)
            Dim barcodeToRaise As String = Nothing
            SyncLock _sync
                If _autoSubmitOnTimeout AndAlso _isScanMode AndAlso Not String.IsNullOrWhiteSpace(_buffer) AndAlso _buffer.Length >= _minimumBarcodeLength Then
                    barcodeToRaise = _buffer
                End If
                _buffer = ""
                _isScanMode = False
                _wasLeaked = False
                _lastInputTime = DateTime.MinValue
                _clearTimer.Stop()
            End SyncLock

            If Not String.IsNullOrWhiteSpace(barcodeToRaise) Then
                RaiseBarcodeScannedOnUIThread(barcodeToRaise)
            End If
        End Sub

        Private Sub RaiseBarcodeScannedOnUIThread(barcode As String)
            Dim app = Application.Current
            Dim dispatcher = app?.Dispatcher
            If dispatcher IsNot Nothing AndAlso Not dispatcher.CheckAccess() Then
                dispatcher.BeginInvoke(Sub() RaiseEvent BarcodeScanned(barcode))
                Return
            End If
            RaiseEvent BarcodeScanned(barcode)
        End Sub

        Public Sub ResetLeakedFlag()
            _wasLeaked = False
        End Sub

        Public Function ProcessKeyDown(e As KeyEventArgs) As Boolean
            Dim isTerminator As Boolean =
                (e.Key = Key.Enter) OrElse
                (_acceptTabTerminator AndAlso e.Key = Key.Tab)

            If isTerminator Then
                Dim barcodeToRaise As String = Nothing
                SyncLock _sync
                    If _isScanMode AndAlso Not String.IsNullOrWhiteSpace(_buffer) AndAlso _buffer.Length >= _minimumBarcodeLength Then
                        barcodeToRaise = _buffer
                    End If

                    _buffer = ""
                    _isScanMode = False
                    _wasLeaked = False
                    _lastInputTime = DateTime.MinValue
                End SyncLock

                If Not String.IsNullOrWhiteSpace(barcodeToRaise) Then
                    RaiseBarcodeScannedOnUIThread(barcodeToRaise)
                    Return True
                End If
            End If
            Return False
        End Function

        Public Function ProcessTextInput(text As String) As Boolean
            Dim now = DateTime.Now
            Dim interval = (now - _lastInputTime).TotalMilliseconds

            SyncLock _sync
                If _lastInputTime <> DateTime.MinValue AndAlso interval < _scannerSpeedThresholdMs Then
                    If Not _isScanMode AndAlso _buffer.Length = 1 Then
                        _wasLeaked = True
                    End If
                    _isScanMode = True
                End If

                _buffer &= text
                _lastInputTime = now

                _clearTimer.Stop()
                _clearTimer.Start()

                If _isScanMode Then
                    Return True
                End If
            End SyncLock

            Return False
        End Function

        Public Sub ClearLeakedInput(focusedElement As UIElement)
            If focusedElement IsNot Nothing AndAlso TypeOf focusedElement Is System.Windows.Controls.TextBox Then
                Dim tb = DirectCast(focusedElement, System.Windows.Controls.TextBox)
                If Not String.IsNullOrEmpty(tb.Text) Then
                    ' Hapus 1 karakter terakhir yang bocor
                    tb.Text = tb.Text.Substring(0, tb.Text.Length - 1)
                    ' Pindahkan kursor ke akhir
                    tb.SelectionStart = tb.Text.Length
                End If
            End If
            _wasLeaked = False
        End Sub

        Public Sub BeepSuccess()
            Try
                Console.Beep(1200, 150)
            Catch
                ' Fallback
                System.Media.SystemSounds.Beep.Play()
            End Try
        End Sub

        Public Sub BeepError()
            Try
                Console.Beep(400, 400)
            Catch
                ' Fallback
                System.Media.SystemSounds.Hand.Play()
            End Try
        End Sub
    End Class
End Namespace
