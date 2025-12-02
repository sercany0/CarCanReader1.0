Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Services.Interfaces

Namespace Services.Plugins
    ''' <summary>
    ''' Hafif, thread-safe event aggregator. UI thread davranışını değiştirmeden plugin mesajlaşmasını sağlar.
    ''' </summary>
    Public Class InMemoryEventAggregator
        Implements IEventAggregator

        Private ReadOnly _handlers As New Dictionary(Of Type, List(Of [Delegate]))()
        Private ReadOnly _lock As New Object()

        Public Sub Subscribe(Of TEvent)(handler As Action(Of TEvent)) Implements IEventAggregator.Subscribe
            If handler Is Nothing Then Throw New ArgumentNullException(NameOf(handler))

            SyncLock _lock
                Dim eventType = GetType(TEvent)
                If Not _handlers.ContainsKey(eventType) Then
                    _handlers(eventType) = New List(Of [Delegate])()
                End If
                _handlers(eventType).Add(handler)
            End SyncLock
        End Sub

        Public Sub Unsubscribe(Of TEvent)(handler As Action(Of TEvent)) Implements IEventAggregator.Unsubscribe
            If handler Is Nothing Then Return

            SyncLock _lock
                Dim eventType = GetType(TEvent)
                If _handlers.ContainsKey(eventType) Then
                    _handlers(eventType).Remove(handler)
                    If _handlers(eventType).Count = 0 Then
                        _handlers.Remove(eventType)
                    End If
                End If
            End SyncLock
        End Sub

        Public Sub Publish(Of TEvent)(evt As TEvent) Implements IEventAggregator.Publish
            Dim snapshot As List(Of [Delegate]) = Nothing

            SyncLock _lock
                Dim eventType = GetType(TEvent)
                If _handlers.ContainsKey(eventType) Then
                    snapshot = New List(Of [Delegate])(_handlers(eventType))
                End If
            End SyncLock

            If snapshot Is Nothing OrElse snapshot.Count = 0 Then
                Return
            End If

            For Each handler In snapshot
                Try
                    CType(handler, Action(Of TEvent))(evt)
                Catch ex As Exception
                    ' UI davranışını bozmayı önlemek için hatayı yutar ve izlemeye not düşer.
                    Debug.WriteLine($"EventAggregator publish hatası: {ex.Message}")
                End Try
            Next
        End Sub
    End Class
End Namespace
