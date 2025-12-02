Option Strict On
Option Explicit On

Namespace Services.Plugins
    ''' <summary>
    ''' Plugin keşfi sırasında tanımlayıcı meta bilgileri taşır.
    ''' </summary>
    Public Class PluginMetadata
        Public Property Name As String
        Public Property Version As String
        Public Property Description As String
        Public Property Author As String
    End Class
End Namespace
