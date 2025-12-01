Imports Newtonsoft.Json.Linq
Imports Services.Interfaces

Namespace Services.Providers
    ''' <summary>
    ''' In-memory configuration provider for testing or reversible flows.
    ''' </summary>
    Public Class InMemoryConfigProvider
        Implements IConfigProvider

        Private _data As JObject

        Public Sub New()
            _data = JObject.Parse("{ }")
        End Sub

        Public Sub New(seed As JObject)
            _data = If(seed, JObject.Parse("{ }")).DeepClone()
        End Sub

        Public Function LoadConfig() As JObject Implements IConfigProvider.LoadConfig
            Return CType(_data.DeepClone(), JObject)
        End Function

        Public Sub SaveConfig(data As JObject) Implements IConfigProvider.SaveConfig
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If
            _data = CType(data.DeepClone(), JObject)
        End Sub

        Public Function Exists() As Boolean Implements IConfigProvider.Exists
            Return _data IsNot Nothing
        End Function

        Public Function GetLocation() As String Implements IConfigProvider.GetLocation
            Return "(in-memory config provider)"
        End Function
    End Class
End Namespace
