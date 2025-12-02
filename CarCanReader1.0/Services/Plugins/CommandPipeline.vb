Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Threading
Imports System.Threading.Tasks
Imports Services.Interfaces

Namespace Services.Plugins
    ''' <summary>
    ''' Basit, sıra tabanlı komut hattı. UI davranışına dokunmadan plugin adımlarını zincirler.
    ''' </summary>
    Public Class CommandPipeline
        Implements ICommandPipeline

        Private ReadOnly _steps As New List(Of ICommandPipelineStep)()
        Private ReadOnly _syncRoot As New Object()

        Public Sub AddStep(step As ICommandPipelineStep) Implements ICommandPipeline.AddStep
            If step Is Nothing Then Throw New ArgumentNullException(NameOf(step))
            SyncLock _syncRoot
                _steps.Add(step)
            End SyncLock
        End Sub

        Public Async Function ExecuteAsync(envelope As CommandEnvelope, cancellationToken As CancellationToken) As Task(Of CommandPipelineResult) Implements ICommandPipeline.ExecuteAsync
            If envelope Is Nothing Then Throw New ArgumentNullException(NameOf(envelope))

            Dim stepsSnapshot As ICommandPipelineStep()
            SyncLock _syncRoot
                stepsSnapshot = _steps.ToArray()
            End SyncLock

            If stepsSnapshot.Length = 0 Then
                Return New CommandPipelineResult()
            End If

            Dim terminal As Func(Of CommandEnvelope, CancellationToken, Task(Of CommandPipelineResult)) = Function(env, ct)
                                                                                                                 Dim result As New CommandPipelineResult()
                                                                                                                 result.Success = True
                                                                                                                 Return Task.FromResult(result)
                                                                                                             End Function

            Dim nextDelegate As Func(Of CommandEnvelope, CancellationToken, Task(Of CommandPipelineResult)) = terminal

            For i As Integer = stepsSnapshot.Length - 1 To 0 Step -1
                Dim stepInstance As ICommandPipelineStep = stepsSnapshot(i)
                Dim currentNext = nextDelegate
                nextDelegate = Function(env, ct) stepInstance.InvokeAsync(env, currentNext, ct)
            Next

            Try
                Return Await nextDelegate(envelope, cancellationToken).ConfigureAwait(False)
            Catch ex As Exception
                Return CommandPipelineResult.FromException(ex)
            End Try
        End Function
    End Class
End Namespace
