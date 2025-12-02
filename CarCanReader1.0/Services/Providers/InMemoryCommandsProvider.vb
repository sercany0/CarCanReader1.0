Imports Newtonsoft.Json.Linq
Imports Services.Interfaces

Namespace Services.Providers
    ''' <summary>
    ''' In-memory provider for commands JSON, useful for tests and reversible migrations.
    ''' </summary>
    Public Class InMemoryCommandsProvider
        Implements ICommandsProvider

        Private _data As JObject

        Public Sub New()
            _data = JObject.Parse("{ }")
        End Sub

        Public Sub New(seed As JObject)
            _data = If(seed, JObject.Parse("{ }")).DeepClone()
        End Sub

        Public Function Load() As JObject Implements ICommandsProvider.Load
            Return CType(_data.DeepClone(), JObject)
        End Function

        Public Sub Save(data As JObject) Implements ICommandsProvider.Save
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If
            _data = CType(data.DeepClone(), JObject)
        End Sub

        Public Function Exists() As Boolean Implements ICommandsProvider.Exists
            Return _data IsNot Nothing
        End Function

        Public Function GetLocation() As String Implements ICommandsProvider.GetLocation
            Return "(in-memory commands provider)"
        End Function
    End Class
End Namespace
