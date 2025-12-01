' FilterManager.vb
' CAN ID filtreleme servisi
' MainForm'dan taşınan filtre mantığını içerir
'
' ÖNEMLİ: Bu filtre SADECE log çıktısını etkiler!
' Dashboard, Learning Engine ve Fault Analyzer bu filtreden etkilenmez.
' project-spec.md Section 10'a göre:
'   "Filtering must apply ONLY to log output, not dashboard, learning engine, fault analyzer"
'
' Kullanım Örneği:
'   Dim filter As New FilterManager()
'   filter.ApplyFilter(FilterMode.ShowOnlyId, &H123)
'   
'   ' Frame alındığında:
'   If filter.ShouldShowInLog(frameId) Then
'       AddLog("RX: " & frame)  ' Sadece log'a yaz
'   End If
'   
'   ' Dashboard, Learning Engine vs. her zaman çalışır (filtreden bağımsız)
'   dashboard.ProcessFrame(frameId, data)
'   learningEngine.AnalyzeFrame(frameId, data)

Namespace Services

''' <summary>
''' Filtre modu tanımları
''' </summary>
Public Enum FilterMode
    ''' <summary>
    ''' Filtre kapalı - tüm ID'ler gösterilir
    ''' </summary>
    None = 0

    ''' <summary>
    ''' Sadece belirtilen ID gösterilir
    ''' </summary>
    ShowOnlyId = 1

    ''' <summary>
    ''' Belirtilen ID gizlenir, diğerleri gösterilir
    ''' </summary>
    HideId = 2

    ''' <summary>
    ''' Sadece belirtilen aralıktaki ID'ler gösterilir
    ''' </summary>
    ShowRange = 3

    ''' <summary>
    ''' Belirtilen aralıktaki ID'ler gizlenir, diğerleri gösterilir
    ''' </summary>
    HideRange = 4
End Enum

''' <summary>
''' CAN ID filtreleme yöneticisi
''' SADECE log çıktısını filtreler - dashboard/learning engine etkilenmez
''' </summary>
Public Class FilterManager

#Region "Olaylar (Events)"

    ''' <summary>
    ''' Filtre durumu değiştiğinde tetiklenir
    ''' </summary>
    Public Event OnFilterChanged(isEnabled As Boolean, mode As FilterMode)

    ''' <summary>
    ''' Filtre temizlendiğinde tetiklenir
    ''' </summary>
    Public Event OnFilterCleared()

    ''' <summary>
    ''' Filtre hatası oluştuğunda tetiklenir
    ''' </summary>
    Public Event OnFilterError(message As String)

#End Region

#Region "Özel Alanlar"

    ' Filtre durumu
    Private _isEnabled As Boolean = False

    ' Aktif filtre modu
    Private _currentMode As FilterMode = FilterMode.None

    ' Tek ID filtreleme için ID değeri
    Private _filterId As Integer = -1

    ' Aralık filtreleme için başlangıç ID
    Private _filterFromId As Integer = -1

    ' Aralık filtreleme için bitiş ID
    Private _filterToId As Integer = -1

#End Region

#Region "Özellikler (Properties)"

    ''' <summary>
    ''' Filtrenin aktif olup olmadığını döndürür
    ''' </summary>
    Public ReadOnly Property IsEnabled As Boolean
        Get
            Return _isEnabled
        End Get
    End Property

    ''' <summary>
    ''' Aktif filtre modunu döndürür
    ''' </summary>
    Public ReadOnly Property CurrentMode As FilterMode
        Get
            Return _currentMode
        End Get
    End Property

    ''' <summary>
    ''' Filtrelenen tek ID değerini döndürür (ShowOnlyId/HideId modları için)
    ''' </summary>
    Public ReadOnly Property FilteredId As Integer
        Get
            Return _filterId
        End Get
    End Property

    ''' <summary>
    ''' Aralık başlangıç ID değerini döndürür
    ''' </summary>
    Public ReadOnly Property RangeFromId As Integer
        Get
            Return _filterFromId
        End Get
    End Property

    ''' <summary>
    ''' Aralık bitiş ID değerini döndürür
    ''' </summary>
    Public ReadOnly Property RangeToId As Integer
        Get
            Return _filterToId
        End Get
    End Property

#End Region

