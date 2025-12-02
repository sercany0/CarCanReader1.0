' ICommandRepository.vb
' Komut depolama katmanının arayüz taslağı

Imports Newtonsoft.Json.Linq

Namespace Services
    ''' <summary>
    ''' JSON tabanlı komut verisini yönetmek için kullanılan repository arayüzü.
    ''' Mevcut CommandRepository public API'sini DI uyumlu hale getirmek için soyutlar.
    ''' </summary>
    Public Interface ICommandRepository
        Event OnDataLoaded()
        Event OnDataSaved()
        Event OnError(message As String)
        Event OnCommandAdded(brand As String, model As String, moduleName As String, commandName As String)
        Event OnCommandUpdated(brand As String, model As String, moduleName As String, commandName As String)
        Event OnCommandDeleted(brand As String, model As String, moduleName As String, commandName As String)
        Event OnDataChanged()

        Property JsonFilePath As String
        ReadOnly Property IsLoaded As Boolean
        ReadOnly Property RawData As JObject
        ReadOnly Property HasUnsavedChanges As Boolean

        Sub Load()
        Sub Save()
        Sub SaveAs(filePath As String)
        Sub LoadFrom(filePath As String)
        Sub Reload()

        Function GetBrands() As List(Of String)
        Function GetModels(brand As String) As List(Of String)
        Function GetModules(brand As String, model As String) As List(Of String)
        Function GetCommands(brand As String, model As String, moduleName As String) As List(Of String)
        Function GetCommand(brand As String, model As String, moduleName As String, commandName As String) As String
        Function GetCommandToken(brand As String, model As String, moduleName As String, commandName As String) As JToken
        Function CommandExists(brand As String, model As String, moduleName As String, commandName As String) As Boolean

        Sub AddCommand(brand As String, model As String, moduleName As String, commandName As String, frame As String)
        Sub UpdateCommand(brand As String, model As String, moduleName As String, oldName As String, newName As String, frame As String)
        Sub DeleteCommand(brand As String, model As String, moduleName As String, commandName As String)

        Sub AddBrand(brandName As String)
        Sub AddModel(brand As String, modelName As String)
        Sub AddModule(brand As String, model As String, moduleName As String)
        Sub DeleteBrand(brandName As String)
        Sub DeleteModel(brand As String, modelName As String)
        Sub DeleteModule(brand As String, model As String, moduleName As String)

        Sub AddToggleCommand(brand As String, model As String, moduleName As String, commandName As String, onFrame As String, offFrame As String, Optional byteIndex As Integer = -1, Optional bitIndex As Integer = -1)
        Function IsToggleCommand(brand As String, model As String, moduleName As String, commandName As String) As Boolean
        Function GetToggleOnFrame(brand As String, model As String, moduleName As String, commandName As String) As String
        Function GetToggleOffFrame(brand As String, model As String, moduleName As String, commandName As String) As String

        Function GetBrandCount() As Integer
        Function GetTotalCommandCount() As Integer
        Function GetStatistics() As Dictionary(Of String, Integer)
        Function SearchCommands(searchTerm As String) As List(Of Tuple(Of String, String, String, String))

        Function ExportToJson() As String
        Sub ImportFromJson(jsonString As String)
        Sub Merge(other As CommandRepository)

        Function GetStatusReport() As String
    End Interface
End Namespace
