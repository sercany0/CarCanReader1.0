' Reacts to configuration updates and applies OBD-related adjustments without UI dependencies.
Public Class ConfigChangeHandler
    Private ReadOnly _log As Action(Of String)
    Private ReadOnly _obdService As OBDService

    Public Sub New(log As Action(Of String), obdService As OBDService)
        _log = log
        _obdService = obdService
    End Sub

    Public Sub HandleConfigChanged(section As String)
        Try
            _log?.Invoke($"⚙️ Ayar değişti: {section}")

            If section = "obd" Then
                _obdService.SetPollingInterval(Config.OBD.PollingInterval)
            End If
        Catch ex As Exception
            ErrorHandler.Instance.LogError(ex, "ConfigChangeHandler.HandleConfigChanged")
        End Try
    End Sub
End Class
