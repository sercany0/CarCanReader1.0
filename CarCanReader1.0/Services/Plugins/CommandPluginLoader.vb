Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports Services.Interfaces

Namespace Services.Plugins
    ''' <summary>
    ''' Plugin keşfi ve yükleme yaşam döngüsünü yönetir. UI kablolarına dokunmadan, modüler komut setlerinin takılmasını sağlar.
    ''' </summary>
    Public Class CommandPluginLoader
        Public Event PluginLoaded(metadata As PluginMetadata)
        Public Event PluginFailed(path As String, message As String)

        Private ReadOnly _pluginFolder As String
        Private ReadOnly _pipeline As ICommandPipeline
        Private ReadOnly _eventAggregator As IEventAggregator
        Private ReadOnly _loadedAssemblies As HashSet(Of String)

        Public Sub New(pluginFolder As String, pipeline As ICommandPipeline, eventAggregator As IEventAggregator)
            If String.IsNullOrWhiteSpace(pluginFolder) Then Throw New ArgumentNullException(NameOf(pluginFolder))
            If pipeline Is Nothing Then Throw New ArgumentNullException(NameOf(pipeline))
            If eventAggregator Is Nothing Then Throw New ArgumentNullException(NameOf(eventAggregator))

            _pluginFolder = pluginFolder
            _pipeline = pipeline
            _eventAggregator = eventAggregator
            _loadedAssemblies = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>
        ''' Plugin dizinindeki DLL'leri bulur, ICommandPlugin implementasyonlarını örnekler ve kayıtlarını yapar.
        ''' </summary>
        Public Async Function DiscoverAndLoadAsync(cancellationToken As CancellationToken) As Task
            If Not Directory.Exists(_pluginFolder) Then
                Return
            End If

            Dim dllFiles = Directory.GetFiles(_pluginFolder, "*.dll", SearchOption.TopDirectoryOnly)
            For Each dllPath In dllFiles
                cancellationToken.ThrowIfCancellationRequested()
                Await LoadPluginAsync(dllPath, cancellationToken).ConfigureAwait(False)
            Next
        End Function

        Private Async Function LoadPluginAsync(dllPath As String, cancellationToken As CancellationToken) As Task
            If String.IsNullOrWhiteSpace(dllPath) Then
                Return
            End If

            Dim normalized = Path.GetFullPath(dllPath)
            If _loadedAssemblies.Contains(normalized) Then
                Return
            End If

            Try
                Dim assembly = Assembly.LoadFrom(normalized)
                _loadedAssemblies.Add(normalized)

                Dim pluginTypes = assembly.GetTypes().Where(Function(t) GetType(ICommandPlugin).IsAssignableFrom(t) AndAlso Not t.IsAbstract AndAlso t.GetConstructor(Type.EmptyTypes) IsNot Nothing).ToArray()

                For Each pluginType In pluginTypes
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim plugin = CType(Activator.CreateInstance(pluginType), ICommandPlugin)
                    If plugin.Metadata Is Nothing Then
                        RaiseEvent PluginFailed(normalized, $"{pluginType.FullName} için Metadata eksik")
                        Continue For
                    End If

                    Await plugin.InitializeAsync(_pipeline, _eventAggregator, cancellationToken).ConfigureAwait(False)
                    RaiseEvent PluginLoaded(plugin.Metadata)
                Next
            Catch ex As Exception
                RaiseEvent PluginFailed(normalized, ex.Message)
            End Try
        End Function
    End Class
End Namespace
