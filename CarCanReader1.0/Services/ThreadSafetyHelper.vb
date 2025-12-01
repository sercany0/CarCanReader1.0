' ThreadSafetyHelper.vb
' Thread-safe operasyonlar için yardımcı sınıflar
'
' Kullanım:
'   EventHelper.SafeRaise(MyEvent, Me, args)
'   UIHelper.SafeInvoke(form, Sub() label.Text = "test")

Imports System.Collections.Concurrent
Imports System.Threading

Namespace Services

#Region "Event Helper"

    ''' <summary>
    ''' Thread-safe event raise işlemleri için yardımcı sınıf
    ''' </summary>
    Public NotInheritable Class EventHelper

        ''' <summary>
        ''' Delegate'i null-check yaparak güvenli şekilde çağırır
        ''' </summary>
        Public Shared Sub SafeRaise(Of T As EventArgs)(handler As EventHandler(Of T), sender As Object, args As T)
            Dim h = handler
            If h IsNot Nothing Then
                Try
                    h.Invoke(sender, args)
                Catch ex As Exception
                    Debug.WriteLine($"Event raise error: {ex.Message}")
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Basit EventHandler için null-check ile güvenli çağırma
        ''' </summary>
        Public Shared Sub SafeRaise(handler As EventHandler, sender As Object, args As EventArgs)
            Dim h = handler
            If h IsNot Nothing Then
                Try
                    h.Invoke(sender, args)
                Catch ex As Exception
                    Debug.WriteLine($"Event raise error: {ex.Message}")
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Parametresiz event delegate için güvenli çağırma
        ''' </summary>
        Public Shared Sub SafeRaise(handler As [Delegate])
            If handler IsNot Nothing Then
                Try
                    handler.DynamicInvoke()
                Catch ex As Exception
                    Debug.WriteLine($"Event raise error: {ex.Message}")
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Tek parametreli event delegate için güvenli çağırma
        ''' </summary>
        Public Shared Sub SafeRaise(Of T)(handler As Action(Of T), arg As T)
            Dim h = handler
            If h IsNot Nothing Then
                Try
                    h.Invoke(arg)
                Catch ex As Exception
                    Debug.WriteLine($"Event raise error: {ex.Message}")
                End Try
            End If
        End Sub

    End Class

#End Region

#Region "UI Helper"

    ''' <summary>
    ''' UI thread güncellemeleri için yardımcı sınıf
    ''' </summary>
    Public NotInheritable Class UIHelper

        ''' <summary>
        ''' Control için thread-safe invoke
        ''' </summary>
        Public Shared Sub SafeInvoke(control As Control, action As Action)
            If control Is Nothing OrElse action Is Nothing Then Return
            If control.IsDisposed OrElse control.Disposing Then Return

            Try
                If control.InvokeRequired Then
                    control.BeginInvoke(action)
                Else
                    action.Invoke()
                End If
            Catch ex As ObjectDisposedException
                ' Control disposed olmuş, ignore
            Catch ex As InvalidOperationException
                ' Handle oluşturulmamış, ignore
            Catch ex As Exception
                Debug.WriteLine($"UIHelper.SafeInvoke error: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Sonuç döndüren thread-safe invoke
        ''' </summary>
        Public Shared Function SafeInvoke(Of T)(control As Control, func As Func(Of T), defaultValue As T) As T
            If control Is Nothing OrElse func Is Nothing Then Return defaultValue
            If control.IsDisposed OrElse control.Disposing Then Return defaultValue

            Try
                If control.InvokeRequired Then
                    Return CType(control.Invoke(func), T)
                Else
                    Return func.Invoke()
                End If
            Catch ex As Exception
                Debug.WriteLine($"UIHelper.SafeInvoke<T> error: {ex.Message}")
                Return defaultValue
            End Try
        End Function

        ''' <summary>
        ''' Async bekleme ile invoke (timeout destekli)
        ''' </summary>
        Public Shared Async Function SafeInvokeAsync(control As Control, action As Action, Optional timeoutMs As Integer = 5000) As Task(Of Boolean)
            If control Is Nothing OrElse action Is Nothing Then Return False
            If control.IsDisposed OrElse control.Disposing Then Return False

            Try
                Dim tcs As New TaskCompletionSource(Of Boolean)()
                Dim cts As New CancellationTokenSource(timeoutMs)

                cts.Token.Register(Sub() tcs.TrySetResult(False))

                If control.InvokeRequired Then
                    control.BeginInvoke(Sub()
                                            Try
                                                action.Invoke()
                                                tcs.TrySetResult(True)
                                            Catch
                                                tcs.TrySetResult(False)
                                            End Try
                                        End Sub)
                Else
                    action.Invoke()
                    tcs.TrySetResult(True)
                End If

                Return Await tcs.Task

            Catch ex As Exception
                Debug.WriteLine($"UIHelper.SafeInvokeAsync error: {ex.Message}")
                Return False
            End Try
        End Function

    End Class

#End Region

#Region "Thread-Safe Buffer"

    ''' <summary>
    ''' Thread-safe string buffer (Serial port okuma için)
    ''' </summary>
    Public Class ThreadSafeBuffer
        Private ReadOnly _queue As New ConcurrentQueue(Of String)()
        Private ReadOnly _maxSize As Integer
        Private _totalCount As Long = 0

        Public Sub New(Optional maxSize As Integer = 10000)
            _maxSize = maxSize
        End Sub

        ''' <summary>
        ''' Buffer'a string ekler
        ''' </summary>
        Public Sub Enqueue(item As String)
            _queue.Enqueue(item)
            Interlocked.Increment(_totalCount)

            ' Overflow koruması
            While _queue.Count > _maxSize
                Dim discarded As String = Nothing
                _queue.TryDequeue(discarded)
            End While
        End Sub

        ''' <summary>
        ''' Buffer'dan string alır
        ''' </summary>
        Public Function TryDequeue(ByRef item As String) As Boolean
            Return _queue.TryDequeue(item)
        End Function

        ''' <summary>
        ''' Tüm öğeleri alır ve buffer'ı temizler
        ''' </summary>
        Public Function DequeueAll() As List(Of String)
            Dim items As New List(Of String)()
            Dim item As String = Nothing

            While _queue.TryDequeue(item)
                items.Add(item)
            End While

            Return items
        End Function

        ''' <summary>
        ''' Buffer'daki öğe sayısı
        ''' </summary>
        Public ReadOnly Property Count As Integer
            Get
                Return _queue.Count
            End Get
        End Property

        ''' <summary>
        ''' Toplam eklenen öğe sayısı
        ''' </summary>
        Public ReadOnly Property TotalProcessed As Long
            Get
                Return _totalCount
            End Get
        End Property

        ''' <summary>
        ''' Buffer boş mu?
        ''' </summary>
        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return _queue.IsEmpty
            End Get
        End Property

        ''' <summary>
        ''' Buffer'ı temizler
        ''' </summary>
        Public Sub Clear()
            Dim item As String = Nothing
            While _queue.TryDequeue(item)
            End While
        End Sub

    End Class

#End Region

#Region "Timeout Helper"

    ''' <summary>
    ''' Timeout mekanizması için yardımcı sınıf
    ''' </summary>
    Public NotInheritable Class TimeoutHelper

        ''' <summary>
        ''' Action'ı timeout ile çalıştırır
        ''' </summary>
        Public Shared Function RunWithTimeout(action As Action, timeoutMs As Integer) As Boolean
            Try
                Dim runTask As Task = Task.Run(action)
                Return runTask.Wait(timeoutMs)
            Catch ex As AggregateException
                Debug.WriteLine($"RunWithTimeout error: {ex.InnerException?.Message}")
                Return False
            Catch ex As Exception
                Debug.WriteLine($"RunWithTimeout error: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Async action'ı timeout ile çalıştırır
        ''' </summary>
        Public Shared Async Function RunWithTimeoutAsync(Of T)(asyncFunc As Func(Of Task(Of T)), timeoutMs As Integer, defaultValue As T) As Task(Of T)
            Try
                Using cts As New CancellationTokenSource(timeoutMs)
                    Dim mainTask As Task(Of T) = asyncFunc()
                    Dim delayTask As Task = Task.Delay(timeoutMs, cts.Token)
                    Dim completedTask As Task = Await Task.WhenAny(mainTask, delayTask)

                    If completedTask Is mainTask Then
                        cts.Cancel()
                        Return Await mainTask
                    Else
                        Return defaultValue
                    End If
                End Using
            Catch ex As Exception
                Debug.WriteLine($"RunWithTimeoutAsync error: {ex.Message}")
                Return defaultValue
            End Try
        End Function

    End Class

