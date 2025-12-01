Imports Newtonsoft.Json.Linq

Namespace Services.Interfaces
    ''' <summary>
    ''' Abstraction for configuration storage and retrieval.
    ''' </summary>
    Public Interface IConfigProvider
        Function LoadConfig() As JObject
        Sub SaveConfig(data As JObject)
        Function Exists() As Boolean
        Function GetLocation() As String
    End Interface
End Namespace
