Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports Newtonsoft.Json.Linq

Namespace Services.Plugins
    ''' <summary>
    ''' Komut işleme hattına taşınan, modüler genişlemeye uygun komut kabı.
    ''' Null güvenli başlatıcılar ve kopyalama desteği içerir.
    ''' </summary>
    Public Class CommandEnvelope
        Public Sub New()
            Me.New(String.Empty, New JObject(), Nothing)
        End Sub

        Public Sub New(commandName As String, payload As JObject)
            Me.New(commandName, payload, Nothing)
        End Sub

        Public Sub New(commandName As String, payload As JObject, tags As IDictionary(Of String, Object))
            CommandName = If(commandName, String.Empty)
            Payload = If(payload, New JObject())
            Me.Tags = If(tags, New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase))
        End Sub

        Public Property CommandName As String
        Public Property Payload As JObject
        Public Property Tags As IDictionary(Of String, Object)

        ''' <summary>
        ''' Envelope içeriğinin bağımsız bir kopyasını üretir (payload deep clone edilir).
        ''' </summary>
        Public Function Clone() As CommandEnvelope
            Dim payloadClone As JObject = CType(Payload.DeepClone(), JObject)
            Dim tagCopy As New Dictionary(Of String, Object)(Tags)
            Return New CommandEnvelope(CommandName, payloadClone, tagCopy)
        End Function
    End Class
End Namespace
