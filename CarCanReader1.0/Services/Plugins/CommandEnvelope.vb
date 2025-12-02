Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Namespace Services.Plugins
    ''' <summary>
    ''' Komut işleme hattına taşınan, modüler genişlemeye uygun komut kabı.
    ''' </summary>
    Public Class CommandEnvelope
        Public Property CommandName As String
        Public Property Payload As JObject
        Public Property Tags As IDictionary(Of String, Object)

        Public Sub New()
            Tags = New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)
        End Sub
    End Class
End Namespace