#Region "Filtre Uygulama Metodları"

    ''' <summary>
    ''' Tek ID için filtre uygular
    ''' </summary>
    ''' <param name="mode">Filtre modu (ShowOnlyId veya HideId)</param>
    ''' <param name="id">Filtrelenecek CAN ID</param>
    Public Sub ApplyFilter(mode As FilterMode, id As Integer)
        ' Mod kontrolü
        If mode <> FilterMode.ShowOnlyId AndAlso mode <> FilterMode.HideId Then
            RaiseEvent OnFilterError("Tek ID filtresi için geçersiz mod. ShowOnlyId veya HideId kullanın.")
            Return
        End If

        ' ID aralık kontrolü (standart CAN: 0x000 - 0x7FF)
        If id < 0 OrElse id > &H7FF Then
            RaiseEvent OnFilterError($"ID aralık dışı: 0x{id:X3} (0x000-0x7FF olmalı)")
            Return
        End If

        ' Filtre değerlerini ayarla
        _currentMode = mode
        _filterId = id
        _filterFromId = -1
        _filterToId = -1
        _isEnabled = True

        ' Olayı tetikle
        RaiseEvent OnFilterChanged(_isEnabled, _currentMode)
    End Sub

    ''' <summary>
    ''' ID aralığı için filtre uygular
    ''' </summary>
    ''' <param name="mode">Filtre modu (ShowRange veya HideRange)</param>
    ''' <param name="fromId">Aralık başlangıç ID (dahil)</param>
    ''' <param name="toId">Aralık bitiş ID (dahil)</param>
    Public Sub ApplyRangeFilter(mode As FilterMode, fromId As Integer, toId As Integer)
        ' Mod kontrolü
        If mode <> FilterMode.ShowRange AndAlso mode <> FilterMode.HideRange Then
            RaiseEvent OnFilterError("Aralık filtresi için geçersiz mod. ShowRange veya HideRange kullanın.")
            Return
        End If

        ' ID aralık kontrolü
        If fromId < 0 OrElse fromId > &H7FF Then
            RaiseEvent OnFilterError($"Başlangıç ID aralık dışı: 0x{fromId:X3}")
            Return
        End If

        If toId < 0 OrElse toId > &H7FF Then
            RaiseEvent OnFilterError($"Bitiş ID aralık dışı: 0x{toId:X3}")
            Return
        End If

        ' Aralık mantık kontrolü
        If fromId > toId Then
            RaiseEvent OnFilterError($"Başlangıç ID (0x{fromId:X3}) bitiş ID'den (0x{toId:X3}) büyük olamaz")
            Return
        End If

        ' Filtre değerlerini ayarla
        _currentMode = mode
        _filterId = -1
        _filterFromId = fromId
        _filterToId = toId
        _isEnabled = True

        ' Olayı tetikle
        RaiseEvent OnFilterChanged(_isEnabled, _currentMode)
    End Sub

    ''' <summary>
    ''' Filtreyi temizler ve devre dışı bırakır
    ''' </summary>
    Public Sub ClearFilter()
        _isEnabled = False
        _currentMode = FilterMode.None
        _filterId = -1
        _filterFromId = -1
        _filterToId = -1

        ' Olayları tetikle
        RaiseEvent OnFilterCleared()
        RaiseEvent OnFilterChanged(False, FilterMode.None)
    End Sub

#End Region

#Region "Filtre Kontrol Metodları"

    ''' <summary>
    ''' OBD-II cevap ID aralığını kontrol eder (0x7E8-0x7EF)
    ''' Bu ID'ler her zaman filtreden geçer
    ''' </summary>
    Public Shared Function IsOBDResponseId(id As Integer) As Boolean
        Return id >= &H7E8 AndAlso id <= &H7EF
    End Function

    ''' <summary>
    ''' Belirtilen ID'nin log'da gösterilip gösterilmeyeceğini belirler
    ''' 
    ''' ÖNEMLİ: Bu metod SADECE log çıktısı için kullanılmalıdır!
    ''' Dashboard, Learning Engine ve Fault Analyzer bu kontrolü KULLANMAMALIDIR.
    ''' OBD-II cevap ID'leri (0x7E8-0x7EF) her zaman geçer.
    ''' </summary>
    ''' <param name="id">Kontrol edilecek CAN ID</param>
    ''' <returns>True = log'da göster, False = log'da gösterme</returns>
    Public Function ShouldShowInLog(id As Integer) As Boolean
        ' OBD-II cevap ID'leri her zaman gösterilir
        If IsOBDResponseId(id) Then
            Return True
        End If

        ' Filtre kapalıysa her şeyi göster
        If Not _isEnabled Then
            Return True
        End If

        ' Moda göre kontrol
        Select Case _currentMode

            Case FilterMode.None
                ' Filtre yok, göster
                Return True

            Case FilterMode.ShowOnlyId
                ' Sadece bu ID gösterilir
                Return (id = _filterId)

            Case FilterMode.HideId
                ' Bu ID gizlenir, diğerleri gösterilir
                Return (id <> _filterId)

            Case FilterMode.ShowRange
                ' Sadece aralıktaki ID'ler gösterilir
                Return (id >= _filterFromId AndAlso id <= _filterToId)

            Case FilterMode.HideRange
                ' Aralıktaki ID'ler gizlenir, diğerleri gösterilir
                Return (id < _filterFromId OrElse id > _filterToId)

            Case Else
                ' Bilinmeyen mod, güvenli tarafta kal ve göster
                Return True

        End Select
    End Function

    ''' <summary>
    ''' ShouldShowInLog'un tersi - ID'nin filtrelenip filtrelenmediğini döndürür
    ''' </summary>
    ''' <param name="id">Kontrol edilecek CAN ID</param>
    ''' <returns>True = filtre tarafından engellendi, False = filtre geçti</returns>
    Public Function IsFilteredOut(id As Integer) As Boolean
        Return Not ShouldShowInLog(id)
    End Function

