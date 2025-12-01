Imports Newtonsoft.Json.Linq

Namespace Services.Interfaces
    ''' <summary>
    ''' Platform-agnostic provider for commands data persistence.
    ''' </summary>
    Public Interface ICommandsProvider
        Function Load() As JObject
        Sub Save(data As JObject)
        Function Exists() As Boolean
        Function GetLocation() As String
    End Interface
End Namespace
