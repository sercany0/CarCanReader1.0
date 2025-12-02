Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Namespace Services.Plugins
    ''' <summary>
    ''' Komut hattı çalıştırma sonucunu temsil eder.
    ''' </summary>
    Public Class CommandPipelineResult
        Public Property Success As Boolean
        Public Property Response As JObject
        Public Property Errors As List(Of String)

        Public Sub New()
            Errors = New List(Of String)()
        End Sub
    End Class
End Namespace
