Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Interfaces
    ''' <summary>
    ''' Komut setlerini modüler olarak eklemek için plugin sözleşmesi.
    ''' </summary>
    Public Interface ICommandPlugin
        ReadOnly Property Metadata As Services.Plugins.PluginMetadata
        Function InitializeAsync(pipeline As ICommandPipeline, eventAggregator As IEventAggregator, cancellationToken As CancellationToken) As Task
    End Interface
End Namespace
