Imports System.IO.Ports
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Coordinates connection-related flows (wizard, quick connect, status updates) outside MainForm UI code.
Public Class ConnectionFlowManager
    Private ReadOnly _serialPort As SerialPort
    Private ReadOnly _updateStatus As Action(Of Boolean, String, String)
    Private ReadOnly _log As Action(Of String)
    Private ReadOnly _uiInvoker As Action(Of Action)
    Private ReadOnly _lastDataUpdater As Action(Of DateTime)

    Public Sub New(serialPort As SerialPort, updateStatus As Action(Of Boolean, String, String), log As Action(Of String), uiInvoker As Action(Of Action), lastDataUpdater As Action(Of DateTime))
        _serialPort = serialPort
        _updateStatus = updateStatus
        _log = log
        _uiInvoker = uiInvoker
        _lastDataUpdater = lastDataUpdater
    End Sub

    Public Sub CheckAndShowWizardIfNeeded()
        Dim lastPort = Config.Serial.LastPort
        Dim lastBaudRate = Config.Serial.LastBaudRate
        Dim lastProtocol = Config.Serial.LastProtocol
        Dim lastSuccess = Config.Serial.LastConnectionSuccessful

        If String.IsNullOrEmpty(lastPort) OrElse Not lastSuccess Then
            Dim result = MessageBox.Show(
                "Bağlantı kurulum sihirbazını başlatmak ister misiniz?" & vbCrLf &
                "Wizard size adım adım bağlantı kurmanızda yardımcı olacak.",
                "Bağlantı Wizard'ı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ShowConnectionWizard()
            End If
        Else
            TryQuickConnect()
        End If
    End Sub

    Public Sub ShowConnectionWizard()
        Dim wizard As New ConnectionWizard()
        If wizard.ShowDialog() = DialogResult.OK Then
            If wizard.ConnectionSuccessful AndAlso Not String.IsNullOrEmpty(wizard.SelectedPort) Then
                ConnectWithWizardSettings(wizard.SelectedPort, wizard.SelectedBaudRate, wizard.SelectedProtocol)
                SaveConnectionInfo(wizard.SelectedPort, wizard.SelectedBaudRate, wizard.SelectedProtocol)
                _log?.Invoke($"💾 Son bağlantı kaydedildi: {wizard.SelectedPort} ({wizard.SelectedBaudRate}, {wizard.SelectedProtocol})")
            End If
        End If
    End Sub

    Public Sub TryQuickConnect()
        Dim lastPort = Config.Serial.LastPort
        Dim lastBaudRate = Config.Serial.LastBaudRate

        If String.IsNullOrEmpty(lastPort) Then
            Return
        End If

        _log?.Invoke($"🔄 Son bağlantı deneniyor: {lastPort} ({lastBaudRate})")

        Dim connectTask = Task.Run(Sub()
                                       Try
                                           If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
                                               _serialPort.Close()
                                               Threading.Thread.Sleep(100)
                                           End If

                                           _serialPort.PortName = lastPort
                                           _serialPort.BaudRate = lastBaudRate
                                           _serialPort.ReadTimeout = 2000
                                           _serialPort.WriteTimeout = 2000
                                           _serialPort.DtrEnable = True
                                           _serialPort.RtsEnable = True

                                           _serialPort.Open()
                                           Threading.Thread.Sleep(100)
                                           _serialPort.WriteLine("O")
                                           Threading.Thread.Sleep(100)

                                           _uiInvoker?.Invoke(Sub()
                                                                  _updateStatus(True, lastPort, lastBaudRate.ToString())
                                                                  _lastDataUpdater(DateTime.Now)
                                                                  _log?.Invoke($"✅ Hızlı bağlantı başarılı: {lastPort}")
                                                              End Sub)
                                       Catch ex As Exception
                                           _uiInvoker?.Invoke(Sub()
                                                                  _updateStatus(False, "", "")
                                                                  _log?.Invoke($"❌ Hızlı bağlantı başarısız: {ex.Message}")
                                                                  SuggestWizard("Hızlı bağlantı başarısız oldu")
                                                              End Sub)
                                       End Try
                                   End Sub)

        If Not connectTask.Wait(5000) Then
            _log?.Invoke("⏱️ Hızlı bağlantı timeout")
            _updateStatus(False, "", "")
            SuggestWizard("Bağlantı timeout")
        End If
    End Sub

    Public Sub ConnectWithWizardSettings(port As String, baudRate As Integer, protocol As String)
        If _serialPort IsNot Nothing AndAlso _serialPort.IsOpen Then
            _serialPort.Close()
            Threading.Thread.Sleep(100)
        End If

        _serialPort.PortName = port
        _serialPort.BaudRate = baudRate
        _serialPort.ReadTimeout = 2000
        _serialPort.WriteTimeout = 2000
        _serialPort.DtrEnable = True
        _serialPort.RtsEnable = True

        _serialPort.Open()
        Threading.Thread.Sleep(100)
        _serialPort.WriteLine("O")
        Threading.Thread.Sleep(100)

        If Not String.IsNullOrEmpty(protocol) Then
            If protocol.Contains("500K") Then
                _serialPort.WriteLine("S5")
            ElseIf protocol.Contains("250K") Then
                _serialPort.WriteLine("S3")
            ElseIf protocol.Contains("125K") Then
                _serialPort.WriteLine("S2")
            End If
        End If

        _updateStatus(True, port, baudRate.ToString())
        _lastDataUpdater(DateTime.Now)
        _log?.Invoke($"✅ Wizard ile bağlandı: {port} ({baudRate}, {protocol})")
    End Sub

    Public Sub SaveConnectionInfo(port As String, baudRate As Integer, protocol As String)
        Config.Serial.LastPort = port
        Config.Serial.LastBaudRate = baudRate
        Config.Serial.LastProtocol = protocol
        Config.Serial.LastConnectionSuccessful = True
        Config.Serial.LastConnectionTime = DateTime.Now
        Config.Save()
    End Sub

    Public Sub UpdateConnectionStatus(isConnected As Boolean, port As String, baudRate As String)
        If isConnected AndAlso Not String.IsNullOrEmpty(port) Then
            Dim protocol = Config.Serial.LastProtocol
            If Not String.IsNullOrEmpty(protocol) Then
                _updateStatus(True, port, $"{baudRate}, {protocol}")
            Else
                _updateStatus(True, port, baudRate)
            End If
        Else
            _updateStatus(False, "", "")
        End If
    End Sub

    Public Sub SuggestWizard(reason As String)
        Dim result = MessageBox.Show(
            $"Bağlantı kurulamadı: {reason}" & vbCrLf & vbCrLf &
            "Bağlantı kurulum sihirbazını kullanmak ister misiniz?",
            "Bağlantı Hatası",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ShowConnectionWizard()
        End If
    End Sub
End Class
