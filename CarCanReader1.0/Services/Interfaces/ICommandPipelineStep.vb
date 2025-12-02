Imports System.Threading
Imports System.Threading.Tasks

Namespace Services.Interfaces
    ''' <summary>
    ''' Komut akışında ardışık olarak çalışacak adım sözleşmesi.
    ''' </summary>
    Public Interface ICommandPipelineStep
        Function InvokeAsync(envelope As Services.Plugins.CommandEnvelope, nextStep As Func(Of Services.Plugins.CommandEnvelope, CancellationToken, Task(Of Services.Plugins.CommandPipelineResult)), cancellationToken As CancellationToken) As Task(Of Services.Plugins.CommandPipelineResult)
    End Interface
End Namespace
