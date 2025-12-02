Imports System

Namespace Services.Interfaces
    ''' <summary>
    ''' Basit bir event aggregator arayüzü. UI kablolarını bozmadan, modüler bileşenlerin gevşek bağlı haberleşmesini sağlar.
    ''' </summary>
    Public Interface IEventAggregator
        Sub Subscribe(Of TEvent)(handler As Action(Of TEvent))
        Sub Unsubscribe(Of TEvent)(handler As Action(Of TEvent))
        Sub Publish(Of TEvent)(evt As TEvent)
    End Interface
End Namespace
