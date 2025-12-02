Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Interfaces
    ''' <summary>
    ''' Komut işleme hattı için genişletilebilir sözleşme.
    ''' </summary>
    Public Interface ICommandPipeline
        Sub AddStep(step As ICommandPipelineStep)
        Function ExecuteAsync(envelope As Services.Plugins.CommandEnvelope, cancellationToken As CancellationToken) As Task(Of Services.Plugins.CommandPipelineResult)
    End Interface
End Namespace