#End Region

#Region "Disposable Base"

    ''' <summary>
    ''' IDisposable pattern için base sınıf
    ''' </summary>
    Public MustInherit Class DisposableBase
        Implements IDisposable

        Private _isDisposed As Boolean = False
        Protected ReadOnly _disposeLock As New Object()

        ''' <summary>
        ''' Disposed oldu mu?
        ''' </summary>
        Public ReadOnly Property IsDisposed As Boolean
            Get
                Return _isDisposed
            End Get
        End Property

        ''' <summary>
        ''' Managed kaynakları temizle (override edilmeli)
        ''' </summary>
        Protected MustOverride Sub DisposeManagedResources()

        ''' <summary>
        ''' Unmanaged kaynakları temizle (override edilebilir)
        ''' </summary>
        Protected Overridable Sub DisposeUnmanagedResources()
            ' Base implementation boş
        End Sub

        ''' <summary>
        ''' Dispose pattern implementasyonu
        ''' </summary>
        Protected Overridable Sub Dispose(disposing As Boolean)
            SyncLock _disposeLock
                If _isDisposed Then Return

                If disposing Then
                    ' Managed kaynakları temizle
                    Try
                        DisposeManagedResources()
                    Catch ex As Exception
                        Debug.WriteLine($"DisposeManagedResources error: {ex.Message}")
                    End Try
                End If

                ' Unmanaged kaynakları temizle
                Try
                    DisposeUnmanagedResources()
                Catch ex As Exception
                    Debug.WriteLine($"DisposeUnmanagedResources error: {ex.Message}")
                End Try

                _isDisposed = True
            End SyncLock
        End Sub

        ''' <summary>
        ''' IDisposable.Dispose
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        ''' <summary>
        ''' Finalizer
        ''' </summary>
        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub

        ''' <summary>
        ''' Disposed kontrolü (exception fırlat)
        ''' </summary>
        Protected Sub ThrowIfDisposed()
            If _isDisposed Then
                Throw New ObjectDisposedException(Me.GetType().Name)
            End If
        End Sub

    End Class

#End Region

#Region "Rate Limiter"

    ''' <summary>
    ''' İşlem hızı sınırlayıcı (flooding önleme)
    ''' </summary>
    Public Class RateLimiter
        Private ReadOnly _minIntervalMs As Integer
        Private _lastExecutionTime As DateTime = DateTime.MinValue
        Private ReadOnly _lock As New Object()

        Public Sub New(minIntervalMs As Integer)
            _minIntervalMs = minIntervalMs
        End Sub

        ''' <summary>
        ''' Action'ı rate limit uygulayarak çalıştırır
        ''' </summary>
        Public Function TryExecute(action As Action) As Boolean
            SyncLock _lock
                Dim now = DateTime.Now
                Dim elapsed = (now - _lastExecutionTime).TotalMilliseconds

                If elapsed >= _minIntervalMs Then
                    _lastExecutionTime = now
                    Try
                        action?.Invoke()
                        Return True
                    Catch ex As Exception
                        Debug.WriteLine($"RateLimiter execute error: {ex.Message}")
                        Return False
                    End Try
                End If

                Return False
            End SyncLock
        End Function

        ''' <summary>
        ''' Son çalıştırmadan bu yana geçen süre
        ''' </summary>
        Public ReadOnly Property ElapsedSinceLastExecution As TimeSpan
            Get
                Return DateTime.Now - _lastExecutionTime
            End Get
        End Property

    End Class

#End Region

End Namespace