#End Region

#Region "Yardımcı Metodlar"

    ''' <summary>
    ''' Hex string'i integer ID'ye çevirir
    ''' </summary>
    ''' <param name="hexId">Hex string (örn: "123", "7FF")</param>
    ''' <param name="id">Çevrilen ID değeri</param>
    ''' <returns>Başarılı ise True</returns>
    Public Shared Function TryParseHexId(hexId As String, ByRef id As Integer) As Boolean
        If String.IsNullOrWhiteSpace(hexId) Then
            id = -1
            Return False
        End If

        hexId = hexId.Trim().ToUpper()

        ' 0x prefix varsa kaldır
        If hexId.StartsWith("0X") Then
            hexId = hexId.Substring(2)
        End If

        Return Integer.TryParse(hexId, Globalization.NumberStyles.HexNumber, Nothing, id)
    End Function

    ''' <summary>
    ''' Aktif filtre durumunu okunabilir string olarak döndürür
    ''' </summary>
    Public Function GetStatusText() As String
        If Not _isEnabled Then
            Return "Filtre kapalı"
        End If

        Select Case _currentMode

            Case FilterMode.ShowOnlyId
                Return $"Sadece ID 0x{_filterId:X3} gösteriliyor"

            Case FilterMode.HideId
                Return $"ID 0x{_filterId:X3} gizleniyor"

            Case FilterMode.ShowRange
                Return $"Sadece 0x{_filterFromId:X3} - 0x{_filterToId:X3} aralığı gösteriliyor"

            Case FilterMode.HideRange
                Return $"0x{_filterFromId:X3} - 0x{_filterToId:X3} aralığı gizleniyor"

            Case Else
                Return "Filtre aktif"

        End Select
    End Function

    ''' <summary>
    ''' Filtre modunu Türkçe string olarak döndürür (UI için)
    ''' </summary>
    Public Shared Function GetModeDisplayName(mode As FilterMode) As String
        Select Case mode
            Case FilterMode.None
                Return "Filtre yok"
            Case FilterMode.ShowOnlyId
                Return "Sadece bu ID"
            Case FilterMode.HideId
                Return "Bu ID'yi gizle"
            Case FilterMode.ShowRange
                Return "ID aralığını göster"
            Case FilterMode.HideRange
                Return "ID aralığını gizle"
            Case Else
                Return "Bilinmeyen"
        End Select
    End Function

    ''' <summary>
    ''' Türkçe mod adından FilterMode enum değerini döndürür
    ''' </summary>
    Public Shared Function ParseModeFromDisplayName(displayName As String) As FilterMode
        Select Case displayName
            Case "Sadece bu ID"
                Return FilterMode.ShowOnlyId
            Case "Bu ID'yi gizle"
                Return FilterMode.HideId
            Case "ID aralığını göster"
                Return FilterMode.ShowRange
            Case "ID aralığını gizle"
                Return FilterMode.HideRange
            Case Else
                Return FilterMode.None
        End Select
    End Function

#End Region

#Region "İstatistikler (Opsiyonel)"

    ' Filtreleme istatistikleri için sayaçlar
    Private _totalFramesChecked As Long = 0
    Private _framesPassedFilter As Long = 0
    Private _framesBlockedByFilter As Long = 0

    ''' <summary>
    ''' Filtreleme istatistiklerini sıfırlar
    ''' </summary>
    Public Sub ResetStatistics()
        _totalFramesChecked = 0
        _framesPassedFilter = 0
        _framesBlockedByFilter = 0
    End Sub

    ''' <summary>
    ''' ShouldShowInLog ile aynı, ama istatistikleri de günceller
    ''' </summary>
    Public Function ShouldShowInLogWithStats(id As Integer) As Boolean
        _totalFramesChecked += 1

        Dim result As Boolean = ShouldShowInLog(id)

        If result Then
            _framesPassedFilter += 1
        Else
            _framesBlockedByFilter += 1
        End If

        Return result
    End Function

    ''' <summary>
    ''' Toplam kontrol edilen frame sayısı
    ''' </summary>
    Public ReadOnly Property TotalFramesChecked As Long
        Get
            Return _totalFramesChecked
        End Get
    End Property

    ''' <summary>
    ''' Filtreden geçen frame sayısı
    ''' </summary>
    Public ReadOnly Property FramesPassedFilter As Long
        Get
            Return _framesPassedFilter
        End Get
    End Property

    ''' <summary>
    ''' Filtre tarafından engellenen frame sayısı
    ''' </summary>
    Public ReadOnly Property FramesBlockedByFilter As Long
        Get
            Return _framesBlockedByFilter
        End Get
    End Property

#End Region

End Class

End Namespace

