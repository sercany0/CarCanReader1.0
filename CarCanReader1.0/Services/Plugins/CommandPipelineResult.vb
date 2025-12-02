Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Namespace Services.Plugins
    ''' <summary>
    ''' Komut hattı çalıştırma sonucunu temsil eder.
    ''' </summary>
    Public Class CommandPipelineResult
        Public Sub New()
            Success = True
            Errors = New List(Of String)()
        End Sub

        Public Property Success As Boolean
        Public Property Response As JObject
        Public Property Errors As List(Of String)

        Public Sub AddError(message As String)
            Success = False
            If Not String.IsNullOrWhiteSpace(message) Then
                Errors.Add(message)
            End If
        End Sub

        Public Shared Function FromException(ex As Exception) As CommandPipelineResult
            Dim result As New CommandPipelineResult()
            result.AddError(ex.Message)
            Return result
        End Function
    End Class
End Namespace
