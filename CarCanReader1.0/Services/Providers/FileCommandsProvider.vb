Imports System.IO
Imports Newtonsoft.Json.Linq
Imports Services.Interfaces

Namespace Services.Providers
    ''' <summary>
    ''' File-system based provider for commands JSON with atomic writes and basic validation.
    ''' </summary>
    Public Class FileCommandsProvider
        Implements ICommandsProvider

        Private ReadOnly _filePath As String
        Private ReadOnly _encoding As Text.Encoding = Text.Encoding.UTF8

        Public Sub New(filePath As String)
            _filePath = filePath
        End Sub

        Public Function Load() As JObject Implements ICommandsProvider.Load
            EnsureDirectory()
            If Not File.Exists(_filePath) Then
                Return JObject.Parse("{ }")
            End If

            Dim jsonText = File.ReadAllText(_filePath, _encoding)
            If String.IsNullOrWhiteSpace(jsonText) Then
                Return JObject.Parse("{ }")
            End If

            Return JObject.Parse(jsonText)
        End Function

        Public Sub Save(data As JObject) Implements ICommandsProvider.Save
            If data Is Nothing Then
                Throw New ArgumentNullException(NameOf(data))
            End If

            EnsureDirectory()
            Dim tempPath = _filePath & ".tmp"
            Dim jsonText = data.ToString(Newtonsoft.Json.Formatting.Indented)

            File.WriteAllText(tempPath, jsonText, _encoding)

            Dim parsed = JObject.Parse(File.ReadAllText(tempPath, _encoding))
            If parsed Is Nothing Then
                Throw New InvalidDataException("Commands JSON validation failed")
            End If

            If File.Exists(_filePath) Then
                File.Replace(tempPath, _filePath, _filePath & ".bak", ignoreMetadataErrors:=True)
            Else
                File.Move(tempPath, _filePath)
            End If
        End Sub

        Public Function Exists() As Boolean Implements ICommandsProvider.Exists
            Return File.Exists(_filePath)
        End Function

        Public Function GetLocation() As String Implements ICommandsProvider.GetLocation
            Return _filePath
        End Function

        Private Sub EnsureDirectory()
            Dim dir = Path.GetDirectoryName(_filePath)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If
        End Sub
    End Class
End Namespace
