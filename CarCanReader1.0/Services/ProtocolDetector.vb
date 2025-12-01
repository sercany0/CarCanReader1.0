' ProtocolDetector.vb
' CAN protokol algılama servisi
' Farklı CAN hızlarını test eder ve uygun protokolü bulur

Imports System.IO.Ports
Imports System.Threading
Imports System.Threading.Tasks

Namespace Services

    ''' <summary>
    ''' CAN protokol algılama servisi
    ''' </summary>
    Public Class ProtocolDetector

#Region "Protocol Definitions"

        Public Enum CANProtocol
            CAN_500K = 500000
            CAN_250K = 250000
            CAN_125K = 125000
        End Enum

        Public Class ProtocolInfo
            Public Property Protocol As CANProtocol
            Public Property Name As String
            Public Property BaudRate As Integer
            Public Property IsDetected As Boolean = False
            Public Property ResponseReceived As Boolean = False
            Public Property ErrorMessage As String = ""

            Public Sub New(protocol As CANProtocol, name As String, baudRate As Integer)
                Me.Protocol = protocol
                Me.Name = name
                Me.BaudRate = baudRate
            End Sub
        End Class

#End Region

#Region "Events"

        Public Event OnProtocolTested(protocol As ProtocolInfo)
        Public Event OnProtocolDetected(protocol As ProtocolInfo)
        Public Event OnError(message As String)

#End Region

#Region "Private Fields"

        Private _serialPort As SerialPort = Nothing
        Private _testTimeout As Integer = 3000 ' 3 saniye
        Private _responseReceived As Boolean = False
        Private ReadOnly _responseLock As New Object()
        Private _cancellationTokenSource As CancellationTokenSource = Nothing

#End Region

#Region "Public Methods"

        ''' <summary>
        ''' Tüm protokolleri test eder
        ''' </summary>
        Public Async Function DetectProtocolAsync(portName As String, baudRate As Integer) As Task(Of ProtocolInfo)
            Try
                ' Protokol listesi
                Dim protocols As New List(Of ProtocolInfo) From {
                    New ProtocolInfo(CANProtocol.CAN_500K, "CAN 500K (High Speed)", 500000),
                    New ProtocolInfo(CANProtocol.CAN_250K, "CAN 250K (Low Speed)", 250000),
                    New ProtocolInfo(CANProtocol.CAN_125K, "CAN 125K", 125000)
                }

                ' Her protokolü test et
                For Each protocolInfo In protocols
                    RaiseEvent OnProtocolTested(protocolInfo)

                    Dim result = Await TestProtocolAsync(portName, baudRate, protocolInfo)
                    If result Then
                        RaiseEvent OnProtocolDetected(protocolInfo)
                        Return protocolInfo
                    End If
                Next

                ' Hiçbiri çalışmadı
                Return Nothing

            Catch ex As Exception
                RaiseEvent OnError($"Protokol algılama hatası: {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Belirli bir protokolü test eder
        ''' </summary>
        Private Async Function TestProtocolAsync(portName As String, baudRate As Integer, protocolInfo As ProtocolInfo) As Task(Of Boolean)
            Try
                ' Port'u aç
                If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
                    _serialPort.Close()
                    Thread.Sleep(100)
                End If

                _serialPort = New SerialPort(portName, baudRate)
                _serialPort.ReadTimeout = _testTimeout
                _serialPort.WriteTimeout = 1000
                _serialPort.DtrEnable = True
                _serialPort.RtsEnable = True

                _serialPort.Open()
                Thread.Sleep(100)

                ' SLCAN modunu aç
                _serialPort.WriteLine("O")
                Thread.Sleep(100)

                ' Protokol hızını ayarla
                Dim speedCmd As String = GetSpeedCommand(protocolInfo.Protocol)
                _serialPort.WriteLine(speedCmd)
                Thread.Sleep(100)

                ' OBD-II test isteği gönder (Mode 01, PID 00)
                SyncLock _responseLock
                    _responseReceived = False
                End SyncLock

                ' DataReceived event'ini bağla
                AddHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived

                ' Test frame'i gönder
                Dim testFrame As String = "t7DF8" & "0201" & "00" & "0000000000"
                _serialPort.WriteLine(testFrame)

                ' Yanıt bekle
                Dim received = Await WaitForResponseAsync(_testTimeout)

                ' Event'i kaldır
                RemoveHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived

                If received Then
                    protocolInfo.IsDetected = True
                    protocolInfo.ResponseReceived = True
                    Return True
                Else
                    protocolInfo.ErrorMessage = "Yanıt alınamadı"
                    Return False
                End If

            Catch ex As Exception
                protocolInfo.ErrorMessage = ex.Message
                Return False
            Finally
                ' Port'u kapat
                Try
                    If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
                        RemoveHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived
                        _serialPort.Close()
                    End If
                Catch
                End Try
            End Try
        End Function

        ''' <summary>
        ''' Protokol hızı için SLCAN komutunu döndürür
        ''' </summary>
        Private Function GetSpeedCommand(protocol As CANProtocol) As String
            Select Case protocol
                Case CANProtocol.CAN_500K
                    Return "S5" ' 500 kbps
                Case CANProtocol.CAN_250K
                    Return "S3" ' 250 kbps
                Case CANProtocol.CAN_125K
                    Return "S2" ' 125 kbps
                Case Else
                    Return "S5"
            End Select
        End Function

        ''' <summary>
        ''' Yanıt bekler
        ''' </summary>
        Private Async Function WaitForResponseAsync(timeoutMs As Integer) As Task(Of Boolean)
            Dim startTime = DateTime.Now

            While (DateTime.Now - startTime).TotalMilliseconds < timeoutMs
                SyncLock _responseLock
                    If _responseReceived Then
                        Return True
                    End If
                End SyncLock
                Await Task.Delay(50)
            End While

            Return False
        End Function

        ''' <summary>
        ''' Serial port data received handler
        ''' </summary>
        Private Sub SerialPort_DataReceived(sender As Object, e As SerialDataReceivedEventArgs)
            Try
                If _serialPort Is Nothing OrElse Not _serialPort.IsOpen Then Return

                While _serialPort.BytesToRead > 0
                    Dim line As String = _serialPort.ReadLine()
                    If Not String.IsNullOrWhiteSpace(line) Then
                        ' OBD-II yanıt kontrolü (0x7E8-0x7EF arası ID'ler)
                        If line.StartsWith("t7E") OrElse line.StartsWith("T7E") Then
                            SyncLock _responseLock
                                _responseReceived = True
                            End SyncLock
                            Return
                        End If
                    End If
                End While
            Catch
                ' Timeout veya okuma hatası - sessizce devam et
            End Try
        End Sub

        ''' <summary>
        ''' Temizlik
        ''' </summary>
        Public Sub Dispose()
            Try
                If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
                    RemoveHandler _serialPort.DataReceived, AddressOf SerialPort_DataReceived
                    _serialPort.Close()
                    _serialPort.Dispose()
                End If
            Catch
            End Try
        End Sub

#End Region

    End Class

End Namespace

